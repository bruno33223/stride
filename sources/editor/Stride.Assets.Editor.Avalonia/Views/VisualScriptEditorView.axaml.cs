// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.CodeAnalysis;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum.ViewModels;
using Point = Avalonia.Point;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<VisualScriptEditorViewModel>]
public partial class VisualScriptEditorView : UserControl, IAssetEditorView, IVisualScriptViewModelService
{
    private BlockNodeVertex? draggedBlock;
    private Point dragStartMouse;
    private readonly Dictionary<BlockNodeVertex, Point> initialBlockPositions = new();
    private bool isDraggingBlock;

    private NodeViewModel? draggedVariableNode;
    private Point variableDragStartPoint;
    private bool isDraggingVariable;
    private Point lastDropPosition = new(200, 200);

    public VisualScriptEditorView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        // Wire modal symbol search dialog buttons
        BtnSelectSymbolSearch.Click += (_, _) => ConfirmSymbolSearch();
        BtnCancelSymbolSearch.Click += (_, _) => CancelSymbolSearch();
        BtnCloseSymbolSearch.Click += (_, _) => CancelSymbolSearch();

        // Clicking outside the card on the backdrop closes/cancels the modal
        SymbolSearchOverlay.PointerPressed += (s, e) =>
        {
            if (e.Source == SymbolSearchOverlay)
            {
                CancelSymbolSearch();
                e.Handled = true;
            }
        };

        // Text box keyboard navigation and shortcuts
        TxtSymbolSearch.KeyDown += OnSymbolSearchTextBoxKeyDown;

        // ListBox double-tap / selection shortcuts
        LstSymbolSearchResults.DoubleTapped += (_, _) => ConfirmSymbolSearch();
        LstSymbolSearchResults.KeyDown += OnSymbolSearchResultsKeyDown;

        // Wire 2D canvas mouse interactions
        GraphCanvasGrid.PointerPressed += OnGraphCanvasPointerPressed;
        GraphCanvasGrid.PointerMoved += OnGraphCanvasPointerMoved;
        GraphCanvasGrid.PointerReleased += OnGraphCanvasPointerReleased;

        // Wire overridable methods combo selection
        OverridableMethodsComboBox.SelectionChanged += OnOverridableMethodsSelectionChanged;

        // Wire selection synchronization for variables & parameters listboxes
        VariablesListBox.SelectionChanged += OnVariablesSelectionChanged;
        ParametersListBox.SelectionChanged += OnParametersSelectionChanged;

        // Wire variable & parameter drag-and-drop
        VariablesListBox.AddHandler(PointerPressedEvent, OnVariableListPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ParametersListBox.AddHandler(PointerPressedEvent, OnParameterListPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        AddHandler(PointerMovedEvent, OnRootPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, OnRootPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is VisualScriptEditorViewModel vm)
        {
            vm.VisualScriptViewModelService = this;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnOverridableMethodsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (OverridableMethodsComboBox.SelectedItem != null && DataContext is VisualScriptEditorViewModel vm)
        {
            var selected = OverridableMethodsComboBox.SelectedItem;
            OverridableMethodsComboBox.SelectedItem = null;
            if (vm.AddNewMethodCommand.CanExecute(selected))
            {
                vm.AddNewMethodCommand.Execute(selected);
            }
        }
    }

    private void OnVariablesSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is VisualScriptEditorViewModel vm)
        {
            if (e.RemovedItems != null)
            {
                foreach (var item in e.RemovedItems)
                {
                    if (item != null)
                        vm.SelectedProperties.Remove(item);
                }
            }
            if (e.AddedItems != null)
            {
                foreach (var item in e.AddedItems)
                {
                    if (item != null && !vm.SelectedProperties.Contains(item))
                        vm.SelectedProperties.Add(item);
                }
            }
        }
    }

