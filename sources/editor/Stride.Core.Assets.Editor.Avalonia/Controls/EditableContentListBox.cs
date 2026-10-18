// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Assets.Editor.Avalonia.Controls
{
    /// <summary>
    /// A <see cref="ListBox"/> control where items can be edited in place.
    /// </summary>
    public class EditableContentListBox : ListBox
    {
        private ScrollViewer? scrollViewer;

        /// <summary>
        /// Identifies the <see cref="CanEdit"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> CanEditProperty =
            AvaloniaProperty.Register<EditableContentListBox, bool>(nameof(CanEdit), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="EditItemTemplate"/> styled property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> EditItemTemplateProperty =
            AvaloniaProperty.Register<EditableContentListBox, IDataTemplate?>(nameof(EditItemTemplate));

        /// <summary>
        /// Gets the command that initiates in-place editing of the selected item.
        /// </summary>
        public static ICommand BeginEditCommand { get; private set; }

        /// <summary>
        /// Gets the command that commits in-place editing of the currently edited item.
        /// </summary>
        public static ICommand CommitEditCommand { get; private set; }

        /// <summary>
        /// Gets the command that cancels in-place editing of the currently edited item.
        /// </summary>
        public static ICommand CancelEditCommand { get; private set; }

        static EditableContentListBox()
        {
            BeginEditCommand = new SimpleRelayCommand(() => { });
            CommitEditCommand = new SimpleRelayCommand(() => { });
            CancelEditCommand = new SimpleRelayCommand(() => { });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditableContentListBox"/> class.
        /// </summary>
        public EditableContentListBox()
        {
            AttachedToVisualTree += OnAttachedToVisualTree;
        }

        /// <summary>
        /// Gets or sets whether items in this list box can be edited.
        /// </summary>
        public bool CanEdit
        {
            get => GetValue(CanEditProperty);
            set => SetValue(CanEditProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used when an item is in edit mode.
        /// </summary>
        public IDataTemplate? EditItemTemplate
        {
            get => GetValue(EditItemTemplateProperty);
            set => SetValue(EditItemTemplateProperty, value);
        }

        /// <summary>
        /// Initiates in-place editing on the currently selected item.
        /// </summary>
        public void BeginEdit()
        {
            if (!CanEdit)
                return;

            var selectedItem = SelectedItem;
            if (selectedItem == null)
                return;

            ScrollIntoView(selectedItem);

            var selectedContainer = ContainerFromItem(selectedItem) as EditableContentListBoxItem;
            if (selectedContainer != null && selectedContainer.CanEdit)
            {
                selectedContainer.IsEditing = true;
            }
        }

        /// <summary>
        /// Commits the in-place editing on the current item.
        /// </summary>
        public void CommitEdit()
        {
            var editingItem = GetCurrentEditingContainer();
            if (editingItem != null)
            {
                editingItem.CommitEdit();
            }
        }

        /// <summary>
        /// Cancels the in-place editing on the current item.
        /// </summary>
        public void CancelEdit()
        {
            var editingItem = GetCurrentEditingContainer();
            if (editingItem != null)
            {
                editingItem.CancelEdit();
            }
        }

        private EditableContentListBoxItem? GetCurrentEditingContainer()
        {
            foreach (var item in ItemsSource ?? Enumerable.Empty<object>())
            {
                if (ContainerFromItem(item) is EditableContentListBoxItem container && container.IsEditing)
                    return container;
            }
            return null;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            scrollViewer = this.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += ScrollViewerScrollChanged;
            }
        }

        /// <inheritdoc/>
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new EditableContentListBoxItem
            {
                EditItemTemplate = EditItemTemplate
            };
        }


        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.F2 && CanEdit)
            {
                BeginEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CommitEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelEdit();
                e.Handled = true;
            }
        }

        private void ScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.OffsetDelta.Y) < 1e-3 && Math.Abs(e.OffsetDelta.X) < 1e-3)
                return;

            var editingItem = GetCurrentEditingContainer();
            if (editingItem != null)
            {
                editingItem.CancelEdit();
            }
        }
    }

    /// <summary>
    /// Represents an item inside an <see cref="EditableContentListBox"/> supporting in-place editing.
    /// </summary>
    [PseudoClasses(":editing")]
    public class EditableContentListBoxItem : ListBoxItem
    {
        /// <summary>
        /// Identifies the <see cref="CanEdit"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> CanEditProperty =
            AvaloniaProperty.Register<EditableContentListBoxItem, bool>(nameof(CanEdit), defaultValue: true);

        /// <summary>
        /// Identifies the <see cref="IsEditing"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> IsEditingProperty =
            AvaloniaProperty.Register<EditableContentListBoxItem, bool>(nameof(IsEditing), defaultValue: false);

        /// <summary>
        /// Identifies the <see cref="EditItemTemplate"/> styled property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> EditItemTemplateProperty =
            AvaloniaProperty.Register<EditableContentListBoxItem, IDataTemplate?>(nameof(EditItemTemplate));

        static EditableContentListBoxItem()
        {
            IsEditingProperty.Changed.AddClassHandler<EditableContentListBoxItem>((x, e) => x.OnIsEditingChanged(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditableContentListBoxItem"/> class.
        /// </summary>
        public EditableContentListBoxItem()
        {
        }

        /// <summary>
        /// Gets or sets whether this item can be edited.
        /// </summary>
        public bool CanEdit
        {
            get => GetValue(CanEditProperty);
            set => SetValue(CanEditProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this item is currently in edit mode.
        /// </summary>
        public bool IsEditing
        {
            get => GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used for editing.
        /// </summary>
        public IDataTemplate? EditItemTemplate
        {
            get => GetValue(EditItemTemplateProperty);
            set => SetValue(EditItemTemplateProperty, value);
        }

        /// <summary>
        /// Begins editing this item.
        /// </summary>
        public void BeginEdit()
        {
            if (CanEdit)
            {
                IsEditing = true;
            }
        }

        /// <summary>
        /// Commits the in-place editing.
        /// </summary>
        public void CommitEdit()
        {
            IsEditing = false;
            Focus();
        }

        /// <summary>
        /// Cancels the in-place editing.
        /// </summary>
        public void CancelEdit()
        {
            IsEditing = false;
            Focus();
        }

        private void OnIsEditingChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var isEditing = (bool)(e.NewValue ?? false);
            PseudoClasses.Set(":editing", isEditing);
            if (!isEditing && (bool)(e.OldValue ?? false))
            {
                Focus();
            }
        }
    }

    internal sealed class SimpleRelayCommand : ICommand
    {
        private readonly Action execute;
        public SimpleRelayCommand(Action execute) => this.execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
