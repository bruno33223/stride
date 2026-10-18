// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Transactions;

namespace Stride.Assets.Editor.ViewModels;

public class VisualScriptMethodEditorViewModel : DispatcherViewModel, IAddChildViewModel
{
    private readonly VisualScriptEditorViewModel editor;
    private readonly VisualScriptMethodViewModel method;

    private readonly IObjectNode blocksNode;
    private readonly IObjectNode linksNode;
    private readonly IObjectNode parametersNode;

    private readonly ObservableList<BlockNodeVertex> blocks = new();
    private readonly Dictionary<Block, BlockNodeVertex> blockMapping = new();

    private readonly ObservableList<LinkNodeEdge> links = new();
    private readonly Dictionary<Link, LinkNodeEdge> linkMapping = new();

    private GraphViewModel? parameters;
    private Int2 blockCreationPosition;

    public VisualScriptMethodEditorViewModel(VisualScriptEditorViewModel editor, VisualScriptMethodViewModel method)
        : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
    {
        this.editor = editor;
        this.method = method;

        var methodNode = editor.Session.AssetNodeContainer.GetNode(method.Method);
        blocksNode = methodNode[nameof(method.Method.Blocks)].Target;
        linksNode = methodNode[nameof(method.Method.Links)].Target;
        parametersNode = methodNode[nameof(method.Method.Parameters)].Target;

        DeleteSelectionCommand = new AnonymousCommand(ServiceProvider, DeleteSelection);
        ContextMenuOpeningCommand = new AnonymousCommand<object?>(ServiceProvider, ContextMenuOpening);
        RunBlockTemplateCommand = new AnonymousCommand<object?>(ServiceProvider, RunBlockTemplate);

        AddNewParameterCommand = new AnonymousCommand(ServiceProvider, AddNewParameter);
        RemoveSelectedParametersCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedParameters);
    }

    public async Task<bool> Initialize()
    {
        SelectedBlocks.CollectionChanged += SelectedBlocks_CollectionChanged;
        SelectedLinks.CollectionChanged += SelectedLinks_CollectionChanged;

        parameters = GraphViewModel.Create(ServiceProvider, new[] { new SinglePropertyProvider(parametersNode) });

        using (var transaction = editor.UndoRedoService.CreateTransaction())
        {
            blocksNode.ItemChanged += BlocksContentChanged;
            foreach (var block in (IEnumerable<Block>)blocksNode.Retrieve())
                await AddBlockViewModel(block);

            linksNode.ItemChanged += LinksContentChanged;
            foreach (var link in (IEnumerable<Link>)linksNode.Retrieve())
                AddLinkViewModel(link);

            editor.UndoRedoService.SetName(transaction, "Update graph asset");
        }

        editor.Diagnostics.CollectionChanged += Diagnostics_CollectionChanged;

        return true;
    }

    public override void Destroy()
    {
        editor.Diagnostics.CollectionChanged -= Diagnostics_CollectionChanged;

        blocksNode.ItemChanged -= BlocksContentChanged;
        linksNode.ItemChanged -= LinksContentChanged;

        SelectedBlocks.CollectionChanged -= SelectedBlocks_CollectionChanged;
        SelectedLinks.CollectionChanged -= SelectedLinks_CollectionChanged;

        parameters?.Destroy();

        base.Destroy();
    }

    public VisualScriptEditorViewModel Editor => editor;

    public VisualScriptMethodViewModel Method => method;

    public ObservableList<BlockNodeVertex> Blocks => blocks;

    public ObservableList<LinkNodeEdge> Links => links;

    public ObservableCollection<BlockNodeVertex> SelectedBlocks { get; } = new();

    public ObservableCollection<LinkNodeEdge> SelectedLinks { get; } = new();

    public ICommandBase DeleteSelectionCommand { get; }

    public ICommandBase ContextMenuOpeningCommand { get; }

    public ICommandBase RunBlockTemplateCommand { get; }

    public ICommandBase AddNewParameterCommand { get; }

    public ICommandBase RemoveSelectedParametersCommand { get; }

    public NodeViewModel? Parameters => parameters?.RootNode;

    public ObservableList<object> SelectedParameters { get; } = new();

    private void AddNewParameter()
    {
        var count = Parameters?.Children.Count ?? method.Method.Parameters.Count;
        var parameter = new Parameter("bool", $"variable{count}");
        method.AddParameter(parameter);
    }

    private void RemoveSelectedParameters()
    {
        using var transaction = editor.UndoRedoService.CreateTransaction();
        var parametersToDelete = SelectedParameters
            .Select(x => x is NodeViewModel nvm ? nvm.NodeValue as Parameter : x as Parameter)
            .Where(x => x != null)
            .ToList();

        foreach (var parameter in parametersToDelete)
        {
            method.RemoveParameter(parameter!);
        }

        editor.UndoRedoService.SetName(transaction, "Delete variable(s)");
    }

    public void FinalizeMoves()
    {
        using var transaction = editor.UndoRedoService.CreateTransaction();
        foreach (var selectedVertex in SelectedBlocks)
        {
            selectedVertex.FinalizeMove();
        }

        editor.UndoRedoService.SetName(transaction, "Moved graph elements");
    }

    #region Drag & Drop

    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        if (children.Count != 1)
        {
            message = "Only one element can be dropped at a time";
            return false;
        }

        var child = children.Single();
        if (child is NodeViewModel nodeVm && nodeVm.NodeValue is Symbol variable)
        {
            message = $"Add variable {variable}";
            return true;
        }

        message = "Can only drop variables";
        return false;
    }

    async void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        var child = children.Single();
        if (child is NodeViewModel nodeVm && nodeVm.NodeValue is Symbol symbol)
        {
            var service = Editor.VisualScriptViewModelService;
            if (service != null)
            {
                var block = await service.TransformVariableIntoBlock(symbol);
                if (block != null)
                {
                    method.AddBlock(block);
                }
            }
        }
    }

    #endregion

    private void SelectedBlocks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (BlockNodeVertex item in e.NewItems)
            {
                if (!item.IsSelected)
                    item.IsSelected = true;
            }
        }
        if (e.OldItems != null)
        {
            foreach (BlockNodeVertex item in e.OldItems)
            {
                if (item.IsSelected)
                    item.IsSelected = false;
            }
        }

        editor.EditorProperties.UpdateTypeAndName(SelectedBlocks, _ => "Block", x => x.ViewModel.Title, "blocks");
        editor.EditorProperties.GenerateSelectionPropertiesAsync(SelectedBlocks.Select(x => x.ViewModel)).Forget();
    }

    private void SelectedLinks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (LinkNodeEdge item in e.NewItems)
            {
                if (!item.IsSelected)
                    item.IsSelected = true;
            }
        }
        if (e.OldItems != null)
        {
            foreach (LinkNodeEdge item in e.OldItems)
            {
                if (item.IsSelected)
                    item.IsSelected = false;
            }
        }

        editor.EditorProperties.UpdateTypeAndName(SelectedLinks, _ => "Link", x => $"{x.Source.Title} -> {x.Target.Title}", "links");
        editor.EditorProperties.GenerateSelectionPropertiesAsync(SelectedLinks.Select(x => x.ViewModel)).Forget();
    }

    private void DeleteSelection()
    {
        using var transaction = editor.UndoRedoService.CreateTransaction();

        foreach (var link in SelectedLinks.ToArray())
            method.RemoveLink(link.ViewModel.Link);

        foreach (var block in SelectedBlocks.ToArray())
            method.RemoveBlock(block.ViewModel.Block);

        SelectedLinks.Clear();
        SelectedBlocks.Clear();

        editor.UndoRedoService.SetName(transaction, "Deleting selected graph elements");
    }

    public void ContextMenuOpening(object? mousePosition)
    {
        if (mousePosition is Vector2 vec)
            blockCreationPosition = new Int2((int)Math.Round(vec.X), (int)Math.Round(vec.Y));
        else if (mousePosition is Int2 i)
            blockCreationPosition = i;
    }

    private async void RunBlockTemplate(object? templateObj)
    {
        IBlockFactory? blockFactory = null;
        if (templateObj is BlockTemplateDescriptionViewModel vm)
            blockFactory = vm.BlockFactory;
        else if (templateObj is IBlockFactory factory)
            blockFactory = factory;

        if (blockFactory == null)
            return;

        var block = await blockFactory.Create();
        if (block == null)
            return;

        block.Position = blockCreationPosition;
        method.AddBlock(block);
    }

    private void AddLinkViewModel(Link link)
    {
        if (!blockMapping.TryGetValue(link.Source.Owner, out var source)
            || !blockMapping.TryGetValue(link.Target.Owner, out var target))
            throw new InvalidOperationException("Could not find source or target block when creating a link");

        if (!source.ViewModel.Slots.TryGetValue(link.Source, out var linkSource)
            || !target.ViewModel.Slots.TryGetValue(link.Target, out var linkTarget))
            throw new InvalidOperationException("Could not find source or target slot when creating a link");

        var viewModel = new VisualScriptLinkViewModel(this, link, linkSource, linkTarget);
        var linkNodeEdge = new LinkNodeEdge(viewModel, source, target);

        foreach (var diagnostic in editor.Diagnostics.Where(x => x.LinkId.HasValue && x.LinkId.Value == link.Id))
            linkNodeEdge.ViewModel.Diagnostics.Add(diagnostic);

        links.Add(linkNodeEdge);
        linkMapping.Add(link, linkNodeEdge);

        linkSource.Links.Add(viewModel);
        linkTarget.Links.Add(viewModel);
    }

    private void RemoveLinkViewModel(Link link)
    {
        if (!linkMapping.TryGetValue(link, out var linkNodeEdge))
        {
            throw new InvalidOperationException("Could not find link to delete");
        }

        var viewModel = linkNodeEdge.ViewModel;
        viewModel.SourceSlot.Links.Remove(viewModel);
        viewModel.TargetSlot.Links.Remove(viewModel);

        links.Remove(linkNodeEdge);
        linkMapping.Remove(link);
    }

    private async Task AddBlockViewModel(Block block)
    {
        using var transaction = editor.UndoRedoService.CreateTransaction(TransactionFlags.KeepParentsAlive);

        var viewModel = new VisualScriptBlockViewModel(this, block);
        var blockNodeVertex = new BlockNodeVertex(viewModel);

        foreach (var diagnostic in editor.Diagnostics.Where(x => x.BlockId.HasValue && x.BlockId.Value == block.Id))
            viewModel.Diagnostics.Add(diagnostic);

        blocks.Add(blockNodeVertex);
        blockMapping.Add(block, blockNodeVertex);

        await this.method.RegenerateSlots(block);

        editor.UndoRedoService.SetName(transaction, "Added block");
    }

    private void RemoveBlockViewModel(Block block)
    {
        if (!blockMapping.TryGetValue(block, out var blockNodeVertex))
        {
            throw new InvalidOperationException("Could not find block to delete");
        }
        blocks.Remove(blockNodeVertex);
        blockMapping.Remove(block);
    }

    private void BlocksContentChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
                break;

            case ContentChangeType.CollectionAdd:
            {
                var block = (Block)e.NewValue!;
                AddBlockViewModel(block).Forget();
                break;
            }
            case ContentChangeType.CollectionRemove:
            {
                var block = (Block)e.OldValue!;
                RemoveBlockViewModel(block);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LinksContentChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
                break;

            case ContentChangeType.CollectionAdd:
            {
                var link = (Link)e.NewValue!;
                AddLinkViewModel(link);
                break;
            }
            case ContentChangeType.CollectionRemove:
            {
                var link = (Link)e.OldValue!;
                RemoveLinkViewModel(link);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Diagnostics_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                foreach (var block in blocks)
                    block.ViewModel.Diagnostics.Clear();
                foreach (var link in links)
                    link.ViewModel.Diagnostics.Clear();
                break;

            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    foreach (VisualScriptDiagnostic diagnostic in e.NewItems)
                    {
                        if (diagnostic.BlockId.HasValue)
                            FindBlockFromId(diagnostic.BlockId.Value)?.ViewModel.Diagnostics.Add(diagnostic);
                        if (diagnostic.LinkId.HasValue)
                            FindLinkFromId(diagnostic.LinkId.Value)?.ViewModel.Diagnostics.Add(diagnostic);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    foreach (VisualScriptDiagnostic diagnostic in e.OldItems)
                    {
                        if (diagnostic.BlockId.HasValue)
                            FindBlockFromId(diagnostic.BlockId.Value)?.ViewModel.Diagnostics.Remove(diagnostic);
                        if (diagnostic.LinkId.HasValue)
                            FindLinkFromId(diagnostic.LinkId.Value)?.ViewModel.Diagnostics.Remove(diagnostic);
                    }
                }
                break;
        }
    }

    private BlockNodeVertex? FindBlockFromId(Guid blockId)
    {
        if (method.Method.Blocks.TryGetValue(blockId, out var block)
            && blockMapping.TryGetValue(block, out var blockNode))
        {
            return blockNode;
        }

        return null;
    }

    private LinkNodeEdge? FindLinkFromId(Guid linkId)
    {
        if (method.Method.Links.TryGetValue(linkId, out var link)
            && linkMapping.TryGetValue(link, out var linkNode))
        {
            return linkNode;
        }

        return null;
    }
}
