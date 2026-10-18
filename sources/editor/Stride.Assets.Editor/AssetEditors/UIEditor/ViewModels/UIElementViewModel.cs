// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Assets.Editor.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.UI;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

[DebuggerDisplay("{Name} [{ElementType}]")]
public class UIElementViewModel : UIHierarchyItemViewModel, IEditorDesignPartViewModel<UIElementDesign, UIElement>, IIsEditableViewModel, IAssetPropertyProviderViewModel
{
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;
    private UILibraryViewModel? sourceLibrary;
    private bool isEditing;

    public UIElementViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign)
        : this(editor, asset, elementDesign, null)
    {
    }

    protected UIElementViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UIBaseViewModel asset, [NotNull] UIElementDesign elementDesign, [CanBeNull] IEnumerable<UIElementDesign>? children)
        : base(editor, asset, children)
    {
        AssetSideUIElement = elementDesign.UIElement;
        ElementType = AssetSideUIElement.GetType();
        UIElementDesign = elementDesign;
        var assetNode = editor.NodeContainer.GetOrCreateNode(elementDesign.UIElement);
        nameNodeBinding = new MemberGraphNodeBinding<string>(assetNode[nameof(UIElement.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService);

        PanelCommand = new AnonymousCommand<PanelCommandMode>(ServiceProvider, PanelCommandImpl);
        RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);

        UpdateSourceLibrary();
        var basePrefabNode = Editor.NodeContainer.GetNode(UIElementDesign)[nameof(UIElementDesign.Base)];
        if (basePrefabNode != null)
        {
            basePrefabNode.ValueChanged += BaseElementChanged;
        }
    }

    public UIElement AssetSideUIElement { get; }

    public Type ElementType { get; }

    public AbsoluteId Id => new AbsoluteId(Asset.Id, AssetSideUIElement.Id);

    public bool IsEditable => true;

    public bool IsEditing { get => isEditing; set => SetValue(ref isEditing, value); }

    /// <inheritdoc/>
    public override string? Name { get => nameNodeBinding.Value; set => nameNodeBinding.Value = value ?? string.Empty; }

    public UILibraryViewModel? SourceLibrary { get => sourceLibrary; private set => SetValue(ref sourceLibrary, value); }

    public ICommandBase PanelCommand { get; }

    public ICommandBase RenameCommand { get; }

    internal UIElementDesign UIElementDesign { get; }

    public UIElementDesign PartDesign => UIElementDesign;

    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

    public bool CanDuplicate()
    {
        return Parent != null;
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        nameNodeBinding.Dispose();
        var basePrefabNode = Editor.NodeContainer.GetNode(UIElementDesign)?[nameof(UIElementDesign.Base)];
        if (basePrefabNode != null)
        {
            basePrefabNode.ValueChanged -= BaseElementChanged;
        }
        base.Destroy();
    }

    [NotNull]
    public UIElementViewModel Duplicate()
    {
        var parentItem = Parent as UIHierarchyItemViewModel;
        if (parentItem == null) throw new InvalidOperationException("This operation can only be executed on a child element.");

        var subTreeRoot = AssetSideUIElement.Id;
        var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
        var clonedHierarchy = UIAssetPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, subTreeRoot.Yield(), flags, out _);
        AssetPartsAnalysis.GenerateNewBaseInstanceIds(clonedHierarchy);

        var addedRoot = clonedHierarchy.Parts[clonedHierarchy.RootParts.Single().Id];
        if (Asset is UILibraryViewModel && !string.IsNullOrEmpty(addedRoot.UIElement.Name))
            addedRoot.UIElement.Name = NamingHelper.ComputeNewName(addedRoot.UIElement.Name, parentItem.Children, x => x.Name ?? string.Empty);
        Asset.AssetHierarchyPropertyGraph.AddPartToAsset(clonedHierarchy.Parts, addedRoot, (parentItem as UIElementViewModel)?.AssetSideUIElement, parentItem.Children.IndexOf(this) + 1);
        var cloneId = addedRoot.UIElement.Id;

        var partId = new AbsoluteId(Asset.Id, cloneId);
        var viewModel = (UIElementViewModel)Editor.FindPartViewModel(partId);
        if (viewModel == null) throw new InvalidOperationException($"{nameof(viewModel)} can't be null.");
        return viewModel;
    }

    /// <inheritdoc />
    public override GraphNodePath GetNodePath()
    {
        var node = new GraphNodePath(Editor.NodeContainer.GetNode(Asset.Asset));
        node.PushMember(nameof(UIAsset.Hierarchy));
        node.PushTarget();
        node.PushMember(nameof(UIAsset.Hierarchy.Parts));
        node.PushTarget();
        node.PushIndex(new NodeIndex(Id.ObjectId));
        node.PushMember(nameof(UIElementDesign.UIElement));
        node.PushTarget();
        return node;
    }

    [NotNull]
    public UIElementViewModel GetRootElementViewModel()
    {
        UIElementViewModel root = this;
        var source = this;
        while (source != null)
        {
            root = source;
            source = source.Parent as UIElementViewModel;
        }
        return root;
    }

    /// <inheritdoc />
    protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, ref string message)
    {
        message = $"{GetDropLocationName()} does not support adding or inserting children";
        return false;
    }

    /// <inheritdoc/>
    protected override string GetDropLocationName()
    {
        return UIEditorBaseViewModel.GetDisplayName(AssetSideUIElement);
    }

    protected T GetDependencyPropertyValue<T>(UIElement element, PropertyKey<T> property)
    {
        var dependencyPropertiesNode = Editor.NodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)];
        return (T)dependencyPropertiesNode.Retrieve(new NodeIndex(property));
    }

    protected bool RemoveDependencyProperty(UIElement element, PropertyKey property)
    {
        var dependencyPropertiesNode = Editor.NodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)].Target;
        var propertyIndex = new NodeIndex(property);
        if (!dependencyPropertiesNode.Indices.Contains(propertyIndex))
            return false;

        var value = dependencyPropertiesNode.Retrieve(propertyIndex);
        dependencyPropertiesNode.Remove(value, propertyIndex);
        return true;
    }

    protected void SetDependencyPropertyValue<T>(UIElement element, PropertyKey<T> property, T value)
    {
        var dependencyPropertiesNode = Editor.NodeContainer.GetOrCreateNode(element)[nameof(UIElement.DependencyProperties)].Target;
        var propertyIndex = new NodeIndex(property);
        if (!dependencyPropertiesNode.Indices.Contains(propertyIndex))
        {
            dependencyPropertiesNode.Add(value, propertyIndex);
        }
        else
        {
            dependencyPropertiesNode.Update(value, propertyIndex);
        }
    }

    private void UpdateSourceLibrary()
    {
        SourceLibrary = UIElementDesign.Base != null ? Editor.Session.GetAssetById(UIElementDesign.Base.BasePartAsset.Id) as UILibraryViewModel : null;
    }

    private void BaseElementChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        UpdateSourceLibrary();
    }

    private void PanelCommandImpl(PanelCommandMode mode)
    {
        if (Parent is UIElementViewModel parentElement)
        {
            PanelViewModel.ChangeChildElementLayoutProperties(Editor.NodeContainer, Editor.UndoRedoService, parentElement.AssetSideUIElement, AssetSideUIElement, mode);
        }
    }

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    IObjectNode IPropertyProviderViewModel.GetRootNode()
    {
        return Editor.NodeContainer.GetNode(AssetSideUIElement);
    }

    GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
    {
        return GetNodePath();
    }

    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member)
    {
        if (typeof(PropertyContainerClass).IsAssignableFrom(member.Type))
        {
            return false;
        }
        var assetPropertyProvider = (IPropertyProviderViewModel)Asset;
        return assetPropertyProvider.ShouldConstructMember(member);
    }

    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);
}
