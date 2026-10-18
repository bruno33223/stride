// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.UI;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.UI;
using Stride.UI.Panels;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

public static class PanelViewModel
{
    public static void ChangeChildElementLayoutProperties(
        INodeContainer nodeContainer,
        IUndoRedoService undoRedo,
        UIElement parentPanel,
        UIElement child,
        PanelCommandMode mode)
    {
        if (parentPanel == null || child == null)
            return;

        using var transaction = undoRedo.CreateTransaction();

        if (parentPanel is Canvas)
        {
            var pinOrigin = GetDependencyPropertyValue(nodeContainer, child, Canvas.PinOriginPropertyKey);
            switch (mode)
            {
                case PanelCommandMode.PinTopLeft:
                    pinOrigin.X = 0.0f;
                    pinOrigin.Y = 0.0f;
                    break;
                case PanelCommandMode.PinTop:
                    pinOrigin.X = 0.5f;
                    pinOrigin.Y = 0.0f;
                    break;
                case PanelCommandMode.PinTopRight:
                    pinOrigin.X = 1.0f;
                    pinOrigin.Y = 0.0f;
                    break;
                case PanelCommandMode.PinLeft:
                    pinOrigin.X = 0.0f;
                    pinOrigin.Y = 0.5f;
                    break;
                case PanelCommandMode.PinCenter:
                    pinOrigin.X = 0.5f;
                    pinOrigin.Y = 0.5f;
                    break;
                case PanelCommandMode.PinRight:
                    pinOrigin.X = 1.0f;
                    pinOrigin.Y = 0.5f;
                    break;
                case PanelCommandMode.PinBottomLeft:
                    pinOrigin.X = 0.0f;
                    pinOrigin.Y = 1.0f;
                    break;
                case PanelCommandMode.PinBottom:
                    pinOrigin.X = 0.5f;
                    pinOrigin.Y = 1.0f;
                    break;
                case PanelCommandMode.PinBottomRight:
                    pinOrigin.X = 1.0f;
                    pinOrigin.Y = 1.0f;
                    break;
                case PanelCommandMode.PinFront:
                    pinOrigin.Z = 1.0f;
                    break;
                case PanelCommandMode.PinMiddle:
                    pinOrigin.Z = 0.5f;
                    break;
                case PanelCommandMode.PinBack:
                    pinOrigin.Z = 0.0f;
                    break;
                default:
                    return;
            }
            SetDependencyPropertyValue(nodeContainer, child, Canvas.PinOriginPropertyKey, pinOrigin);
            undoRedo.SetName(transaction, $"Change Pin Origin of {child.Name ?? child.GetType().Name}");
            return;
        }

        if (parentPanel is GridBase)
        {
            PropertyKey<int> propertyKey;
            int offset;
            switch (mode)
            {
                case PanelCommandMode.MoveUp:
                    propertyKey = GridBase.RowPropertyKey;
                    offset = -1;
                    break;
                case PanelCommandMode.MoveDown:
                    propertyKey = GridBase.RowPropertyKey;
                    offset = 1;
                    break;
                case PanelCommandMode.MoveLeft:
                    propertyKey = GridBase.ColumnPropertyKey;
                    offset = -1;
                    break;
                case PanelCommandMode.MoveRight:
                    propertyKey = GridBase.ColumnPropertyKey;
                    offset = 1;
                    break;
                case PanelCommandMode.MoveBack:
                    propertyKey = GridBase.LayerPropertyKey;
                    offset = -1;
                    break;
                case PanelCommandMode.MoveFront:
                    propertyKey = GridBase.LayerPropertyKey;
                    offset = 1;
                    break;
                default:
                    return;
            }

            var currentValue = GetDependencyPropertyValue(nodeContainer, child, propertyKey);
            var newValue = Math.Max(0, currentValue + offset);
            SetDependencyPropertyValue(nodeContainer, child, propertyKey, newValue);
            undoRedo.SetName(transaction, $"Move {child.Name ?? child.GetType().Name}");
            return;
        }

        if (parentPanel is StackPanel stackPanel)
        {
            var children = stackPanel.Children;
            var index = children.IndexOf(child);
            if (index == -1)
                return;

            int newIndex;
            switch (mode)
            {
                case PanelCommandMode.MoveDown:
                    newIndex = index + 1;
                    if (newIndex >= children.Count)
                        return;
                    break;
                case PanelCommandMode.MoveUp:
                    newIndex = index - 1;
                    if (newIndex < 0)
                        return;
                    break;
                default:
                    return;
            }

            var childrenNode = nodeContainer.GetOrCreateNode(stackPanel)[nameof(Panel.Children)].Target;
            childrenNode.Remove(child, new NodeIndex(index));
            childrenNode.Add(child, new NodeIndex(newIndex));
            undoRedo.SetName(transaction, $"Move {child.Name ?? child.GetType().Name}");
        }
    }

