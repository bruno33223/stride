// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.ViewModels;

public class CurveRenderSeries
{
    public string Name { get; set; } = string.Empty;

    public Color Color { get; set; } = Color.White;

    public List<Vector2> Points { get; } = new();
}
