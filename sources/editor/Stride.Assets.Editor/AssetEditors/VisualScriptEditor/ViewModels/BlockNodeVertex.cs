// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels;

public class BlockNodeVertex : DispatcherViewModel, IAddChildViewModel
{
    public const int SnapGridSize = 16;
    private bool isSelected;

    public BlockNodeVertex(VisualScriptBlockViewModel viewModel)
        : base(viewModel.SafeArgument(nameof(viewModel)).ServiceProvider)
    {
        ViewModel = viewModel;
        InputSlots = new ObservableList<VisualScriptSlotViewModel>();
        OutputSlots = new ObservableList<VisualScriptSlotViewModel>();

        // Add initial slots
        foreach (var slot in viewModel.Block.Slots)
        {
            var slots = slot.Direction == SlotDirection.Input ? InputSlots : OutputSlots;
            var slotViewModel = new VisualScriptSlotViewModel(viewModel, slot);
            slots.Add(slotViewModel);
            viewModel.Slots[slot] = slotViewModel;
        }

        viewModel.Block.Slots.CollectionChanged += Slots_CollectionChanged;
        ViewModel.PositionChanged += ViewModel_PositionChanged;
    }

    public VisualScriptBlockViewModel ViewModel { get; }

    public ObservableList<VisualScriptSlotViewModel> InputSlots { get; }

    public ObservableList<VisualScriptSlotViewModel> OutputSlots { get; }

    public string Title => ViewModel.Title;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (SetValue(ref isSelected, value))
            {
                var selectedBlocks = ViewModel.Method.SelectedBlocks;
                if (value)
                {
                    if (!selectedBlocks.Contains(this))
                        selectedBlocks.Add(this);
                }
                else
                {
                    selectedBlocks.Remove(this);
                }
            }
        }
    }

    public Int2 Position
    {
        get => ViewModel.Position;
        set => ViewModel.Position = value;
    }

    public double PositionX
    {
        get => ViewModel.Position.X;
        set
        {
            if (ViewModel.Position.X != (int)Math.Round(value))
            {
                ViewModel.Position = new Int2((int)Math.Round(value), ViewModel.Position.Y);
                OnPropertyChanged();
            }
        }
    }

    public double PositionY
    {
        get => ViewModel.Position.Y;
        set
        {
            if (ViewModel.Position.Y != (int)Math.Round(value))
            {
                ViewModel.Position = new Int2(ViewModel.Position.X, (int)Math.Round(value));
                OnPropertyChanged();
            }
        }
    }

    public override void Destroy()
    {
        ViewModel.Block.Slots.CollectionChanged -= Slots_CollectionChanged;
        ViewModel.PositionChanged -= ViewModel_PositionChanged;
        base.Destroy();
    }

    private void ViewModel_PositionChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(PositionX));
        OnPropertyChanged(nameof(PositionY));
    }

    private void Slots_CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                var slot = (Slot)e.Item!;
                var slots = slot.Direction == SlotDirection.Input ? InputSlots : OutputSlots;
                var slotViewModel = new VisualScriptSlotViewModel(ViewModel, slot);
                slots.Add(slotViewModel);
                ViewModel.Slots[slot] = slotViewModel;
                break;
            }
            case NotifyCollectionChangedAction.Remove:
            {
                var slot = (Slot)e.Item!;
                var slots = slot.Direction == SlotDirection.Input ? InputSlots : OutputSlots;
                if (ViewModel.Slots.TryGetValue(slot, out var slotViewModel))
                {
                    slots.Remove(slotViewModel);
                    ViewModel.Slots.Remove(slot);
                }
                break;
            }
        }
    }

    public void AddOutgoing(BlockNodeVertex target, VisualScriptSlotViewModel from, VisualScriptSlotViewModel to)
    {
        ViewModel.Method.Method.AddLink(new Link
        {
            Source = from.Slot,
            Target = to.Slot,
        });
    }

    public void FinalizeMove()
    {
        var current = ViewModel.Position;
        var snapped = RoundSnapPosition(current);
        if (current != snapped)
        {
            ViewModel.Position = snapped;
        }
    }

    public static Int2 RoundSnapPosition(Int2 position)
    {
        return new Int2(
            ((position.X + SnapGridSize / 2) / SnapGridSize) * SnapGridSize,
            ((position.Y + SnapGridSize / 2) / SnapGridSize) * SnapGridSize);
    }

    public override string ToString()
    {
        return ViewModel.Block.Title;
    }

    bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        return ViewModel.CanAddChildren(children, modifiers, out message);
    }

    void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        ViewModel.AddChildren(children, modifiers);
    }
}
