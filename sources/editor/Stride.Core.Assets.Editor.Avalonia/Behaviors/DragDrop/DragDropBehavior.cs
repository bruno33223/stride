// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public abstract class DragDropBehavior<TControl, TContainer> : Behavior<TControl>, IDragDropBehavior
        where TControl : Control
        where TContainer : Control
    {
        public const double InsertThreshold = 4.0;

        public static readonly StyledProperty<bool> CanDragProperty =
            AvaloniaProperty.Register<DragDropBehavior<TControl, TContainer>, bool>(nameof(CanDrag), true);

        public static readonly StyledProperty<bool> CanDropProperty =
            AvaloniaProperty.Register<DragDropBehavior<TControl, TContainer>, bool>(nameof(CanDrop), true);

        public static readonly StyledProperty<bool> CanInsertProperty =
            AvaloniaProperty.Register<DragDropBehavior<TControl, TContainer>, bool>(nameof(CanInsert), false);

        public static readonly StyledProperty<DisplayDropAdorner> DisplayDropAdornerProperty =
            AvaloniaProperty.Register<DragDropBehavior<TControl, TContainer>, DisplayDropAdorner>(nameof(DisplayDropAdorner), DisplayDropAdorner.Never);

        public static readonly StyledProperty<bool> DisplayInsertAdornerProperty =
            AvaloniaProperty.Register<DragDropBehavior<TControl, TContainer>, bool>(nameof(DisplayInsertAdorner), false);

        public bool CanDrag { get => GetValue(CanDragProperty); set => SetValue(CanDragProperty, value); }

        public bool CanDrop { get => GetValue(CanDropProperty); set => SetValue(CanDropProperty, value); }

        public bool CanInsert { get => GetValue(CanInsertProperty); set => SetValue(CanInsertProperty, value); }

        public DisplayDropAdorner DisplayDropAdorner { get => GetValue(DisplayDropAdornerProperty); set => SetValue(DisplayDropAdornerProperty, value); }

        public bool DisplayInsertAdorner { get => GetValue(DisplayInsertAdornerProperty); set => SetValue(DisplayInsertAdornerProperty, value); }

        public virtual IEnumerable<object> GetItemsToDrag(TContainer container)
        {
            return Enumerable.Empty<object>();
        }

        public virtual IInsertChildViewModel? GetInsertTargetItem(TContainer container, Point mousePosition, out InsertPosition insertPosition)
        {
            insertPosition = InsertPosition.Before;
            return null;
        }

        public virtual IAddChildViewModel? GetDropTargetItem(TContainer container)
        {
            return null;
        }

        public virtual TContainer? GetContainer(object source)
        {
            return AssociatedObject as TContainer;
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                DragDrop.SetAllowDrop(AssociatedObject, CanDrop || CanInsert);
            }
        }
    }
}
