// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Base class for the view model of an <see cref="AssetCompositeHierarchyViewModel{TAssetPartDesign,TAssetPart}"/> editor.
/// </summary>
/// <typeparam name="TAssetPartDesign">The type of a part design.</typeparam>
/// <typeparam name="TAssetPart">The type of a part.</typeparam>
/// <typeparam name="TAssetViewModel"></typeparam>
/// <typeparam name="TItemViewModel">The type of a real <see cref="AssetCompositeItemViewModel"/> that can be copied/cut/pasted.</typeparam>
public abstract class AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>
    : AssetCompositeEditorViewModel<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>, AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
    where TAssetViewModel : AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>
    where TItemViewModel : AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel>
{
    private TItemViewModel rootPart;
    private bool updateSelectionGuard;

    protected AssetCompositeHierarchyEditorViewModel(TAssetViewModel asset)
        : base(asset)
    {
        rootPart = CreateRootPartViewModel();

        SelectedContent.CollectionChanged += SelectedContentCollectionChanged;
        SelectedItems.CollectionChanged += SelectedItemsCollectionChanged;
    }

    public TItemViewModel RootPart { get => rootPart; private set => SetValue(ref rootPart, value); }

    public ObservableSet<TItemViewModel> SelectedItems { get; } = [];

    public ICommandBase? CopyCommand { get; protected set; }

    public ICommandBase? CutCommand { get; protected set; }

    public ICommandBase? DeleteCommand { get; protected set; }

    public ICommandBase? DuplicateSelectionCommand { get; protected set; }

    public ICommandBase? PasteCommand { get; protected set; }

    /// <summary>
    /// Gets the root items in the given collection, excluding any item whose parent or ancestor is also in the collection.
    /// </summary>
    public static ISet<TItemViewModel> GetCommonRoots(IReadOnlyCollection<TItemViewModel> items)
    {
        var hashSet = new HashSet<TItemViewModel>(items);
        foreach (var item in items)
        {
            var parent = item.Parent;
            while (parent != null)
            {
                if (hashSet.Contains(parent))
                {
                    hashSet.Remove(item);
                    break;
                }
                parent = parent.Parent;
            }
        }
        return hashSet;
    }

    /// <summary>
    /// Gets the root items in the given collection, excluding any item whose parent or ancestor is also in the collection.
    /// </summary>
    public static ISet<TViewModel> GetCommonRoots<TViewModel>(IReadOnlyCollection<TViewModel> items)
        where TViewModel : AssetCompositeItemViewModel
    {
        var hashSet = new HashSet<TViewModel>(items);
        foreach (var item in items)
        {
            var parent = (item as AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel>)?.Parent;
            while (parent != null)
            {
                if (hashSet.Contains((TViewModel)(object)parent))
                {
                    hashSet.Remove(item);
                    break;
                }
                parent = parent.Parent;
            }
        }
        return hashSet;
    }
    
    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>));

        // FIXME xplat-editor
        //PasteAsRootMonitor.Destroy();
        //PasteMonitor.Destroy();

        // Unregister collection
        SelectedItems.CollectionChanged -= SelectedItemsCollectionChanged;
        SelectedContent.CollectionChanged -= SelectedContentCollectionChanged;

        // Clear the property grid if any of our items was selected.
        // TODO: this should be factorized with UI editor (at least) and with Sprite editor (ideally)
        //if (Session.ActiveProperties.Selection.OfType< AssetCompositeItemViewModel>().Any(x => x == RootPart))
        {
            // TODO: reimplement this!
            Session.ActiveProperties.GenerateSelectionPropertiesAsync(Enumerable.Empty<IPropertyProviderViewModel>()).Forget();
        }
        // Destroy all parts recursively
        RootPart?.Destroy();
        base.Destroy();
    }

    /// <inheritdoc/>
    public override IAssetPartViewModel? FindPartViewModel(AbsoluteId id)
    {
        if (RootPart is IAssetPartViewModel item && id == item.Id)
            return item;

        return RootPart?.EnumerateChildren().BreadthFirst(x => x.EnumerateChildren()).FirstOrDefault(part => part is IAssetPartViewModel viewModel && viewModel.Id == id) as IAssetPartViewModel;
    }

    protected abstract TItemViewModel CreateRootPartViewModel();

    protected abstract Task RefreshEditorProperties();

    /// <summary>
    /// Called when the content of <see cref="AssetCompositeEditorViewModel.SelectedContent"/> changed.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <remarks>
    /// Default implementation populates <see cref="SelectedItems"/> by filtering elements of type <typeparamref name="TItemViewModel"/>.
    /// </remarks>
    protected virtual void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(SelectedContent.OfType<TItemViewModel>());
    }

    /// <summary>
    /// Called when the content of <see cref="SelectedItems"/> changed.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <remarks>
    /// Default implementation populates <see cref="AssetCompositeEditorViewModel{,}.SelectedContent"/> with the same elements.
    /// </remarks>
    protected virtual void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedContent.Clear();
        SelectedContent.AddRange(SelectedItems);
    }

    private void SelectedContentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (updateSelectionGuard)
            return;

        try
        {
            updateSelectionGuard = true;
            SelectedContentCollectionChanged(args.Action);
            // Refresh the property grid asynchronously
            RefreshEditorProperties().Forget();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }

    private void SelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (updateSelectionGuard)
            return;

        try
        {
            updateSelectionGuard = true;
            SelectedItemsCollectionChanged(args.Action);
            // Refresh the property grid asynchronously
            RefreshEditorProperties().Forget();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }
}
