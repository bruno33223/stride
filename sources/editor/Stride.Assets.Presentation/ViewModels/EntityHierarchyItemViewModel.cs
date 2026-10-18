// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels;

public interface IEntityFactory
{
    Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent);
}

public abstract class EntityHierarchyItemViewModel : AssetCompositeItemViewModel<EntityHierarchyViewModel, EntityHierarchyItemViewModel>, IAssetPartViewModel, IAddChildViewModel, IInsertChildViewModel
{
    private bool isExpanded = true;
    private bool isEditing;
    private bool isVisible = true;
    private bool isLoaded = true;
    private bool isLocked;

    protected EntityHierarchyItemViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities)
        : base(asset)
    {
        AddItems(childEntities.Select(asset.CreatePartViewModel));
    }

    /// <inheritdoc />
    public abstract AbsoluteId Id { get; }

    /// <summary>
    /// An enumeration of the items represented by this item.
    /// </summary>
    /// <remarks>
    /// In case of an <see cref="EntityViewModel"/> it is equivalent to <c>this</c>.
    /// </remarks>
    // FIXME: find a better name
    public abstract IEnumerable<EntityViewModel> InnerSubEntities { get; }

    public virtual bool IsEditable => true;

    public virtual bool IsEditing { get => isEditing; set => SetValue(ref isEditing, value); }

    public virtual bool IsExpanded { get => isExpanded; set => SetValue(ref isExpanded, value); }

    public virtual bool IsVisible { get => isVisible; set => SetValue(ref isVisible, value); }

    public virtual bool IsLoaded { get => isLoaded; set => SetValue(ref isLoaded, value); }

    public virtual bool IsLocked { get => isLocked; set => SetValue(ref isLocked, value); }

    public virtual Task NotifyGameSidePartAdded() => Task.CompletedTask;

    public IEnumerable<EntityViewModel> TransformChildren => Children.SelectMany(x => x.InnerSubEntities);

    public IEnumerable<EntityFolderViewModel> Folders => Children.OfType<EntityFolderViewModel>();

    public new void AddItem(EntityHierarchyItemViewModel item) => base.AddItem(item);

    public new void RemoveItem(EntityHierarchyItemViewModel item) => base.RemoveItem(item);

    public EntityHierarchyItemViewModel? TransformParent => Parent;

    protected EntityHierarchyAssetBase EntityHierarchy => (EntityHierarchyAssetBase)Asset.Asset;

    /// <inheritdoc/>
    public virtual bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        message = string.Empty;
        if (children == null || children.Count == 0) return false;
        foreach (var child in children)
        {
            if (ReferenceEquals(child, this))
            {
                message = "Cannot drop entity onto itself";
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public virtual void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
    }

    public virtual EntityHierarchyElementViewModel Owner => this as EntityHierarchyElementViewModel ?? Parent?.Owner!;

    public virtual int EntityCount => 0;

    public virtual int IndexOfEntity(EntityViewModel entity) => -1;

    public virtual int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection)
    {
        var indexInParent = 0;
        var i = 0;
        foreach (var entity in entityCollection)
        {
            if (i++ == indexInEntity)
                break;

            if (string.IsNullOrEmpty(entity.Folder))
                ++indexInParent;
        }
        return indexInParent;
    }

    public EntityHierarchyItemViewModel FindOrCreateFolder(string? folderPath, bool expand = true)
    {
        if (string.IsNullOrEmpty(folderPath))
            return this;

        var folders = folderPath.Split(new[] { EntityFolderViewModel.FolderSeparator }, StringSplitOptions.RemoveEmptyEntries);
        if (expand)
            IsExpanded = true;

        EntityFolderViewModel? subFolder = null;
        var parent = this;
        foreach (var folderName in folders)
        {
            subFolder = parent.Children.OfType<EntityFolderViewModel>().FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
            if (subFolder == null)
            {
                subFolder = new EntityFolderViewModel(parent.Asset, folderName, Enumerable.Empty<EntityDesign>());
                parent.AddItem(subFolder);
            }
            if (expand)
                subFolder.IsExpanded = true;
            parent = subFolder;
        }

        return subFolder ?? this;
    }

    public void InsertEntityViewModel(EntityViewModel entity, int index, bool expand = true)
    {
        if (entity.IsDestroyed)
            throw new InvalidOperationException("The entity to insert has already been destroyed");

        if (index < 0 || index >= Children.Count)
        {
            AddItem(entity);
        }
        else
        {
            InsertItem(index, entity);
        }

        if (expand)
        {
            IsExpanded = true;
        }
    }

    public static void RemoveEntityViewModel(EntityViewModel entity)
    {
        if (entity.Parent == null) throw new InvalidOperationException($"{nameof(entity)}.{nameof(entity.Parent)} cannot be null");
        entity.Parent.RemoveItem(entity);
        entity.Destroy();
    }

    protected virtual EntityComponent? CreateComponentFromAsset(AssetViewModel asset)
    {
        return null;
    }

    protected virtual int GetInsertionIndex(InsertPosition position)
    {
        return 0;
    }

    /// <inheritdoc/>
    public virtual bool CanInsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, out string message)
    {
        message = string.Empty;
        if (Parent == null) return false;
        return Parent.CanAddChildren(children, modifiers, out message);
    }

    /// <inheritdoc/>
    public virtual void InsertChildren(IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers)
    {
        Parent?.AddChildren(children, modifiers);
    }
}

