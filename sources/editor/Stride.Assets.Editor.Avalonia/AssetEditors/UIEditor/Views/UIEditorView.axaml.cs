// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.AssetEditors.UIEditor.Views;

/// <summary>
/// Interaction logic and base view model binding for UIEditorView.
/// </summary>
public abstract partial class UIEditorView : UserControl, IEditorView, IAssetEditorView
{
    private readonly TaskCompletionSource editorInitializationNotifier = new();

    protected UIEditorView()
    {
        Focusable = true;
    }

    /// <inheritdoc/>
    public Task EditorInitialization => editorInitializationNotifier.Task;

    /// <inheritdoc/>
    public virtual async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
    {
        // Don't set the actual Editor property until the editor object is fully initialized
        if (!await editor.Initialize())
        {
            editor.Destroy();
            editorInitializationNotifier.TrySetResult();
            return false;
        }

        editorInitializationNotifier.TrySetResult();
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Prevent arrow keys to be involved in unwanted navigation
        switch (e.Key)
        {
            case Key.Up:
            case Key.Down:
            case Key.Left:
            case Key.Right:
                e.Handled = true;
                break;
        }
    }
}
