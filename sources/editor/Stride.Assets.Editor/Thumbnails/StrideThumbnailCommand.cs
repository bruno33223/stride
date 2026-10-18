// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Base command for rendering Stride thumbnails.
/// </summary>
public abstract class StrideThumbnailCommand<TRuntimeAsset> : Stride.Editor.Thumbnails.StrideThumbnailCommand<TRuntimeAsset>
    where TRuntimeAsset : class
{
    protected StrideThumbnailCommand(Stride.Editor.Thumbnails.ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, Stride.Editor.Thumbnails.ThumbnailCommandParameters parameters)
        : base(context, assetItem, assetFinder, url, parameters)
    {
    }
}
