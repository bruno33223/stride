// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public sealed class EnumToResource : OneWayValueConverter<EnumToResource>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Value can be null when the control is removed from the visual tree and the related property is unbound.
        if (value is null)
            return null;

        var session = SessionViewModel.Instance;
        return session?.ServiceProvider.TryGet<IAssetsPluginService>()?.GetImageForEnum(session, value);
    }
}