    public static void ChangeLayoutType(
        UIAssetBase asset,
        INodeContainer nodeContainer,
        IUndoRedoService undoRedo,
        AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement> graph,
        UIElementDesign sourcePanelDesign,
        Type targetPanelType)
    {
        if (sourcePanelDesign.UIElement is not Panel sourcePanel || targetPanelType == sourcePanel.GetType())
            return;

        if (!typeof(Panel).IsAssignableFrom(targetPanelType))
            return;

        using var transaction = undoRedo.CreateTransaction();

        Panel? targetPanel = null;
        var children = sourcePanel.Children.ToList();

        if (sourcePanel is StackPanel stackPanel && typeof(GridBase).IsAssignableFrom(targetPanelType))
        {
            var columns = 1;
            var rows = 1;
            var layers = 1;
            var count = children.Count;
            if (count > 0)
            {
                switch (stackPanel.Orientation)
                {
                    case Orientation.Horizontal:
                        columns = count;
                        break;
                    case Orientation.Vertical:
                        rows = count;
                        break;
                    case Orientation.InDepth:
                        layers = count;
                        break;
                }
            }

            if (typeof(Grid).IsAssignableFrom(targetPanelType))
            {
                targetPanel = CreateGrid(columns, rows, layers, StripType.Auto);
            }
            else if (typeof(UniformGrid).IsAssignableFrom(targetPanelType))
            {
                targetPanel = CreateUniformGrid(columns, rows, layers);
            }

            if (targetPanel != null)
            {
                PropertyKey<int> propertyKey = stackPanel.Orientation switch
                {
                    Orientation.Horizontal => GridBase.ColumnPropertyKey,
                    Orientation.InDepth => GridBase.LayerPropertyKey,
                    _ => GridBase.RowPropertyKey,
                };

                for (var i = 0; i < count; i++)
                {
                    SetDependencyPropertyValue(nodeContainer, children[i], propertyKey, i);
                }
            }
        }
        else if (sourcePanel is GridBase gridBase && typeof(StackPanel).IsAssignableFrom(targetPanelType))
        {
            var orientation = gridBase switch
            {
                Grid g => GetOrientation(g.ColumnDefinitions.Count, g.RowDefinitions.Count, g.LayerDefinitions.Count),
                UniformGrid ug => GetOrientation(ug.Columns, ug.Rows, ug.Layers),
                _ => Orientation.Vertical,
            };

            targetPanel = new StackPanel { Orientation = orientation };

            // Sort children
            children = children
                .OrderBy(e => e.DependencyProperties.Get(GridBase.RowPropertyKey))
                .ThenBy(e => e.DependencyProperties.Get(GridBase.ColumnPropertyKey))
                .ThenBy(e => e.DependencyProperties.Get(GridBase.LayerPropertyKey))
                .ToList();

            foreach (var child in children)
            {
                RemoveDependencyProperty(nodeContainer, child, GridBase.ColumnPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, GridBase.ColumnSpanPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, GridBase.RowPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, GridBase.RowSpanPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, GridBase.LayerPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, GridBase.LayerSpanPropertyKey);
            }
        }
        else if (sourcePanel is Grid grid && typeof(UniformGrid).IsAssignableFrom(targetPanelType))
        {
            targetPanel = CreateUniformGrid(grid.ColumnDefinitions.Count, grid.RowDefinitions.Count, grid.LayerDefinitions.Count);
        }
        else if (sourcePanel is UniformGrid uniformGrid && typeof(Grid).IsAssignableFrom(targetPanelType))
        {
            targetPanel = CreateGrid(uniformGrid.Columns, uniformGrid.Rows, uniformGrid.Layers, StripType.Star);
        }

        if (sourcePanel is Canvas)
        {
            foreach (var child in children)
            {
                RemoveDependencyProperty(nodeContainer, child, Canvas.AbsolutePositionPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, Canvas.PinOriginPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, Canvas.RelativePositionPropertyKey);
                RemoveDependencyProperty(nodeContainer, child, Canvas.RelativeSizePropertyKey);
            }
        }

        targetPanel ??= (Panel)Activator.CreateInstance(targetPanelType)!;
        targetPanel.Id = Guid.NewGuid();
        targetPanel.Name = sourcePanel.Name;

        CopyCommonProperties(nodeContainer, sourcePanel, targetPanel);

        var targetDesign = new UIElementDesign(targetPanel);
        var parentPart = asset.GetParent(sourcePanelDesign.UIElement);

        // Remove source panel
        graph.RemovePartFromAsset(sourcePanelDesign);

        // Add target panel in parent
        var newCollection = new AssetPartCollection<UIElementDesign, UIElement> { targetDesign };
        graph.AddPartToAsset(newCollection, targetDesign, parentPart, 0);

        // Move children to target panel
        foreach (var child in children)
        {
            if (asset.Hierarchy.Parts.TryGetValue(child.Id, out var childDesign))
            {
                graph.RemovePartFromAsset(childDesign);
                var childCollection = new AssetPartCollection<UIElementDesign, UIElement> { childDesign };
                graph.AddPartToAsset(childCollection, childDesign, targetPanel, targetPanel.Children.Count);
            }
        }

        undoRedo.SetName(transaction, $"Change layout type to {targetPanelType.Name}");
    }

