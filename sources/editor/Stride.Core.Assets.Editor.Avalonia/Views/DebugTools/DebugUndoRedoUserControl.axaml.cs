// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views.DebugTools;

/// <summary>
/// Interaction logic for DebugUndoRedoUserControl.axaml
/// </summary>
public partial class DebugUndoRedoUserControl : UserControl, IDebugPage, IDestroyable
{
    public DebugUndoRedoUserControl()
    {
        InitializeComponent();
    }

    public DebugUndoRedoUserControl(IViewModelServiceProvider serviceProvider, IUndoRedoService undoRedo)
    {
        InitializeComponent();
        DataContext = new DebugUndoRedoViewModel(serviceProvider, undoRedo);
    }

    public string Title { get; set; } = "Undo / Redo";

    public void Destroy()
    {
        (DataContext as IDestroyable)?.Destroy();
    }
}
