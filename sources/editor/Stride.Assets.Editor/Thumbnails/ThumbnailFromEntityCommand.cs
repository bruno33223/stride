// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Command to create thumbnail by rendering an entity.
/// </summary>
public abstract class ThumbnailFromEntityCommand<TRuntimeAsset> : Stride.Editor.Thumbnails.ThumbnailFromEntityCommand<TRuntimeAsset>
    where TRuntimeAsset : class
{
    protected ThumbnailFromEntityCommand(Stride.Editor.Thumbnails.ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, Stride.Editor.Thumbnails.ThumbnailCommandParameters parameters)
        : base(context, assetItem, assetFinder, url, parameters)
    {
    }
}
