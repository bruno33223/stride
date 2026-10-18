// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Assets.Editor.Avalonia.Views;

public abstract class VisualScriptSlotBrush<T> : OneWayValueConverter<T> where T : VisualScriptSlotBrush<T>, IValueConverter, new()
{
    protected abstract string ResourceSuffix { get; }

    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not VisualScriptSlotViewModel slot)
            return null;

        var kind = slot.Kind;
        var resourceKey = kind + ResourceSuffix;

        if (parameter is StyledElement styledElement && styledElement.TryFindResource(resourceKey, out var res))
            return res;

        if (Application.Current != null && Application.Current.TryFindResource(resourceKey, out var appRes))
            return appRes;

        return null;
    }
}

public class VisualScriptSlotBrushConnectorFill : VisualScriptSlotBrush<VisualScriptSlotBrushConnectorFill>
{
    protected override string ResourceSuffix => "ConnectorFill";
}

public class VisualScriptSlotBrushMouseOverConnectorFill : VisualScriptSlotBrush<VisualScriptSlotBrushMouseOverConnectorFill>
{
    protected override string ResourceSuffix => "MouseOverConnectorFill";
}

public class VisualScriptSlotBrushConnectorStroke : VisualScriptSlotBrush<VisualScriptSlotBrushConnectorStroke>
{
    protected override string ResourceSuffix => "ConnectorStroke";
}
