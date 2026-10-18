// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Scripts;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

public class LinkNodeEdge : DispatcherViewModel
{
    private bool isSelected;

    public LinkNodeEdge(VisualScriptLinkViewModel viewModel, BlockNodeVertex source, BlockNodeVertex target)
        : base(viewModel.SafeArgument(nameof(viewModel)).ServiceProvider)
    {
        ViewModel = viewModel;
        Source = source;
        Target = target;
        SourceSlot = viewModel.SourceSlot;
        TargetSlot = viewModel.TargetSlot;
        if (SourceSlot == null || TargetSlot == null)
        {
            throw new InvalidOperationException("Could not find appropriate slots for this link");
        }
    }

    public VisualScriptLinkViewModel ViewModel { get; }

    public BlockNodeVertex Source { get; }

    public BlockNodeVertex Target { get; }

    public VisualScriptSlotViewModel SourceSlot { get; }

    public VisualScriptSlotViewModel TargetSlot { get; }

    public SlotKind Kind => ViewModel.Kind;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (SetValue(ref isSelected, value))
            {
                var selectedLinks = ViewModel.Method.SelectedLinks;
                if (value)
                {
                    if (!selectedLinks.Contains(this))
                        selectedLinks.Add(this);
                }
                else
                {
                    selectedLinks.Remove(this);
                }
            }
        }
    }
}
