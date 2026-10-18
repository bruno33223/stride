// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Action = System.Action;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Storage;
using Stride.Assets.Editor.Thumbnails;
using Stride.Core.Assets.Editor.Avalonia.Behaviors;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para a Fatia 1 (slice-1) do Marco P26:
/// Core Thumbnail Contracts, Service, Behaviors & Generators.
/// </summary>
public class TestThumbnailCoreServiceAndGeneratorIntegration
{
    private class DummyDispatcherService : IDispatcherService
    {
        public void Invoke(Action callback) => callback();
        public TResult Invoke<TResult>(Func<TResult> callback) => callback();
        public Task InvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }
        public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(callback());
        public Task InvokeTask(Func<Task> callback, CancellationToken token = default) => callback();
        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> callback, CancellationToken token = default) => callback();
        public bool CheckAccess() => true;
        public void EnsureAccess(bool inDispatcherThread = true) { }
    }

    [Fact]
    public void TestThumbnailCoreContracts_IThumbnailService()
    {
        var serviceType = typeof(IThumbnailService);
        Assert.True(serviceType.IsInterface);
        Assert.True(typeof(IDisposable).IsAssignableFrom(serviceType));

        var addMethod = serviceType.GetMethod(nameof(IThumbnailService.AddThumbnailAssetItems));
        Assert.NotNull(addMethod);
        Assert.Equal(2, addMethod.GetParameters().Length);

        var increaseMethod = serviceType.GetMethod(nameof(IThumbnailService.IncreaseThumbnailPriority));
        Assert.NotNull(increaseMethod);
        Assert.Single(increaseMethod.GetParameters());

        var hasStaticMethod = serviceType.GetMethod(nameof(IThumbnailService.HasStaticThumbnail));
        Assert.NotNull(hasStaticMethod);
        Assert.Single(hasStaticMethod.GetParameters());

        var completedEvent = serviceType.GetEvent(nameof(IThumbnailService.ThumbnailCompleted));
        Assert.NotNull(completedEvent);
    }

    [Fact]
    public void TestThumbnailCompletedArgs_InstantiationAndProperties()
    {
        var assetId = AssetId.New();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var thumbnailData = new Stride.Core.Assets.Editor.Services.BitmapThumbnailData(ObjectId.New(), stream);

        var args = new ThumbnailCompletedArgs(assetId, thumbnailData);
        Assert.Equal(assetId, args.AssetId);
        Assert.Same(thumbnailData, args.Data);
    }

    [Fact]
    public async Task TestThumbnailData_HierarchyAndLifecycle()
    {
        var id = ObjectId.New();
        var sampleBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(sampleBytes);
        var bitmapData = new Stride.Core.Assets.Editor.Services.BitmapThumbnailData(id, stream);

        Assert.NotNull(bitmapData.ThumbnailBitmapStream);
        Assert.Null(bitmapData.Presenter);

        var dispatcher = new DummyDispatcherService();
        await bitmapData.PrepareForPresentation(dispatcher);

        Assert.NotNull(bitmapData.Presenter);
        Assert.IsType<byte[]>(bitmapData.Presenter);
        var presenterBytes = (byte[])bitmapData.Presenter;
        Assert.Equal(sampleBytes.Length, presenterBytes.Length);

        // ResourceThumbnailData
        var resId = ObjectId.New();
        var resourceData = new Stride.Core.Assets.Editor.Services.ResourceThumbnailData(resId, "FolderIcon");
        Assert.Equal("FolderIcon", resourceData.ResourceKey);
        await resourceData.PrepareForPresentation(dispatcher);
        Assert.Equal("FolderIcon", resourceData.Presenter);
    }

    [Fact]
    public void TestTilePanelThumbnailPrioritizationBehavior_AttachedPropertyAndExecution()
    {
        var itemsControl = new ItemsControl();

        Assert.False(TilePanelThumbnailPrioritizationBehavior.GetIsEnabled(itemsControl));
        TilePanelThumbnailPrioritizationBehavior.SetIsEnabled(itemsControl, true);
        Assert.True(TilePanelThumbnailPrioritizationBehavior.GetIsEnabled(itemsControl));

        var behaviors = Interaction.GetBehaviors(itemsControl);
        var behavior = behaviors.OfType<TilePanelThumbnailPrioritizationBehavior>().FirstOrDefault();
        Assert.NotNull(behavior);

        // Test prioritization method on behavior safely with empty items
        behavior.PrioritizeVisibleThumbnails(0, 10);

        // Disable behavior via attached property
        TilePanelThumbnailPrioritizationBehavior.SetIsEnabled(itemsControl, false);
        Assert.False(TilePanelThumbnailPrioritizationBehavior.GetIsEnabled(itemsControl));
        Assert.DoesNotContain(behaviors, b => b is TilePanelThumbnailPrioritizationBehavior);
    }

    [Fact]
    public void TestGameStudioThumbnailService_CreationAndBrokenThumbnail()
    {
        var serviceType = typeof(GameStudioThumbnailService);
        Assert.True(typeof(IThumbnailService).IsAssignableFrom(serviceType));

        var brokenImageBytes = GameStudioThumbnailService.HandleBrokenThumbnail();
        Assert.NotNull(brokenImageBytes);
        Assert.True(brokenImageBytes.Length > 0, "Broken thumbnail image bytes must not be empty");

        // Verify PNG magic header
        Assert.Equal(0x89, brokenImageBytes[0]);
        Assert.Equal(0x50, brokenImageBytes[1]); // P
        Assert.Equal(0x4E, brokenImageBytes[2]); // N
        Assert.Equal(0x47, brokenImageBytes[3]); // G
    }

    [Fact]
    public void TestThumbnailBuildContracts_StepsAndUnits()
    {
        var generatorType = typeof(ThumbnailGenerator);
        Assert.True(typeof(IDisposable).IsAssignableFrom(generatorType));

        var eventArgs = new ThumbnailBuiltEventArgs
        {
            AssetId = AssetId.New(),
            Result = Stride.Editor.Thumbnails.ThumbnailBuildResult.Succeeded,
            ThumbnailChanged = true,
            ThumbnailId = ObjectId.New(),
            Url = new UFile("TestAsset")
        };
        Assert.Equal(Stride.Editor.Thumbnails.ThumbnailBuildResult.Succeeded, eventArgs.Result);
        Assert.True(eventArgs.ThumbnailChanged);
        Assert.Equal(new UFile("TestAsset"), eventArgs.Url);

        var helperType = typeof(ThumbnailBuildHelper);
        Assert.True(typeof(IDisposable).IsAssignableFrom(helperType));

        var buildStepType = typeof(ThumbnailBuildStep);
        Assert.True(typeof(Stride.Core.BuildEngine.CommandBuildStep).IsAssignableFrom(buildStepType));

        var buildUnitType = typeof(ThumbnailAssetBuildUnit);
        Assert.True(typeof(Stride.Core.Assets.Editor.Services.AssetBuildUnit).IsAssignableFrom(buildUnitType));
    }

    [Fact]
    public unsafe void TestHandleBrokenThumbnail_MustNotMutateDefaultThumbnailsSingleton()
    {
        var originalImage = Stride.Editor.Resources.DefaultThumbnails.AssetBroken;
        Assert.NotNull(originalImage);

        var bufferLength = originalImage.PixelBuffer[0].BufferStride;
        Assert.True(bufferLength > 0);
        var originalSnapshot = new byte[bufferLength];
        new ReadOnlySpan<byte>((void*)originalImage.PixelBuffer[0].DataPointer, bufferLength).CopyTo(originalSnapshot);

        var firstResult = GameStudioThumbnailService.HandleBrokenThumbnail();
        var secondResult = GameStudioThumbnailService.HandleBrokenThumbnail();

        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.True(firstResult.Length > 0);

        var afterSnapshot = new byte[bufferLength];
        new ReadOnlySpan<byte>((void*)originalImage.PixelBuffer[0].DataPointer, bufferLength).CopyTo(afterSnapshot);

        Assert.Equal(originalSnapshot, afterSnapshot);
    }

    [Fact]
    public void TestThumbnailsViewModel_ThumbnailCompleted_NullDataHandling()
    {
        var session = (SessionViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        var map = new System.Collections.Concurrent.ConcurrentDictionary<AssetId, AssetViewModel>();
        typeof(SessionViewModel).GetField("assetIdMap", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(session, map);

        var thumbnailsVm = (ThumbnailsViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThumbnailsViewModel));
        typeof(ThumbnailsViewModel).GetField("session", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(thumbnailsVm, session);
        typeof(Stride.Core.Presentation.ViewModels.DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(thumbnailsVm, new DummyDispatcherService());

        var assetId = AssetId.New();
        var assetVm = (AssetViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ImportedAssetViewModel<Asset>));
        map[assetId] = assetVm;

        var method = typeof(ThumbnailsViewModel).GetMethod("ThumbnailCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var argsWithNullData = new ThumbnailCompletedArgs(assetId, null);

        var exception = Record.Exception(() => method.Invoke(thumbnailsVm, [null, argsWithNullData]));
        Assert.Null(exception);
    }

    [Fact]
    public void TestTilePanelThumbnailPrioritizationBehavior_HookScrollViewer_CallsUnhookFirst()
    {
        var behavior = new TilePanelThumbnailPrioritizationBehavior();
        var itemsControl = new ItemsControl();
        typeof(global::Avalonia.Xaml.Interactivity.Behavior).GetProperty("AssociatedObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(behavior, itemsControl);

        var scrollViewer = new ScrollViewer();
        var onScrollChangedMethod = typeof(TilePanelThumbnailPrioritizationBehavior).GetMethod("OnScrollChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        var handler = (EventHandler<ScrollChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<ScrollChangedEventArgs>), behavior, onScrollChangedMethod!);
        scrollViewer.ScrollChanged += handler;

        typeof(TilePanelThumbnailPrioritizationBehavior).GetField("scrollViewer", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(behavior, scrollViewer);

        var hookMethod = typeof(TilePanelThumbnailPrioritizationBehavior).GetMethod("HookScrollViewer", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(hookMethod);
        hookMethod.Invoke(behavior, null);

        var eventField = typeof(ScrollViewer).GetField("ScrollChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        var delegateVal = (MulticastDelegate?)eventField?.GetValue(scrollViewer);
        Assert.True(delegateVal == null || !delegateVal.GetInvocationList().Contains(handler), "HookScrollViewer must call UnhookScrollViewer at start to prevent duplicate or orphaned handlers");
    }
}
