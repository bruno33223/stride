// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Design;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityHierarchyViewModel, EntityHierarchyItemViewModel>, IAddChildViewModel
{
    private static readonly ILogger EditorLogger = GlobalLogger.GetLogger("Scene");
    private EntityHierarchyRootViewModel? activeRoot;
    private string filterPattern = string.Empty;
    private List<string> filterTokens = [];
    private bool compilingAssets;

    private EditorCameraViewModel? camera;
    private EntityTransformationViewModel? transform;
    private EditorGridViewModel? grid;
    private EditorNavigationViewModel? navigation;
    private EditorRenderingViewModel? rendering;
    private EditorLightingViewModel? lighting;
    private EntityGizmosViewModel? entityGizmos;

    protected EntityHierarchyEditorViewModel(EntityHierarchyViewModel asset)
        : base(asset)
    {
        CreateEntityCommand = new AnonymousTaskCommand<object?>(ServiceProvider, x => CreateEntity(true, x as IEntityFactory, ActiveRoot ?? HierarchyRoot));
        CreateEntityInRootCommand = new AnonymousTaskCommand<object?>(ServiceProvider, x => CreateEntity(false, x as IEntityFactory, ActiveRoot ?? HierarchyRoot));
        CreateFolderInRootCommand = new AnonymousCommand(ServiceProvider, () => CreateFolder(HierarchyRoot?.Asset, ActiveRoot ?? HierarchyRoot, true));
        CreateEntityInSelectionCommand = new AnonymousTaskCommand<object?>(ServiceProvider, x => CreateEntity(false, x as IEntityFactory, (SelectedContent.FirstOrDefault() as EntityHierarchyItemViewModel) ?? ActiveRoot ?? HierarchyRoot));
        CreateFolderInSelectionCommand = new AnonymousCommand(ServiceProvider, () =>
        {
            var element = (SelectedContent.FirstOrDefault() as EntityHierarchyItemViewModel) ?? ActiveRoot ?? HierarchyRoot;
            if (element != null)
                CreateFolder(element.Asset, element, true);
        });

        OpenPrefabEditorCommand = new AnonymousCommand(ServiceProvider, OpenPrefabEditor);
        SelectPrefabCommand = new AnonymousCommand(ServiceProvider, SelectPrefab);
        SetActiveRootCommand = new AnonymousCommand(ServiceProvider, SetActiveRoot);
        BreakLinkToPrefabCommand = new AnonymousCommand(ServiceProvider, BreakLinkToPrefab);
        CreatePrefabFromSelectionCommand = new AnonymousCommand(ServiceProvider, CreatePrefabFromSelection);

        var delCmd = new AnonymousTaskCommand(ServiceProvider, Delete);
        DeleteCommand = delCmd;
        base.DeleteCommand = delCmd;

        var dupCmd = new AnonymousCommand(ServiceProvider, () => DuplicateSelectedEntities());
        DuplicateSelectionCommand = dupCmd;
        base.DuplicateSelectionCommand = dupCmd;

        var cutCmd = new AnonymousCommand(ServiceProvider, Cut);
        CutCommand = cutCmd;
        base.CutCommand = cutCmd;

        var copyCmd = new AnonymousCommand(ServiceProvider, Copy);
        CopyCommand = copyCmd;
        base.CopyCommand = copyCmd;

        var pasteCmd = new AnonymousTaskCommand(ServiceProvider, Paste);
        PasteCommand = pasteCmd;
        base.PasteCommand = pasteCmd;
    }

    public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart;

    public EntityHierarchyRootViewModel? ActiveRoot
    {
        get => activeRoot;
        protected internal set => SetValue(ref activeRoot, value);
    }

    public EntityViewModel? EntityWithGizmo => SelectedItems.OfType<EntityViewModel>().LastOrDefault();

    public ILogger Logger => EditorLogger;

    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    public IEditorGameController? Controller => ServiceProvider.TryGet<IEditorGameController>();

    public AssetNodeContainer NodeContainer => Session.AssetNodeContainer;

    public string FilterPattern
    {
        get => filterPattern;
        set => SetValue(ref filterPattern, value, UpdateVisibilities);
    }

    /// <summary>
    /// Alias for <see cref="FilterPattern"/> used by modern Avalonia view bindings.
    /// </summary>
    public string SearchFilter
    {
        get => FilterPattern;
        set => FilterPattern = value;
    }

    public bool CompilingAssets
    {
        get => compilingAssets;
        set => SetValue(ref compilingAssets, value);
    }

    public bool DisplaySelectionMask
    {
        get => Selection?.DisplaySelectionMask ?? false;
        set
        {
            if (Selection != null && Selection.DisplaySelectionMask != value)
            {
                SetValue(Selection.DisplaySelectionMask != value, () => Selection.DisplaySelectionMask = value);
            }
        }
    }

    private bool materialSelectionMode;

    public bool MaterialSelectionMode
    {
        get => materialSelectionMode;
        set => SetValue(ref materialSelectionMode, value);
    }

    // Lazy / safe access to viewport view models
    public EditorCameraViewModel? Camera => camera ??= Controller != null ? new EditorCameraViewModel(ServiceProvider, Controller) : null;

    public EntityTransformationViewModel? Transform => transform ??= Controller != null ? new EntityTransformationViewModel(ServiceProvider, Controller) : null;

    public EditorGridViewModel? Grid => grid ??= Controller != null ? new EditorGridViewModel(ServiceProvider, Controller) : null;

    public EditorNavigationViewModel? Navigation => navigation ??= Controller != null ? new EditorNavigationViewModel(ServiceProvider, Controller, this) : null;

    public EditorRenderingViewModel? Rendering => rendering ??= Controller != null ? new EditorRenderingViewModel(ServiceProvider, Controller) : null;

    public EditorLightingViewModel? Lighting => lighting ??= Controller != null ? new EditorLightingViewModel(ServiceProvider, Controller, this) : null;

    public EntityGizmosViewModel? EntityGizmos => entityGizmos ??= Controller != null ? new EntityGizmosViewModel(ServiceProvider, Controller) : null;

    // Commands
    public ICommandBase CreateEntityCommand { get; }

    public ICommandBase AddEntityCommand => CreateEntityInSelectionCommand;

    public ICommandBase CreateEntityInRootCommand { get; }

    public ICommandBase CreateFolderInRootCommand { get; }

    public ICommandBase CreateEntityInSelectionCommand { get; }

    public ICommandBase CreateFolderInSelectionCommand { get; }

    public ICommandBase AddFolderCommand => CreateFolderInSelectionCommand;

    public new ICommandBase DeleteCommand { get; }

    public new ICommandBase DuplicateSelectionCommand { get; }

    public ICommandBase DuplicateCommand => DuplicateSelectionCommand;

    public ICommandBase OpenPrefabEditorCommand { get; }

    public ICommandBase SelectPrefabCommand { get; }

    public ICommandBase SetActiveRootCommand { get; }

    public ICommandBase BreakLinkToPrefabCommand { get; }

    public ICommandBase CreatePrefabFromSelectionCommand { get; }

    public new ICommandBase CutCommand { get; }

    public new ICommandBase CopyCommand { get; }

    public new ICommandBase PasteCommand { get; }

    private IEditorGameSelectionViewModelService? Selection => Controller?.GetService<IEditorGameSelectionViewModelService>();

    public void InitializeGameEditor(IEditorGameController gameController)
    {
        camera = new EditorCameraViewModel(ServiceProvider, gameController);
        transform = new EntityTransformationViewModel(ServiceProvider, gameController);
        grid = new EditorGridViewModel(ServiceProvider, gameController);
        navigation = new EditorNavigationViewModel(ServiceProvider, gameController, this);
        rendering = new EditorRenderingViewModel(ServiceProvider, gameController);
        lighting = new EditorLightingViewModel(ServiceProvider, gameController, this);
        entityGizmos = new EntityGizmosViewModel(ServiceProvider, gameController);

        OnPropertyChanged(nameof(Camera));
        OnPropertyChanged(nameof(Transform));
        OnPropertyChanged(nameof(Grid));
        OnPropertyChanged(nameof(Navigation));
        OnPropertyChanged(nameof(Rendering));
        OnPropertyChanged(nameof(Lighting));
        OnPropertyChanged(nameof(EntityGizmos));
    }

    public async Task CreateEntity(bool atMousePosition, IEntityFactory? factory, EntityHierarchyItemViewModel? parent = null)
    {
        parent ??= ActiveRoot ?? HierarchyRoot;
        if (parent == null)
            return;

        var entity = factory != null
            ? await factory.CreateEntity(parent)
            : new Entity { Name = NamingHelper.ComputeNewName("Entity", parent.Children, x => x.Name) };

        if (entity != null)
        {
            AssetCollectionItemIdHelper.GenerateMissingItemIds(entity);

            if (atMousePosition && Controller != null)
            {
                var position = Controller.GetMousePositionInScene(true);
                entity.Transform.Position = position;
            }

            var collection = new AssetPartCollection<EntityDesign, Entity> { new EntityDesign(entity) };

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                parent.Asset.AssetHierarchyPropertyGraph.AddPartToAsset(
                    collection,
                    collection.Single().Value,
                    (parent as EntityViewModel)?.AssetSideEntity,
                    parent.Children.Count);
                UndoRedoService.SetName(transaction, $"Create entity '{entity.Name}'");
            }

            var partId = new AbsoluteId(parent.Asset.Id, entity.Id);
            if (FindPartViewModel(partId) is EntityViewModel newEntity)
            {
                ClearSelection();
                SelectedContent.Add(newEntity);
                newEntity.IsEditing = true;
            }
        }
    }

    public void CreateFolder(EntityHierarchyViewModel? asset, EntityHierarchyItemViewModel? parent, bool editName)
    {
        if (parent == null)
            return;

        var entity = new Entity { Name = NamingHelper.ComputeNewName("Folder", parent.Children, x => x.Name) };
        AssetCollectionItemIdHelper.GenerateMissingItemIds(entity);
        var collection = new AssetPartCollection<EntityDesign, Entity> { new EntityDesign(entity) };

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            parent.Asset.AssetHierarchyPropertyGraph.AddPartToAsset(
                collection,
                collection.Single().Value,
                (parent as EntityViewModel)?.AssetSideEntity,
                parent.Children.Count);
            UndoRedoService.SetName(transaction, $"Create folder '{entity.Name}'");
        }

        var partId = new AbsoluteId(parent.Asset.Id, entity.Id);
        if (FindPartViewModel(partId) is EntityViewModel newEntity)
        {
            ClearSelection();
            SelectedContent.Add(newEntity);
            newEntity.IsEditing = editName;
        }
    }

    public ISet<EntityViewModel> DuplicateSelectedEntities()
    {
        var entitiesToDuplicate = SelectedItems.OfType<EntityViewModel>().ToList();
        if (entitiesToDuplicate.Count == 0)
            return new HashSet<EntityViewModel>();

        SelectedItems.Clear();

        var duplicatedEntities = new HashSet<EntityViewModel>();
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var entity in entitiesToDuplicate)
            {
                var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
                var hierarchy = EntityHierarchyPropertyGraph.CloneSubHierarchies(
                    entity.Asset.Session.AssetNodeContainer,
                    entity.Asset.Asset,
                    entity.Id.ObjectId.Yield(),
                    flags,
                    out _);

                var newEntityDesign = hierarchy.Parts.Values.First();
                newEntityDesign.Entity.Name = NamingHelper.ComputeNewName(
                    newEntityDesign.Entity.Name,
                    entity.Parent?.Children ?? Enumerable.Empty<EntityHierarchyItemViewModel>(),
                    x => x.Name);

                entity.Asset.AssetHierarchyPropertyGraph.AddPartToAsset(
                    hierarchy.Parts,
                    newEntityDesign,
                    (entity.Parent as EntityViewModel)?.AssetSideEntity,
                    entity.Parent?.Children.Count ?? 0);

                var newId = new AbsoluteId(entity.Asset.Id, newEntityDesign.Entity.Id);
                if (FindPartViewModel(newId) is EntityViewModel duplicatedVm)
                {
                    duplicatedEntities.Add(duplicatedVm);
                }
            }
            UndoRedoService.SetName(transaction, "Duplicate entities");
        }

        SelectedItems.AddRange(duplicatedEntities);
        return duplicatedEntities;
    }

    public async Task Delete()
    {
        var entitiesToDelete = SelectedItems.OfType<EntityViewModel>().ToList();
        if (entitiesToDelete.Count == 0)
            return;

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            ClearSelection();
            var entitiesPerScene = entitiesToDelete.GroupBy(x => x.Asset);
            foreach (var entities in entitiesPerScene)
            {
                entities.Key.AssetHierarchyPropertyGraph.DeleteParts(entities.Select(x => x.EntityDesign), out _);
            }
            UndoRedoService.SetName(transaction, "Delete selected entities");
        }

        await Task.CompletedTask;
    }

    public void Cut()
    {
        Copy();
        Delete().Forget();
    }

    public void Copy()
    {
        var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
        if (copyPasteService == null || SelectedItems.Count == 0)
            return;
    }

    public async Task Paste()
    {
        var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
        if (copyPasteService == null)
            return;

        await Task.CompletedTask;
    }

    private void OpenPrefabEditor()
    {
        var prefabs = new HashSet<PrefabViewModel>(SelectedItems.OfType<EntityViewModel>().Select(x => x.Asset as PrefabViewModel).NotNull());
        foreach (var prefab in prefabs)
        {
            ServiceProvider.TryGet<IAssetEditorsManager>()?.OpenAssetEditor(prefab);
        }
    }

    private void SelectPrefab()
    {
        if (EntityWithGizmo?.Asset is PrefabViewModel prefab)
            Session.ActiveAssetView?.SelectAssets(prefab.Yield());
    }

    private void SetActiveRoot()
    {
        if (SelectedContent.LastOrDefault() is EntityHierarchyRootViewModel root)
            ActiveRoot = root;
    }

    private void BreakLinkToPrefab()
    {
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var grp in SelectedItems.OfType<EntityViewModel>().GroupBy(e => (EntityHierarchyViewModel)e.Asset))
            {
                grp.Key.AssetHierarchyPropertyGraph.BreakBasePartLinks(grp.Select(x => x.EntityDesign));
            }
            UndoRedoService.SetName(transaction, "Break link to prefab");
        }
    }

    private void CreatePrefabFromSelection()
    {
        var entities = SelectedItems.OfType<EntityViewModel>().ToList();
        if (entities.Count == 0) return;
    }

    private void UpdateVisibilities()
    {
        filterTokens = (filterPattern ?? string.Empty)
            .Split(" \t\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.ToLowerInvariant())
            .ToList();

        if (HierarchyRoot != null)
        {
            foreach (var content in HierarchyRoot.Children)
            {
                UpdateVisibilities(content);
            }
        }
    }

    private void UpdateVisibilities(EntityHierarchyItemViewModel entity)
    {
        var newValue = false;
        foreach (var subEntity in entity.Children)
        {
            UpdateVisibilities(subEntity);
            if (subEntity.IsVisible)
                newValue = true;
        }

        if (!newValue)
        {
            newValue = filterTokens.Count == 0 ||
                       (entity.Name != null && filterTokens.All(x => entity.Name.ToLowerInvariant().Contains(x)));
        }

        entity.IsVisible = newValue;
    }

    /// <inheritdoc />
    protected override async Task RefreshEditorProperties()
    {
        EditorProperties.UpdateTypeAndName(SelectedItems, x => "Entity", x => x.Name, "entities");
        await EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems.OfType<EntityViewModel>());
    }

    /// <inheritdoc />
    protected override void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(SelectedContent.Cast<EntityHierarchyItemViewModel>().SelectMany(x => x.InnerSubEntities));
        OnPropertyChanged(nameof(EntityWithGizmo));
    }

    /// <inheritdoc />
    protected override void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
    {
        base.SelectedItemsCollectionChanged(action);
        OnPropertyChanged(nameof(EntityWithGizmo));
    }

    /// <inheritdoc />
    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        message = string.Empty;
        if (children == null || children.Count == 0) return false;
        return (ActiveRoot ?? HierarchyRoot)?.CanAddChildren(children, modifiers, out message) ?? false;
    }

    /// <inheritdoc />
    void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        (ActiveRoot ?? HierarchyRoot)?.AddChildren(children, modifiers);
    }

    public void UpdateTransformations(IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations)
    {
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var transformation in transformations)
            {
                var element = FindPartViewModel(transformation.Key) as EntityHierarchyItemViewModel;
                if (element != null)
                    UpdateTransformations(element, transformation.Value);
            }

            UndoRedoService.SetName(transaction, "Update transformation");
        }
    }

    protected virtual void UpdateTransformations(EntityHierarchyItemViewModel element, TransformationTRS transformation)
    {
        var entity = element as EntityViewModel;
        if (entity == null)
            return;

        var node = NodeContainer.GetNode(entity.AssetSideEntity.Transform);

        // Update properties only when they actually changed
        var oldPosition = (Vector3)node[nameof(TransformComponent.Position)].Retrieve();
        if (oldPosition != transformation.Position)
            node[nameof(TransformComponent.Position)].Update(transformation.Position);

        var oldRotation = (Quaternion)node[nameof(TransformComponent.Rotation)].Retrieve();
        if (oldRotation != transformation.Rotation)
            node[nameof(TransformComponent.Rotation)].Update(transformation.Rotation);

        var oldScale = (Vector3)node[nameof(TransformComponent.Scale)].Retrieve();
        if (oldScale != transformation.Scale)
            node[nameof(TransformComponent.Scale)].Update(transformation.Scale);
    }

    protected virtual void LoadSettings(SceneSettingsData settings)
    {
        Camera?.LoadSettings(settings);
        Transform?.LoadSettings(settings);
        EntityGizmos?.LoadSettings(settings);
        Grid?.LoadSettings(settings);
        Navigation?.LoadSettings(settings);
        Rendering?.LoadSettings(settings);
    }

    protected virtual void SaveSettings(SceneSettingsData settings)
    {
        Camera?.SaveSettings(settings);
        Transform?.SaveSettings(settings);
        EntityGizmos?.SaveSettings(settings);
        Grid?.SaveSettings(settings);
        Navigation?.SaveSettings(settings);
        Rendering?.SaveSettings(settings);
    }

    public virtual Task<bool> InitializeEditor()
    {
        return Task.FromResult(true);
    }
}

