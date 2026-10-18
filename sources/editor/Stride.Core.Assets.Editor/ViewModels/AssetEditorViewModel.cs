// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// An interface that represents the view model of an asset editor.
/// </summary>
public interface IAssetEditorViewModel : IDestroyable
{
    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    AssetViewModel Asset { get; }

    /// <summary>
    /// A task that completes only when the editor has been successfully initialized;
    /// </summary>
    Task EditorInitialized => Task.CompletedTask;

    /// <summary>
    /// Initializes this editor asynchronously.
    /// </summary>
    /// <returns>A task that completes when the editor initialization has ended. This task result indicates if the initialization was successful.</returns>
    Task<bool> Initialize() => Task.FromResult(true);

    /// <summary>
    /// Processes anything before closing the editor, such as saving the file.
    /// </summary>
    /// <param name="save"><c>true</c> if file should be saved, <c>false</c> if the file can be closed without asking user, null if user should be asked.</param>
    /// <returns><c>true</c> if windows can be closed, <c>false</c> if user interrupted (should only happen when <see cref="save"/> is null).</returns>
    bool PreviewClose(bool? save) => true;
}

/// <summary>
/// An interface that represents the view model of an asset editor.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
public interface IAssetEditorViewModel<out TAsset> : IAssetEditorViewModel
    where TAsset : AssetViewModel
{
    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    new TAsset Asset { get; }
}

/// <summary>
/// Base view model for asset editors.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
public class AssetEditorViewModel<TAsset> : AssetEditorViewModel, IAssetEditorViewModel<TAsset>
    where TAsset : AssetViewModel
{
    public AssetEditorViewModel(TAsset asset)
        : base(asset)
    {
    }

    /// <inheritdoc cref="IAssetEditorViewModel{T}.Asset" />
    public override TAsset Asset => (TAsset)base.Asset;

    public SessionObjectPropertiesViewModel EditorProperties => Session.ActiveProperties;
}

/// <summary>
/// Base view model for asset editors.
/// </summary>
public abstract class AssetEditorViewModel : DispatcherViewModel, IAssetEditorViewModel
{
    protected AssetEditorViewModel(AssetViewModel asset)
        : base(asset.ServiceProvider)
    {
        Asset = asset;
    }

    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    public virtual AssetViewModel Asset { get; }

    /// <summary>
    /// The current session.
    /// </summary>
    public SessionViewModel Session => (SessionViewModel)Asset.Session;

    public virtual Task<bool> Initialize() => Task.FromResult(true);

    public virtual bool PreviewClose(bool? save) => true;
}
