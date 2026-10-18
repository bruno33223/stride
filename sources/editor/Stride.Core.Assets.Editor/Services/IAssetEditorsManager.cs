// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// An interface representing a service that can open and close editor instances for assets.
/// </summary>
public interface IAssetEditorsManager
{
    /// <summary>
    /// Immediately closes all open asset editor windows.
    /// </summary>
    /// <param name="save">True if files should be saved, false if files can be closed without asking user, null if user should be asked.</param>
    /// <returns>True if all windows succesfully closed, false if user interrupted.</returns>
    bool CloseAllEditorWindows(bool? save);

    /// <summary>
    /// Immediately closes all editor windows that are hidden.
    /// </summary>
    void CloseAllHiddenWindows();

    /// <summary>
    /// Closes the curve editor window.
    /// </summary>
    void CloseCurveEditorWindow();

    /// <summary>
    /// Closes the editor window for the given asset. If no editor window for this asset exists, this method does nothing.
    /// </summary>
    /// <param name="asset">The asset for which to close the editor window.</param>
    /// <param name="save">True if file should be saved, false if file can be closed without asking user, null if user should be asked.</param>
    /// <returns>True if windows could be closed, false if user interrupted.</returns>
    bool CloseAssetEditorWindow(AssetViewModel asset, bool? save);

    /// <summary>
    /// Immediately hides all editor windows.
    /// </summary>
    void HideAllAssetEditorWindows();

    /// <summary>
    /// Opens (and activates) an editor window for the given <paramref name="curve"/>.
    /// </summary>
    /// <param name="curve">The curve to show in the curve editor.</param>
    /// <param name="name">The name to display in the curve editor.</param>
    void OpenCurveEditorWindow(object curve, string name);

    /// <summary>
    /// Opens (and activates) an editor window for the given asset. If an editor window for this asset already exists, simply activates it.
    /// </summary>
    /// <param name="asset">The asset for which to show an editor window.</param>
    Task OpenAssetEditorWindow(AssetViewModel asset);

    /// <summary>
    /// Opens an editor for the given asset, or activates it if already opened.
    /// </summary>
    /// <param name="asset">The asset to open.</param>
    void OpenAssetEditor(AssetViewModel asset);

    /// <summary>
    /// Closes the editor for the given asset if opened.
    /// </summary>
    /// <param name="asset">The asset to close.</param>
    /// <returns>True if the editor was closed, false otherwise.</returns>
    bool CloseAssetEditor(AssetViewModel asset);

    /// <summary>
    /// Try to find an opened editor for the given asset.
    /// </summary>
    /// <typeparam name="TEditor"></typeparam>
    /// <param name="asset"></param>
    /// <param name="assetEditor"></param>
    /// <returns></returns>
    bool TryGetAssetEditor<TEditor>(AssetViewModel asset, [MaybeNullWhen(false)] out TEditor assetEditor) where TEditor : AssetEditorViewModel;
}
