// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors;

public class TreeViewDragDropBehavior : Behavior<TreeView>, IDragDropBehavior
{
    public static readonly StyledProperty<bool> CanDragProperty =
        AvaloniaProperty.Register<TreeViewDragDropBehavior, bool>(nameof(CanDrag), true);

    public static readonly StyledProperty<bool> CanDropProperty =
        AvaloniaProperty.Register<TreeViewDragDropBehavior, bool>(nameof(CanDrop), true);

    public static readonly StyledProperty<DisplayDropAdorner> DisplayDropAdornerProperty =
        AvaloniaProperty.Register<TreeViewDragDropBehavior, DisplayDropAdorner>(nameof(DisplayDropAdorner), DisplayDropAdorner.Never);

    public static readonly StyledProperty<bool> AllowDropOnEmptyAreaProperty =
        AvaloniaProperty.Register<TreeViewDragDropBehavior, bool>(nameof(AllowDropOnEmptyArea), false);

    public bool CanDrag { get => GetValue(CanDragProperty); set => SetValue(CanDragProperty, value); }
    public bool CanDrop { get => GetValue(CanDropProperty); set => SetValue(CanDropProperty, value); }
    public DisplayDropAdorner DisplayDropAdorner { get => GetValue(DisplayDropAdornerProperty); set => SetValue(DisplayDropAdornerProperty, value); }
    public bool AllowDropOnEmptyArea { get => GetValue(AllowDropOnEmptyAreaProperty); set => SetValue(AllowDropOnEmptyAreaProperty, value); }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            DragDrop.SetAllowDrop(AssociatedObject, CanDrop);
        }
    }
}
