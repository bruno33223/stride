// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.View.Behaviors;

public class TreeViewAutoExpandBehavior : StyledElementBehavior<TreeView>
{
    private readonly HashSet<string> expandedPropertyPaths = [];
    private readonly HashSet<string> collapsedPropertyPaths = [];

    protected override void OnAttached()
    {
        base.OnAttached();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
    }

    public void TrackExpansion(TreeViewItem item, bool isExpanded)
    {
        if (item.DataContext is not IChildViewModel viewModel)
            return;

        var propertyPath = GetPath(viewModel);
        if (string.IsNullOrEmpty(propertyPath))
            return;

        if (isExpanded)
        {
            expandedPropertyPaths.Add(propertyPath);
            collapsedPropertyPaths.Remove(propertyPath);
        }
        else
        {
            expandedPropertyPaths.Remove(propertyPath);
            collapsedPropertyPaths.Add(propertyPath);
        }
    }

    public bool IsPathExpanded(string propertyPath)
    {
        return expandedPropertyPaths.Contains(propertyPath);
    }

    public bool IsPathCollapsed(string propertyPath)
    {
        return collapsedPropertyPaths.Contains(propertyPath);
    }

    private static string GetPath(IChildViewModel viewModel)
    {
        return viewModel.GetParent() != null ? GetPath(viewModel.GetParent()) + "." + GetName(viewModel) : GetName(viewModel);
    }

    private static string GetName(IChildViewModel viewModel)
    {
        return viewModel.GetName().Replace('.', '$');
    }
}
