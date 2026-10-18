// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.UI;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

public abstract class UIRootViewModel : UIHierarchyItemViewModel
{
    private readonly IObjectNode? rootPartsNode;

    protected UIRootViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, IEnumerable<UIElementDesign>? rootElements)
        : base(editor, asset, rootElements)
    {
        var node = Editor.NodeContainer.GetNode(UIAsset.Hierarchy);
        if (node != null && node[nameof(AssetCompositeHierarchyData<UIElementDesign, UIElement>.RootParts)] != null)
        {
            rootPartsNode = node[nameof(AssetCompositeHierarchyData<UIElementDesign, UIElement>.RootParts)].Target;
            rootPartsNode.ItemChanged += RootUIElementsChanged;
        }
    }

    [NotNull]
    public IEnumerable<UIElementViewModel> RootElements => Children.OfType<UIElementViewModel>();

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(UIRootViewModel));
        if (rootPartsNode != null)
        {
            rootPartsNode.ItemChanged -= RootUIElementsChanged;
        }
        base.Destroy();
    }

    public override Task NotifyGameSidePartAdded()
    {
        foreach (var child in Children.OfType<UIHierarchyItemViewModel>())
        {
            child.NotifyGameSidePartAdded().Forget();
        }
        return base.NotifyGameSidePartAdded();
    }

    /// <inheritdoc/>
    public override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(Editor.NodeContainer.GetNode(Asset.Asset));
        path.PushMember(nameof(UIAsset.Hierarchy));
        path.PushTarget();
        return path;
    }

    public abstract void ReplaceRootElement([NotNull] UIElement sourcePanel, [NotNull] AssetCompositeHierarchyData<UIElementDesign, UIElement> hierarchy, Guid targetPanelId);

    /// <inheritdoc/>
    protected override string GetDropLocationName()
    {
        return Asset.Name;
    }

    protected virtual void OnRootUIElementsChanged([NotNull] ItemChangeEventArgs e)
    {
    }

    private void RootUIElementsChanged(object? sender, [NotNull] ItemChangeEventArgs e)
    {
        UIElement rootElement;
        switch (e.ChangeType)
        {
            case ContentChangeType.CollectionUpdate:
                break;
            case ContentChangeType.CollectionAdd:
                rootElement = (UIElement)e.NewValue;
                break;
            case ContentChangeType.CollectionRemove:
                rootElement = (UIElement)e.OldValue;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (e.ChangeType == ContentChangeType.CollectionAdd)
        {
            var element = (UIElement)e.NewValue;
            var childDesign = UIAsset.Hierarchy.Parts[element.Id];
            var viewModel = (UIElementViewModel)Editor.CreatePartViewModel(Asset, childDesign);
            InsertItem(e.Index.Int, viewModel);
        }
        else if (e.ChangeType == ContentChangeType.CollectionRemove)
        {
            var partId = new AbsoluteId(Asset.Id, ((UIElement)e.OldValue).Id);
            var element = (UIElementViewModel)Editor.FindPartViewModel(partId);
            if (element != null)
            {
                RemoveChildViewModel(element);
            }
        }

        OnRootUIElementsChanged(e);
    }
}
