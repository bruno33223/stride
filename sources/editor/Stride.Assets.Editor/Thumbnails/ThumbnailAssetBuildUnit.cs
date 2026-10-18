// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Assets;
using Stride.Core.Assets;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Build unit for compiling asset thumbnails within the asset builder service.
/// </summary>
[Stride.Core.DataContractIgnore]
public class ThumbnailAssetBuildUnit : Stride.Editor.Thumbnails.ThumbnailAssetBuildUnit
{
    public ThumbnailAssetBuildUnit(AssetItem asset, GameSettingsAsset gameSettings, Stride.Editor.Thumbnails.GameStudioThumbnailService thumbnailService, int priorityOrder)
        : base(asset, gameSettings, thumbnailService, priorityOrder)
    {
    }
}
