// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart("PART_TextBox", typeof(TextBox), IsRequired = true)]
public sealed class NumericTextBox : TemplatedControl
{
    private bool internalValueSet;
    private bool isSyncingTextAndValueProperties;
    private bool isTextChangedFromUI;
    private IDisposable? textBoxTextChangedSubscription;

    private global::Avalonia.Point dragStartPoint;
    private double dragStartValue;
    private bool isPointerPressedForDrag;
    private bool isDragging;
    private IPointer? capturedPointer;

    public NumericTextBox()
    {
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPreviewPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPreviewPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerCaptureLostEvent, OnPreviewPointerCaptureLost, RoutingStrategies.Tunnel);
    }

    static NumericTextBox()
    {
        MaximumProperty.Changed.AddClassHandler<NumericTextBox>(OnMaximumPropertyChanged);
        MinimumProperty.Changed.AddClassHandler<NumericTextBox>(OnMinimumPropertyChanged);
        TextProperty.Changed.AddClassHandler<NumericTextBox, string?>(OnTextPropertyChanged);
        ValueProperty.Changed.AddClassHandler<NumericTextBox, double?>(OnValuePropertyChanged);
    }

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<NumericTextBox, int>(nameof(DecimalPlaces), -1);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(Maximum), double.MaxValue, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(Minimum), double.MinValue, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> SmallChangeProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(SmallChange), 1.0);

    public static readonly StyledProperty<bool> AllowMouseDragProperty =
        AvaloniaProperty.Register<NumericTextBox, bool>(nameof(AllowMouseDrag), true);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<NumericTextBox, string?>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBox.TextAlignmentProperty.AddOwner<NumericTextBox>();

    public static readonly StyledProperty<double?> ValueProperty =
        AvaloniaProperty.Register<NumericTextBox, double?>(nameof(Value), 0.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        TextBox.PlaceholderTextProperty.AddOwner<NumericTextBox>();

    public static readonly RoutedEvent<NumericTextBoxValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<NumericTextBox, NumericTextBoxValueChangedEventArgs>(nameof(ValueChanged), RoutingStrategies.Bubble);

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public bool AllowMouseDrag
    {
        get => GetValue(AllowMouseDragProperty);
        set => SetValue(AllowMouseDragProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public double? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public event EventHandler<NumericTextBoxValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>
    /// Gets the TextBox template part.
    /// </summary>
    private TextBox? TextBox { get; set; }

    protected override void OnInitialized()
    {
        if (!internalValueSet)
        {
            SyncTextAndValueProperties(false, null, true);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (TextBox is not null)
        {
            textBoxTextChangedSubscription?.Dispose();
        }
        TextBox = e.NameScope.Find<TextBox>("PART_TextBox");
        if (TextBox is not null)
        {
            textBoxTextChangedSubscription = TextBox.TextProperty.Changed.AddClassHandler<TextBox>((_, _) => TextBox_OnTextPropertyChanged());
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                var commitSuccess = CommitInput();
                e.Handled = !commitSuccess;
                break;
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        CommitInput(true);
        base.OnLostFocus(e);
    }

    private bool CommitInput(bool forceTextUpdate = false)
    {
        if (SyncTextAndValueProperties(true, Text, forceTextUpdate))
        {
            BindingOperations.GetBindingExpressionBase(this, ValueProperty)?.UpdateSource();
            return true;
        }

        return false;
    }

    private double? ConvertTextToValue(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var currentValueText = ConvertValueToText(Value);
        if (Equals(currentValueText, text))
            return Value;

        if (double.TryParse(text, NumberStyles.Any, NumberFormatInfo.CurrentInfo, out var result))
            return MathUtil.Clamp(result, Minimum, Maximum);

        throw new InvalidDataException("Input string was not in a correct format.");

    }

    private string ConvertValueToText(double? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var decimalPlaces = DecimalPlaces;
        var coercedValue = decimalPlaces < 0 ? value.Value : Math.Round(value.Value, decimalPlaces);
        return coercedValue.ToString(CultureInfo.InvariantCulture);
    }

    private void OnTextPropertyChanged()
    {
        if (!IsInitialized)
            return;

        SyncTextAndValueProperties(true, Text);
    }

    private void OnValuePropertyChanged(double? oldValue, double? newValue)
    {
        if (!internalValueSet && IsInitialized)
        {
            SyncTextAndValueProperties(false, null, true);
        }

        RaiseEvent(new NumericTextBoxValueChangedEventArgs(ValueChangedEvent, oldValue, newValue));
    }

    private bool SyncTextAndValueProperties(bool updateValueFromText, string? text, bool forceTextUpdate = false)
    {
        if (isSyncingTextAndValueProperties)
            return true;

        isSyncingTextAndValueProperties = true;
        var textIsValid = true;
        try
        {
            if (updateValueFromText)
            {
                try
                {
                    var newValue = ConvertTextToValue(text);
                    if (!Equals(newValue, Value))
                    {
                        SetValueInternal(newValue);
                    }
                }
                catch
                {
                    textIsValid = false;
                }
            }

            // Do not touch the ongoing text input from user.
            if (!isTextChangedFromUI)
            {
                if (forceTextUpdate)
                {
                    var newText = ConvertValueToText(Value);
                    if (!Equals(Text, newText))
                    {
                        SetCurrentValue(TextProperty, newText);
                    }
                }
            }
        }
        finally
        {
            isSyncingTextAndValueProperties = false;
        }
        return textIsValid;

        void SetValueInternal(double? value)
        {
            internalValueSet = true;
            try
            {
                SetCurrentValue(ValueProperty, value);
            }
            finally
            {
                internalValueSet = false;
            }
        }
    }

    private void TextBox_OnTextPropertyChanged()
    {
        try
        {
            isTextChangedFromUI = true;
            if (TextBox is not null)
            {
                SetCurrentValue(TextProperty, TextBox.Text);
            }
        }
        finally
        {
            isTextChangedFromUI = false;
        }
    }

    private static void OnMaximumPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs _)
    {
        var needValidation = false;
        if (sender.Minimum > sender.Maximum)
        {
            sender.SetCurrentValue(MinimumProperty, sender.Maximum);
            needValidation = true;
        }
        if (sender.Value > sender.Maximum)
        {
            sender.SetCurrentValue(ValueProperty, sender.Maximum);
            needValidation = true;
        }

        if (needValidation)
        {
            sender.SyncTextAndValueProperties(false, null, true);
        }
    }

    private static void OnMinimumPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs _)
    {
        var needValidation = false;
        if (sender.Maximum < sender.Minimum)
        {
            sender.SetCurrentValue(MaximumProperty, sender.Minimum);
            needValidation = true;
        }
        if (sender.Value < sender.Minimum)
        {
            sender.SetCurrentValue(ValueProperty, sender.Minimum);
            needValidation = true;
        }

        if (needValidation)
        {
            sender.SyncTextAndValueProperties(false, null, true);
        }
    }

    private static void OnTextPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs<string?> e)
    {
        sender.OnTextPropertyChanged();
    }

    private static void OnValuePropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs<double?> e)
    {
        sender.OnValuePropertyChanged(e.OldValue.GetValueOrDefault(), e.NewValue.GetValueOrDefault());
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!AllowMouseDrag || !IsEnabled)
            return;

        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed)
            return;

        isPointerPressedForDrag = true;
        isDragging = false;
        dragStartPoint = e.GetPosition(this);
        dragStartValue = Value ?? 0.0;
        capturedPointer = e.Pointer;
    }

    private void OnPreviewPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isPointerPressedForDrag)
            return;

        var currentPoint = e.GetPosition(this);
        var deltaX = currentPoint.X - dragStartPoint.X;

        if (!isDragging && Math.Abs(deltaX) >= 3)
        {
            isDragging = true;
            capturedPointer?.Capture(this);
            Cursor = new Cursor(StandardCursorType.SizeWestEast);
        }

        if (isDragging)
        {
            double sensitivity = IsSet(SmallChangeProperty)
                ? SmallChange
                : (DecimalPlaces >= 0 ? 0.01 : 1.0);

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                sensitivity *= 0.1;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                sensitivity *= 10.0;

            double deltaValue;
            if (DecimalPlaces < 0 && (!IsSet(SmallChangeProperty) || SmallChange == 1.0))
            {
                deltaValue = (deltaX / 4.0) * sensitivity;
            }
            else
            {
                deltaValue = deltaX * sensitivity;
            }

            var rawValue = dragStartValue + deltaValue;
            var clampedValue = MathUtil.Clamp(rawValue, Minimum, Maximum);
            if (DecimalPlaces >= 0)
            {
                clampedValue = Math.Round(clampedValue, DecimalPlaces);
            }
            else
            {
                clampedValue = Math.Round(clampedValue);
            }

            if (!Equals(Value, clampedValue))
            {
                SetCurrentValue(ValueProperty, clampedValue);
                SyncTextAndValueProperties(false, null, true);
            }

            e.Handled = true;
        }
    }

    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!isPointerPressedForDrag)
            return;

        isPointerPressedForDrag = false;

        if (isDragging)
        {
            isDragging = false;
            capturedPointer?.Capture(null);
            capturedPointer = null;
            Cursor = null;
            CommitInput(true);
            e.Handled = true;
        }
        else
        {
            capturedPointer = null;
        }
    }

    private void OnPreviewPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        isPointerPressedForDrag = false;
        if (isDragging)
        {
            isDragging = false;
            capturedPointer = null;
            Cursor = null;
            CommitInput(true);
        }
    }
}
