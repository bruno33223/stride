// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Xunit;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Solutions;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Editor.Preview.Views;
using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAudioPreviewViewAndControlsIntegration
{
    private sealed class TestPluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => [];
        public void EnsureInitialized(ILogger logger) { }
        public Type? GetAssetViewModelType(Type assetType) => null;
        public Type? GetEditorViewModelType(Type viewModelType) => null;
        public Type? GetEditorViewType(Type editorViewModelType) => null;
        public Type? GetPreviewViewModelType(Type previewType) => null;
        public Type? GetPreviewViewType(Type previewType) => null;
        public IReadOnlyList<Type> GetPrimitiveTypes() => [];
    }

    private sealed class TestDebugService : IEditorDebugService
    {
        public IDebugPage? CreateLogDebugPage(Logger logger, string title, bool register = true) => null;
        public IDebugPage? CreateUndoRedoDebugPage(IUndoRedoService actionService, string title, bool register = true) => null;
        public IDebugPage? CreateAssetNodesDebugPage(SessionViewModel session, string title, bool register = true) => null;
        public void RegisterDebugPage(IDebugPage? page) { }
        public void UnregisterDebugPage(IDebugPage? page) { }
    }

    private sealed class TestMainViewModel : IMainViewModel
    {
        public StatusViewModel Status { get; }
        public TestMainViewModel(IViewModelServiceProvider sp) { Status = new StatusViewModel(sp); }
    }

    private sealed class MockEditorDialogService : IEditorDialogService
    {
        public bool HasMainWindow => false;
        public void Exit(int exitCode = 0) { }
        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons, MessageBoxImage image)
            => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
            => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
        public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons, MessageBoxImage image)
            => Task.FromResult(MessageBoxResult.OK);
        public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
            => Task.FromResult(0);
        public Task<Stride.Core.IO.UFile?> OpenFilePickerAsync(Stride.Core.IO.UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<Stride.Core.IO.UFile?>(null);
        public Task<IReadOnlyList<Stride.Core.IO.UFile>> OpenMultipleFilesPickerAsync(Stride.Core.IO.UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<IReadOnlyList<Stride.Core.IO.UFile>>([]);
        public Task<Stride.Core.IO.UDirectory?> OpenFolderPickerAsync(Stride.Core.IO.UDirectory? initialPath = null)
            => Task.FromResult<Stride.Core.IO.UDirectory?>(null);
        public Task<Stride.Core.IO.UFile?> SaveFilePickerAsync(Stride.Core.IO.UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
            => Task.FromResult<Stride.Core.IO.UFile?>(null);
        public void ShowProgressWindow(WorkProgressViewModel workProgress) { }
        public INewProjectDialog CreateNewProjectDialog(SessionViewModel session) => null!;
        public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null) => null!;
    }

    private static PackageSession CreateMockPackageSession()
    {
        var session = (PackageSession)RuntimeHelpers.GetUninitializedObject(typeof(PackageSession));

        typeof(PackageSession).GetField("dependenciesLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new object());

        typeof(PackageSession).GetField("packages", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new PackageCollection());

        typeof(PackageSession).GetField("packagesCopy", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new Dictionary<Package, Package>());

        var upgradesField = typeof(PackageSession).GetField("pendingPackageUpgradesPerPackage", BindingFlags.Instance | BindingFlags.NonPublic);
        if (upgradesField != null)
        {
            upgradesField.SetValue(session, Activator.CreateInstance(upgradesField.FieldType));
        }

        var constraintField = typeof(PackageSession).GetField("constraintProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        if (constraintField != null)
        {
            constraintField.SetValue(session, Activator.CreateInstance(constraintField.FieldType));
        }

        var solution = new Solution { FullPath = "TestSolution.sln" };
        typeof(PackageSession).GetField("VSSolution", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, solution);

        var projectsField = typeof(PackageSession).GetField("<Projects>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        projectsField?.SetValue(session, new ProjectCollection());

        var assemblyContainerField = typeof(PackageSession).GetField("<AssemblyContainer>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        assemblyContainerField?.SetValue(session, new AssemblyContainer());

        var depMgrCtor = typeof(AssetDependencyManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [typeof(PackageSession)],
            null);
        var depMgr = depMgrCtor!.Invoke([session]);
        typeof(PackageSession).GetField("dependencies", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, depMgr);

        return session;
    }

    private static (SessionViewModel sessionVm, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new UndoRedoService(10));
        sp.RegisterService(new TestPluginService());
        sp.RegisterService(new TestDebugService());
        sp.RegisterService(new MockEditorDialogService());

        var packageSession = CreateMockPackageSession();
        var mainVm = new TestMainViewModel(sp);
        var logger = new LoggerResult();

        var ctor = typeof(SessionViewModel).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IViewModelServiceProvider), typeof(PackageSession), typeof(IMainViewModel), typeof(ILogger)],
            null);

        var sessionVm = (SessionViewModel)ctor!.Invoke([sp, packageSession, mainVm, logger]);
        return (sessionVm, sp);
    }

    private static SoundPreviewViewModel CreateSoundPreviewViewModel()
    {
        var (sessionVm, _) = CreateTestSession();
        return new SoundPreviewViewModel(sessionVm);
    }

    [Fact]
    public void TestAudioPreviewViewHierarchyAndAttributes()
    {
        var viewType = typeof(SoundPreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(viewType), "SoundPreviewView must inherit from StridePreviewView.");
        Assert.True(typeof(IPreviewView).IsAssignableFrom(viewType), "SoundPreviewView must implement IPreviewView.");

        var attr = viewType.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(SoundPreview), attr.AssetPreviewType);

        // Control properties reflection checks
        var playBtnProp = viewType.GetProperty("PlayButton", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(playBtnProp);
        Assert.True(typeof(Button).IsAssignableFrom(playBtnProp.PropertyType));

        var pauseBtnProp = viewType.GetProperty("PauseButton", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(pauseBtnProp);
        Assert.True(typeof(Button).IsAssignableFrom(pauseBtnProp.PropertyType));

        var volSliderProp = viewType.GetProperty("VolumeSlider", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(volSliderProp);
        Assert.True(typeof(Slider).IsAssignableFrom(volSliderProp.PropertyType));

        var playheadSliderProp = viewType.GetProperty("PlayheadSlider", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(playheadSliderProp);
        Assert.True(typeof(Slider).IsAssignableFrom(playheadSliderProp.PropertyType));

        var timeTextProp = viewType.GetProperty("TimeTextBlock", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(timeTextProp);
        Assert.True(typeof(TextBlock).IsAssignableFrom(timeTextProp.PropertyType));

        var buildingTextProp = viewType.GetProperty("BuildingTextBlock", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(buildingTextProp);
        Assert.True(typeof(TextBlock).IsAssignableFrom(buildingTextProp.PropertyType));
    }

    [Fact]
    public void TestAudioPreviewViewInitialStateAndBuildingMessage()
    {
        var view = new SoundPreviewView();

        var playBtn = view.GetType().GetProperty("PlayButton")?.GetValue(view) as Button;
        var pauseBtn = view.GetType().GetProperty("PauseButton")?.GetValue(view) as Button;
        var volSlider = view.GetType().GetProperty("VolumeSlider")?.GetValue(view) as Slider;
        var playheadSlider = view.GetType().GetProperty("PlayheadSlider")?.GetValue(view) as Slider;
        var timeText = view.GetType().GetProperty("TimeTextBlock")?.GetValue(view) as TextBlock;
        var buildingText = view.GetType().GetProperty("BuildingTextBlock")?.GetValue(view) as TextBlock;

        Assert.NotNull(playBtn);
        Assert.NotNull(pauseBtn);
        Assert.NotNull(volSlider);
        Assert.NotNull(playheadSlider);
        Assert.NotNull(timeText);
        Assert.NotNull(buildingText);

        // Initial state before audio is loaded / valid
        Assert.Equal("Building preview sound asset...", buildingText.Text);
        Assert.True(buildingText.IsVisible, "Building message should be visible when audio is not valid.");
        Assert.False(playheadSlider.IsVisible, "Playhead slider should be hidden when audio is not valid.");
        Assert.False(timeText.IsVisible, "Time text should be hidden when audio is not valid.");
    }

    [Fact]
    public void TestAudioPreviewViewControlsWithValidAudioAndScrubbing()
    {
        var view = new SoundPreviewView();
        var vm = CreateSoundPreviewViewModel();

        view.DataContext = vm;

        // Simulate audio valid update via reflection or properties
        var updateMethod = typeof(SoundPreviewViewModel).GetMethod("UpdateViewModelTime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(updateMethod);

        var duration = TimeSpan.FromSeconds(120.0);
        var current = TimeSpan.FromSeconds(35.5);
        updateMethod.Invoke(vm, [true, false, current, duration]);

        var playheadSlider = (Slider)view.GetType().GetProperty("PlayheadSlider")!.GetValue(view)!;
        var timeText = (TextBlock)view.GetType().GetProperty("TimeTextBlock")!.GetValue(view)!;
        var buildingText = (TextBlock)view.GetType().GetProperty("BuildingTextBlock")!.GetValue(view)!;

        // When valid: building message hidden, controls visible
        Assert.False(buildingText.IsVisible, "Building message should be hidden when audio is valid.");
        Assert.True(playheadSlider.IsVisible, "Playhead slider should be visible when audio is valid.");
        Assert.True(timeText.IsVisible, "Time text should be visible when audio is valid.");

        // Check formatted time string (mm:ss:fff)
        Assert.Equal("00:35:500 / 02:00:000", timeText.Text);

        // Check slider values
        Assert.Equal(120.0, playheadSlider.Maximum);
        Assert.Equal(35.5, playheadSlider.Value);

        // Test Scrubbing: changing PlayheadSlider.Value updates ViewModel CurrentValue
        playheadSlider.Value = 75.0;
        Assert.Equal(75.0, vm.CurrentValue);
        Assert.Equal(TimeSpan.FromSeconds(75.0), vm.CurrentTime);
    }

    [Fact]
    public void TestAudioPreviewViewVolumeSliderInteraction()
    {
        var view = new SoundPreviewView();
        var vm = CreateSoundPreviewViewModel();
        view.DataContext = vm;

        var volSlider = (Slider)view.GetType().GetProperty("VolumeSlider")!.GetValue(view)!;
        Assert.Equal(0.0, volSlider.Minimum);
        Assert.Equal(1.0, volSlider.Maximum);

        // ViewModel default is 1.0
        Assert.Equal(1.0, volSlider.Value);

        // Adjusting slider updates VM
        volSlider.Value = 0.65;
        Assert.Equal(0.65, vm.MasterVolume);

        // Adjusting VM updates slider
        vm.MasterVolume = 0.42;
        Assert.Equal(0.42, volSlider.Value);
    }

    [Fact]
    public void TestAudioPreviewViewIconsAndToolTips()
    {
        var view = new SoundPreviewView();
        var playBtn = (Button)view.GetType().GetProperty("PlayButton")!.GetValue(view)!;
        var pauseBtn = (Button)view.GetType().GetProperty("PauseButton")!.GetValue(view)!;

        // ToolTips
        Assert.NotNull(ToolTip.GetTip(playBtn));
        Assert.Contains("Play", ToolTip.GetTip(playBtn)?.ToString());

        Assert.NotNull(ToolTip.GetTip(pauseBtn));
        Assert.Contains("Pause", ToolTip.GetTip(pauseBtn)?.ToString());

        // Icons in button Content
        Assert.NotNull(playBtn.Content);
        Assert.NotNull(pauseBtn.Content);
    }

    [Fact]
    public async Task TestAudioGizmosAndFactoryIntegration()
    {
        // 1. AudioEmitterGizmo
        var emitterGizmoType = typeof(AudioEmitterGizmo);
        Assert.True(typeof(BillboardingGizmo<AudioEmitterComponent>).IsAssignableFrom(emitterGizmoType));
        var emitterGizmoAttr = emitterGizmoType.GetCustomAttribute<GizmoComponentAttribute>();
        Assert.NotNull(emitterGizmoAttr);
        Assert.Equal(typeof(AudioEmitterComponent), emitterGizmoAttr.ComponentType);

        // 2. AudioListenerGizmo
        var listenerGizmoType = typeof(AudioListenerGizmo);
        Assert.True(typeof(BillboardingGizmo<AudioListenerComponent>).IsAssignableFrom(listenerGizmoType));
        var listenerGizmoAttr = listenerGizmoType.GetCustomAttribute<GizmoComponentAttribute>();
        Assert.NotNull(listenerGizmoAttr);
        Assert.Equal(typeof(AudioListenerComponent), listenerGizmoAttr.ComponentType);

        // 3. AudioEmitterFactory & AudioListenerFactory
        var emitterFactoryType = typeof(AudioEmitterFactory);
        var emitterDisplayAttr = emitterFactoryType.GetCustomAttribute<DisplayAttribute>();
        Assert.NotNull(emitterDisplayAttr);
        Assert.Equal("Audio emitter", emitterDisplayAttr.Name);
        Assert.Equal("Audio", emitterDisplayAttr.Category);

        var listenerFactoryType = typeof(AudioListenerFactory);
        var listenerDisplayAttr = listenerFactoryType.GetCustomAttribute<DisplayAttribute>();
        Assert.NotNull(listenerDisplayAttr);
        Assert.Equal("Audio listener", listenerDisplayAttr.Name);
        Assert.Equal("Audio", listenerDisplayAttr.Category);

        var emitterFactory = new AudioEmitterFactory();
        var listenerFactory = new AudioListenerFactory();

        var emitterEntity = await emitterFactory.CreateEntity(null!);
        Assert.NotNull(emitterEntity);
        Assert.NotNull(emitterEntity.Get<AudioEmitterComponent>());

        var listenerEntity = await listenerFactory.CreateEntity(null!);
        Assert.NotNull(listenerEntity);
        Assert.NotNull(listenerEntity.Get<AudioListenerComponent>());
    }

    [Fact]
    public void TestAudioPreviewViewParityAndLineCount()
    {
        var soundPreviewFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../sources/editor/Stride.Assets.Editor.Avalonia/Views/Preview/SoundPreviewView.cs"));
        Assert.True(File.Exists(soundPreviewFile), $"SoundPreviewView.cs file must exist at {soundPreviewFile}");
        var lineCount = File.ReadAllLines(soundPreviewFile).Length;

        // Expanded native Avalonia 12 implementation should be substantive (>= 80 lines)
        Assert.True(lineCount >= 80, $"SoundPreviewView.cs should have at least 80 lines for complete interactive UI, but has {lineCount}.");
    }
}
