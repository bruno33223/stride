// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;
using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Command to generate a thumbnail from static image data.
/// </summary>
public abstract class StaticThumbnailCommand<T> : Stride.Editor.Thumbnails.StaticThumbnailCommand<T>
{
    public StaticThumbnailCommand(string url, byte[] staticImageData, Int2 thumbnailSize, bool loadAsSRgb, IAssetFinder assetFinder)
        : base(url, staticImageData, thumbnailSize, loadAsSRgb, assetFinder)
    {
    }
}
