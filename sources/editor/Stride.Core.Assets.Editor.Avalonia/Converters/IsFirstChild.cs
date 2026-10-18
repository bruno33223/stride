// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public class IsFirstChild : OneWayValueConverter<IsFirstChild>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var node = value as NodeViewModel;
        return node?.Parent != null && node.Parent.Children.FirstOrDefault(x => x.IsVisible) == node;
    }
}
