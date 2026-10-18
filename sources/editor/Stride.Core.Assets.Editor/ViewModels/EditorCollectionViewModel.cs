// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class EditorCollectionViewModel : DispatcherViewModel, IAssetEditorsManager
{
    private AssetEditorViewModel? activeEditor;
    private readonly ObservableList<AssetEditorViewModel> editors = [];
    private readonly Dictionary<AssetViewModel, AssetEditorViewModel> openedAssets = new();

    public EditorCollectionViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
        CloseAssetEditorCommand = new AnonymousCommand<AssetEditorViewModel>(ServiceProvider, x => { if (x != null) CloseAssetEditor(x); });
        Session.DeletedAssetsChanged += AssetsDeleted;
        ServiceProvider.RegisterService(this);
    }

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EditorCollectionViewModel));
        Session.DeletedAssetsChanged -= AssetsDeleted;

        foreach (var editor in editors.ToList())
        {
            editor.Destroy();
        }
        editors.Clear();
        openedAssets.Clear();

        ServiceProvider.UnregisterService(this);
        base.Destroy();
    }

    public AssetEditorViewModel? ActiveEditor
    {
        get => activeEditor;
        set => SetValue(ref activeEditor, value);
    }

    public IReadOnlyObservableCollection<AssetEditorViewModel> Editors => editors;

    public SessionViewModel Session { get; }

    public ICommandBase CloseAssetEditorCommand { get; }

    public void OpenAssetEditor(AssetViewModel asset)
    {
        if (openedAssets.TryGetValue(asset, out var editor))
        {
             ActiveEditor = editor;
             return;
        }

        var editorType = ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewModelType(asset.GetType());
        if (editorType is not null)
        {
            editor = (AssetEditorViewModel)Activator.CreateInstance(editorType, asset)!;
            openedAssets.Add(asset, editor);
            editors.Add(editor);
            ActiveEditor = editor;
        }
    }

    public bool CloseAssetEditor(AssetViewModel asset)
    {
        if (openedAssets.TryGetValue(asset, out var editor))
        {
            return CloseAssetEditor(editor);
        }
        return false;
    }

    public bool CloseAssetEditor(AssetEditorViewModel editor)
    {
        if (openedAssets.Remove(editor.Asset))
        {
            editors.Remove(editor);
            if (ActiveEditor == editor)
            {
                ActiveEditor = editors.LastOrDefault();
            }
            editor.Destroy();
            return true;
        }
        return false;
    }

    public bool TryGetAssetEditor<TEditor>(AssetViewModel asset, [MaybeNullWhen(false)] out TEditor assetEditor) where TEditor : AssetEditorViewModel
    {
        if (openedAssets.TryGetValue(asset, out var found) && found is TEditor editor)
        {
            assetEditor = editor;
            return true;
        }

        assetEditor = null;
        return false;
    }


    public bool CloseAllEditorWindows(bool? save)
    {
        foreach (var editor in editors.ToList())
        {
            CloseAssetEditor(editor);
        }
        return true;
    }

    public void CloseAllHiddenWindows()
    {
    }

    public void CloseCurveEditorWindow()
    {
    }

    public bool CloseAssetEditorWindow(AssetViewModel asset, bool? save)
    {
        return CloseAssetEditor(asset);
    }

    public void HideAllAssetEditorWindows()
    {
    }

    public void OpenCurveEditorWindow(object curve, string name)
    {
    }

    public Task OpenAssetEditorWindow(AssetViewModel asset)
    {
        OpenAssetEditor(asset);
        return Task.CompletedTask;
    }

    private void AssetsDeleted(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (AssetViewModel asset in e.NewItems.OfType<AssetViewModel>())
            {
                CloseAssetEditor(asset);
            }
        }
    }
}
