// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets.Entities;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.ViewModel.CopyPasteProcessors;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Core.Assets.Editor.Tests;

public class TestEntityCopyPasteProcessorsIntegration
{
    private const string PresentationAssemblyName = "Stride.Assets.Presentation";
    private const string CopyProcessorTypeName = "Stride.Assets.Presentation.ViewModel.CopyPasteProcessors.EntityComponentCopyProcessor";
    private const string ComponentPasteProcessorTypeName = "Stride.Assets.Presentation.ViewModel.CopyPasteProcessors.EntityComponentPasteProcessor";
    private const string HierarchyPasteProcessorTypeName = "Stride.Assets.Presentation.ViewModel.CopyPasteProcessors.EntityHierarchyPasteProcessor";

    private class TestPasteItem : IPasteItem
    {
        public object Data { get; set; } = null!;
        public IPasteProcessor? Processor { get; set; }
    }

    private class MockDialogService : IDialogService
    {
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

        public bool HasMainWindow => true;

        public void Exit(int exitCode = 0) { }
    }

    private static SessionViewModel CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new MockDialogService());

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        var nodeContainer = new AssetNodeContainer();
        var nodeContainerProp = typeof(SessionViewModel).GetProperty("AssetNodeContainer");
        if (nodeContainerProp?.GetSetMethod(true) != null)
            nodeContainerProp.GetSetMethod(true)!.Invoke(session, [nodeContainer]);
        else
            typeof(SessionViewModel).GetField("<AssetNodeContainer>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, nodeContainer);

        return session;
    }

    [Fact]
    public void TestConcreteProcessorsDirectInstantiation()
    {
        var copyProcessor = new EntityComponentCopyProcessor();
        Assert.NotNull(copyProcessor);
        Assert.True(copyProcessor.Accept(typeof(TransformComponent)));

        var pasteComponentProcessor = new EntityComponentPasteProcessor();
        Assert.NotNull(pasteComponentProcessor);
        Assert.True(pasteComponentProcessor.Accept(typeof(Entity), typeof(EntityComponent), typeof(TransformComponent)));

        var pasteHierarchyProcessor = new EntityHierarchyPasteProcessor();
        Assert.NotNull(pasteHierarchyProcessor);
        Assert.NotNull(EntityHierarchyPasteProcessor.TargetFolderKey);
    }

    [Fact]
    public void TestProcessorsTypesExistInPresentationAssembly()
    {
        var asm = typeof(StrideDefaultAssetsPlugin).Assembly;

        var copyType = asm.GetType(CopyProcessorTypeName);
        Assert.True(copyType != null, $"Expected type '{CopyProcessorTypeName}' to exist in '{PresentationAssemblyName}'.");
        Assert.True(typeof(ICopyProcessor).IsAssignableFrom(copyType));

        var componentPasteType = asm.GetType(ComponentPasteProcessorTypeName);
        Assert.True(componentPasteType != null, $"Expected type '{ComponentPasteProcessorTypeName}' to exist in '{PresentationAssemblyName}'.");
        Assert.True(typeof(AssetPropertyPasteProcessor).IsAssignableFrom(componentPasteType));

        var hierarchyPasteType = asm.GetType(HierarchyPasteProcessorTypeName);
        Assert.True(hierarchyPasteType != null, $"Expected type '{HierarchyPasteProcessorTypeName}' to exist in '{PresentationAssemblyName}'.");
        Assert.True(typeof(AssetCompositeHierarchyPasteProcessor<EntityDesign, Entity>).IsAssignableFrom(hierarchyPasteType));
    }

    [Fact]
    public void TestStrideDefaultAssetsPluginRegistersProcessorsConcreteInstances()
    {
        var session = CreateTestSession();
        var plugin = new StrideDefaultAssetsPlugin();

        var copyProcessors = new List<ICopyProcessor>();
        plugin.RegisterCopyProcessors(copyProcessors, session);

        Assert.NotEmpty(copyProcessors);
        Assert.Contains(copyProcessors, p => p.GetType().FullName == CopyProcessorTypeName);

        var pasteProcessors = new List<IPasteProcessor>();
        plugin.RegisterPasteProcessors(pasteProcessors, session);

        Assert.NotEmpty(pasteProcessors);
        Assert.Contains(pasteProcessors, p => p.GetType().FullName == ComponentPasteProcessorTypeName);
        Assert.Contains(pasteProcessors, p => p.GetType().FullName == HierarchyPasteProcessorTypeName);
    }

    [Fact]
    public void TestEntityComponentCopyProcessorBehavior()
    {
        var asm = typeof(StrideDefaultAssetsPlugin).Assembly;
        var copyType = asm.GetType(CopyProcessorTypeName);
        Assert.NotNull(copyType);

        var processor = (ICopyProcessor)Activator.CreateInstance(copyType)!;
        Assert.NotNull(processor);

        // Accept tests
        Assert.True(processor.Accept(typeof(TransformComponent)));
        Assert.True(processor.Accept(typeof(EntityComponentCollection)));
        Assert.False(processor.Accept(typeof(string)));
        Assert.False(processor.Accept(typeof(Entity)));

        // Process TransformComponent clearing Children
        var parentTransform = new TransformComponent();
        var childTransform = new TransformComponent();
        parentTransform.Children.Add(childTransform);
        Assert.Single(parentTransform.Children);

        object data = parentTransform;
        var processed = processor.Process(ref data, null!);
        Assert.True(processed);
        Assert.Empty(((TransformComponent)data).Children);

        // Process EntityComponentCollection clearing Children of contained transforms
        var collection = new EntityComponentCollection();
        var t1 = new TransformComponent();
        t1.Children.Add(new TransformComponent());
        collection.Add(t1);

        object collData = collection;
        var processedColl = processor.Process(ref collData, null!);
        Assert.True(processedColl);
        Assert.Empty(((TransformComponent)collection[0]).Children);
    }

    [Fact]
    public void TestEntityComponentPasteProcessorBehavior()
    {
        var asm = typeof(StrideDefaultAssetsPlugin).Assembly;
        var pasteType = asm.GetType(ComponentPasteProcessorTypeName);
        Assert.NotNull(pasteType);

        var processor = (AssetPropertyPasteProcessor)Activator.CreateInstance(pasteType)!;
        Assert.NotNull(processor);

        // Accept
        Assert.True(processor.Accept(typeof(Entity), typeof(EntityComponent), typeof(TransformComponent)));
        Assert.True(processor.Accept(typeof(Entity), typeof(EntityComponentCollection), typeof(TransformComponent)));
        Assert.False(processor.Accept(typeof(Entity), typeof(string), typeof(TransformComponent)));

        // Quantum node testing with EntityComponentCollection
        var propertyGraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var entity = new Entity("TestEntity");
        var entityDesign = new EntityDesign(entity);
        var scene = new SceneAsset();
        scene.Hierarchy.Parts.Add(entityDesign);
        var assetItem = new AssetItem("Scenes/TestScene", scene);

        var propertyGraph = new EntityHierarchyPropertyGraph(propertyGraphContainer, assetItem, new LoggerResult());
        propertyGraphContainer.RegisterGraph(propertyGraph);

        var entityNode = propertyGraphContainer.NodeContainer.GetNode(entity);
        var componentsNode = (IObjectNode)entityNode[nameof(Entity.Components)].Target;

        var canRemoveMethod = pasteType.GetMethod("CanRemoveItem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(canRemoveMethod);

        // Cannot remove transform component at index 0
        var canRemove0 = (bool)canRemoveMethod.Invoke(processor, [componentsNode, new NodeIndex(0)])!;
        Assert.False(canRemove0);

        var canInsertMethod = pasteType.GetMethod("CanInsertItem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(canInsertMethod);

        // Cannot insert another TransformComponent (disallow multiple components)
        var newTransform = new TransformComponent();
        var canInsertTransform = (bool)canInsertMethod.Invoke(processor, [componentsNode, new NodeIndex(1), newTransform])!;
        Assert.False(canInsertTransform);

        // Can insert component that allows multiple or single not yet present
        var canReplaceMethod = pasteType.GetMethod("CanReplaceItem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(canReplaceMethod);

        // Cannot replace TransformComponent by another component type (e.g., CameraComponent)
        var camera = new CameraComponent();
        var canReplaceTransformWithCamera = (bool)canReplaceMethod.Invoke(processor, [componentsNode, new NodeIndex(0), camera])!;
        Assert.False(canReplaceTransformWithCamera);

        // Can replace TransformComponent with another TransformComponent
        var replacementTransform = new TransformComponent
        {
            Position = new Stride.Core.Mathematics.Vector3(1, 2, 3),
            Rotation = Stride.Core.Mathematics.Quaternion.RotationX(0.5f),
            Scale = new Stride.Core.Mathematics.Vector3(2, 2, 2)
        };
        var canReplaceTransform = (bool)canReplaceMethod.Invoke(processor, [componentsNode, new NodeIndex(0), replacementTransform])!;
        Assert.True(canReplaceTransform);

        // ReplaceItem updates Position, Rotation, Scale on the existing TransformComponent
        var replaceItemMethod = pasteType.GetMethod("ReplaceItem", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(replaceItemMethod);
        replaceItemMethod.Invoke(processor, [componentsNode, new NodeIndex(0), replacementTransform]);

        Assert.Equal(new Stride.Core.Mathematics.Vector3(1, 2, 3), entity.Transform.Position);
        Assert.Equal(replacementTransform.Rotation, entity.Transform.Rotation);
        Assert.Equal(new Stride.Core.Mathematics.Vector3(2, 2, 2), entity.Transform.Scale);
    }

    [Fact]
    public async Task TestEntityHierarchyPasteProcessorBehavior()
    {
        var asm = typeof(StrideDefaultAssetsPlugin).Assembly;
        var pasteType = asm.GetType(HierarchyPasteProcessorTypeName);
        Assert.NotNull(pasteType);

        var processor = (IPasteProcessor)Activator.CreateInstance(pasteType)!;
        Assert.NotNull(processor);

        // Check TargetFolderKey property
        var targetFolderKeyField = pasteType.GetField("TargetFolderKey", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(targetFolderKeyField);
        var targetFolderKey = targetFolderKeyField.GetValue(null) as PropertyKey<string>;
        Assert.NotNull(targetFolderKey);

        // Check Accept
        Assert.True(processor.Accept(typeof(SceneAsset), typeof(AssetCompositeHierarchyData<EntityDesign, Entity>), typeof(AssetCompositeHierarchyData<EntityDesign, Entity>)));
        Assert.True(processor.Accept(typeof(SceneAsset), typeof(Entity), typeof(AssetCompositeHierarchyData<EntityDesign, Entity>)));
        Assert.False(processor.Accept(typeof(SceneAsset), typeof(string), typeof(AssetCompositeHierarchyData<EntityDesign, Entity>)));

        // Test Paste into EntityHierarchyPropertyGraph
        var propertyGraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var scene = new SceneAsset();
        var assetItem = new AssetItem("Scenes/TestScene", scene);
        var propertyGraph = new EntityHierarchyPropertyGraph(propertyGraphContainer, assetItem, new LoggerResult());
        propertyGraphContainer.RegisterGraph(propertyGraph);

        var pastedEntity = new Entity("PastedChild");
        var pastedDesign = new EntityDesign(pastedEntity) { Folder = "SubFolder" };
        var hierarchyData = new AssetCompositeHierarchyData<EntityDesign, Entity>();
        hierarchyData.RootParts.Add(pastedEntity);
        hierarchyData.Parts.Add(pastedDesign);

        var pasteItem = new TestPasteItem { Data = hierarchyData, Processor = processor };
        var rootNode = propertyGraph.RootNode;
        var nodeAccessor = new NodeAccessor(rootNode, NodeIndex.Empty);
        var container = new PropertyContainer();
        container.Set(targetFolderKey, "BaseFolder");

        await processor.Paste(pasteItem, propertyGraph, ref nodeAccessor, ref container);

        // Verify pasted into hierarchy with combined folder
        Assert.Contains(scene.Hierarchy.Parts.Values, p => p.Entity.Name == "PastedChild");
        var addedPart = scene.Hierarchy.Parts.Values.First(p => p.Entity.Name == "PastedChild");
        Assert.Equal("BaseFolder/SubFolder", addedPart.Folder);
    }
}
