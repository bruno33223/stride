// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Dedicated graphics and preview rendering engine for thumbnail generation.
/// </summary>
[Stride.Core.DataContractIgnore]
public class ThumbnailGenerator : Stride.Editor.Thumbnails.ThumbnailGenerator
{
    public ThumbnailGenerator(EffectCompilerBase effectCompiler)
        : base(effectCompiler)
    {
    }
}
