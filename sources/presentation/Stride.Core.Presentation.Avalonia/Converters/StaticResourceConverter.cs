// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// A converter that resolves the specified resource key from Application resources in Avalonia.
/// </summary>
public class StaticResourceConverter : OneWayValueConverter<StaticResourceConverter>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value != null && Application.Current != null && Application.Current.TryFindResource(value.ToString() ?? string.Empty, out var resource))
        {
            return resource;
        }
        return AvaloniaProperty.UnsetValue;
    }
}
