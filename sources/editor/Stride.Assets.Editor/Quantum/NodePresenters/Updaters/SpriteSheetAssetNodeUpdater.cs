// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Extensions;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class SpriteSheetAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is SpriteSheetViewModel asset)
        {
            if (node is { Name: nameof(SpriteSheetAsset.Sprites), Value: List<SpriteInfo> })
            {
                node.Children.ForEach(x => x.IsVisible = false);
                node.IsVisible = false;
            }
            if (typeof(SpriteInfo).IsAssignableFrom(node.Type))
            {
                var isUI = asset.Asset.Type == SpriteSheetType.UI;
                var bordersNode = node.TryGetChild(nameof(SpriteInfo.Borders));
                if (bordersNode != null)
                    bordersNode.IsVisible = isUI;

                var centerNode = node.TryGetChild(nameof(SpriteInfo.Center));
                if (centerNode != null)
                    centerNode.IsVisible = !isUI;

                var centerFromMiddleNode = node.TryGetChild(nameof(SpriteInfo.CenterFromMiddle));
                if (centerFromMiddleNode != null)
                    centerFromMiddleNode.IsVisible = !isUI;
            }
        }
    }
}
