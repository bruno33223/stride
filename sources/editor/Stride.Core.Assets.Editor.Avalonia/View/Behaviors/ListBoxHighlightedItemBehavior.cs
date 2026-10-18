// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.View.Behaviors;

public class ListBoxHighlightedItemBehavior : StyledElementBehavior<ListBox>
{
    /// <summary>
    /// Identifies the <see cref="IsEnabled"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> IsEnabledProperty =
        AvaloniaProperty.Register<ListBoxHighlightedItemBehavior, bool>(nameof(IsEnabled), true);

    /// <summary>
    /// Identifies the <see cref="HighlightedItem"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<object?> HighlightedItemProperty =
        AvaloniaProperty.Register<ListBoxHighlightedItemBehavior, object?>(nameof(HighlightedItem));

    /// <summary>
    /// Identifies the <see cref="UseSelectedItemIfAvailable"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<bool> UseSelectedItemIfAvailableProperty =
        AvaloniaProperty.Register<ListBoxHighlightedItemBehavior, bool>(nameof(UseSelectedItemIfAvailable));

    /// <summary>
    /// Identifies the <see cref="SelectHighlightedWhenEnteringControl"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<Control?> SelectHighlightedWhenEnteringControlProperty =
        AvaloniaProperty.Register<ListBoxHighlightedItemBehavior, Control?>(nameof(SelectHighlightedWhenEnteringControl));

    /// <summary>
    /// Identifies the <see cref="DelayToUpdate"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<double> DelayToUpdateProperty =
        AvaloniaProperty.Register<ListBoxHighlightedItemBehavior, double>(nameof(DelayToUpdate));

    private ListBoxItem? lastHoveredItem;
    private int lastHoveredItemId;

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public object? HighlightedItem
    {
        get => GetValue(HighlightedItemProperty);
        set => SetValue(HighlightedItemProperty, value);
    }

    public bool UseSelectedItemIfAvailable
    {
        get => GetValue(UseSelectedItemIfAvailableProperty);
        set => SetValue(UseSelectedItemIfAvailableProperty, value);
    }

    public Control? SelectHighlightedWhenEnteringControl
    {
        get => GetValue(SelectHighlightedWhenEnteringControlProperty);
        set => SetValue(SelectHighlightedWhenEnteringControlProperty, value);
    }

    public double DelayToUpdate
    {
        get => GetValue(DelayToUpdateProperty);
        set => SetValue(DelayToUpdateProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            AssociatedObject.PointerMoved += TimedUpdateHighlightedItem;
            AssociatedObject.SelectionChanged += UpdateHighlightedItem;
        }

        if (SelectHighlightedWhenEnteringControl is not null)
        {
            SelectHighlightedWhenEnteringControl.PointerEntered += OnPointerEnter;
            SelectHighlightedWhenEnteringControl.PointerExited += OnPointerLeave;
        }
    }

    protected override void OnDetaching()
    {
        if (SelectHighlightedWhenEnteringControl is not null)
        {
            SelectHighlightedWhenEnteringControl.PointerEntered -= OnPointerEnter;
            SelectHighlightedWhenEnteringControl.PointerExited -= OnPointerLeave;
        }

        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged -= UpdateHighlightedItem;
            AssociatedObject.PointerMoved -= TimedUpdateHighlightedItem;
        }

        base.OnDetaching();
    }

    private void UpdateHighlightedItem(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsEnabled || AssociatedObject is null)
            return;

        if (UseSelectedItemIfAvailable && AssociatedObject.SelectedItem is not null)
        {
            SetCurrentValue(HighlightedItemProperty, AssociatedObject.SelectedItem);
        }
    }

    private async void TimedUpdateHighlightedItem(object? sender, PointerEventArgs e)
    {
        if (!IsEnabled || AssociatedObject is null)
            return;

        var source = e.Source as Visual;
        var hoveredItem = source as ListBoxItem ?? source?.FindAncestorOfType<ListBoxItem>();
        UpdateLastHoveredItem(hoveredItem);
        var hoveredId = lastHoveredItemId;

        if (hoveredItem is null)
            return;

        if (DelayToUpdate <= 0)
        {
            SetCurrentValue(HighlightedItemProperty, hoveredItem.DataContext);
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(DelayToUpdate));

        if (hoveredId == lastHoveredItemId)
        {
            SetCurrentValue(HighlightedItemProperty, hoveredItem.DataContext);
        }
    }

    private void UpdateLastHoveredItem(ListBoxItem? hoveredItem)
    {
        if (!ReferenceEquals(hoveredItem, lastHoveredItem))
        {
            lastHoveredItem = hoveredItem;
            ++lastHoveredItemId;
        }
    }

    private void OnPointerEnter(object? sender, PointerEventArgs e)
    {
        if (!IsEnabled || AssociatedObject is null)
            return;

        AssociatedObject.SelectedItem = HighlightedItem;
        UpdateLastHoveredItem(null);
    }

    private void OnPointerLeave(object? sender, PointerEventArgs e)
    {
        if (!IsEnabled || AssociatedObject is null)
            return;

        AssociatedObject.SelectedItem = null;
        UpdateLastHoveredItem(null);
    }
}
