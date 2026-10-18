// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class TreeViewStopEditOnLostFocusBehavior : Behavior<TreeViewItem>
    {
        public static readonly AttachedProperty<bool> IsEditingProperty =
            AvaloniaProperty.RegisterAttached<TreeViewStopEditOnLostFocusBehavior, TreeViewItem, bool>("IsEditing");

        public string EventName { get; set; } = "LostFocus";

        public TreeViewStopEditOnLostFocusBehavior()
        {
        }

        protected override void OnAttached()
        {
            if (AssociatedObject == null)
                throw new InvalidOperationException("This behavior must be attached to an instance of TreeViewItem.");

            base.OnAttached();
            AssociatedObject.LostFocus += OnLostFocus;
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.LostFocus -= OnLostFocus;
            }
            base.OnDetaching();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            OnEvent();
        }

        public virtual void OnEvent()
        {
            if (AssociatedObject != null)
            {
                if (AssociatedObject.DataContext is IIsEditableViewModel editable)
                {
                    editable.IsEditing = false;
                }
                AssociatedObject.SetValue(IsEditingProperty, false);
            }
        }
    }
}
