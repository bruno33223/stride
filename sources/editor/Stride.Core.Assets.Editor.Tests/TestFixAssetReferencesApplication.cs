// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.GameStudio.Avalonia.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestFixAssetReferencesApplication
{
    public class MockAssetReferenceHolder
    {
        public string? ReferencePath { get; set; } = "Old/Broken/Path";

        public List<string> ReferencesList { get; set; } = ["Old/Ref/0", "Old/Ref/1"];
    }

    private static IViewModelServiceProvider CreateServiceProvider()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        return sp;
    }

    [Fact]
    public void TestApplyFixesReplacesMemberNodeReference()
    {
        var sp = CreateServiceProvider();
        var vm = new FixAssetReferencesViewModel(sp);

        var container = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var holder = new MockAssetReferenceHolder();
        var rootNode = container.GetOrCreateNode(holder);
        var memberNode = rootNode[nameof(MockAssetReferenceHolder.ReferencePath)];

        var item = new FixAssetReferenceItemViewModel(null, "Old/Broken/Path", "New/Fixed/Path", false)
        {
            Node = memberNode,
            TargetReference = "New/Fixed/Path"
        };
        vm.AddItem(item);

        vm.ApplyFixes();

        Assert.Equal("New/Fixed/Path", holder.ReferencePath);
    }

    [Fact]
    public void TestApplyFixesClearsReferenceWhenEmptyAndNotIgnored()
    {
        var sp = CreateServiceProvider();
        var vm = new FixAssetReferencesViewModel(sp);

        var container = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var holder = new MockAssetReferenceHolder { ReferencePath = "Old/Broken/Path" };
        var rootNode = container.GetOrCreateNode(holder);
        var memberNode = rootNode[nameof(MockAssetReferenceHolder.ReferencePath)];

        var item = new FixAssetReferenceItemViewModel(null, "Old/Broken/Path", "", false)
        {
            Node = memberNode,
            TargetReference = ""
        };
        vm.AddItem(item);

        vm.ApplyFixes();

        Assert.Null(holder.ReferencePath);
    }

    [Fact]
    public void TestApplyFixesDoesNotModifyWhenIgnored()
    {
        var sp = CreateServiceProvider();
        var vm = new FixAssetReferencesViewModel(sp);

        var container = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var holder = new MockAssetReferenceHolder { ReferencePath = "Preserved/Path" };
        var rootNode = container.GetOrCreateNode(holder);
        var memberNode = rootNode[nameof(MockAssetReferenceHolder.ReferencePath)];

        var item = new FixAssetReferenceItemViewModel(null, "Old/Broken/Path", "Should/Not/Apply", true)
        {
            Node = memberNode,
            TargetReference = "Should/Not/Apply",
            Ignore = true
        };
        vm.AddItem(item);

        vm.ApplyFixes();

        Assert.Equal("Preserved/Path", holder.ReferencePath);
    }

    [Fact]
    public void TestApplyFixesReplacesIndexedCollectionElement()
    {
        var sp = CreateServiceProvider();
        var vm = new FixAssetReferencesViewModel(sp);

        var container = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var holder = new MockAssetReferenceHolder();
        var rootNode = container.GetOrCreateNode(holder);
        var listNode = rootNode[nameof(MockAssetReferenceHolder.ReferencesList)].Target;

        var item = new FixAssetReferenceItemViewModel(null, "Old/Ref/1", "New/Ref/1", false)
        {
            Node = listNode,
            Index = new NodeIndex(1),
            TargetReference = "New/Ref/1"
        };
        vm.AddItem(item);

        vm.ApplyFixes();

        Assert.Equal("New/Ref/1", holder.ReferencesList[1]);
    }

    [Fact]
    public void TestApplyFixesWithTargetAssetObject()
    {
        var sp = CreateServiceProvider();
        var vm = new FixAssetReferencesViewModel(sp);

        var container = new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } };
        var holder = new MockAssetReferenceHolder();
        var rootNode = container.GetOrCreateNode(holder);
        var memberNode = rootNode[nameof(MockAssetReferenceHolder.ReferencePath)];

        var item = new FixAssetReferenceItemViewModel(null, "Old/Broken/Path", "", false)
        {
            Node = memberNode,
            TargetAsset = "DirectTargetAsset"
        };
        vm.AddItem(item);

        vm.ApplyFixes();

        Assert.Equal("DirectTargetAsset", holder.ReferencePath);
    }
}
