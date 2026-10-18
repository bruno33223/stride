// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stride.Assets.Presentation;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

public class EntityComponentToResource : OneWayValueConverter<EntityComponentToResource>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<EntityComponent> components)
            return null;

        var componentTypes = components.Select(x => x.GetType());
        var compToUse = StrideDefaultAssetsPlugin.GetHighestOrderComponent(componentTypes);
        return TypeToResource.FetchResourceFromType(compToUse, true);
    }
}
