// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.AssetEditors.GraphicsCompositorEditor.Views;

/// <summary>
/// Interaction logic for GraphicsCompositorEditorView in Avalonia.
/// </summary>
[AssetEditorView<GraphicsCompositorEditorViewModel>]
public partial class GraphicsCompositorEditorView : Stride.Assets.Editor.Avalonia.Views.GraphicsCompositorEditorView, IEditorView, IAssetEditorView
{
    private readonly TaskCompletionSource editorInitializationNotifier = new();

    public GraphicsCompositorEditorView()
    {
        Focusable = true;
    }

    /// <inheritdoc/>
    public Task EditorInitialization => editorInitializationNotifier.Task;

    /// <inheritdoc/>
    public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
    {
        var graphicsEditor = editor as GraphicsCompositorEditorViewModel;

        if (graphicsEditor == null || !await graphicsEditor.Initialize())
        {
            editor.Destroy();
            editorInitializationNotifier.TrySetResult();
            return false;
        }

        editorInitializationNotifier.TrySetResult();
        return true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Focus this element so shortcuts and operations work properly
        Focus();
    }
}
