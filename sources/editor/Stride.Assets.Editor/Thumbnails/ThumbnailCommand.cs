// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Base class for thumbnail commands.
/// </summary>
public abstract class ThumbnailCommand : Stride.Editor.Thumbnails.ThumbnailCommand
{
    protected ThumbnailCommand(string url, AssetItem assetItem, ThumbnailCommandParameters parameters, IAssetFinder assetFinder)
        : base(url, assetItem, parameters, assetFinder)
    {
    }
}
