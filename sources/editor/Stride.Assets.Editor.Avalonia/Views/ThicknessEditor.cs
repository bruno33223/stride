// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Assets.Editor.Avalonia.Views;

using Thickness = Stride.UI.Thickness;

public class ThicknessEditor : VectorEditorBase<Thickness?>
{
    static ThicknessEditor()
    {
        BackProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        BottomProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        FrontProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        LeftProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        RightProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        TopProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
    }

    /// <summary>
    /// Identifies the <see cref="Back"/> styled property.
    /// </summary>
    public static readonly StyledProperty<float?> BackProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Back), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    /// <summary>
    /// Identifies the <see cref="Bottom"/> styled property.
    /// </summary>
    public static readonly StyledProperty<float?> BottomProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Bottom), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    /// <summary>
    /// Identifies the <see cref="Front"/> styled property.
    /// </summary>
    public static readonly StyledProperty<float?> FrontProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Front), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    /// <summary>
    /// Identifies the <see cref="Left"/> styled property.
    /// </summary>
    public static readonly StyledProperty<float?> LeftProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Left), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    /// <summary>
    /// Identifies the <see cref="Right"/> styled property.
    /// </summary>
    public static readonly StyledProperty<float?> RightProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Right), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    /// <summary>
    /// Identifies the <see cref="Top"/> styled property.
    /// </summary>
    public static readonly StyledProperty<float?> TopProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Top), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Back"/> component of the <see cref="Thickness"/> associated to this control.
    /// </summary>
    public float? Back { get => GetValue(BackProperty); set => SetValue(BackProperty, value); }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Bottom"/> component of the <see cref="Thickness"/> associated to this control.
    /// </summary>
    public float? Bottom { get => GetValue(BottomProperty); set => SetValue(BottomProperty, value); }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Front"/> component of the <see cref="Thickness"/> associated to this control.
    /// </summary>
    public float? Front { get => GetValue(FrontProperty); set => SetValue(FrontProperty, value); }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Left"/> component of the <see cref="Thickness"/> associated to this control.
    /// </summary>
    public float? Left { get => GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Right"/> component of the <see cref="Thickness"/> associated to this control.
    /// </summary>
    public float? Right { get => GetValue(RightProperty); set => SetValue(RightProperty, value); }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Top"/> component of the <see cref="Thickness"/> associated to this control.
    /// </summary>
    public float? Top { get => GetValue(TopProperty); set => SetValue(TopProperty, value); }

    protected override void UpdateComponentsFromValue(Thickness? value)
    {
        if (value is not { } thickness)
            return;

        SetCurrentValue(LeftProperty, thickness.Left);
        SetCurrentValue(TopProperty, thickness.Top);
        SetCurrentValue(BackProperty, thickness.Back);
        SetCurrentValue(RightProperty, thickness.Right);
        SetCurrentValue(BottomProperty, thickness.Bottom);
        SetCurrentValue(FrontProperty, thickness.Front);
    }

    protected override Thickness? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == LeftProperty)
            return Left.HasValue && Value.HasValue ? new Thickness(Left.Value, Value.Value.Top, Value.Value.Back, Value.Value.Right, Value.Value.Bottom, Value.Value.Front) : null;
        if (property == TopProperty)
            return Top.HasValue && Value.HasValue ? new Thickness(Value.Value.Left, Top.Value, Value.Value.Back, Value.Value.Right, Value.Value.Bottom, Value.Value.Front) : null;
        if (property == BackProperty)
            return Back.HasValue && Value.HasValue ? new Thickness(Value.Value.Left, Value.Value.Top, Back.Value, Value.Value.Right, Value.Value.Bottom, Value.Value.Front) : null;
        if (property == RightProperty)
            return Right.HasValue && Value.HasValue ? new Thickness(Value.Value.Left, Value.Value.Top, Value.Value.Back, Right.Value, Value.Value.Bottom, Value.Value.Front) : null;
        if (property == BottomProperty)
            return Bottom.HasValue && Value.HasValue ? new Thickness(Value.Value.Left, Value.Value.Top, Value.Value.Back, Value.Value.Right, Bottom.Value, Value.Value.Front) : null;
        if (property == FrontProperty)
            return Front.HasValue && Value.HasValue ? new Thickness(Value.Value.Left, Value.Value.Top, Value.Value.Back, Value.Value.Right, Value.Value.Bottom, Front.Value) : null;

        throw new ArgumentException($"Property unsupported: {property}");
    }

    protected override Thickness? UpateValueFromFloat(float value)
    {
        return Thickness.UniformCuboid(value);
    }
}
