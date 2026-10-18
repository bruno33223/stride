// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Stride.Core.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Helper para gerenciar a expansão e o colapso recursivo de itens em árvores hierárquicas compostas de assets (Avalonia).
/// </summary>
public static class AssetCompositeHierarchyTreeViewHelper
{
    public static ICommand CollapseAllItems { get; } = new TreeViewActionCommand(CollapseAll);

    public static ICommand ExpandAllItems { get; } = new TreeViewActionCommand(ExpandAll);

    public static void CollapseAll([NotNull] TreeView treeView)
    {
        ArgumentNullException.ThrowIfNull(treeView);

        var count = treeView.ItemCount;
        for (var i = 0; i < count; i++)
        {
            var viewItem = treeView.ContainerFromIndex(i) as TreeViewItem;
            if (viewItem == null)
                continue;

            CollapseElementRecursively(viewItem);
        }

        // Reseta o scroll para o elemento do topo se disponível
        (treeView.ContainerFromIndex(0) as Control)?.BringIntoView();
    }

    public static void ExpandAll([NotNull] TreeView treeView)
    {
        ArgumentNullException.ThrowIfNull(treeView);

        var items = treeView.ItemsSource ?? treeView.Items;
        if (items != null)
        {
            foreach (var rawItem in items)
            {
                if (rawItem is AssetCompositeItemViewModel item)
                {
                    ExpandElementRecursively(treeView, item);
                }
                else if (rawItem is TreeViewItem treeViewItem)
                {
                    ExpandTreeViewItemRecursively(treeViewItem);
                }
            }
        }
    }

    public static void CollapseElementRecursively([NotNull] TreeViewItem viewItem)
    {
        ArgumentNullException.ThrowIfNull(viewItem);

        var count = viewItem.ItemCount;
        for (var i = 0; i < count; i++)
        {
            var child = viewItem.ContainerFromIndex(i) as TreeViewItem;
            if (child == null)
                continue;

            CollapseElementRecursively(child);
        }

        viewItem.IsExpanded = false;
    }

    public static void ExpandElementRecursively([NotNull] TreeView treeView, [NotNull] AssetCompositeItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(treeView);
        ArgumentNullException.ThrowIfNull(item);

        // Como a TreeView pode ter virtualização e muitos itens, despachamos a expansão recursiva entre frames
        Dispatcher.UIThread.Post(() =>
        {
            var viewItem = GetTreeViewItemFor(treeView, item);
            if (viewItem == null)
                return;

            viewItem.IsExpanded = true;
            foreach (var child in item.EnumerateChildren())
            {
                ExpandElementRecursively(treeView, child);
            }
        }, DispatcherPriority.Background);
    }

    public static void ExpandTreeViewItemRecursively([NotNull] TreeViewItem viewItem)
    {
        ArgumentNullException.ThrowIfNull(viewItem);
        viewItem.IsExpanded = true;

        var count = viewItem.ItemCount;
        for (var i = 0; i < count; i++)
        {
            if (viewItem.ContainerFromIndex(i) is TreeViewItem child)
            {
                ExpandTreeViewItemRecursively(child);
            }
        }

        foreach (var rawChild in viewItem.Items)
        {
            if (rawChild is TreeViewItem childItem)
            {
                ExpandTreeViewItemRecursively(childItem);
            }
        }
    }

    public static TreeViewItem? GetTreeViewItemFor([NotNull] TreeView treeView, [NotNull] object item)
    {
        ArgumentNullException.ThrowIfNull(treeView);
        ArgumentNullException.ThrowIfNull(item);

        return treeView.TreeContainerFromItem(item) as TreeViewItem
            ?? FindTreeViewItemRecursively(treeView, item);
    }

    private static TreeViewItem? FindTreeViewItemRecursively(ItemsControl control, object item)
    {
        var count = control.ItemCount;
        for (var i = 0; i < count; i++)
        {
            if (control.ContainerFromIndex(i) is TreeViewItem viewItem)
            {
                if (viewItem.DataContext == item || Equals(viewItem.Header, item))
                    return viewItem;

                var found = FindTreeViewItemRecursively(viewItem, item);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private sealed class TreeViewActionCommand : ICommand
    {
        private readonly Action<TreeView> action;

        public TreeViewActionCommand(Action<TreeView> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => parameter is TreeView;

        public void Execute(object? parameter)
        {
            if (parameter is TreeView treeView)
            {
                action(treeView);
            }
        }
    }
}
