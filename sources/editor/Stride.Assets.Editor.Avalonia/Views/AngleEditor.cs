// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Represents a control that allows editing an angle value stored in radians but displayed in degrees.
/// </summary>
public class AngleEditor : VectorEditorBase<float>
{
    static AngleEditor()
    {
        DegreesProperty.Changed.AddClassHandler<AngleEditor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<float> DegreesProperty =
        AvaloniaProperty.Register<AngleEditor, float>(nameof(Degrees), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceDegreesValue);

    public float Degrees
    {
        get => GetValue(DegreesProperty);
        set => SetValue(DegreesProperty, value);
    }

    public override void ResetValue()
    {
        Value = DefaultValue;
    }

    protected override void UpdateComponentsFromValue(float value)
    {
        var degrees = MathUtil.RadiansToDegrees(value);
        if (DecimalPlaces >= 0)
        {
            degrees = MathF.Round(degrees, DecimalPlaces);
        }
        SetCurrentValue(DegreesProperty, degrees);
    }

    protected override float UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == DegreesProperty)
        {
            return MathUtil.DegreesToRadians(Degrees);
        }

        throw new ArgumentException($"Property unsupported: {property}");
    }

    protected override float UpateValueFromFloat(float value)
    {
        return MathUtil.DegreesToRadians(value);
    }

    private static float CoerceDegreesValue(AvaloniaObject sender, float basevalue)
    {
        var editor = (AngleEditor)sender;
        var decimalPlaces = editor.DecimalPlaces;
        return decimalPlaces < 0 ? basevalue : MathF.Round(basevalue, decimalPlaces);
    }
}
