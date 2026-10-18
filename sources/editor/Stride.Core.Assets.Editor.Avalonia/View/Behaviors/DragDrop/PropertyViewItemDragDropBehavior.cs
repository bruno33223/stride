// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.View.Behaviors;

public class PropertyViewItemDragDropBehavior : Behavior<Control>, IDragDropBehavior
{
    public static readonly StyledProperty<bool> CanDragProperty =
        AvaloniaProperty.Register<PropertyViewItemDragDropBehavior, bool>(nameof(CanDrag), true);

    public static readonly StyledProperty<bool> CanDropProperty =
        AvaloniaProperty.Register<PropertyViewItemDragDropBehavior, bool>(nameof(CanDrop), true);

    public static readonly StyledProperty<DisplayDropAdorner> DisplayDropAdornerProperty =
        AvaloniaProperty.Register<PropertyViewItemDragDropBehavior, DisplayDropAdorner>(nameof(DisplayDropAdorner), DisplayDropAdorner.Never);

    public bool CanDrag { get => GetValue(CanDragProperty); set => SetValue(CanDragProperty, value); }
    public bool CanDrop { get => GetValue(CanDropProperty); set => SetValue(CanDropProperty, value); }
    public DisplayDropAdorner DisplayDropAdorner { get => GetValue(DisplayDropAdornerProperty); set => SetValue(DisplayDropAdornerProperty, value); }
}