    public static void Ungroup(
        UIAssetBase asset,
        IUndoRedoService undoRedo,
        AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement> graph,
        UIElementDesign panelDesign)
    {
        if (panelDesign.UIElement is not Panel panel)
            return;

        var parentElement = asset.GetParent(panel);
        if (parentElement == null)
            return;

        using var transaction = undoRedo.CreateTransaction();
        var children = panel.Children.ToList();

        // Remove panel
        graph.RemovePartFromAsset(panelDesign);

        // Insert children into parent element
        foreach (var child in children)
        {
            if (asset.Hierarchy.Parts.TryGetValue(child.Id, out var childDesign))
            {
                var collection = new AssetPartCollection<UIElementDesign, UIElement> { childDesign };
                graph.AddPartToAsset(collection, childDesign, parentElement, 0);
            }
        }

        undoRedo.SetName(transaction, $"Ungroup {children.Count} elements");
    }

    public static void GroupInto(
        UIAssetBase asset,
        IUndoRedoService undoRedo,
        AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement> graph,
        IReadOnlyList<UIElementDesign> selectedElements,
        Type targetPanelType)
    {
        if (selectedElements.Count == 0 || !typeof(Panel).IsAssignableFrom(targetPanelType))
            return;

        var parentElement = asset.GetParent(selectedElements[0].UIElement);

        using var transaction = undoRedo.CreateTransaction();

        var newPanel = (Panel)Activator.CreateInstance(targetPanelType)!;
        newPanel.Id = Guid.NewGuid();
        newPanel.Name = targetPanelType.Name;
        var newPanelDesign = new UIElementDesign(newPanel);

        // Add new panel in parent
        var panelCollection = new AssetPartCollection<UIElementDesign, UIElement> { newPanelDesign };
        graph.AddPartToAsset(panelCollection, newPanelDesign, parentElement, 0);

        // Move selected elements to new panel
        foreach (var elem in selectedElements)
        {
            graph.RemovePartFromAsset(elem);
            var itemCollection = new AssetPartCollection<UIElementDesign, UIElement> { elem };
            graph.AddPartToAsset(itemCollection, elem, newPanel, newPanel.Children.Count);
        }

        undoRedo.SetName(transaction, $"Group into {targetPanelType.Name}");
    }

