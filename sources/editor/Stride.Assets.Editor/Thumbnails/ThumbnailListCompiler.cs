// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core.Assets.Compiler;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Compiler that generates build steps for producing a list of thumbnails.
/// </summary>
public class ThumbnailListCompiler : Stride.Editor.Thumbnails.ThumbnailListCompiler
{
    public ThumbnailListCompiler(Stride.Editor.Thumbnails.ThumbnailGenerator generator, EventHandler<Stride.Editor.Thumbnails.ThumbnailBuiltEventArgs> builtAction, AssetCompilerRegistry thumbnailCompilerRegistry)
        : base(generator, builtAction, thumbnailCompilerRegistry)
    {
    }
}
