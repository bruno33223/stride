// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Editor.Preview;

/// <summary>
/// A <see cref="Engine.Game"/> instance specialized to render previews and create thumbnails.
/// </summary>
public class PreviewGame : Stride.Editor.Preview.PreviewGame
{
    public PreviewGame(IEffectCompiler effectCompiler)
        : base(effectCompiler)
    {
    }
}
