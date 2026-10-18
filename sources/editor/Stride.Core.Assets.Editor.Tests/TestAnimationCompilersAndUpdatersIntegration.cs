// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Animations;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Rendering;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAnimationCompilersAndUpdatersIntegration
{
    private static readonly Assembly EditorAssembly = typeof(AnimationPreviewViewModel).Assembly;
    private static readonly Assembly CoreEditorAssembly = typeof(AnimationAssetPreviewCompiler).Assembly;

    private static (SessionViewModel session, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        return (session, sp);
    }

    private static T CreateUninitialized<T>() where T : class
    {
        return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

    #region Fake Node Presenter Implementation

    private class FakeAssetNodePresenter : IAssetNodePresenter
    {
        public Dictionary<string, FakeAssetNodePresenter> ChildrenDict = new();
        public List<FakeAssetNodePresenter> ChildrenList = new();
        public List<INodePresenter> Dependencies = new();

        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Type Type { get; set; } = typeof(object);
        public object Value { get; set; } = null!;
        public bool IsVisible { get; set; } = true;
        public int? Order { get; set; }
        public NodeIndex Index { get; set; } = NodeIndex.Empty;
        public AssetViewModel? Asset { get; set; }
        public INodePresenter? Parent { get; set; }
        public INodePresenter Root => Parent?.Root ?? this;
        public IReadOnlyList<INodePresenter> Children => ChildrenList;
        public List<INodePresenterCommand> Commands { get; } = new();
        public PropertyContainerClass AttachedProperties { get; } = new();
        public bool IsEnumerable => false;
        public bool IsReadOnly { get; set; }
        public string CombineKey { get; set; } = string.Empty;
        public IPropertyProviderViewModel? PropertyProvider => null;
        public ITypeDescriptor? Descriptor => null;
        public INodePresenterFactory Factory => null!;
        AssetNodePresenterFactory IAssetNodePresenter.Factory => null!;
        public bool HasBase => false;
        public bool IsInherited => false;
        public bool IsOverridden => false;

        public event EventHandler<ValueChangingEventArgs>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
        public event EventHandler<EventArgs>? OverrideChanging;
        public event EventHandler<EventArgs>? OverrideChanged;

        public INodePresenter this[string childName]
        {
            get
            {
                if (!ChildrenDict.TryGetValue(childName, out var child))
                {
                    child = new FakeAssetNodePresenter { Name = childName, Parent = this, Asset = this.Asset };
                    ChildrenDict[childName] = child;
                    ChildrenList.Add(child);
                }
                return child;
            }
        }

        IAssetNodePresenter IAssetNodePresenter.this[string childName] => (IAssetNodePresenter)this[childName];

        public void AddChild(FakeAssetNodePresenter child)
        {
            child.Parent = this;
            if (child.Asset == null) child.Asset = this.Asset;
            ChildrenDict[child.Name] = child;
            ChildrenList.Add(child);
        }

        public void AddDependency(INodePresenter node, bool refreshOnNestedNodeChanges)
        {
            Dependencies.Add(node);
        }

        public void UpdateValue(object? newValue) => Value = newValue!;
        public void AddItem(object value) { }
        public void AddItem(object value, NodeIndex index) { }
        public void RemoveItem(object value, NodeIndex index) { }
        public NodeAccessor GetNodeAccessor() => default;
        public void ChangeParent(INodePresenter newParent) => Parent = newParent;
        public void Rename(string newName, bool overwriteCombineKey = true) => Name = newName;
        public INodePresenter? TryGetChild(string childName) => ChildrenDict.TryGetValue(childName, out var c) ? c : null;
        public bool IsObjectReference(object? value) => false;
        public void ResetOverride() { }
        public void Dispose() { }
    }

    #endregion

    [Fact]
    public void TestAnimationAssetPreviewCompiler_MetadataAndDependencies()
    {
        var compilerType = typeof(AnimationAssetPreviewCompiler);
        Assert.NotNull(compilerType);

        Assert.True(typeof(AnimationAssetCompiler).IsAssignableFrom(compilerType),
            "AnimationAssetPreviewCompiler must inherit from AnimationAssetCompiler.");

        var attr = compilerType.GetCustomAttribute<AssetCompilerAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(AnimationAsset).AssemblyQualifiedName, attr.TypeName);
        Assert.Equal(typeof(PreviewCompilationContext), attr.CompilationContext);

        var compiler = new AnimationAssetPreviewCompiler();
        var asset = new AnimationAsset();
        var assetItem = new AssetItem("Animations/TestAnim", asset);

        var inputTypes = compiler.GetInputTypes(assetItem).ToList();
        Assert.NotEmpty(inputTypes);

        // Inherited from base: SkeletonAsset
        var skeletonDep = inputTypes.FirstOrDefault(d => d.AssetType == typeof(SkeletonAsset));
        Assert.Equal(typeof(SkeletonAsset), skeletonDep.AssetType);
        Assert.Equal(typeof(AssetCompilationContext), skeletonDep.CompilationContext);
        Assert.Equal(BuildDependencyType.Runtime | BuildDependencyType.CompileContent, skeletonDep.DependencyType);

        // Added in preview compiler: ModelAsset and MaterialAsset
        var modelDep = inputTypes.FirstOrDefault(d => d.AssetType == typeof(ModelAsset));
        Assert.Equal(typeof(ModelAsset), modelDep.AssetType);
        Assert.Equal(typeof(AssetCompilationContext), modelDep.CompilationContext);
        Assert.Equal(BuildDependencyType.Runtime | BuildDependencyType.CompileContent, modelDep.DependencyType);

        var materialDep = inputTypes.FirstOrDefault(d => d.AssetType == typeof(MaterialAsset));
        Assert.Equal(typeof(MaterialAsset), materialDep.AssetType);
        Assert.Equal(typeof(AssetCompilationContext), materialDep.CompilationContext);
        Assert.Equal(BuildDependencyType.Runtime | BuildDependencyType.CompileContent, materialDep.DependencyType);
    }

    [Fact]
    public void TestAnimationAssetEditorGameCompiler_MetadataAndBuildStep()
    {
        var compilerType = typeof(AnimationAssetEditorGameCompiler);
        Assert.NotNull(compilerType);

        Assert.True(typeof(AssetCompilerBase).IsAssignableFrom(compilerType),
            "AnimationAssetEditorGameCompiler must inherit from AssetCompilerBase.");

        var attr = compilerType.GetCustomAttribute<AssetCompilerAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(AnimationAsset).AssemblyQualifiedName, attr.TypeName);
        Assert.Equal(typeof(EditorGameCompilationContext), attr.CompilationContext);

        var compiler = new AnimationAssetEditorGameCompiler();
        var asset = new AnimationAsset();
        var assetItem = new AssetItem("Animations/TestGameAnim", asset);

        var context = new AssetCompilerContext { CompilationContext = typeof(EditorGameCompilationContext) };
        var result = new AssetCompilerResult();

        var prepareMethod = compilerType.GetMethod("Prepare", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(prepareMethod);

        prepareMethod.Invoke(compiler, [context, assetItem, "Animations/TestGameAnim", result]);

        Assert.Equal(1, result.BuildSteps.Count);
        var buildStep = result.BuildSteps.Steps.Single();
        var commandStep = Assert.IsType<CommandBuildStep>(buildStep);
        Assert.IsType<DummyAssetCommand<AnimationAsset, AnimationClip>>(commandStep.Command);
    }

    [Fact]
    public void TestAnimationAssetNodeUpdater_NodeVisibilityAndDependencies()
    {
        var updater = new AnimationAssetNodeUpdater();
        var animVm = CreateUninitialized<AnimationViewModel>();

        // 1. AnimationTimeMinimum and AnimationTimeMaximum should become invisible
        var minNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.AnimationTimeMinimum), Asset = animVm, IsVisible = true };
        var maxNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.AnimationTimeMaximum), Asset = animVm, IsVisible = true };

        updater.UpdateNode(minNode);
        updater.UpdateNode(maxNode);

        Assert.False(minNode.IsVisible);
        Assert.False(maxNode.IsVisible);

        // 2. Base clip duration visibility based on Enabled child node
        var parentDuration = new FakeAssetNodePresenter { Name = "ClipDuration", Asset = animVm };
        var enabledChild = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.Enabled), Value = false, Asset = animVm };
        parentDuration.AddChild(enabledChild);

        var startNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.StartAnimationTime), Parent = parentDuration, Asset = animVm, IsVisible = true };
        var endNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.EndAnimationTime), Parent = parentDuration, Asset = animVm, IsVisible = true };

        updater.UpdateNode(startNode);
        updater.UpdateNode(endNode);
        Assert.False(startNode.IsVisible);
        Assert.False(endNode.IsVisible);

        enabledChild.Value = true;
        updater.UpdateNode(startNode);
        updater.UpdateNode(endNode);
        Assert.True(startNode.IsVisible);
        Assert.True(endNode.IsVisible);

        // 3. Difference clip duration visibility based on Enabled child node
        var parentDiffDuration = new FakeAssetNodePresenter { Name = "DifferenceClipDuration", Asset = animVm };
        var diffEnabledChild = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDurationUnchecked.Enabled), Value = false, Asset = animVm };
        parentDiffDuration.AddChild(diffEnabledChild);

        var diffStartNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox), Parent = parentDiffDuration, Asset = animVm, IsVisible = true };
        var diffEndNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDurationUnchecked.EndAnimationTimeBox), Parent = parentDiffDuration, Asset = animVm, IsVisible = true };

        updater.UpdateNode(diffStartNode);
        updater.UpdateNode(diffEndNode);
        Assert.False(diffStartNode.IsVisible);
        Assert.False(diffEndNode.IsVisible);

        diffEnabledChild.Value = true;
        updater.UpdateNode(diffStartNode);
        updater.UpdateNode(diffEndNode);
        Assert.True(diffStartNode.IsVisible);
        Assert.True(diffEndNode.IsVisible);

        // 4. Skeleton hides PivotPosition and ScaleImport
        var rootAnimNode = new FakeAssetNodePresenter { Name = "AnimationAssetRoot", Type = typeof(AnimationAsset), Asset = animVm };
        var skeletonNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.Skeleton), Value = new Skeleton(), Asset = animVm };
        var pivotNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.PivotPosition), Asset = animVm, IsVisible = true };
        var scaleNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.ScaleImport), Asset = animVm, IsVisible = true };

        rootAnimNode.AddChild(skeletonNode);
        rootAnimNode.AddChild(pivotNode);
        rootAnimNode.AddChild(scaleNode);

        updater.UpdateNode(rootAnimNode);

        Assert.False(pivotNode.IsVisible);
        Assert.False(scaleNode.IsVisible);
        Assert.Contains(skeletonNode, rootAnimNode.Dependencies);
    }

    [Fact]
    public void TestAnimationAssetNodeUpdater_FinalizeTree()
    {
        var updater = new AnimationAssetNodeUpdater();
        var animVm = CreateUninitialized<AnimationViewModel>();

        var animAsset = new AnimationAsset();
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(animVm, new AssetItem("test", animAsset));

        var root = new FakeAssetNodePresenter { Name = "Root", Asset = animVm };
        var clipDurationNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.ClipDuration), Asset = animVm };
        var enabledNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.Enabled), Asset = animVm };
        var startNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.StartAnimationTime), Asset = animVm };
        var endNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.EndAnimationTime), Asset = animVm };

        clipDurationNode.AddChild(enabledNode);
        clipDurationNode.AddChild(startNode);
        clipDurationNode.AddChild(endNode);
        root.AddChild(clipDurationNode);

        updater.FinalizeTree(root);

        Assert.Contains(enabledNode, startNode.Dependencies);
        Assert.Contains(enabledNode, endNode.Dependencies);
    }

    [Fact]
    public void TestAnimationAssetNodeUpdater_DifferenceAnimationFinalizeTree()
    {
        var updater = new AnimationAssetNodeUpdater();
        var animVm = CreateUninitialized<AnimationViewModel>();

        var diffType = new DifferenceAnimationAssetType();
        var animAsset = new AnimationAsset { Type = diffType };
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(animVm, new AssetItem("test_diff", animAsset));

        var root = new FakeAssetNodePresenter { Name = "Root", Asset = animVm };

        var typeNode = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.Type), Asset = animVm };
        var clipDurationNode = new FakeAssetNodePresenter { Name = nameof(DifferenceAnimationAssetType.ClipDuration), Asset = animVm };
        var modeNode = new FakeAssetNodePresenter { Name = nameof(DifferenceAnimationAssetType.Mode), Asset = animVm };
        var enabledNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDurationUnchecked.Enabled), Asset = animVm };
        var startNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox), Asset = animVm };
        var endNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDurationUnchecked.EndAnimationTimeBox), Asset = animVm };

        clipDurationNode.AddChild(enabledNode);
        clipDurationNode.AddChild(startNode);
        clipDurationNode.AddChild(endNode);

        typeNode.AddChild(clipDurationNode);
        typeNode.AddChild(modeNode);
        root.AddChild(typeNode);

        // Also add base ClipDuration to avoid NRE in base finalize block
        var baseClipDuration = new FakeAssetNodePresenter { Name = nameof(AnimationAsset.ClipDuration), Asset = animVm };
        var baseEnabledNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.Enabled), Asset = animVm };
        var baseStartNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.StartAnimationTime), Asset = animVm };
        var baseEndNode = new FakeAssetNodePresenter { Name = nameof(AnimationAssetDuration.EndAnimationTime), Asset = animVm };
        baseClipDuration.AddChild(baseEnabledNode);
        baseClipDuration.AddChild(baseStartNode);
        baseClipDuration.AddChild(baseEndNode);
        root.AddChild(baseClipDuration);

        updater.FinalizeTree(root);

        Assert.Contains(modeNode, clipDurationNode.Dependencies);
        Assert.Contains(enabledNode, startNode.Dependencies);
        Assert.Contains(enabledNode, endNode.Dependencies);
    }

    [Fact]
    public void TestSkeletonAssetNodeUpdater_DisplayNameIndentation()
    {
        var updater = new SkeletonAssetNodeUpdater();
        var skelVm = CreateUninitialized<SkeletonViewModel>();

        var node = new FakeAssetNodePresenter
        {
            Name = "NodeInformationItem",
            Type = typeof(NodeInformation),
            Asset = skelVm,
            IsVisible = true
        };

        var nameChild = new FakeAssetNodePresenter { Name = nameof(NodeInformation.Name), Value = "Arm_L", Asset = skelVm };
        var depthChild = new FakeAssetNodePresenter { Name = nameof(NodeInformation.Depth), Value = 3, Asset = skelVm };
        var preserveChild = new FakeAssetNodePresenter { Name = nameof(NodeInformation.Preserve), Value = true, Asset = skelVm, IsVisible = true };

        node.AddChild(nameChild);
        node.AddChild(depthChild);
        node.AddChild(preserveChild);

        updater.UpdateNode(node);

        // Children should be hidden
        Assert.False(nameChild.IsVisible);
        Assert.False(depthChild.IsVisible);
        Assert.False(preserveChild.IsVisible);

        // Display name should have 2 * depth (6) leading spaces + Name
        Assert.Equal("      Arm_L", node.DisplayName);
    }

    [Fact]
    public void TestSkeletonAssetNodeUpdater_IgnoresNonSkeletonViewModel()
    {
        var updater = new SkeletonAssetNodeUpdater();
        var animVm = CreateUninitialized<AnimationViewModel>();

        var node = new FakeAssetNodePresenter
        {
            Name = "NodeInformationItem",
            Type = typeof(NodeInformation),
            Asset = animVm,
            IsVisible = true
        };

        var nameChild = new FakeAssetNodePresenter { Name = nameof(NodeInformation.Name), Value = "Arm_L", Asset = animVm };
        var depthChild = new FakeAssetNodePresenter { Name = nameof(NodeInformation.Depth), Value = 3, Asset = animVm };
        node.AddChild(nameChild);
        node.AddChild(depthChild);

        updater.UpdateNode(node);

        // Should NOT modify DisplayName when asset is not SkeletonViewModel
        Assert.Equal(string.Empty, node.DisplayName);
    }

    [Fact]
    public void TestAnimationPreviewViewModel_MetadataAndInheritance()
    {
        var vmType = typeof(AnimationPreviewViewModel);

        Assert.True(typeof(AssetPreviewViewModel<AnimationPreview>).IsAssignableFrom(vmType),
            "AnimationPreviewViewModel must inherit from AssetPreviewViewModel<AnimationPreview>.");

        Assert.True(typeof(IAnimatedPreviewViewModel).IsAssignableFrom(vmType),
            "AnimationPreviewViewModel must implement IAnimatedPreviewViewModel.");

        var attr = vmType.GetCustomAttribute<AssetPreviewViewModelAttribute<AnimationPreview>>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(AnimationPreview), attr.AssetPreviewType);
    }

    [Fact]
    public void TestAnimationPreviewViewModel_FindModelForPreview()
    {
        var package = new Package();

        // 1. ModelAsset without skeleton -> null
        var modelNoSkel = new ModelAsset { Skeleton = null };
        var modelNoSkelItem = new AssetItem("Models/NoSkel", modelNoSkel);
        package.Assets.Add(modelNoSkelItem);

        var animNoSkel = new AnimationAsset { PreviewModel = AttachedReferenceManager.CreateProxyObject<Model>(modelNoSkelItem.Id, modelNoSkelItem.Location) };
        var animNoSkelItem = new AssetItem("Animations/AnimNoSkel", animNoSkel);
        package.Assets.Add(animNoSkelItem);

        Assert.Null(AnimationPreviewViewModel.FindModelForPreview(animNoSkelItem));

        // 2. ModelAsset with skeleton -> returns model item
        var modelWithSkel = new ModelAsset { Skeleton = new Skeleton() };
        var modelWithSkelItem = new AssetItem("Models/WithSkel", modelWithSkel);
        package.Assets.Add(modelWithSkelItem);

        var animWithSkel = new AnimationAsset { PreviewModel = AttachedReferenceManager.CreateProxyObject<Model>(modelWithSkelItem.Id, modelWithSkelItem.Location) };
        var animWithSkelItem = new AssetItem("Animations/AnimWithSkel", animWithSkel);
        package.Assets.Add(animWithSkelItem);

        var result = AnimationPreviewViewModel.FindModelForPreview(animWithSkelItem);
        Assert.NotNull(result);
        Assert.Same(modelWithSkelItem, result);

        // 3. AnimationAsset with null PreviewModel -> returns null
        var animNullModel = new AnimationAsset { PreviewModel = null };
        var animNullModelItem = new AssetItem("Animations/AnimNullModel", animNullModel);
        package.Assets.Add(animNullModelItem);

        Assert.Null(AnimationPreviewViewModel.FindModelForPreview(animNullModelItem));
    }

    [Fact]
    public void TestAnimationPreviewViewModel_NullSafetyBeforeAttach()
    {
        var (session, _) = CreateTestSession();
        var vm = new AnimationPreviewViewModel(session);

        // Before AttachPreview, setting TimeScale, CurrentTime or executing Play/Pause should NOT throw NullReferenceException
        vm.TimeScale = 2.0f;
        Assert.Equal(2.0f, vm.TimeScale);

        vm.CurrentTime = 5.0f;
        Assert.Equal(5.0f, vm.CurrentTime);

        // PlayCommand & PauseCommand toggling
        Assert.NotNull(vm.PlayCommand);
        Assert.NotNull(vm.PauseCommand);

        vm.PlayCommand.Execute();
        Assert.False(vm.PlayCommand.IsEnabled);
        Assert.True(vm.PauseCommand.IsEnabled);

        vm.PauseCommand.Execute();
        Assert.True(vm.PlayCommand.IsEnabled);
        Assert.False(vm.PauseCommand.IsEnabled);

        Assert.False(vm.IsPlaying);
    }

    [Fact]
    public void TestAnimationPreviewViewModel_UpdateViewModelTime()
    {
        var (session, _) = CreateTestSession();
        var vm = new AnimationPreviewViewModel(session);

        var updateMethod = typeof(AnimationPreviewViewModel).GetMethod("UpdateViewModelTime", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(updateMethod);

        updateMethod.Invoke(vm, [true, 2.5f, 10.0f]);

        Assert.True(vm.IsValid);
        Assert.Equal(2.5f, vm.CurrentTime);
        Assert.Equal(10.0f, vm.Duration);
    }
}
