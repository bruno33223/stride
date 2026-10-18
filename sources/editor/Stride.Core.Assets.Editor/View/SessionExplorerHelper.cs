// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using DirectoryBaseViewModel = Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View;

/// <summary>
/// Abstraction for tree view controls used by <see cref="SessionExplorerHelper"/> across platforms.
/// </summary>
public interface ISessionExplorerTreeView
{
    IEnumerable Items { get; }
    void ExpandItem(object item);
    void CollapseItem(object item);
    void BringIntoView(object item);
}

/// <summary>
/// Helper class providing routed commands and operations for expanding and collapsing items in the Session Explorer.
/// </summary>
public static class SessionExplorerHelper
{
    static SessionExplorerHelper()
    {
        ExpandAssetFolders = new RoutedCommand(nameof(ExpandAssetFolders), typeof(SessionExplorerHelper), OnExpandAssetsFolders);
        ExpandAllFolders = new RoutedCommand(nameof(ExpandAllFolders), typeof(SessionExplorerHelper), OnExpandAllFolders);
        CollapseAllFolders = new RoutedCommand(nameof(CollapseAllFolders), typeof(SessionExplorerHelper), OnCollapseAllFolders);
    }

    public static RoutedCommand ExpandAssetFolders { get; }

    public static RoutedCommand ExpandAllFolders { get; }

    public static RoutedCommand CollapseAllFolders { get; }

    public static Action<object?, bool>? ExpandFoldersAction { get; set; }

    public static Action<object?>? CollapseFoldersAction { get; set; }

    private static void OnExpandAssetsFolders(object? sender)
    {
        if (ExpandFoldersAction != null)
        {
            ExpandFoldersAction(sender, true);
            return;
        }

        if (sender is ISessionExplorerTreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                if (item is PackageCategoryViewModel category)
                {
                    ExpandPackageCategory(treeView, category, true);
                }
            }
        }
        else if (sender is IEnumerable<PackageCategoryViewModel> categories)
        {
            foreach (var category in categories)
            {
                ExpandPackageCategory(null, category, true);
            }
        }
        else if (sender is SessionViewModel session)
        {
            foreach (var package in session.LocalPackages)
            {
                ExpandPackage(null, package, true);
            }
        }
    }

    private static void OnExpandAllFolders(object? sender)
    {
        if (ExpandFoldersAction != null)
        {
            ExpandFoldersAction(sender, false);
            return;
        }

        if (sender is ISessionExplorerTreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                if (item is PackageCategoryViewModel category)
                {
                    ExpandPackageCategory(treeView, category, false);
                }
            }
        }
        else if (sender is IEnumerable<PackageCategoryViewModel> categories)
        {
            foreach (var category in categories)
            {
                ExpandPackageCategory(null, category, false);
            }
        }
        else if (sender is SessionViewModel session)
        {
            foreach (var package in session.LocalPackages)
            {
                ExpandPackage(null, package, false);
            }
        }
    }

    private static void OnCollapseAllFolders(object? sender)
    {
        if (CollapseFoldersAction != null)
        {
            CollapseFoldersAction(sender);
            return;
        }

        if (sender is ISessionExplorerTreeView treeView)
        {
            foreach (var item in treeView.Items)
            {
                CollapseRecursively(treeView, item);
            }
        }
    }

    public static void CollapseRecursively(ISessionExplorerTreeView? treeView, [NotNull] object item)
    {
        if (treeView != null)
        {
            treeView.CollapseItem(item);
        }
    }

    public static void ExpandPackageCategory(ISessionExplorerTreeView? treeView, PackageCategoryViewModel category, bool assetsOnly)
    {
        treeView?.ExpandItem(category);

        foreach (var package in category.Content)
        {
            ExpandPackage(treeView, package, assetsOnly);
        }
    }

    public static void ExpandPackage(ISessionExplorerTreeView? treeView, PackageViewModel package, bool assetsOnly)
    {
        treeView?.ExpandItem(package);

        if (assetsOnly)
        {
            if (package.AssetMountPoint != null)
            {
                ExpandDirectories(treeView, package.AssetMountPoint);
            }
        }
        else
        {
            foreach (var content in package.Content)
            {
                ExpandViewModel(treeView, content);
            }
        }
    }

    public static void ExpandDirectories(ISessionExplorerTreeView? treeView, Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel directory)
    {
        treeView?.ExpandItem(directory);

        foreach (var subDirectory in directory.SubDirectories)
        {
            ExpandDirectories(treeView, subDirectory);
        }
    }

    public static void ExpandViewModel(ISessionExplorerTreeView? treeView, ViewModelBase viewModel)
    {
        if (viewModel is DirectoryBaseViewModel directory)
        {
            ExpandDirectories(treeView, directory);
        }
        else
        {
            treeView?.ExpandItem(viewModel);
        }
    }
}
