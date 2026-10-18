// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public abstract class ColorEditor<T> : TemplatedControl
{
    private bool isUpdating;

    static ColorEditor()
    {
        ValueProperty.Changed.AddClassHandler<ColorEditor<T>>(OnValuePropertyChanged);
        RedProperty.Changed.AddClassHandler<ColorEditor<T>>(OnComponentPropertyChanged);
        GreenProperty.Changed.AddClassHandler<ColorEditor<T>>(OnComponentPropertyChanged);
        BlueProperty.Changed.AddClassHandler<ColorEditor<T>>(OnComponentPropertyChanged);
        AlphaProperty.Changed.AddClassHandler<ColorEditor<T>>(OnComponentPropertyChanged);
        SelectedPaletteColorProperty.Changed.AddClassHandler<ColorEditor<T>>(OnSelectedPaletteColorPropertyChanged);
    }

    public static readonly StyledProperty<T> ValueProperty =
        AvaloniaProperty.Register<ColorEditor<T>, T>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<ColorEditor<T>, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<IReadOnlyDictionary<string, Color4>> PaletteProperty =
        AvaloniaProperty.Register<ColorEditor<T>, IReadOnlyDictionary<string, Color4>>(nameof(Palette));

    public static readonly StyledProperty<KeyValuePair<string, Color4>?> SelectedPaletteColorProperty =
        AvaloniaProperty.Register<ColorEditor<T>, KeyValuePair<string, Color4>?>(nameof(SelectedPaletteColor), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<float> RedProperty =
        AvaloniaProperty.Register<ColorEditor<T>, float>(nameof(Red), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<float> GreenProperty =
        AvaloniaProperty.Register<ColorEditor<T>, float>(nameof(Green), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<float> BlueProperty =
        AvaloniaProperty.Register<ColorEditor<T>, float>(nameof(Blue), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<float> AlphaProperty =
        AvaloniaProperty.Register<ColorEditor<T>, float>(nameof(Alpha), 1.0f, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> HasAlphaProperty =
        AvaloniaProperty.Register<ColorEditor<T>, bool>(nameof(HasAlpha), true);

    public static readonly StyledProperty<float> MaxComponentValueProperty =
        AvaloniaProperty.Register<ColorEditor<T>, float>(nameof(MaxComponentValue), 1.0f);

    public static readonly StyledProperty<int> ComponentDecimalPlacesProperty =
        AvaloniaProperty.Register<ColorEditor<T>, int>(nameof(ComponentDecimalPlaces), 2);

    public static readonly StyledProperty<double> ComponentSmallChangeProperty =
        AvaloniaProperty.Register<ColorEditor<T>, double>(nameof(ComponentSmallChange), 0.01);

    public T Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public IReadOnlyDictionary<string, Color4> Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public KeyValuePair<string, Color4>? SelectedPaletteColor
    {
        get => GetValue(SelectedPaletteColorProperty);
        set => SetValue(SelectedPaletteColorProperty, value);
    }

    public float Red
    {
        get => GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    public float Green
    {
        get => GetValue(GreenProperty);
        set => SetValue(GreenProperty, value);
    }

    public float Blue
    {
        get => GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    public float Alpha
    {
        get => GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    public bool HasAlpha
    {
        get => GetValue(HasAlphaProperty);
        set => SetValue(HasAlphaProperty, value);
    }

    public float MaxComponentValue
    {
        get => GetValue(MaxComponentValueProperty);
        set => SetValue(MaxComponentValueProperty, value);
    }

    public int ComponentDecimalPlaces
    {
        get => GetValue(ComponentDecimalPlacesProperty);
        set => SetValue(ComponentDecimalPlacesProperty, value);
    }

    public double ComponentSmallChange
    {
        get => GetValue(ComponentSmallChangeProperty);
        set => SetValue(ComponentSmallChangeProperty, value);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Value is not null)
        {
            UpdateComponentsFromValue(Value);
        }
    }

    protected abstract void UpdateComponentsFromValue(T value);

    protected abstract void UpdateValueFromComponents();

    protected abstract void SetValueFromColor4(Color4 color4);

    private static void OnValuePropertyChanged(ColorEditor<T> sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender.isUpdating)
            return;

        sender.isUpdating = true;
        try
        {
            if (sender.Value is not null)
            {
                sender.UpdateComponentsFromValue(sender.Value);
            }
        }
        finally
        {
            sender.isUpdating = false;
        }
    }

    private static void OnComponentPropertyChanged(ColorEditor<T> sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender.isUpdating)
            return;

        sender.isUpdating = true;
        try
        {
            sender.UpdateValueFromComponents();
        }
        finally
        {
            sender.isUpdating = false;
        }
    }

    private static void OnSelectedPaletteColorPropertyChanged(ColorEditor<T> sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender.SelectedPaletteColor is { } kvp)
        {
            sender.SetValueFromColor4(kvp.Value);
        }
    }
}

public sealed class ColorEditor : ColorEditor<Color>
{
    public ColorEditor()
    {
        MaxComponentValue = 255f;
        ComponentDecimalPlaces = 0;
        ComponentSmallChange = 1.0;
        HasAlpha = true;
        Alpha = 255f;
    }

    protected override void UpdateComponentsFromValue(Color value)
    {
        Red = value.R;
        Green = value.G;
        Blue = value.B;
        Alpha = value.A;
    }

    protected override void UpdateValueFromComponents()
    {
        var r = (byte)Math.Clamp(Math.Round(Red), 0, 255);
        var g = (byte)Math.Clamp(Math.Round(Green), 0, 255);
        var b = (byte)Math.Clamp(Math.Round(Blue), 0, 255);
        var a = (byte)Math.Clamp(Math.Round(Alpha), 0, 255);
        SetCurrentValue(ValueProperty, new Color(r, g, b, a));
    }

    protected override void SetValueFromColor4(Color4 color4)
    {
        SetCurrentValue(ValueProperty, (Color)color4);
    }
}

public sealed class Color3Editor : ColorEditor<Color3>
{
    public Color3Editor()
    {
        MaxComponentValue = 1.0f;
        ComponentDecimalPlaces = 3;
        ComponentSmallChange = 0.01;
        HasAlpha = false;
        Alpha = 1.0f;
    }

    protected override void UpdateComponentsFromValue(Color3 value)
    {
        Red = value.R;
        Green = value.G;
        Blue = value.B;
        Alpha = 1.0f;
    }

    protected override void UpdateValueFromComponents()
    {
        var r = Math.Clamp(Red, 0.0f, 1.0f);
        var g = Math.Clamp(Green, 0.0f, 1.0f);
        var b = Math.Clamp(Blue, 0.0f, 1.0f);
        SetCurrentValue(ValueProperty, new Color3(r, g, b));
    }

    protected override void SetValueFromColor4(Color4 color4)
    {
        SetCurrentValue(ValueProperty, color4.ToColor3());
    }
}

public sealed class Color4Editor : ColorEditor<Color4>
{
    public Color4Editor()
    {
        MaxComponentValue = 1.0f;
        ComponentDecimalPlaces = 3;
        ComponentSmallChange = 0.01;
        HasAlpha = true;
        Alpha = 1.0f;
    }

    protected override void UpdateComponentsFromValue(Color4 value)
    {
        Red = value.R;
        Green = value.G;
        Blue = value.B;
        Alpha = value.A;
    }

    protected override void UpdateValueFromComponents()
    {
        var r = Math.Clamp(Red, 0.0f, 1.0f);
        var g = Math.Clamp(Green, 0.0f, 1.0f);
        var b = Math.Clamp(Blue, 0.0f, 1.0f);
        var a = Math.Clamp(Alpha, 0.0f, 1.0f);
        SetCurrentValue(ValueProperty, new Color4(r, g, b, a));
    }

    protected override void SetValueFromColor4(Color4 color4)
    {
        SetCurrentValue(ValueProperty, color4);
    }
}
