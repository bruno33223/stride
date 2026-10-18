// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using RoslynPad.Roslyn.Diagnostics;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Presentation.AssetEditors.ScriptEditor;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor;

/// <summary>
/// Interaction logic for ScriptEditorView.axaml
/// </summary>
[AssetEditorView<ScriptEditorViewModel>]
public partial class ScriptEditorView : UserControl, IEditorView, IAssetEditorView
{
    private readonly TaskCompletionSource editorInitializationNotifier = new();
    private ScriptEditorViewModel? editor;
    private SimpleCodeTextEditor? codeEditorBackend;

    public ScriptEditorView()
    {
        InitializeComponent();
        Focusable = true;
    }

    /// <inheritdoc/>
    public Task EditorInitialization => editorInitializationNotifier.Task;

    /// <summary>
    /// Gets the backend code text editor instance.
    /// </summary>
    public SimpleCodeTextEditor? CodeEditor => codeEditorBackend;

    /// <inheritdoc/>
    public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
    {
        this.editor = (ScriptEditorViewModel)editor;
        if (!await editor.Initialize())
        {
            editor.Destroy();
            editorInitializationNotifier.SetResult();
            return false;
        }

        codeEditorBackend = new SimpleCodeTextEditor();

        this.editor.DocumentClosed += Editor_DocumentClosed;
        this.editor.ProcessDiagnostics += Editor_ProcessDiagnostics;

        // Bind SourceTextContainer to backend
        codeEditorBackend.BindSourceTextContainer(this.editor.Workspace, this.editor.SourceTextContainer, this.editor.DocumentId);

        editorInitializationNotifier.SetResult();
        return true;
    }

    private void Editor_ProcessDiagnostics(object? sender, DiagnosticsChangedArgs e)
    {
        codeEditorBackend?.ProcessDiagnostics(e);
    }

    private void Editor_DocumentClosed(object? sender, EventArgs e)
    {
        if (editor != null)
        {
            editor.DocumentClosed -= Editor_DocumentClosed;
            editor.ProcessDiagnostics -= Editor_ProcessDiagnostics;
        }

        codeEditorBackend?.Unbind();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && editor?.Code != null)
        {
            editor.Code.EditorFontSize += e.Delta.Y > 0 ? 1 : -1;
            e.Handled = true;
        }
    }
}
