// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Assets.Editor.Avalonia.Views;

public class CollectionContainsItem : OneWayMultiValueConverter<CollectionContainsItem>
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return false;

        var item = values[0];
        var collection = values[1] as IEnumerable;
        return collection != null && collection.Cast<object?>().Any(x => Equals(x, item));
    }
}
