// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.UI;
using Stride.UI.Controls;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

public static class ContentControlViewModel
{
    public static bool CanSetContent(ContentControl control)
    {
        return control.Content == null;
    }

    public static void SetContent(
        INodeContainer nodeContainer,
        IUndoRedoService undoRedo,
        AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement> graph,
        UIElementDesign controlDesign,
        UIElementDesign childDesign)
    {
        if (controlDesign.UIElement is not ContentControl control)
            return;

        using var transaction = undoRedo.CreateTransaction();

        if (control.Content != null)
        {
            var oldPart = graph.Asset.Hierarchy.Parts.Values.FirstOrDefault(p => p.UIElement == control.Content);
            if (oldPart != null)
                graph.RemovePartFromAsset(oldPart);
        }

        var collection = new AssetPartCollection<UIElementDesign, UIElement> { childDesign };
        graph.AddPartToAsset(collection, childDesign, control, 0);

        undoRedo.SetName(transaction, $"Set content of {control.Name ?? control.GetType().Name}");
    }
}
