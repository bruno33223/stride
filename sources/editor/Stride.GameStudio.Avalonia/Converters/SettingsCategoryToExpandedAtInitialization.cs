// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Stride.Core.Assets.Editor.Settings.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.GameStudio.Avalonia.Converters;

public sealed class SettingsCategoryToExpandedAtInitialization : OneWayValueConverter<SettingsCategoryToExpandedAtInitialization>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SettingsCategoryViewModel category)
        {
            return category.Parent == null;
        }

        return false;
    }
}
