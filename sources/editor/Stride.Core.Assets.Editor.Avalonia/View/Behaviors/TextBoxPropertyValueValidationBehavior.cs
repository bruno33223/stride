// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.View.Behaviors;

public class TextBoxPropertyValueValidationBehavior : StyledElementBehavior<TextBox>
{
    private IBrush? originalBorderBrush;
    private Thickness originalBorderThickness;
    private CornerRadius originalCornerRadius;
    private bool isBorderOverridden;

    /// <summary>
    /// Identifies the <see cref="AdornerStoryboard"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<object?> AdornerStoryboardProperty =
        AvaloniaProperty.Register<TextBoxPropertyValueValidationBehavior, object?>(nameof(AdornerStoryboard));

    /// <summary>
    /// Identifies the <see cref="BorderBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<TextBoxPropertyValueValidationBehavior, IBrush?>(nameof(BorderBrush), Brushes.IndianRed);

    /// <summary>
    /// Identifies the <see cref="BorderCornerRadius"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<double> BorderCornerRadiusProperty =
        AvaloniaProperty.Register<TextBoxPropertyValueValidationBehavior, double>(nameof(BorderCornerRadius), 3.0);

    /// <summary>
    /// Identifies the <see cref="BorderThickness"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<TextBoxPropertyValueValidationBehavior, double>(nameof(BorderThickness), 2.0);

    /// <summary>
    /// Gets or sets the storyboard or visual effect object associated to this behavior.
    /// </summary>
    public object? AdornerStoryboard
    {
        get => GetValue(AdornerStoryboardProperty);
        set => SetValue(AdornerStoryboardProperty, value);
    }

    /// <summary>
    /// Gets or sets the border brush when error highlight is active.
    /// </summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>
    /// Gets or sets the border corner radius when the error highlight is active.
    /// </summary>
    public double BorderCornerRadius
    {
        get => GetValue(BorderCornerRadiusProperty);
        set => SetValue(BorderCornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the border thickness when the error highlight is active.
    /// </summary>
    public double BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            originalBorderBrush = AssociatedObject.BorderBrush;
            originalBorderThickness = AssociatedObject.BorderThickness;
            originalCornerRadius = AssociatedObject.CornerRadius;

            AssociatedObject.LostFocus += OnLostFocus;
            AssociatedObject.GotFocus += OnGotFocus;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.LostFocus -= OnLostFocus;
            AssociatedObject.GotFocus -= OnGotFocus;
            ResetHighlight();
        }

        base.OnDetaching();
    }

    public void ShowValidationFailure()
    {
        if (AssociatedObject is not null && !isBorderOverridden)
        {
            originalBorderBrush = AssociatedObject.BorderBrush;
            originalBorderThickness = AssociatedObject.BorderThickness;
            originalCornerRadius = AssociatedObject.CornerRadius;

            AssociatedObject.BorderBrush = BorderBrush;
            AssociatedObject.BorderThickness = new Thickness(BorderThickness);
            AssociatedObject.CornerRadius = new CornerRadius(BorderCornerRadius);
            isBorderOverridden = true;
        }
    }

    public void ResetHighlight()
    {
        if (AssociatedObject is not null && isBorderOverridden)
        {
            AssociatedObject.BorderBrush = originalBorderBrush;
            AssociatedObject.BorderThickness = originalBorderThickness;
            AssociatedObject.CornerRadius = originalCornerRadius;
            isBorderOverridden = false;
        }
    }

    private void OnGotFocus(object? sender, RoutedEventArgs e)
    {
        ResetHighlight();
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        // Validation occurs when input is committed
    }
}