    public static Grid CreateGrid(int columns, int rows, int layers, StripType stripType)
    {
        var grid = new Grid();
        for (var i = 0; i < Math.Max(1, columns); i++)
            grid.ColumnDefinitions.Add(new StripDefinition { Type = stripType });
        for (var i = 0; i < Math.Max(1, rows); i++)
            grid.RowDefinitions.Add(new StripDefinition { Type = stripType });
        for (var i = 0; i < Math.Max(1, layers); i++)
            grid.LayerDefinitions.Add(new StripDefinition { Type = stripType });
        return grid;
    }

    public static UniformGrid CreateUniformGrid(int columns, int rows, int layers)
    {
        return new UniformGrid
        {
            Columns = Math.Max(1, columns),
            Rows = Math.Max(1, rows),
            Layers = Math.Max(1, layers),
        };
    }

    public static Orientation GetOrientation(int columns, int rows, int layers)
    {
        if (columns > rows)
            return layers > columns ? Orientation.InDepth : Orientation.Horizontal;
        return layers > rows ? Orientation.InDepth : Orientation.Vertical;
    }

    public static void CopyCommonProperties(INodeContainer nodeContainer, Panel sourcePanel, Panel targetPanel)
    {
        var sourceNode = nodeContainer.GetOrCreateNode(sourcePanel);
        var targetNode = nodeContainer.GetOrCreateNode(targetPanel);

        foreach (var targetChild in targetNode.Members.Where(x =>
            x.Name != nameof(Panel.Children) &&
            x.Name != nameof(UIElement.Id) &&
            x.Name != nameof(UIElement.DependencyProperties)))
        {
            var sourceChild = sourceNode.TryGetChild(targetChild.Name);
            if (sourceChild != null)
            {
                var val = sourceChild.Retrieve();
                if (val != null)
                {
                    targetChild.Update(AssetCloner.Clone(val));
                }
            }
        }

        sourcePanel.DependencyProperties.CopyTo(targetPanel.DependencyProperties);
    }

    public static T GetDependencyPropertyValue<T>(INodeContainer nodeContainer, UIElement element, PropertyKey<T> property)
    {
        var dependencyPropertiesNode = nodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)];
        var result = dependencyPropertiesNode.Retrieve(new NodeIndex(property));
        return result is T tVal ? tVal : default!;
    }

    public static bool RemoveDependencyProperty(INodeContainer nodeContainer, UIElement element, PropertyKey property)
    {
        var dependencyPropertiesNode = nodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)].Target;
        var propertyIndex = new NodeIndex(property);
        if (!dependencyPropertiesNode.Indices.Contains(propertyIndex))
            return false;

        var value = dependencyPropertiesNode.Retrieve(propertyIndex);
        dependencyPropertiesNode.Remove(value, propertyIndex);
        return true;
    }

    public static void SetDependencyPropertyValue<T>(INodeContainer nodeContainer, UIElement element, PropertyKey<T> property, T value)
    {
        var dependencyPropertiesNode = nodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)].Target;
        var propertyIndex = new NodeIndex(property);
        if (!dependencyPropertiesNode.Indices.Contains(propertyIndex))
            dependencyPropertiesNode.Add(value, propertyIndex);
        else
            dependencyPropertiesNode.Update(value, propertyIndex);
    }
}
