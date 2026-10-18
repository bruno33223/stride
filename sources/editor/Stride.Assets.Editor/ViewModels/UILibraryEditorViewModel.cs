// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Assets.Editor.AssetEditors.UILibraryEditor.Services;
using Stride.Assets.Editor.AssetEditors.UILibraryEditor.ViewModels;
using Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;

using UILibraryRootViewModel = Stride.Assets.Editor.AssetEditors.UILibraryEditor.ViewModels.UILibraryRootViewModel;
using UIHierarchyItemViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIHierarchyItemViewModel;
using UIElementViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementViewModel;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<UILibraryViewModel>]
public sealed class UILibraryEditorViewModel : UIEditorBaseViewModel, IAssetEditorViewModel<UILibraryViewModel>
{
    public UILibraryEditorViewModel(UILibraryViewModel asset)
        : this(asset, x => new UILibraryEditorController(asset, (UILibraryEditorViewModel)x))
    {
    }

    public UILibraryEditorViewModel(UILibraryViewModel asset, Func<UIEditorBaseViewModel, IEditorGameController>? controllerFactory)
        : base(asset, controllerFactory)
    {
    }

    /// <inheritdoc />
    public override UILibraryViewModel Asset => (UILibraryViewModel)base.Asset;

    private UILibraryRootViewModel? UILibrary => RootPart as UILibraryRootViewModel;

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(UILibraryEditorViewModel));

        if (UILibrary != null)
        {
            UILibrary.Children.CollectionChanged -= RootElementsCollectionChanged;
        }

        base.Destroy();
    }

    /// <inheritdoc />
    protected override UIHierarchyItemViewModel CreateRootPartViewModel()
    {
        var rootParts = Asset.Asset.Hierarchy.EnumerateRootPartDesigns();
        return new UILibraryRootViewModel(this, Asset, rootParts);
    }

    /// <inheritdoc />
    public override async Task<bool> InitializeEditor()
    {
        if (!await base.InitializeEditor())
            return false;

        if (UILibrary != null)
        {
            UILibrary.Children.ForEach(r => r.PropertyChanged += RootPropertyChanged);
            UILibrary.Children.CollectionChanged += RootElementsCollectionChanged;
            ActiveRoot = UILibrary.Children.OfType<UIElementViewModel>().FirstOrDefault();
        }
        return true;
    }

    private void RootElementsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (UIElementViewModel root in e.NewItems.OfType<UIElementViewModel>())
                    {
                        UpdatePublicUIElementsEntry(root.Id.ObjectId, root.Name);
                        root.PropertyChanged += RootPropertyChanged;
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (UIElementViewModel root in e.OldItems.OfType<UIElementViewModel>())
                    {
                        root.PropertyChanged -= RootPropertyChanged;
                        UpdatePublicUIElementsEntry(root.Id.ObjectId, null);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException("Replace, Move and Reset are not supported on this collection.");

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RootPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UIElementViewModel.Name))
        {
            var viewModel = (UIElementViewModel)sender!;
            UpdatePublicUIElementsEntry(viewModel.Id.ObjectId, viewModel.Name);
        }
    }

    private void UpdatePublicUIElementsEntry(Guid rootId, string? name)
    {
        var node = NodeContainer.GetNode((UILibraryAsset)Asset.Asset)?[nameof(UILibraryAsset.PublicUIElements)]?.Target;
        if (node == null) return;
        var index = new NodeIndex(rootId);

        using var transaction = UndoRedoService.CreateTransaction();
        if (string.IsNullOrWhiteSpace(name))
        {
            if (node.Indices.Contains(index))
            {
                node.Remove(name, index);
                UndoRedoService.SetName(transaction, $"Remove '{name}' export from the UILibrary");
            }
        }
        else
        {
            if (!node.Indices.Contains(index))
            {
                node.Add(name, index);
                UndoRedoService.SetName(transaction, $"Add '{name}' export to the UILibrary");
            }
            else
            {
                node.Update(name, index);
                UndoRedoService.SetName(transaction, "Update name of export in the UILibrary");
            }
        }
    }
}
