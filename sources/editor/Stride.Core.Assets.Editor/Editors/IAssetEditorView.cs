// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Editors;

public interface IAssetEditorView
{
    /// <summary>
    /// Gets a task that is completed when the editor initialization is done.
    /// </summary>
    Task EditorInitialization => Task.CompletedTask;

    /// <summary>
    /// Initializes the editor view with the editor view model.
    /// </summary>
    /// <param name="editor">The editor view model.</param>
    /// <returns>A task that completes when the initialization is done.</returns>
    Task<bool> InitializeEditor(IAssetEditorViewModel editor) => Task.FromResult(true);
}

