// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;

using UIHierarchyItemViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIHierarchyItemViewModel;
using UIElementViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementViewModel;
using UIRootViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIRootViewModel;
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
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Editor.ViewModels;

public abstract class UIEditorBaseViewModel : AssetCompositeHierarchyEditorViewModel<UIElementDesign, UIElement, UIBaseViewModel, UIHierarchyItemViewModel>, IAddChildViewModel
{
    private static readonly ILogger EditorLogger = GlobalLogger.GetLogger("UI");

    private readonly IEditorGameController? controller;
    private UIElementViewModel? activeRoot;
    private string filterPattern = string.Empty;
    private double zoomLevel = 1.0;
    private float snapValue = 1.0f;
    private Color guidelineColor = Color.Red;
    private float guidelineThickness = 2.0f;
    private Color highlightColor = Color.Pink;
    private float highlightThickness = 2.0f;
    private Color selectionColor = Color.LimeGreen;
    private float selectionThickness = 2.0f;
    private Color sizingColor = Color.Cyan;
    private float sizingThickness = 2.0f;

    protected UIEditorBaseViewModel(UIBaseViewModel asset)
        : base(asset)
    {
        // Layout Commands
        GroupIntoCommand = new AnonymousCommand<object?>(ServiceProvider, param => GroupInto(param as IUIElementFactory));
        UngroupCommand = new AnonymousCommand(ServiceProvider, Ungroup);
        ChangeLayoutTypeCommand = new AnonymousCommand<object?>(ServiceProvider, param => ChangeLayoutType(param as IUIElementFactory));
        PanelCommand = new AnonymousCommand<PanelCommandMode>(ServiceProvider, ExecutePanelCommand);

        // Tree Commands
        AddChildCommand = new AnonymousCommand<object?>(ServiceProvider, param => AddChild(param as IUIElementFactory));
        InsertChildCommand = new AnonymousCommand<object?>(ServiceProvider, param => InsertChild(param as IUIElementFactory));
        MoveUpCommand = new AnonymousCommand(ServiceProvider, MoveSelectedUp);
        MoveDownCommand = new AnonymousCommand(ServiceProvider, MoveSelectedDown);
        RenameCommand = new AnonymousCommand(ServiceProvider, () =>
        {
            if (SelectedElement != null)
                SelectedElement.IsEditing = true;
        });

        var delCmd = new AnonymousTaskCommand(ServiceProvider, Delete);
        DeleteCommand = delCmd;
        base.DeleteCommand = delCmd;

        var dupCmd = new AnonymousCommand(ServiceProvider, DuplicateSelectedElements);
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

        // View & Zoom Commands
        ZoomInCommand = new AnonymousCommand(ServiceProvider, () => ZoomLevel = Math.Min(5.0, ZoomLevel + 0.1));
        ZoomOutCommand = new AnonymousCommand(ServiceProvider, () => ZoomLevel = Math.Max(0.1, ZoomLevel - 0.1));
        ResetZoomCommand = new AnonymousCommand(ServiceProvider, () => ZoomLevel = 1.0);
        ShowPropertiesCommand = new AnonymousCommand(ServiceProvider, ShowAssetProperties);

        // Initialize UI Factories
        InitializeFactories();
    }

    protected UIEditorBaseViewModel(UIBaseViewModel asset, Func<UIEditorBaseViewModel, IEditorGameController>? controllerFactory)
        : this(asset)
    {
        if (controllerFactory != null)
        {
            try
            {
                controller = controllerFactory(this);
            }
            catch (Exception ex)
            {
                EditorLogger.Warning("Could not create UIEditorController in current context.", ex);
            }
        }
    }

    public IEditorGameController? Controller => controller;

    public UIRootViewModel HierarchyRoot => (UIRootViewModel)RootPart;

    public UIAssetBase UIAsset => (UIAssetBase)Asset.Asset;

    public ILogger Logger => EditorLogger;

    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    public AssetNodeContainer NodeContainer => Session.AssetNodeContainer;

    public UIElementViewModel? ActiveRoot
    {
        get => activeRoot ?? HierarchyRoot?.Children.OfType<UIElementViewModel>().FirstOrDefault();
        protected internal set => SetValue(ref activeRoot, value);
    }

    public UIElementViewModel? SelectedElement => SelectedItems.OfType<UIElementViewModel>().LastOrDefault();

    public ObservableList<IUIElementFactory> Factories { get; } = [];

    public IReadOnlyList<IUIElementFactory> PanelFactories { get; private set; } = [];

    public ObservableList<IUIElementFactory> FilteredFactories { get; } = [];

    public string FilterPattern
    {
        get => filterPattern;
        set => SetValue(ref filterPattern, value, UpdateFilteredFactories);
    }

