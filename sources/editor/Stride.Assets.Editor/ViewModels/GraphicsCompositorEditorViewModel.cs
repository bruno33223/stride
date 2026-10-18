// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Editor.AssetEditors.GraphicsCompositorEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Rendering;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<GraphicsCompositorViewModel>]
public class GraphicsCompositorEditorViewModel : AssetEditorViewModel<GraphicsCompositorViewModel>
{
    protected readonly GraphViewModelService ViewModelService;

    private readonly IObjectNode graphicsCompositorNode;
    private readonly IObjectNode renderStagesNode;
    private readonly IObjectNode renderFeaturesNode;
    private readonly IObjectNode cameraSlotsNode;
    private readonly IObjectNode sharedRenderersNode;

    private RenderStageViewModel? selectedRenderStage;
    private RenderFeatureViewModel? selectedRenderFeature;
    private GraphicsCompositorCameraSlotsViewModel? selectedCameraSlot;
    private IGraphicsCompositorBlockViewModel? selectedBlock;
    private bool isInitialized;

    public GraphicsCompositorEditorViewModel([NotNull] GraphicsCompositorViewModel graphicsCompositor)
        : base(graphicsCompositor)
    {
        // Create the service needed to manage observable view models
        ViewModelService = new GraphViewModelService(Session.AssetNodeContainer);

        // Update the service provider of this view model
        ServiceProvider = new ViewModelServiceProvider(ServiceProvider, ViewModelService.Yield());

        // Get quantum nodes
        graphicsCompositorNode = Session.AssetNodeContainer.GetNode(graphicsCompositor.Asset);
        renderStagesNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.RenderStages)].Target;
        renderFeaturesNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.RenderFeatures)].Target;
        cameraSlotsNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.Cameras)].Target;
        sharedRenderersNode = graphicsCompositorNode[nameof(GraphicsCompositorAsset.SharedRenderers)].Target;

        // Setup commands
        DeleteSelectionCommand = new AnonymousCommand(ServiceProvider, DeleteSelection);

        AddNewRenderStageCommand = new AnonymousCommand(ServiceProvider, AddNewRenderStage);
        RemoveSelectedRenderStagesCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedRenderStages);
        MoveUpRenderStageCommand = new AnonymousCommand(ServiceProvider, MoveUpRenderStage);
        MoveDownRenderStageCommand = new AnonymousCommand(ServiceProvider, MoveDownRenderStage);

        AddNewRenderFeatureCommand = new AnonymousCommand<object?>(ServiceProvider, AddNewRenderFeature);
        RemoveSelectedRenderFeaturesCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedRenderFeatures);
        MoveUpRenderFeatureCommand = new AnonymousCommand(ServiceProvider, MoveUpRenderFeature);
        MoveDownRenderFeatureCommand = new AnonymousCommand(ServiceProvider, MoveDownRenderFeature);

        AddNewCameraSlotCommand = new AnonymousCommand(ServiceProvider, AddNewCameraSlot);
        RemoveSelectedCameraSlotsCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedCameraSlots);
        MoveUpCameraSlotCommand = new AnonymousCommand(ServiceProvider, MoveUpCameraSlot);
        MoveDownCameraSlotCommand = new AnonymousCommand(ServiceProvider, MoveDownCameraSlot);

        RemoveSelectedSharedRenderersCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedSharedRenderers);
        MoveUpSharedRendererCommand = new AnonymousCommand(ServiceProvider, MoveUpSharedRenderer);
        MoveDownSharedRendererCommand = new AnonymousCommand(ServiceProvider, MoveDownSharedRenderer);

        AddForwardRendererCommand = new AnonymousCommand(ServiceProvider, AddForwardRenderer);
        AddPostProcessingEffectsCommand = new AnonymousCommand(ServiceProvider, AddPostProcessingEffects);

        // Initialize content immediately
        InitializeContent();
    }

    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    public ObservableList<RenderStageViewModel> RenderStages { get; } = [];

    public ObservableList<RenderFeatureViewModel> RenderFeatures { get; } = [];
    public ObservableList<AbstractNodeType> RenderFeatureTypes { get; } = [];

    public ObservableList<GraphicsCompositorCameraSlotsViewModel> CameraSlots { get; } = [];

    public ObservableList<IGraphicsCompositorBlockViewModel> Blocks { get; } = [];

    public IObservableCollection<IGraphicsCompositorBlockViewModel> SelectedSharedRenderers { get; } = new ObservableList<IGraphicsCompositorBlockViewModel>();

    public IObservableCollection<IGraphicsCompositorLinkViewModel> SelectedRendererLinks { get; } = new ObservableList<IGraphicsCompositorLinkViewModel>();

    public ObservableList<object> SelectedRenderStages { get; } = [];

    public ObservableList<object> SelectedRenderFeatures { get; } = [];

    public ObservableList<object> SelectedCameraSlots { get; } = [];

    public ObservableList<SharedRendererFactoryViewModel> SharedRendererFactories { get; } = [];

    public RenderStageViewModel? SelectedRenderStage
    {
        get => selectedRenderStage;
        set
        {
            if (SetValue(ref selectedRenderStage, value))
            {
                SelectedRenderStages.Clear();
                if (value != null)
                    SelectedRenderStages.Add(value);
            }
        }
    }

    public RenderFeatureViewModel? SelectedRenderFeature
    {
        get => selectedRenderFeature;
        set
        {
            if (SetValue(ref selectedRenderFeature, value))
            {
                SelectedRenderFeatures.Clear();
                if (value != null)
                    SelectedRenderFeatures.Add(value);
            }
        }
    }

    public GraphicsCompositorCameraSlotsViewModel? SelectedCameraSlot
    {
        get => selectedCameraSlot;
        set
        {
            if (SetValue(ref selectedCameraSlot, value))
            {
                SelectedCameraSlots.Clear();
                if (value != null)
                    SelectedCameraSlots.Add(value);
            }
        }
    }

    public IGraphicsCompositorBlockViewModel? SelectedBlock
    {
        get => selectedBlock;
        set
        {
            if (SetValue(ref selectedBlock, value))
            {
                SelectedSharedRenderers.Clear();
                if (value != null)
                    SelectedSharedRenderers.Add(value);
            }
        }
    }

    public ISceneRenderer? GameRenderer => Asset.Asset.Game;
    public ISceneRenderer? SingleViewRenderer => Asset.Asset.SingleView;
    public ISceneRenderer? EditorRenderer => Asset.Asset.Editor;

    public ICommandBase DeleteSelectionCommand { get; }

    public ICommandBase AddNewRenderStageCommand { get; }
    public ICommandBase RemoveSelectedRenderStagesCommand { get; }
    public ICommandBase MoveUpRenderStageCommand { get; }
    public ICommandBase MoveDownRenderStageCommand { get; }

    public ICommandBase AddNewRenderFeatureCommand { get; }
    public ICommandBase RemoveSelectedRenderFeaturesCommand { get; }
    public ICommandBase MoveUpRenderFeatureCommand { get; }
    public ICommandBase MoveDownRenderFeatureCommand { get; }

    public ICommandBase AddNewCameraSlotCommand { get; }
    public ICommandBase RemoveSelectedCameraSlotsCommand { get; }
    public ICommandBase MoveUpCameraSlotCommand { get; }
    public ICommandBase MoveDownCameraSlotCommand { get; }

    public ICommandBase RemoveSelectedSharedRenderersCommand { get; }
    public ICommandBase MoveUpSharedRendererCommand { get; }
    public ICommandBase MoveDownSharedRendererCommand { get; }

    public ICommandBase AddForwardRendererCommand { get; }
    public ICommandBase AddPostProcessingEffectsCommand { get; }

    public Task<bool> Initialize()
    {
        InitializeContent();
        return Task.FromResult(true);
    }

    private void InitializeContent()
    {
        if (isInitialized)
            return;
        isInitialized = true;

        // Add a block that represents entry points
        var entryPointOutputs = new[]
        {
            nameof(GraphicsCompositorAsset.Game),
            nameof(GraphicsCompositorAsset.SingleView),
            nameof(GraphicsCompositorAsset.Editor)
        };
        var entryPoint = new EntryPointBlockViewModel(this, graphicsCompositorNode, entryPointOutputs);
        entryPoint.Initialize();
        entryPoint.UpdateSlots();
        Blocks.Add(entryPoint);

        // Add blocks
        sharedRenderersNode.ItemChanged += SharedRenderersChanged;
        foreach (var sharedRendererNode in sharedRenderersNode.ItemReferences)
        {
            var sharedRenderer = (ISharedRenderer)sharedRendererNode.TargetNode.Retrieve();
            AddSharedRendererViewModel(sharedRenderer);
        }

        // Now that we have all blocks with all slots, we can update all links
        foreach (var block in Blocks)
        {
            foreach (var slot in block.OutputSlots.Cast<GraphicsCompositorSlotViewModel>())
            {
                slot.UpdateLink();
            }
        }

        // Add render stages
        renderStagesNode.ItemChanged += RenderStagesChanged;
        foreach (var renderStage in (IEnumerable<RenderStage>)renderStagesNode.Retrieve())
            RenderStages.Add(new RenderStageViewModel(this, renderStage));

        // Add render features
        renderFeaturesNode.ItemChanged += RenderFeaturesChanged;
        foreach (var renderFeature in (IEnumerable<RenderFeature>)renderFeaturesNode.Retrieve())
            RenderFeatures.Add(new RenderFeatureViewModel(this, renderFeature));

        // Add camera slots
        cameraSlotsNode.ItemChanged += CamerasSlotsChanged;
        foreach (var cameraSlot in (IEnumerable<SceneCameraSlot>)cameraSlotsNode.Retrieve())
            CameraSlots.Add(new GraphicsCompositorCameraSlotsViewModel(this, cameraSlot));

        // Update property grid on selected events
        SelectedRenderStages.CollectionChanged += SelectionChanged;
        SelectedRenderFeatures.CollectionChanged += SelectionChanged;
        SelectedCameraSlots.CollectionChanged += SelectionChanged;
        SelectedSharedRenderers.CollectionChanged += SelectionChanged;
        SelectedRendererLinks.CollectionChanged += SelectionChanged;

        // Make selection scope exclusives
        var selectionService = ServiceProvider.TryGet<SelectionService>();
        if (selectionService != null)
        {
            selectionService.RegisterSelectionScope(id => RenderStages.FirstOrDefault(x => x.Id == id), obj => (obj as RenderStageViewModel)?.Id, SelectedRenderStages);
            selectionService.RegisterSelectionScope(id => RenderFeatures.FirstOrDefault(x => x.Id == id), obj => (obj as RenderFeatureViewModel)?.Id, SelectedRenderFeatures);
            selectionService.RegisterSelectionScope(id => CameraSlots.FirstOrDefault(x => x.Id == id), obj => (obj as GraphicsCompositorCameraSlotsViewModel)?.Id, SelectedCameraSlots);
            selectionService.RegisterSelectionScope(id => Blocks.OfType<GraphicsCompositorBlockViewModel>().FirstOrDefault(x => x.Id == id), obj => (obj as GraphicsCompositorBlockViewModel)?.Id, SelectedSharedRenderers);
        }

        // Update available types for factories
        UpdateAvailableTypes();
        AssemblyRegistry.AssemblyRegistered += AssembliesUpdated;
        AssemblyRegistry.AssemblyUnregistered += AssembliesUpdated;
    }

    [NotNull]
    internal IGraphicsCompositorLinkViewModel CreateLink([NotNull] IGraphicsCompositorSlotViewModel source, [NotNull] IGraphicsCompositorSlotViewModel target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        return new GraphicsCompositorLinkViewModel(ServiceProvider, (GraphicsCompositorSlotViewModel)source, (GraphicsCompositorSlotViewModel)target);
    }

    public GraphicsCompositorBlockViewModel? GetBlock(object? value)
    {
        if (value == null)
            return null;

        foreach (var block in Blocks)
        {
            if (block is SharedRendererBlockViewModel sharedRenderedBlock && sharedRenderedBlock.GetSharedRenderer() == value)
            {
                return sharedRenderedBlock;
            }
        }

        return null;
    }

    private async void SelectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not IEnumerable<object> collection)
            return;

        var selectedItems = collection.OfType<IPropertyProviderViewModel>().ToList();
        EditorProperties.UpdateTypeAndName(selectedItems, x => ComputeSelectionName(x, false), x => x.ToString(), ComputeSelectionName(selectedItems.FirstOrDefault(), true));
        await EditorProperties.GenerateSelectionPropertiesAsync(selectedItems);
    }

    private static string? ComputeSelectionName(object? selectedObject, bool plural)
    {
        if (selectedObject is SharedRendererBlockViewModel)
            return !plural ? "Shared renderer" : "shared renderers";
        if (selectedObject is EntryPointBlockViewModel)
            return !plural ? "Entry points" : "entry points";
        if (selectedObject is IGraphicsCompositorLinkViewModel)
            return !plural ? "Renderer link" : "renderer links";
        if (selectedObject is RenderStageViewModel)
            return !plural ? "Render stage" : "render stages";
        if (selectedObject is RenderFeatureViewModel)
            return !plural ? "Render feature" : "render features";
        if (selectedObject is GraphicsCompositorCameraSlotsViewModel)
            return !plural ? "Camera slot" : "camera slots";

        return null;
    }

    public override void Destroy()
    {
        var selectionService = ServiceProvider.TryGet<SelectionService>();
        if (selectionService != null)
        {
            selectionService.UnregisterSelectionScope(SelectedRenderStages);
            selectionService.UnregisterSelectionScope(SelectedRenderFeatures);
            selectionService.UnregisterSelectionScope(SelectedCameraSlots);
            selectionService.UnregisterSelectionScope(SelectedSharedRenderers);
        }

        SelectedRenderStages.CollectionChanged -= SelectionChanged;
        SelectedRenderFeatures.CollectionChanged -= SelectionChanged;
        SelectedCameraSlots.CollectionChanged -= SelectionChanged;
        SelectedSharedRenderers.CollectionChanged -= SelectionChanged;
        SelectedRendererLinks.CollectionChanged -= SelectionChanged;

        sharedRenderersNode.ItemChanged -= SharedRenderersChanged;
        renderStagesNode.ItemChanged -= RenderStagesChanged;
        renderFeaturesNode.ItemChanged -= RenderFeaturesChanged;
        cameraSlotsNode.ItemChanged -= CamerasSlotsChanged;

        AssemblyRegistry.AssemblyRegistered -= AssembliesUpdated;
        AssemblyRegistry.AssemblyUnregistered -= AssembliesUpdated;

        Blocks.Cast<GraphicsCompositorBlockViewModel>().ForEach(x => x.Destroy());
        base.Destroy();
    }

    private void DeleteSelection()
    {
        RemoveSelectedSharedRenderers();
        RemoveSelectedRenderFeatures();
        RemoveSelectedRenderStages();
        RemoveSelectedCameraSlots();
        RemoveSelectedLinks();
    }

    private void RemoveSelectedLinks()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var toRemove = SelectedRendererLinks.Cast<GraphicsCompositorLinkViewModel>().ToList();
        SelectedRendererLinks.Clear();

        foreach (var viewModel in toRemove)
        {
            viewModel.SourceSlot.ClearLink();
            viewModel.Destroy();
            viewModel.SourceSlot.Links.Remove(viewModel);
            viewModel.TargetSlot.Links.Remove(viewModel);
        }

        UndoRedoService.SetName(transaction, "Delete link(s)");
    }

    private void RemoveSelectedSharedRenderers()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var toRemove = SelectedSharedRenderers.OfType<SharedRendererBlockViewModel>().ToList();
        SelectedSharedRenderers.Clear();

        foreach (var viewModel in toRemove)
        {
            var index = Asset.Asset.SharedRenderers.IndexOf(viewModel.GetSharedRenderer());
            if (index < 0)
                continue;

            var itemIndex = new NodeIndex(index);
            sharedRenderersNode.Remove(viewModel.GetSharedRenderer(), itemIndex);
        }

        UndoRedoService.SetName(transaction, "Delete renderer(s)");
    }

    private void MoveUpSharedRenderer()
    {
        var block = (SelectedBlock as SharedRendererBlockViewModel) ?? SelectedSharedRenderers.OfType<SharedRendererBlockViewModel>().FirstOrDefault();
        if (block == null) return;
        var index = Asset.Asset.SharedRenderers.IndexOf(block.GetSharedRenderer());
        if (index > 0)
        {
            MoveSharedRenderer(index, index - 1);
        }
    }

    private void MoveDownSharedRenderer()
    {
        var block = (SelectedBlock as SharedRendererBlockViewModel) ?? SelectedSharedRenderers.OfType<SharedRendererBlockViewModel>().FirstOrDefault();
        if (block == null) return;
        var index = Asset.Asset.SharedRenderers.IndexOf(block.GetSharedRenderer());
        if (index >= 0 && index < Asset.Asset.SharedRenderers.Count - 1)
        {
            MoveSharedRenderer(index, index + 1);
        }
    }

    private void MoveSharedRenderer(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Asset.Asset.SharedRenderers.Count || newIndex < 0 || newIndex >= Asset.Asset.SharedRenderers.Count || oldIndex == newIndex)
            return;

        var renderer = Asset.Asset.SharedRenderers[oldIndex];
        using var transaction = UndoRedoService.CreateTransaction();
        sharedRenderersNode.Remove(renderer, new NodeIndex(oldIndex));
        sharedRenderersNode.Add(renderer, new NodeIndex(newIndex));
        UndoRedoService.SetName(transaction, "Reorder renderer");
    }

    private void AddForwardRenderer()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var renderer = new ForwardRenderer();
        sharedRenderersNode.Add(renderer);
        UndoRedoService.SetName(transaction, "Add Forward Renderer");
    }

    private void AddPostProcessingEffects()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var renderer = new PostProcessingEffects();
        sharedRenderersNode.Add(renderer);
        UndoRedoService.SetName(transaction, "Add Post Processing Effects");
    }

    private void AddNewRenderStage()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var renderStage = new RenderStage();
        renderStagesNode.Add(renderStage);
        UndoRedoService.SetName(transaction, "Create new render stage");
    }

    private void RemoveSelectedRenderStages()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var toRemove = SelectedRenderStages.Cast<RenderStageViewModel>().Select(x => x.RenderStage).ToList();
        SelectedRenderStages.Clear();

        foreach (var renderStage in toRemove)
        {
            var index = Asset.Asset.RenderStages.IndexOf(renderStage);
            if (index < 0)
                continue;

            Asset.PropertyGraph.ClearReferencesToObjects(renderStage.Id.Yield());
            var itemIndex = new NodeIndex(index);
            renderStagesNode.Remove(renderStage, itemIndex);
        }

        UndoRedoService.SetName(transaction, "Delete render stage(s)");
    }

    private void MoveUpRenderStage()
    {
        var stage = SelectedRenderStage ?? SelectedRenderStages.OfType<RenderStageViewModel>().FirstOrDefault();
        if (stage == null) return;
        var index = RenderStages.IndexOf(stage);
        if (index > 0)
        {
            MoveRenderStage(index, index - 1);
            SelectedRenderStage = RenderStages[index - 1];
        }
    }

    private void MoveDownRenderStage()
    {
        var stage = SelectedRenderStage ?? SelectedRenderStages.OfType<RenderStageViewModel>().FirstOrDefault();
        if (stage == null) return;
        var index = RenderStages.IndexOf(stage);
        if (index >= 0 && index < RenderStages.Count - 1)
        {
            MoveRenderStage(index, index + 1);
            SelectedRenderStage = RenderStages[index + 1];
        }
    }

    private void MoveRenderStage(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= RenderStages.Count || newIndex < 0 || newIndex >= RenderStages.Count || oldIndex == newIndex)
            return;

        var stage = RenderStages[oldIndex].RenderStage;
        using var transaction = UndoRedoService.CreateTransaction();
        renderStagesNode.Remove(stage, new NodeIndex(oldIndex));
        renderStagesNode.Add(stage, new NodeIndex(newIndex));
        UndoRedoService.SetName(transaction, "Reorder render stage");
    }

    private void AddNewCameraSlot()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var cameraSlot = new SceneCameraSlot();
        cameraSlotsNode.Add(cameraSlot);
        UndoRedoService.SetName(transaction, "Create new camera slot");
    }

    private void RemoveSelectedCameraSlots()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var toRemove = SelectedCameraSlots.Cast<GraphicsCompositorCameraSlotsViewModel>().Select(x => x.CameraSlot).ToList();
        SelectedCameraSlots.Clear();

        foreach (var viewModel in toRemove)
        {
            var index = Asset.Asset.Cameras.IndexOf(viewModel);
            if (index < 0)
                continue;

            var itemIndex = new NodeIndex(index);
            cameraSlotsNode.Remove(viewModel, itemIndex);
        }

        UndoRedoService.SetName(transaction, "Delete camera slot(s)");
    }

    private void MoveUpCameraSlot()
    {
        var slot = SelectedCameraSlot ?? SelectedCameraSlots.OfType<GraphicsCompositorCameraSlotsViewModel>().FirstOrDefault();
        if (slot == null) return;
        var index = CameraSlots.IndexOf(slot);
        if (index > 0)
        {
            MoveCameraSlot(index, index - 1);
            SelectedCameraSlot = CameraSlots[index - 1];
        }
    }

    private void MoveDownCameraSlot()
    {
        var slot = SelectedCameraSlot ?? SelectedCameraSlots.OfType<GraphicsCompositorCameraSlotsViewModel>().FirstOrDefault();
        if (slot == null) return;
        var index = CameraSlots.IndexOf(slot);
        if (index >= 0 && index < CameraSlots.Count - 1)
        {
            MoveCameraSlot(index, index + 1);
            SelectedCameraSlot = CameraSlots[index + 1];
        }
    }

    private void MoveCameraSlot(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= CameraSlots.Count || newIndex < 0 || newIndex >= CameraSlots.Count || oldIndex == newIndex)
            return;

        var slot = CameraSlots[oldIndex].CameraSlot;
        using var transaction = UndoRedoService.CreateTransaction();
        cameraSlotsNode.Remove(slot, new NodeIndex(oldIndex));
        cameraSlotsNode.Add(slot, new NodeIndex(newIndex));
        UndoRedoService.SetName(transaction, "Reorder camera slot");
    }

    private void AssembliesUpdated(object? sender, AssemblyRegisteredEventArgs e)
    {
        UpdateAvailableTypes();
    }

    private void AddNewRenderFeature(object? parameter)
    {
        var abstractNodeType = parameter as AbstractNodeType ?? RenderFeatureTypes.FirstOrDefault();
        if (abstractNodeType == null)
            return;

        using var transaction = UndoRedoService.CreateTransaction();
        var renderFeature = abstractNodeType.GenerateValue(null);
        renderFeaturesNode.Add(renderFeature);
        UndoRedoService.SetName(transaction, "Create new render feature");
    }

    private void RemoveSelectedRenderFeatures()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var toRemove = SelectedRenderFeatures.Cast<RenderFeatureViewModel>().Select(x => x.RenderFeature).ToList();
        SelectedRenderFeatures.Clear();

        foreach (var renderFeature in toRemove)
        {
            var index = Asset.Asset.RenderFeatures.IndexOf(renderFeature);
            if (index < 0)
                continue;

            var itemIndex = new NodeIndex(index);
            renderFeaturesNode.Remove(renderFeature, itemIndex);
        }

        UndoRedoService.SetName(transaction, "Delete render feature(s)");
    }

    private void MoveUpRenderFeature()
    {
        var feature = SelectedRenderFeature ?? SelectedRenderFeatures.OfType<RenderFeatureViewModel>().FirstOrDefault();
        if (feature == null) return;
        var index = RenderFeatures.IndexOf(feature);
        if (index > 0)
        {
            MoveRenderFeature(index, index - 1);
            SelectedRenderFeature = RenderFeatures[index - 1];
        }
    }

    private void MoveDownRenderFeature()
    {
        var feature = SelectedRenderFeature ?? SelectedRenderFeatures.OfType<RenderFeatureViewModel>().FirstOrDefault();
        if (feature == null) return;
        var index = RenderFeatures.IndexOf(feature);
        if (index >= 0 && index < RenderFeatures.Count - 1)
        {
            MoveRenderFeature(index, index + 1);
            SelectedRenderFeature = RenderFeatures[index + 1];
        }
    }

    private void MoveRenderFeature(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= RenderFeatures.Count || newIndex < 0 || newIndex >= RenderFeatures.Count || oldIndex == newIndex)
            return;

        var feature = RenderFeatures[oldIndex].RenderFeature;
        using var transaction = UndoRedoService.CreateTransaction();
        renderFeaturesNode.Remove(feature, new NodeIndex(oldIndex));
        renderFeaturesNode.Add(feature, new NodeIndex(newIndex));
        UndoRedoService.SetName(transaction, "Reorder render feature");
    }

    private void SharedRenderersChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
                break;

            case ContentChangeType.CollectionAdd:
                {
                    var sharedRenderer = (ISharedRenderer)e.NewValue!;
                    AddSharedRendererViewModel(sharedRenderer);
                    foreach (var block in Blocks.OfType<GraphicsCompositorBlockViewModel>())
                    {
                        block.UpdateOutgoingLinks();
                    }
                    break;
                }
            case ContentChangeType.CollectionRemove:
                {
                    var sharedRenderer = (ISharedRenderer)e.OldValue!;
                    RemoveSharedRendererViewModel(sharedRenderer);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RenderStagesChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
                break;

            case ContentChangeType.CollectionAdd:
                RenderStages.Insert(e.Index.Int, new RenderStageViewModel(this, (RenderStage)e.NewValue!));
                break;

            case ContentChangeType.CollectionRemove:
                RenderStages.RemoveAt(e.Index.Int);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void RenderFeaturesChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
                break;

            case ContentChangeType.CollectionAdd:
                RenderFeatures.Insert(e.Index.Int, new RenderFeatureViewModel(this, (RenderFeature)e.NewValue!));
                break;

            case ContentChangeType.CollectionRemove:
                RenderFeatures.RemoveAt(e.Index.Int);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CamerasSlotsChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
                break;

            case ContentChangeType.CollectionAdd:
                CameraSlots.Insert(e.Index.Int, new GraphicsCompositorCameraSlotsViewModel(this, (SceneCameraSlot)e.NewValue!));
                break;

            case ContentChangeType.CollectionRemove:
                CameraSlots.ElementAt(e.Index.Int).Dispose();
                CameraSlots.RemoveAt(e.Index.Int);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AddSharedRendererViewModel(ISharedRenderer sharedRenderer)
    {
        var viewModel = new SharedRendererBlockViewModel(this, sharedRenderer);
        viewModel.Initialize();
        viewModel.UpdateSlots();
        Blocks.Add(viewModel);
    }

    private void RemoveSharedRendererViewModel(ISharedRenderer sharedRenderer)
    {
        Asset.PropertyGraph.ClearReferencesToObjects(sharedRenderer.Id.Yield());

        var block = GetBlock(sharedRenderer);
        if (block != null)
        {
            Blocks.Remove(block);
            block.Destroy();
        }
    }

    private void UpdateAvailableTypes()
    {
        RenderFeatureTypes.Clear();
        RenderFeatureTypes.AddRange(AbstractNodeType.GetInheritedInstantiableTypes(typeof(RootRenderFeature)));

        var sharedRendererFactories = new List<SharedRendererFactoryViewModel>();
        foreach (var rendererType in typeof(ISharedRenderer).GetInheritedInstantiableTypes())
        {
            if (TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<NonInstantiableAttribute>(rendererType) != null)
                continue;
            if (TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ObsoleteAttribute>(rendererType) != null)
                continue;

            sharedRendererFactories.Add(new SharedRendererFactoryViewModel(ServiceProvider, sharedRenderersNode, rendererType));
        }
        sharedRendererFactories.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        SharedRendererFactories.Clear();
        SharedRendererFactories.AddRange(sharedRendererFactories);
    }
}
