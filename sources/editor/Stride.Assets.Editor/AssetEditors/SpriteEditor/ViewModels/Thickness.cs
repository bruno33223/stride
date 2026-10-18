// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.ViewModels;

public readonly record struct Thickness(double Left, double Top, double Right, double Bottom)
{
    public Thickness(double uniformLength) : this(uniformLength, uniformLength, uniformLength, uniformLength) { }
    public Thickness(double horizontal, double vertical) : this(horizontal, vertical, horizontal, vertical) { }

    public static readonly Thickness Zero = new(0, 0, 0, 0);
}