    public string SearchFilter
    {
        get => FilterPattern;
        set => FilterPattern = value;
    }

    public double ZoomLevel
    {
        get => zoomLevel;
        set => SetValue(ref zoomLevel, value);
    }

    public float SnapValue
    {
        get => snapValue;
        set => SetValue(ref snapValue, value);
    }

    public Color GuidelineColor { get => guidelineColor; set => SetValue(ref guidelineColor, value); }
    public float GuidelineThickness { get => guidelineThickness; set => SetValue(ref guidelineThickness, value); }
    public Color HighlightColor { get => highlightColor; set => SetValue(ref highlightColor, value); }
    public float HighlightThickness { get => highlightThickness; set => SetValue(ref highlightThickness, value); }
    public Color SelectionColor { get => selectionColor; set => SetValue(ref selectionColor, value); }
    public float SelectionThickness { get => selectionThickness; set => SetValue(ref selectionThickness, value); }
    public Color SizingColor { get => sizingColor; set => SetValue(ref sizingColor, value); }
    public float SizingThickness { get => sizingThickness; set => SetValue(ref sizingThickness, value); }

    // Commands
    public ICommandBase GroupIntoCommand { get; }

    public ICommandBase UngroupCommand { get; }

    public ICommandBase ChangeLayoutTypeCommand { get; }

    public ICommandBase PanelCommand { get; }

    public ICommandBase AddChildCommand { get; }

    public ICommandBase InsertChildCommand { get; }

    public ICommandBase MoveUpCommand { get; }

    public ICommandBase MoveDownCommand { get; }

    public ICommandBase RenameCommand { get; }

    public new ICommandBase DeleteCommand { get; }

    public new ICommandBase DuplicateSelectionCommand { get; }

    public ICommandBase DuplicateCommand => DuplicateSelectionCommand;

    public new ICommandBase CutCommand { get; }

    public new ICommandBase CopyCommand { get; }

    public new ICommandBase PasteCommand { get; }

    public ICommandBase ZoomInCommand { get; }

    public ICommandBase ZoomOutCommand { get; }

    public ICommandBase ResetZoomCommand { get; }

    public ICommandBase ShowPropertiesCommand { get; }

    public static string GetDisplayName(UIElement element)
    {
        return string.IsNullOrWhiteSpace(element.Name) ? $"[{element.GetType().Name}]" : element.Name;
    }

    public virtual UIHierarchyItemViewModel CreatePartViewModel(UIBaseViewModel asset, UIElementDesign partDesign)
    {
        return UIElementViewModelFactory.Instance.ProvideViewModel(this, asset, partDesign);
    }

    public virtual ISet<AssetId> GatherAllBasePartAssets(UIHierarchyItemViewModel item, bool includeChildren)
    {
        var result = new HashSet<AssetId>();
        if (item is UIElementViewModel elementVm)
        {
            var baseId = elementVm.UIElementDesign.Base?.BasePartId;
            if (baseId != null)
            {
                result.Add(Asset.Id);
            }
        }
        return result;
    }

    private void InitializeFactories()
    {
        Factories.Clear();
        var standard = UIElementFactoryViewModel.CreateStandardFactories();
        Factories.AddRange(standard);

        PanelFactories = UIElementFactoryViewModel.CreatePanelFactories();

        // Load public elements from any accessible UILibrary
        if (Session?.ActiveAssetView != null)
        {
            foreach (var pkg in Session.AllPackages)
            {
                foreach (var lib in pkg.Assets.OfType<UILibraryViewModel>())
                {
                    if (lib == Asset)
                        continue;

                    var libAsset = lib.Asset;
                    foreach (var pair in libAsset.PublicUIElements)
                    {
                        Factories.Add(new UIElementFactoryViewModel(lib, pair.Key, pair.Value));
                    }
                }
            }
        }

        UpdateFilteredFactories();
    }

