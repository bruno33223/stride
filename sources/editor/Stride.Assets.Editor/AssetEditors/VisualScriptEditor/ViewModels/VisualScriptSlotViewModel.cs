// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Scripts;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// View model for a <see cref="Scripts.Slot"/>.
/// </summary>
public class VisualScriptSlotViewModel : DispatcherViewModel
{
    private readonly Slot slot;
    private readonly IObjectNode slotNode;
    private bool connected;

    public VisualScriptSlotViewModel(VisualScriptBlockViewModel block, Slot slot)
        : base(block.SafeArgument(nameof(block)).ServiceProvider)
    {
        Block = block;
        this.slot = slot;
        slotNode = block.Method.Editor.Session.AssetNodeContainer.GetOrCreateNode(slot);
    }

    public Slot Slot => slot;

    /// <summary>
    /// The block this slot belongs to.
    /// </summary>
    public VisualScriptBlockViewModel Block { get; }

    /// <summary>
    /// Links connected to that slot.
    /// </summary>
    public ObservableList<VisualScriptLinkViewModel> Links { get; } = new();

    public string Name => slot.Name;

    public SlotKind Kind => slot.Kind;

    public SlotDirection Direction => slot.Direction;

    public string? Type => slot.Type;

    public bool IsInput => slot.Direction == SlotDirection.Input;

    public bool IsOutput => slot.Direction == SlotDirection.Output;

    public bool Connected
    {
        get => connected;
        set => SetValue(ref connected, value);
    }

    public string DisplayText => string.IsNullOrEmpty(Type) ? Name : $"{Name} ({Type})";
}
