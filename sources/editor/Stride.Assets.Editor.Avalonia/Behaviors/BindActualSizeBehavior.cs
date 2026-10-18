// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

public class BindActualSizeBehavior : Behavior<Control>
{
    public static readonly StyledProperty<double> ActualWidthProperty =
        AvaloniaProperty.Register<BindActualSizeBehavior, double>(nameof(ActualWidth));

    public static readonly StyledProperty<double> ActualHeightProperty =
        AvaloniaProperty.Register<BindActualSizeBehavior, double>(nameof(ActualHeight));

    public double ActualWidth
    {
        get => GetValue(ActualWidthProperty);
        set => SetValue(ActualWidthProperty, value);
    }

    public double ActualHeight
    {
        get => GetValue(ActualHeightProperty);
        set => SetValue(ActualHeightProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.PropertyChanged += OnPropertyChanged;
            SetCurrentValue(ActualWidthProperty, AssociatedObject.Bounds.Width);
            SetCurrentValue(ActualHeightProperty, AssociatedObject.Bounds.Height);
        }
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.BoundsProperty && e.NewValue is Rect bounds)
        {
            SetCurrentValue(ActualWidthProperty, bounds.Width);
            SetCurrentValue(ActualHeightProperty, bounds.Height);
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.PropertyChanged -= OnPropertyChanged;
        }
        base.OnDetaching();
    }
}
