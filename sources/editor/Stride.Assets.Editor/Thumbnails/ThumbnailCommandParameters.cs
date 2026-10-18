// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;
using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Parameters for thumbnail commands.
/// </summary>
public class ThumbnailCommandParameters : Stride.Editor.Thumbnails.ThumbnailCommandParameters
{
    public ThumbnailCommandParameters(Asset asset, string thumbnailUrl, Int2 thumbnailSize)
        : base(asset, thumbnailUrl, thumbnailSize)
    {
    }
}
