// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Avalonia.Controls;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public class BringSelectionToViewBehavior : Behavior<EditableContentListBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged += OnSelectionChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged -= OnSelectionChanged;
            }
            base.OnDetaching();
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (AssociatedObject != null && AssociatedObject.SelectedIndex >= 0)
            {
                AssociatedObject.ScrollIntoView(AssociatedObject.SelectedIndex);
            }
        }
    }
}
