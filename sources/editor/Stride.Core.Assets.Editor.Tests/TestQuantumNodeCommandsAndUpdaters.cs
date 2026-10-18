// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestQuantumNodeCommandsAndUpdaters
{
    [Fact]
    public void TestQuantumKeysInitialization()
    {
        Assert.NotNull(ModelNodeLinkData.Key);
        Assert.Equal("AvailableNodes", ModelNodeLinkData.AvailableNodes);

        Assert.NotNull(VisualScriptData.OwnerBlockKey);
        Assert.Equal("OwnerBlock", VisualScriptData.OwnerBlock);

        Assert.NotNull(SpriteFontData.Key);
        Assert.Equal("SpriteFontAvailableFonts", SpriteFontData.SpriteFontAvailableFonts);
    }

    [Fact]
    public void TestCommandNamesAndTypes()
    {
        Assert.Equal("AddNewScriptComponent", AddNewScriptComponentCommand.CommandName);
        Assert.Equal("EditCurve", EditCurveCommand.CommandName);
        Assert.Equal("PickupComponent", PickupEntityComponentCommand.CommandName);
        Assert.Equal("PickupEntity", PickupEntityCommand.CommandName);
        Assert.Equal("SetSymbolReference", SetSymbolReferenceCommand.CommandName);
        Assert.Equal("SkeletonNodePreserveAll", SkeletonNodePreserveAllCommand.CommandName);

        var addScriptCmd = new AddNewScriptComponentCommand();
        Assert.Equal("AddNewScriptComponent", addScriptCmd.Name);
        Assert.Equal(CombineMode.CombineOnlyForAll, addScriptCmd.CombineMode);

        var setSymbolCmd = new SetSymbolReferenceCommand();
        Assert.Equal("SetSymbolReference", setSymbolCmd.Name);

        var skeletonCmd = new SkeletonNodePreserveAllCommand();
        Assert.Equal("SkeletonNodePreserveAll", skeletonCmd.Name);

        var pickupEntityCmd = new PickupEntityCommand();
        Assert.Equal("PickupEntity", pickupEntityCmd.Name);

        var pickupComponentCmd = new PickupEntityComponentCommand();
        Assert.Equal("PickupComponent", pickupComponentCmd.Name);
    }

    [Fact]
    public void TestUpdatersInstantiation()
    {
        var fxaaUpdater = new FXAAEffectNodeUpdater();
        Assert.NotNull(fxaaUpdater);

        var fontUpdater = new SpriteFontAssetNodeUpdater();
        Assert.NotNull(fontUpdater);

        var visualScriptUpdater = new VisualScriptNodeUpdater();
        Assert.NotNull(visualScriptUpdater);

        var modelLinkUpdater = new ModelNodeLinkNodeUpdater();
        Assert.NotNull(modelLinkUpdater);

        var entityHierarchyUpdater = new EntityHierarchyEditorNodeUpdater();
        Assert.NotNull(entityHierarchyUpdater);
    }

    [Fact]
    public void TestSymbolReferenceViewModel()
    {
        var vm = new SymbolReferenceViewModel();
        Assert.NotNull(vm);
    }
}