    private void UpdateFilteredFactories()
    {
        FilteredFactories.Clear();
        var query = FilterPattern?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query))
        {
            FilteredFactories.AddRange(Factories);
        }
        else
        {
            var matched = Factories.Where(f =>
                f.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                f.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
            FilteredFactories.AddRange(matched);
        }
    }

    public void AddChild(IUIElementFactory? factory, UIHierarchyItemViewModel? targetParent = null)
    {
        factory ??= Factories.FirstOrDefault(f => f.Name == "Button") ?? Factories.FirstOrDefault();
        if (factory == null)
            return;

        targetParent ??= SelectedElement ?? (UIHierarchyItemViewModel)HierarchyRoot;
        if (targetParent == null)
            return;

        var parentElement = (targetParent as UIElementViewModel)?.AssetSideUIElement;
        var hierarchyData = factory.Create(UIAsset);
        var rootElement = hierarchyData.RootParts.First();
        var rootDesign = hierarchyData.Parts[rootElement.Id];

        // Ensure unique name
        rootDesign.UIElement.Name = NamingHelper.ComputeNewName(factory.Name, targetParent.Children, x => x.Name);

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            var partCollection = new AssetPartCollection<UIElementDesign, UIElement>();
            foreach (var part in hierarchyData.Parts.Values)
                partCollection.Add(part);

            var insertIndex = targetParent.Children.Count;
            Asset.AssetHierarchyPropertyGraph.AddPartToAsset(partCollection, rootDesign, parentElement, insertIndex);
            UndoRedoService.SetName(transaction, $"Add {factory.Name} to {targetParent.Name ?? "Root"}");
        }

        var newPartId = new AbsoluteId(Asset.Id, rootDesign.UIElement.Id);
        if (FindPartViewModel(newPartId) is UIHierarchyItemViewModel newVm)
        {
            ClearSelection();
            SelectedContent.Add(newVm);
        }
    }

    public void InsertChild(IUIElementFactory? factory)
    {
        var current = SelectedElement;
        if (current?.Parent != null)
        {
            AddChild(factory, current.Parent);
        }
        else
        {
            AddChild(factory, HierarchyRoot);
        }
    }

    public void GroupInto(IUIElementFactory? factory)
    {
        var targetType = factory?.ElementType ?? typeof(Grid);
        var selected = SelectedItems.OfType<UIElementViewModel>().ToList();
        if (selected.Count == 0)
            return;

        var designs = selected.Select(x => x.UIElementDesign).ToList();
        PanelViewModel.GroupInto(UIAsset, UndoRedoService, (AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement>)Asset.AssetHierarchyPropertyGraph, designs, targetType);
    }

    public void Ungroup()
    {
        if (SelectedElement is { AssetSideUIElement: Panel } panelElement)
        {
            PanelViewModel.Ungroup(UIAsset, UndoRedoService, (AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement>)Asset.AssetHierarchyPropertyGraph, panelElement.UIElementDesign);
        }
    }

    public void ChangeLayoutType(IUIElementFactory? factory)
    {
        if (factory?.ElementType == null || SelectedElement is not { AssetSideUIElement: Panel } panelElement)
            return;

        PanelViewModel.ChangeLayoutType(UIAsset, NodeContainer, UndoRedoService, (AssetCompositeHierarchyPropertyGraph<UIElementDesign, UIElement>)Asset.AssetHierarchyPropertyGraph, panelElement.UIElementDesign, factory.ElementType);
    }

    public void ExecutePanelCommand(PanelCommandMode mode)
    {
        if (SelectedElement is { Parent: UIElementViewModel { AssetSideUIElement: Panel parentPanel } } childElement)
        {
            PanelViewModel.ChangeChildElementLayoutProperties(NodeContainer, UndoRedoService, parentPanel, childElement.AssetSideUIElement, mode);
        }
    }

    public void MoveSelectedUp()
    {
        if (SelectedElement is { Parent: UIElementViewModel { AssetSideUIElement: StackPanel stackPanel } } child)
        {
            PanelViewModel.ChangeChildElementLayoutProperties(NodeContainer, UndoRedoService, stackPanel, child.AssetSideUIElement, PanelCommandMode.MoveUp);
        }
        else if (SelectedElement is { Parent: UIElementViewModel { AssetSideUIElement: GridBase gridBase } } gridChild)
        {
            PanelViewModel.ChangeChildElementLayoutProperties(NodeContainer, UndoRedoService, gridBase, gridChild.AssetSideUIElement, PanelCommandMode.MoveUp);
        }
    }

    public void MoveSelectedDown()
    {
        if (SelectedElement is { Parent: UIElementViewModel { AssetSideUIElement: StackPanel stackPanel } } child)
        {
            PanelViewModel.ChangeChildElementLayoutProperties(NodeContainer, UndoRedoService, stackPanel, child.AssetSideUIElement, PanelCommandMode.MoveDown);
        }
        else if (SelectedElement is { Parent: UIElementViewModel { AssetSideUIElement: GridBase gridBase } } gridChild)
        {
            PanelViewModel.ChangeChildElementLayoutProperties(NodeContainer, UndoRedoService, gridBase, gridChild.AssetSideUIElement, PanelCommandMode.MoveDown);
        }
    }

    public async Task Delete()
    {
        var elementsToDelete = GetCommonRoots(SelectedItems).OfType<UIElementViewModel>().ToList();
        if (elementsToDelete.Count == 0)
            return;

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            ClearSelection();
            Asset.AssetHierarchyPropertyGraph.DeleteParts(elementsToDelete.Select(x => x.UIElementDesign), out _);
            UndoRedoService.SetName(transaction, $"Delete {elementsToDelete.Count} UI element{(elementsToDelete.Count > 1 ? "s" : string.Empty)}");
        }

        await Task.CompletedTask;
    }

    public void DuplicateSelectedElements()
    {
        var elementsToDuplicate = GetCommonRoots(SelectedItems).OfType<UIElementViewModel>().ToList();
        if (elementsToDuplicate.Count == 0)
            return;

        ClearSelection();
        var duplicatedVms = new List<UIHierarchyItemViewModel>();

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var element in elementsToDuplicate)
            {
                var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
                var hierarchy = UIAssetPropertyGraph.CloneSubHierarchies(
                    NodeContainer,
                    Asset.Asset,
                    element.Id.ObjectId.Yield(),
                    flags,
                    out _);

                var newRootDesign = hierarchy.Parts.Values.First();
                newRootDesign.UIElement.Name = NamingHelper.ComputeNewName(
                    newRootDesign.UIElement.Name,
                    element.Parent?.Children ?? Enumerable.Empty<UIHierarchyItemViewModel>(),
                    x => x.Name);

                var parentPart = (element.Parent as UIElementViewModel)?.AssetSideUIElement;
                var insertIndex = element.Parent?.Children.IndexOf(element) + 1 ?? 0;

                Asset.AssetHierarchyPropertyGraph.AddPartToAsset(
                    hierarchy.Parts,
                    newRootDesign,
                    parentPart,
                    insertIndex);

                var newId = new AbsoluteId(Asset.Id, newRootDesign.UIElement.Id);
                if (FindPartViewModel(newId) is UIHierarchyItemViewModel newVm)
                {
                    duplicatedVms.Add(newVm);
                }
            }
            UndoRedoService.SetName(transaction, "Duplicate UI elements");
        }

        SelectedItems.AddRange(duplicatedVms);
    }

    public void Cut()
    {
        Copy();
        Delete().Forget();
    }

    public void Copy()
    {
        // Copy using copy paste service if available
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

    protected virtual bool CanPaste(bool asRoot)
    {
        var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
        if (copyPasteService == null)
            return false;

        return true;
    }

    public override async Task<bool> Initialize()
    {
        if (!await base.Initialize())
            return false;

        return await InitializeEditor();
    }

    public virtual Task<bool> InitializeEditor()
    {
        ShowAssetProperties();
        return Task.FromResult(true);
    }

    public void ShowAssetProperties()
    {
        Session.ActiveProperties.GenerateSelectionPropertiesAsync(Asset.Yield<IPropertyProviderViewModel>()).Forget();
    }

    /// <inheritdoc />
    protected override Task RefreshEditorProperties()
    {
        EditorProperties.UpdateTypeAndName(SelectedItems.OfType<UIElementViewModel>(), SelectedItems.Count, e => e.ElementType.Name, e => e.AssetSideUIElement.Name, "elements");
        return EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems.OfType<UIElementViewModel>());
    }

    // IAddChildViewModel implementation
    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        message = "Add to UI";
        return children.Count > 0;
    }

    void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        foreach (var child in children)
        {
            if (child is IUIElementFactory factory)
            {
                AddChild(factory, SelectedElement ?? (UIHierarchyItemViewModel)HierarchyRoot);
            }
        }
    }

    public void Select(Guid elementId, bool isAdditive)
    {
        var partId = new AbsoluteId(Asset.Id, elementId);
        var element = FindPartViewModel(partId);
        if (element == null)
            return;

        if (SelectedContent.Contains(element))
            return;

        if (!isAdditive)
            ClearSelection();
        SelectedContent.Add(element);
    }

    public void UpdateProperties(Guid elementId, IReadOnlyDictionary<string, object> changes)
    {
        var partId = new AbsoluteId(Asset.Id, elementId);
        var element = (UIElementViewModel)FindPartViewModel(partId);
        if (element == null)
            return;

        var node = NodeContainer.GetNode(element.AssetSideUIElement);
        if (node == null)
            return;

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            foreach (var kv in changes)
            {
                var propertyName = kv.Key;
                var propertyValue = kv.Value;

                var member = node.TryGetChild(propertyName);
                if (member == null)
                    continue;

                // Update properties only when they actually changed
                var currentValue = member.Retrieve();
                if (currentValue != propertyValue)
                {
                    member.Update(propertyValue);
                }
            }
            UndoRedoService.SetName(transaction, $"Update {element.ElementType.Name}");
        }
    }
}
