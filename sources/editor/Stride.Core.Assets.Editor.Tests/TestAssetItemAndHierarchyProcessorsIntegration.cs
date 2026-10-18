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
using Stride.Assets.UI;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Solutions;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetItemAndHierarchyProcessorsIntegration
{
    private sealed class TestPasteItem : IPasteItem
    {
        public object? Data { get; set; }
        public IPasteProcessor? Processor { get; set; }
    }

    private sealed class Container
    {
        public UIElement? Element { get; set; }
    }

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
        upgradesField?.SetValue(session, Activator.CreateInstance(upgradesField.FieldType));

        var constraintField = typeof(PackageSession).GetField("constraintProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        constraintField?.SetValue(session, Activator.CreateInstance(constraintField.FieldType));

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

    [Fact]
    public void TestScenePostPasteProcessorAcceptAndExecution()
    {
        var processor = new ScenePostPasteProcessor();
        var postInterface = (IAssetPostPasteProcessor)processor;

        Assert.True(postInterface.Accept(typeof(SceneAsset)), "ScenePostPasteProcessor must accept SceneAsset");
        Assert.False(postInterface.Accept(typeof(UIPageAsset)), "ScenePostPasteProcessor must not accept UIPageAsset");

        var scene = new SceneAsset
        {
            Parent = AttachedReferenceManager.CreateProxyObject<Scene>(AssetId.New(), "ParentScene")
        };
        scene.ChildrenIds.Add(AssetId.New());
        scene.ChildrenIds.Add(AssetId.New());

        postInterface.PostPasteDeserialization(scene);

        Assert.Null(scene.Parent);
        Assert.Empty(scene.ChildrenIds);
    }

    [Fact]
    public async Task TestUIHierarchyPasteProcessorAcceptAndPaste()
    {
        var processor = new UIHierarchyPasteProcessor();

        Assert.True(processor.Accept(typeof(UIPageAsset), typeof(UIElement), typeof(AssetCompositeHierarchyData<UIElementDesign, UIElement>)),
            "UIHierarchyPasteProcessor must accept UIPageAsset and AssetCompositeHierarchyData");

        var nodeContainer = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var graphContainer = new AssetPropertyGraphContainer(nodeContainer);

        var uiAsset = new UIPageAsset();
        var assetItem = new AssetItem("TestUI", uiAsset);
        var uiGraph = new UIAssetPropertyGraph(graphContainer, assetItem, new LoggerResult());
        graphContainer.RegisterGraph(uiGraph);

        // 1. Paste at root (parentElement = null)
        var button1 = new Button { Id = Guid.NewGuid(), Name = "Btn1" };
        var design1 = new UIElementDesign(button1);
        var hierarchyData = new AssetCompositeHierarchyData<UIElementDesign, UIElement>();
        hierarchyData.Parts.Add(design1);
        hierarchyData.RootParts.Add(button1);

        var pasteItem = new TestPasteItem { Data = hierarchyData };
        var rootContainer = new Container { Element = null };
        var rootAccessor = new NodeAccessor(nodeContainer.GetOrCreateNode(rootContainer)[nameof(Container.Element)], NodeIndex.Empty);
        var propertyContainer = new PropertyContainer();

        await processor.Paste(pasteItem, uiGraph, ref rootAccessor, ref propertyContainer);

        Assert.Single(uiAsset.Hierarchy.RootParts);
        Assert.Equal(button1.Id, uiAsset.Hierarchy.RootParts[0].Id);
        Assert.True(uiAsset.Hierarchy.Parts.ContainsKey(button1.Id));

        // 2. Paste as child into a StackPanel
        var panel = new StackPanel { Id = Guid.NewGuid(), Name = "MyPanel" };
        var panelDesign = new UIElementDesign(panel);
        uiAsset.Hierarchy.RootParts.Add(panel);
        uiAsset.Hierarchy.Parts.Add(panelDesign);

        var button2 = new Button { Id = Guid.NewGuid(), Name = "Btn2" };
        var design2 = new UIElementDesign(button2);
        var childHierarchyData = new AssetCompositeHierarchyData<UIElementDesign, UIElement>();
        childHierarchyData.Parts.Add(design2);
        childHierarchyData.RootParts.Add(button2);

        var pasteItem2 = new TestPasteItem { Data = childHierarchyData };
        var panelContainer = new Container { Element = panel };
        var panelAccessor = new NodeAccessor(nodeContainer.GetOrCreateNode(panelContainer)[nameof(Container.Element)], NodeIndex.Empty);

        await processor.Paste(pasteItem2, uiGraph, ref panelAccessor, ref propertyContainer);

        Assert.Contains(button2, panel.Children);
        Assert.True(uiAsset.Hierarchy.Parts.ContainsKey(button2.Id));
    }

    [Fact]
    public void TestAssetItemPasteProcessorNullValidation()
    {
        var (sessionVm, _) = CreateTestSession();
        var processor = new AssetItemPasteProcessor(sessionVm);
        var graphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer());

        object? dummyData = new List<AssetItem>();
        Assert.Throws<ArgumentNullException>("targetRootObject", () =>
        {
            processor.ProcessDeserializedData(graphContainer, null!, typeof(AssetItem), ref dummyData!, false, null, null!, null!);
        });

        object targetObject = new List<AssetItem>();
        object? nullData = null;
        Assert.Throws<ArgumentNullException>("data", () =>
        {
            processor.ProcessDeserializedData(graphContainer, targetObject, typeof(AssetItem), ref nullData!, false, null, null!, null!);
        });
    }

    [Fact]
    public void TestAssetItemPasteProcessorClonesExistingAssetAndAppliesPostProcessors()
    {
        var (sessionVm, sp) = CreateTestSession();

        var copyPasteService = new CopyPasteService(new AssetPropertyGraphContainer(new AssetNodeContainer()));
        var scenePostProcessor = new ScenePostPasteProcessor();
        copyPasteService.RegisterProcessor(scenePostProcessor);
        sp.RegisterService(copyPasteService);

        var processor = new AssetItemPasteProcessor(sessionVm);

        var originalScene = new SceneAsset();
        originalScene.ChildrenIds.Add(AssetId.New());
        originalScene.ChildrenIds.Add(AssetId.New());
        var originalId = originalScene.Id;

        var existingItem = new AssetItem("Scenes/Level1", originalScene);

        // Register asset viewmodel stub in session
        var assetVm = (AssetViewModel)RuntimeHelpers.GetUninitializedObject(typeof(AssetViewModel<SceneAsset>));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, existingItem);
        sessionVm.RegisterAsset(assetVm);

        Assert.NotNull(sessionVm.GetAssetById(originalId));

        var pasteList = new List<AssetItem>
        {
            new AssetItem("Scenes/Level1_Copy", originalScene)
        };
        AssetCollectionItemIdHelper.GenerateMissingItemIds(pasteList);
        object dataObj = pasteList;

        var result = processor.ProcessDeserializedData(
            copyPasteService.PropertyGraphContainer,
            new List<AssetItem>(),
            typeof(List<AssetItem>),
            ref dataObj,
            false,
            originalId,
            new Yaml.YamlAssetMetadata<OverrideType>(),
            new Yaml.YamlAssetPath());

        Assert.True(result);
        var resultList = Assert.IsAssignableFrom<IList<AssetItem>>(dataObj);
        Assert.Single(resultList);

        var pastedItem = resultList[0];
        Assert.NotEqual(originalId, pastedItem.Id);
        Assert.NotEqual(originalId, pastedItem.Asset.Id);

        var pastedScene = Assert.IsType<SceneAsset>(pastedItem.Asset);
        // Verify ScenePostPasteProcessor was executed
        Assert.Null(pastedScene.Parent);
        Assert.Empty(pastedScene.ChildrenIds);
    }

    [Fact]
    public void TestStrideDefaultAssetsPluginRegistersProcessors()
    {
        var (sessionVm, _) = CreateTestSession();
        var plugin = new StrideDefaultAssetsPlugin();

        var pasteProcessors = new List<IPasteProcessor>();
        plugin.RegisterPasteProcessors(pasteProcessors, sessionVm);

        Assert.Contains(pasteProcessors, p => p is UIHierarchyPasteProcessor);

        var postProcessors = new List<IAssetPostPasteProcessor>();
        plugin.RegisterPostPasteProcessors(postProcessors, sessionVm);

        Assert.Contains(postProcessors, p => p is ScenePostPasteProcessor);
    }
}