    private void OnParametersSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is VisualScriptEditorViewModel vm && vm.VisibleMethod != null)
        {
            if (e.RemovedItems != null)
            {
                foreach (var item in e.RemovedItems)
                {
                    if (item != null)
                        vm.VisibleMethod.SelectedParameters.Remove(item);
                }
            }
            if (e.AddedItems != null)
            {
                foreach (var item in e.AddedItems)
                {
                    if (item != null && !vm.VisibleMethod.SelectedParameters.Contains(item))
                        vm.VisibleMethod.SelectedParameters.Add(item);
                }
            }
        }
    }

    #region Drag & Drop Variables to Graph Canvas
    private void OnVariableListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox)
            return; // Allow inline editing without initiating drag

        var sourceVisual = e.Source as Visual;
        NodeViewModel? node = null;
        while (sourceVisual != null && sourceVisual != VariablesListBox)
        {
            if (sourceVisual.DataContext is NodeViewModel nvm)
            {
                node = nvm;
                break;
            }
            sourceVisual = sourceVisual.GetVisualParent();
        }

        if (node != null)
        {
            draggedVariableNode = node;
            variableDragStartPoint = e.GetPosition(this);
            isDraggingVariable = false;
        }
    }

    private void OnParameterListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox)
            return; // Allow inline editing without initiating drag

        var sourceVisual = e.Source as Visual;
        NodeViewModel? node = null;
        while (sourceVisual != null && sourceVisual != ParametersListBox)
        {
            if (sourceVisual.DataContext is NodeViewModel nvm)
            {
                node = nvm;
                break;
            }
            sourceVisual = sourceVisual.GetVisualParent();
        }

        if (node != null)
        {
            draggedVariableNode = node;
            variableDragStartPoint = e.GetPosition(this);
            isDraggingVariable = false;
        }
    }

    private void OnRootPointerMoved(object? sender, PointerEventArgs e)
    {
        if (draggedVariableNode == null)
            return;

        var currentPos = e.GetPosition(this);
        var delta = currentPos - variableDragStartPoint;

        if (!isDraggingVariable && (Math.Abs(delta.X) > 4 || Math.Abs(delta.Y) > 4))
        {
            isDraggingVariable = true;
            e.Pointer.Capture(this);

            var symName = (draggedVariableNode["Name_"] as NodeViewModel)?.NodeValue?.ToString()
                          ?? (draggedVariableNode.NodeValue as Symbol)?.Name
                          ?? draggedVariableNode.Name;
            TxtDragVisualName.Text = symName;
            DragVisualBorder.IsVisible = true;
        }

        if (isDraggingVariable)
        {
            Canvas.SetLeft(DragVisualBorder, currentPos.X + 12);
            Canvas.SetTop(DragVisualBorder, currentPos.Y + 12);
        }
    }

    private async void OnRootPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (isDraggingVariable && draggedVariableNode != null)
        {
            DragVisualBorder.IsVisible = false;
            e.Pointer.Capture(null);

            var dropPos = e.GetPosition(GraphCanvasGrid);
            var isOverCanvas = dropPos.X >= 0 && dropPos.X <= GraphCanvasGrid.Width
                            && dropPos.Y >= 0 && dropPos.Y <= GraphCanvasGrid.Height;

            var targetSymbol = draggedVariableNode.NodeValue as Symbol;
            draggedVariableNode = null;
            isDraggingVariable = false;

            if (isOverCanvas && targetSymbol != null && DataContext is VisualScriptEditorViewModel vm && vm.VisibleMethod != null)
            {
                lastDropPosition = dropPos;
                var block = await TransformVariableIntoBlock(targetSymbol);
                if (block != null)
                {
                    vm.VisibleMethod.Method.AddBlock(block);
                }
            }
            e.Handled = true;
        }
        else
        {
            draggedVariableNode = null;
            isDraggingVariable = false;
            DragVisualBorder.IsVisible = false;
        }
    }
    #endregion

    #region IVisualScriptViewModelService Implementation
    public async Task<Block?> TransformVariableIntoBlock(Stride.Assets.Scripts.Symbol symbol)
    {
        if (symbol == null)
            return null;

        var tcs = new TaskCompletionSource<Block?>();

        var contextMenu = new ContextMenu();

        var getMenuItem = new MenuItem
        {
            Header = $"Get {symbol.Name}",
            Icon = new PathIcon
            {
                Data = StreamGeometry.Parse("M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4Z"),
                Width = 12,
                Height = 12,
                Foreground = Brush.Parse("#0098FF")
            }
        };

        var setMenuItem = new MenuItem
        {
            Header = $"Set {symbol.Name}",
            Icon = new PathIcon
            {
                Data = StreamGeometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z"),
                Width = 12,
                Height = 12,
                Foreground = Brush.Parse("#30D158")
            }
        };

        getMenuItem.Click += (_, _) =>
        {
            var block = new VariableGet
            {
                Name = symbol.Name,
                Position = new Int2((int)Math.Round(lastDropPosition.X), (int)Math.Round(lastDropPosition.Y))
            };
            contextMenu.Close();
            tcs.TrySetResult(block);
        };

        setMenuItem.Click += (_, _) =>
        {
            var block = new VariableSet
            {
                Name = symbol.Name,
                Position = new Int2((int)Math.Round(lastDropPosition.X), (int)Math.Round(lastDropPosition.Y))
            };
            contextMenu.Close();
            tcs.TrySetResult(block);
        };

        contextMenu.Closed += (_, _) =>
        {
            tcs.TrySetResult(null);
        };

        contextMenu.Items.Add(getMenuItem);
        contextMenu.Items.Add(setMenuItem);

        Canvas.SetLeft(ContextMenuAnchor, lastDropPosition.X);
        Canvas.SetTop(ContextMenuAnchor, lastDropPosition.Y);

        contextMenu.Open(ContextMenuAnchor);

        return await tcs.Task;
    }
    #endregion

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualScriptEditorViewModel.SymbolSearchOpen))
        {
            if (DataContext is VisualScriptEditorViewModel vm && vm.SymbolSearchOpen)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    TxtSymbolSearch.Focus();
                    TxtSymbolSearch.SelectAll();
                }, DispatcherPriority.Input);
            }
        }
    }

    private void ConfirmSymbolSearch()
    {
        if (DataContext is VisualScriptEditorViewModel vm)
        {
            if (vm.SymbolSearchValidatedItem == null)
            {
                vm.SymbolSearchValidatedItem = LstSymbolSearchResults.SelectedItem as ISymbol
                                               ?? vm.SymbolSearchValues.FirstOrDefault();
            }

            vm.SymbolSearchOpen = false;
        }
    }

    private void CancelSymbolSearch()
    {
        if (DataContext is VisualScriptEditorViewModel vm)
        {
            vm.SymbolSearchValidatedItem = null;
            vm.SymbolSearchOpen = false;
        }
    }

    private void OnSymbolSearchTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmSymbolSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelSymbolSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (DataContext is VisualScriptEditorViewModel vm && vm.SymbolSearchValues.Count > 0)
            {
                int newIdx = Math.Min(LstSymbolSearchResults.SelectedIndex + 1, vm.SymbolSearchValues.Count - 1);
                LstSymbolSearchResults.SelectedIndex = Math.Max(0, newIdx);
                LstSymbolSearchResults.ScrollIntoView(LstSymbolSearchResults.SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            if (DataContext is VisualScriptEditorViewModel vm && vm.SymbolSearchValues.Count > 0)
            {
                int newIdx = Math.Max(0, LstSymbolSearchResults.SelectedIndex - 1);
                LstSymbolSearchResults.SelectedIndex = newIdx;
                LstSymbolSearchResults.ScrollIntoView(LstSymbolSearchResults.SelectedIndex);
            }
            e.Handled = true;
        }
    }

    private void OnSymbolSearchResultsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmSymbolSearch();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelSymbolSearch();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is VisualScriptEditorViewModel vm)
        {
            if (vm.SymbolSearchOpen)
            {
                if (e.Key == Key.Escape)
                {
                    CancelSymbolSearch();
                    e.Handled = true;
                    return;
                }
                if (e.Key == Key.Enter)
                {
                    ConfirmSymbolSearch();
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                // Delete selection shortcut
                if ((e.Key == Key.Delete || e.Key == Key.Back) && e.Source is not TextBox)
                {
                    if (vm.VisibleMethod?.DeleteSelectionCommand.CanExecute(null) == true)
                    {
                        vm.VisibleMethod.DeleteSelectionCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }
    }

    private void OnGraphCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not VisualScriptEditorViewModel vm || vm.VisibleMethod == null)
            return;

        if (vm.SymbolSearchOpen)
            return;

        var sourceVisual = e.Source as Visual;
        BlockNodeVertex? clickedBlock = null;

        while (sourceVisual != null && sourceVisual != GraphCanvasGrid)
        {
            if (sourceVisual.DataContext is BlockNodeVertex bnv)
            {
                clickedBlock = bnv;
                break;
            }
            sourceVisual = sourceVisual.GetVisualParent();
        }

        bool isCtrlOrShift = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift)) != 0;

        if (clickedBlock != null)
        {
            if (isCtrlOrShift)
            {
                clickedBlock.IsSelected = !clickedBlock.IsSelected;
            }
            else
            {
                if (!clickedBlock.IsSelected)
                {
                    foreach (var b in vm.VisibleMethod.Blocks)
                        b.IsSelected = false;
                    foreach (var l in vm.VisibleMethod.Links)
                        l.IsSelected = false;

                    clickedBlock.IsSelected = true;
                }
            }

            // Start block dragging
            draggedBlock = clickedBlock;
            dragStartMouse = e.GetPosition(GraphCanvasGrid);
            initialBlockPositions.Clear();
            foreach (var b in vm.VisibleMethod.SelectedBlocks)
            {
                initialBlockPositions[b] = new Point(b.PositionX, b.PositionY);
            }

            isDraggingBlock = false;
            e.Pointer.Capture(GraphCanvasGrid);
            e.Handled = true;
        }
        else
        {
            // Clicked on empty canvas background -> clear selection
            if (!isCtrlOrShift)
            {
                foreach (var b in vm.VisibleMethod.Blocks)
                    b.IsSelected = false;
                foreach (var l in vm.VisibleMethod.Links)
                    l.IsSelected = false;
                vm.VisibleMethod.SelectedBlocks.Clear();
                vm.VisibleMethod.SelectedLinks.Clear();
            }
        }
    }

    private void OnGraphCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (draggedBlock == null || DataContext is not VisualScriptEditorViewModel vm || vm.VisibleMethod == null)
            return;

        var currentPos = e.GetPosition(GraphCanvasGrid);
        var delta = currentPos - dragStartMouse;

        if (!isDraggingBlock && (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3))
        {
            isDraggingBlock = true;
        }

        if (isDraggingBlock)
        {
            foreach (var (b, initPos) in initialBlockPositions)
            {
                b.PositionX = Math.Max(0, initPos.X + delta.X);
                b.PositionY = Math.Max(0, initPos.Y + delta.Y);
            }
        }
    }

    private void OnGraphCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (draggedBlock != null)
        {
            if (isDraggingBlock && DataContext is VisualScriptEditorViewModel vm && vm.VisibleMethod != null)
            {
                vm.VisibleMethod.FinalizeMoves();
            }

            e.Pointer.Capture(null);
            draggedBlock = null;
            isDraggingBlock = false;
            initialBlockPositions.Clear();
            e.Handled = true;
        }
    }
}
