// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Avalonia.Extensions
{
    public static class ControlExtensions
    {
        public static void ExpandSessionObjects([NotNull] this TreeView treeView, List<SessionObjectViewModel> sessionObjects)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var shouldStop = true;
                    var treeViewItems = treeView.GetVisualDescendants().OfType<TreeViewItem>().ToList();

                    foreach (var sessionObject in sessionObjects.ToList())
                    {
                        var item = treeViewItems.FirstOrDefault(x => ReferenceEquals(x.DataContext, sessionObject));
                        if (item == null)
                            continue;

                        item.IsExpanded = true;
                        sessionObjects.Remove(sessionObject);
                        shouldStop = false;
                    }

                    if (!shouldStop && sessionObjects.Count > 0)
                    {
                        ExpandSessionObjects(treeView, sessionObjects);
                    }
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
            }, DispatcherPriority.Background);
        }

        [CanBeNull]
        public static string? GetSessionObjectPath([NotNull] this TreeView treeView, SessionObjectViewModel sessionObject)
        {
            var item = treeView.GetVisualDescendants().OfType<TreeViewItem>()
                .FirstOrDefault(x => ReferenceEquals(x.DataContext, sessionObject));

            if (item == null)
                return null;

            var path = "";
            var currentItem = item;
            while (currentItem != null)
            {
                if (currentItem.DataContext is not SessionObjectViewModel currentSessionObject)
                    return null;

                path = currentSessionObject.Name + '/' + path;
                currentItem = currentItem.FindAncestorOfType<TreeViewItem>();
            }
            return path;
        }
    }
}
