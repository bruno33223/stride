// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Assets.Editor.Avalonia.View.Behaviors;

public class TextBoxVectorPropertyValueValidationBehavior : StyledElementBehavior<VectorEditorBase>
{
    private readonly List<TextBox> attachedTextBoxes = [];

    /// <summary>
    /// Identifies the <see cref="AdornerStoryboard"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<object?> AdornerStoryboardProperty =
        AvaloniaProperty.Register<TextBoxVectorPropertyValueValidationBehavior, object?>(nameof(AdornerStoryboard));

    /// <summary>
    /// Identifies the <see cref="BorderBrush"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<TextBoxVectorPropertyValueValidationBehavior, IBrush?>(nameof(BorderBrush), Brushes.IndianRed);

    /// <summary>
    /// Identifies the <see cref="BorderCornerRadius"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<double> BorderCornerRadiusProperty =
        AvaloniaProperty.Register<TextBoxVectorPropertyValueValidationBehavior, double>(nameof(BorderCornerRadius), 3.0);

    /// <summary>
    /// Identifies the <see cref="BorderThickness"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<double> BorderThicknessProperty =
        AvaloniaProperty.Register<TextBoxVectorPropertyValueValidationBehavior, double>(nameof(BorderThickness), 2.0);

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
    /// Gets or sets the border corner radius when error highlight is active.
    /// </summary>
    public double BorderCornerRadius
    {
        get => GetValue(BorderCornerRadiusProperty);
        set => SetValue(BorderCornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the border thickness when error highlight is active.
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
            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
        }
    }

    private void OnAssociatedObjectLoaded(object? sender, RoutedEventArgs e)
    {
        if (AssociatedObject is null) return;

        attachedTextBoxes.Clear();
        var textBoxes = AssociatedObject.GetVisualDescendants().OfType<TextBox>().ToList();
        foreach (var tb in textBoxes)
        {
            attachedTextBoxes.Add(tb);
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
        }
        attachedTextBoxes.Clear();

        base.OnDetaching();
    }

    public void ShowValidationFailure(TextBox? textBox = null)
    {
        var targets = textBox != null ? [textBox] : attachedTextBoxes;
        foreach (var tb in targets)
        {
            tb.BorderBrush = BorderBrush;
            tb.BorderThickness = new Thickness(BorderThickness);
            tb.CornerRadius = new CornerRadius(BorderCornerRadius);
        }
    }

    public void ResetHighlight(TextBox? textBox = null)
    {
        var targets = textBox != null ? [textBox] : attachedTextBoxes;
        foreach (var tb in targets)
        {
            tb.ClearValue(TextBox.BorderBrushProperty);
            tb.ClearValue(TextBox.BorderThicknessProperty);
            tb.ClearValue(TextBox.CornerRadiusProperty);
        }
    }
}
