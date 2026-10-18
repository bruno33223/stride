// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class ListBoxDragDropBehavior : DragDropBehavior<ListBox, ListBoxItem>
    {
        public override IEnumerable<object> GetItemsToDrag(ListBoxItem container)
        {
            if (container != null && AssociatedObject != null)
            {
                var sourceItem = container.DataContext;
                return AssociatedObject.SelectedItems != null && AssociatedObject.SelectedItems.Contains(sourceItem)
                    ? AssociatedObject.SelectedItems.Cast<object>()
                    : sourceItem.ToEnumerable<object>();
            }
            return Enumerable.Empty<object>();
        }

        public override IInsertChildViewModel GetInsertTargetItem(ListBoxItem container, Point mousePosition, out InsertPosition insertPosition)
        {
            insertPosition = InsertPosition.Before;
            if (container == null)
                return null;

            if (mousePosition.Y >= 0 && mousePosition.Y <= InsertThreshold)
            {
                insertPosition = InsertPosition.Before;
                return container.DataContext as IInsertChildViewModel;
            }
            if (mousePosition.Y >= container.Bounds.Height - InsertThreshold && mousePosition.Y <= container.Bounds.Height)
            {
                insertPosition = InsertPosition.After;
                return container.DataContext as IInsertChildViewModel;
            }
            return null;
        }
    }
}
