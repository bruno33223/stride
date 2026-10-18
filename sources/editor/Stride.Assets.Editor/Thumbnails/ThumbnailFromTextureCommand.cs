// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Command to create thumbnail using background texture and title text.
/// </summary>
public abstract class ThumbnailFromTextureCommand<TRuntimeAsset> : Stride.Editor.Thumbnails.ThumbnailFromTextureCommand<TRuntimeAsset>
    where TRuntimeAsset : class
{
    protected ThumbnailFromTextureCommand(Stride.Editor.Thumbnails.ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, Stride.Editor.Thumbnails.ThumbnailCommandParameters parameters)
        : base(context, assetItem, assetFinder, url, parameters)
    {
    }
}
