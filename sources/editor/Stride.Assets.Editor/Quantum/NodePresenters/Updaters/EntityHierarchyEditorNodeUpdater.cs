// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

public sealed class EntityHierarchyEditorNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Root.Value is not Entity entity || node.Asset is null || !TryGetEditor(node.Asset, out var editor))
            return;

        var partId = new AbsoluteId(node.Asset.Id, entity.Id);
        var viewModel = editor.FindPartViewModel(partId) as EntityViewModel;
        viewModel?.UpdateNodePresenter(node);
    }

    protected override void FinalizeTree(IAssetNodePresenter root)
    {
        if (root.Value is not Entity entity || root.Asset is null || !TryGetEditor(root.Asset, out var editor))
            return;

        var partId = new AbsoluteId(root.Asset.Id, entity.Id);
        var viewModel = editor.FindPartViewModel(partId) as EntityViewModel;
        viewModel?.FinalizeNodePresenterTree(root);
        base.FinalizeTree(root);
    }

    private static bool TryGetEditor(AssetViewModel asset, [MaybeNullWhen(false)] out EntityHierarchyEditorViewModel editor)
    {
        return asset.ServiceProvider.Get<IAssetEditorsManager>().TryGetAssetEditor(asset, out editor);
    }
}
