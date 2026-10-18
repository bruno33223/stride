// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Models;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Engine;
using Stride.Rendering;
using Stride.SpriteStudio.Offline;
using Stride.SpriteStudio.Runtime;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

public sealed class ModelNodeLinkNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        var entity = node.Root.Value as Entity;
        var asset = node.Asset;
        if (asset == null || entity == null)
            return;

        if (node.Name == nameof(ModelNodeLinkComponent.Target) && node.Parent?.Value is ModelNodeLinkComponent)
        {
            var parent = (IAssetNodePresenter)node.Parent;
            parent.AttachedProperties.Set(ModelNodeLinkData.Key, GetAvailableNodesForLink(asset, (ModelNodeLinkComponent)parent?.Value!));
        }

        if (node.Name == nameof(SpriteStudioNodeLinkComponent.Target) && node.Parent?.Value is SpriteStudioNodeLinkComponent)
        {
            var parent = (IAssetNodePresenter)node.Parent;
            parent.AttachedProperties.Set(ModelNodeLinkData.Key, GetAvailableNodesForLink(asset, (SpriteStudioNodeLinkComponent)parent?.Value!));
        }
        var physicsComponent = node.Value as PhysicsComponent;
        if (physicsComponent != null)
        {
            node.AttachedProperties.Set(ModelNodeLinkData.Key, GetAvailableNodesForLink(asset, physicsComponent));
        }
    }

    private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, ModelNodeLinkComponent? modelNodeLinkComponent)
    {
        return GetAvailableNodesForLink(viewModel, modelNodeLinkComponent?.Target?.Model ?? modelNodeLinkComponent?.Entity?.Transform.Parent?.Entity?.Get<ModelComponent>()?.Model);
    }

    private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, SpriteStudioNodeLinkComponent? spriteStudioNodeLinkComponent)
    {
        return GetAvailableNodesForLink(viewModel, spriteStudioNodeLinkComponent?.Target?.Sheet ?? spriteStudioNodeLinkComponent?.Entity?.Transform.Parent?.Entity?.Get<SpriteStudioComponent>()?.Sheet);
    }

    private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, PhysicsComponent? physicsComponent)
    {
        return GetAvailableNodesForLink(viewModel, physicsComponent?.Entity?.Get<ModelComponent>()?.Model);
    }

    private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, Model? model)
    {
        if (model == null)
            return Enumerable.Empty<NodeInformation>();

        var parentModelAsset = viewModel?.AssetItem.Package.Session.FindAssetFromProxyObject(model);
        var modelAsset = parentModelAsset?.Asset as ModelAsset;
        if (modelAsset != null)
        {
            var skeletonAsset = parentModelAsset?.Package.FindAssetFromProxyObject(modelAsset.Skeleton);
            if (skeletonAsset != null)
            {
                return ((SkeletonAsset)skeletonAsset.Asset).Nodes;
            }
        }
        return Enumerable.Empty<NodeInformation>();
    }

    private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, SpriteStudioSheet? sheet)
    {
        if (sheet == null)
            return Enumerable.Empty<NodeInformation>();

        var parentModelAsset = viewModel.AssetItem.Package.Session.FindAssetFromProxyObject(sheet);
        var modelAsset = parentModelAsset?.Asset as SpriteStudioModelAsset;
        if (modelAsset != null)
        {
            return modelAsset.NodeNames.Select(nodeName => new NodeInformation(nodeName, 0, true));
        }
        return Enumerable.Empty<NodeInformation>();
    }
}
