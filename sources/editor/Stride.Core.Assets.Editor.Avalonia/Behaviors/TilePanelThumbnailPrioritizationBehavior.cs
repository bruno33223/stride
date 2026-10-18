// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using ISessionObjectViewModel = Stride.Core.Assets.Editor.ViewModels.ISessionObjectViewModel;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// A behavior and attached property for <see cref="ItemsControl"/> that automatically increases the thumbnail
/// compilation priority of items currently visible in the viewport.
/// </summary>
public class TilePanelThumbnailPrioritizationBehavior : Behavior<ItemsControl>
{
    private int previousFirstVisibleItemIndex = -1;
    private int previousLastVisibleItemIndex = -1;
    private ScrollViewer? scrollViewer;

    /// <summary>
    /// Identifies the <see cref="IsEnabledProperty"/> attached property.
    /// </summary>
    public new static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TilePanelThumbnailPrioritizationBehavior, ItemsControl, bool>("IsEnabled", defaultValue: false);

    static TilePanelThumbnailPrioritizationBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<ItemsControl, bool>(OnIsEnabledChanged);
    }

    /// <summary>
    /// Gets the value of the <see cref="IsEnabledProperty"/> attached property for a given <see cref="ItemsControl"/>.
    /// </summary>
    public static bool GetIsEnabled(ItemsControl control) => control.GetValue(IsEnabledProperty);

    /// <summary>
    /// Sets the value of the <see cref="IsEnabledProperty"/> attached property for a given <see cref="ItemsControl"/>.
    /// </summary>
    public static void SetIsEnabled(ItemsControl control, bool value) => control.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(ItemsControl itemsControl, AvaloniaPropertyChangedEventArgs<bool> e)
    {
        var behaviors = Interaction.GetBehaviors(itemsControl);
        var existing = behaviors.OfType<TilePanelThumbnailPrioritizationBehavior>().FirstOrDefault();

        if (e.NewValue.GetValueOrDefault())
        {
            if (existing == null)
            {
                behaviors.Add(new TilePanelThumbnailPrioritizationBehavior());
            }
        }
        else if (existing != null)
        {
            behaviors.Remove(existing);
        }
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded += OnAssociatedObjectLoaded;
            if (AssociatedObject.IsLoaded)
            {
                HookScrollViewer();
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        UnhookScrollViewer();
        if (AssociatedObject != null)
        {
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
        }

        base.OnDetaching();
    }

    private void OnAssociatedObjectLoaded(object? sender, RoutedEventArgs e)
    {
        HookScrollViewer();
        UpdateVisibility();
    }

    private void HookScrollViewer()
    {
        if (AssociatedObject == null)
            return;

        UnhookScrollViewer();

        scrollViewer = AssociatedObject.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }

    private void UnhookScrollViewer()
    {
        if (scrollViewer != null)
        {
            scrollViewer.ScrollChanged -= OnScrollChanged;
            scrollViewer = null;
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateVisibility();
    }

    /// <summary>
    /// Calculates the visible index range and prioritizes thumbnails of visible assets.
    /// </summary>
    public void UpdateVisibility()
    {
        if (AssociatedObject?.ItemsSource == null)
            return;

        var items = AssociatedObject.ItemsSource.Cast<object>().ToList();
        if (items.Count == 0)
            return;

        // If no scrollViewer, prioritize all visible items by default
        if (scrollViewer == null)
        {
            PrioritizeVisibleThumbnails(0, items.Count - 1);
            return;
        }

        var extentHeight = scrollViewer.Extent.Height;
        var viewportHeight = scrollViewer.Viewport.Height;
        var offsetY = scrollViewer.Offset.Y;

        if (extentHeight <= 0 || viewportHeight <= 0)
        {
            PrioritizeVisibleThumbnails(0, Math.Min(items.Count - 1, 30));
            return;
        }

        double itemHeight = extentHeight / items.Count;
        if (itemHeight <= 0)
            itemHeight = 1;

        int firstIndex = (int)(offsetY / itemHeight);
        int lastIndex = (int)((offsetY + viewportHeight) / itemHeight);

        PrioritizeVisibleThumbnails(firstIndex, lastIndex);
    }

    /// <summary>
    /// Prioritizes thumbnail generation for the items in the specified visibility index range.
    /// </summary>
    /// <param name="firstVisibleItemIndex">The zero-based index of the first visible item.</param>
    /// <param name="lastVisibleItemIndex">The zero-based index of the last visible item.</param>
    public void PrioritizeVisibleThumbnails(int firstVisibleItemIndex, int lastVisibleItemIndex)
    {
        if (previousFirstVisibleItemIndex == firstVisibleItemIndex && previousLastVisibleItemIndex == lastVisibleItemIndex)
            return;

        previousFirstVisibleItemIndex = firstVisibleItemIndex;
        previousLastVisibleItemIndex = lastVisibleItemIndex;

        if (AssociatedObject?.ItemsSource == null)
            return;

        var items = AssociatedObject.ItemsSource.Cast<object>().ToList();
        if (items.Count == 0 || firstVisibleItemIndex > lastVisibleItemIndex)
            return;

        int clampedFirst = Math.Max(0, firstVisibleItemIndex);
        int clampedLast = Math.Min(items.Count - 1, lastVisibleItemIndex);
        if (clampedFirst > clampedLast)
            return;

        var sessionObj = items.OfType<ISessionObjectViewModel>().FirstOrDefault();
        if (sessionObj?.Session == null)
            return;

        var count = clampedLast - clampedFirst + 1;
        var visibleAssets = items.GetRange(clampedFirst, count).OfType<AssetViewModel>().ToList();
        if (visibleAssets.Count == 0)
            return;

        if (sessionObj.Session is SessionViewModel sessionVm)
        {
            sessionVm.Thumbnails?.IncreaseThumbnailPriority(visibleAssets);
        }
        else
        {
            var thumbnailService = sessionObj.Session.ServiceProvider?.TryGet<IThumbnailService>();
            thumbnailService?.IncreaseThumbnailPriority(visibleAssets.Select(x => x.AssetItem));
        }
    }
}
