// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public class TreeViewBindableSelectedItemsBehavior : Behavior<TreeView>
    {
        private bool updatingCollection;

        public static readonly StyledProperty<IObservableList<object>?> SelectedItemsProperty =
            AvaloniaProperty.Register<TreeViewBindableSelectedItemsBehavior, IObservableList<object>?>(nameof(SelectedItems));

        public static readonly StyledProperty<bool> GiveFocusOnSelectionChangeProperty =
            AvaloniaProperty.Register<TreeViewBindableSelectedItemsBehavior, bool>(nameof(GiveFocusOnSelectionChange), defaultValue: true);

        static TreeViewBindableSelectedItemsBehavior()
        {
            SelectedItemsProperty.Changed.AddClassHandler<TreeViewBindableSelectedItemsBehavior, IObservableList<object>?>((b, e) =>
                b.OnSelectedItemsPropertyChanged(e.OldValue.GetValueOrDefault(), e.NewValue.GetValueOrDefault()));
        }

        public IObservableList<object>? SelectedItems
        {
            get => GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public bool GiveFocusOnSelectionChange
        {
            get => GetValue(GiveFocusOnSelectionChangeProperty);
            set => SetValue(GiveFocusOnSelectionChangeProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged += OnTreeViewSelectionChanged;
                if (AssociatedObject.SelectedItems is INotifyCollectionChanged notifyCol)
                {
                    notifyCol.CollectionChanged += OnAssociatedSelectedItemsCollectionChanged;
                }
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectionChanged -= OnTreeViewSelectionChanged;
                if (AssociatedObject.SelectedItems is INotifyCollectionChanged notifyCol)
                {
                    notifyCol.CollectionChanged -= OnAssociatedSelectedItemsCollectionChanged;
                }
            }
            if (SelectedItems != null)
            {
                SelectedItems.CollectionChanged -= OnViewModelCollectionChanged;
            }
            base.OnDetaching();
        }

        public void ScrollIntoView(object dataItem)
        {
            // Tenta rolar o item no TreeView
            AssociatedObject?.ScrollIntoView(dataItem);
        }

        private void OnSelectedItemsPropertyChanged(IObservableList<object>? oldList, IObservableList<object>? newList)
        {
            if (oldList != null)
                oldList.CollectionChanged -= OnViewModelCollectionChanged;

            if (newList != null)
            {
                newList.CollectionChanged += OnViewModelCollectionChanged;
                SyncControlFromViewModel();
            }
        }

        private void OnViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SyncControlFromViewModel();
        }

        private void OnTreeViewSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SyncViewModelFromControl(e.AddedItems, e.RemovedItems);
        }

        private void OnAssociatedSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SyncViewModelFromControl(e.NewItems, e.OldItems);
        }

        private void SyncControlFromViewModel()
        {
            if (updatingCollection || AssociatedObject == null || SelectedItems == null)
                return;

            try
            {
                updatingCollection = true;
                if (AssociatedObject.SelectedItems is IList ctrlList)
                {
                    var toRemove = ctrlList.Cast<object>().Where(x => !SelectedItems.Contains(x)).ToList();
                    foreach (var item in toRemove)
                        ctrlList.Remove(item);

                    foreach (var item in SelectedItems)
                    {
                        if (!ctrlList.Contains(item))
                            ctrlList.Add(item);
                    }
                }

                if (SelectedItems.Count > 0 && GiveFocusOnSelectionChange)
                {
                    AssociatedObject.Focus();
                }
            }
            finally
            {
                updatingCollection = false;
            }
        }

        private void SyncViewModelFromControl(IList? added, IList? removed)
        {
            if (updatingCollection || SelectedItems == null)
                return;

            try
            {
                updatingCollection = true;
                if (removed != null)
                {
                    foreach (var item in removed.Cast<object>())
                        SelectedItems.Remove(item);
                }
                if (added != null)
                {
                    foreach (var item in added.Cast<object>())
                    {
                        if (!SelectedItems.Contains(item))
                            SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                updatingCollection = false;
            }
        }
    }
}
