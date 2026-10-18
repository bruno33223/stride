// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for DebugEntityHierarchyEditorUserControl.axaml
/// </summary>
public partial class DebugEntityHierarchyEditorUserControl : UserControl, IDebugPage
{
    public class DebugEntityHierarchyEditorViewModel : DispatcherViewModel
    {
        private readonly EntityHierarchyEditorViewModel editor;
        private readonly Dictionary<AssetId, string>? assetLoadingTimeUrls;
        private readonly EditorServiceGame? game;

        public DebugEntityHierarchyEditorViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] EntityHierarchyEditorViewModel editor)
            : base(serviceProvider)
        {
            this.editor = editor;
            var sceneLoader = editor.Controller?.Loader;

            if (sceneLoader != null)
            {
                // We use reflection to access private fields and protected properties.
                var propertyInfo = typeof(EditorContentLoader).GetProperty("AssetLoadingTimeUrls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (propertyInfo != null)
                {
                    assetLoadingTimeUrls = propertyInfo.GetValue(sceneLoader) as Dictionary<AssetId, string>;
                }

                propertyInfo = typeof(EditorContentLoader).GetProperty("Game", BindingFlags.Instance | BindingFlags.NonPublic);
                if (propertyInfo != null)
                {
                    game = propertyInfo.GetValue(sceneLoader) as EditorServiceGame;
                }
            }

            LoadingTimeUrls = new ObservableList<string>();
            LoadedAssets = new ObservableList<string>();
            RefreshCommand = new AnonymousCommand(ServiceProvider, Refresh);
        }

        public ObservableList<string> LoadingTimeUrls { get; }

        public ObservableList<string> LoadedAssets { get; }

        public ICommandBase RefreshCommand { get; private set; }

        private void Refresh()
        {
            var list = new List<string>();
            if (assetLoadingTimeUrls != null)
            {
                foreach (var entry in assetLoadingTimeUrls)
                {
                    var asset = editor.Session.GetAssetById(entry.Key);
                    list.Add($"{(asset != null ? asset.Url : "<Unknown: " + entry.Key + ">")} -> {entry.Value}");
                }
            }
            LoadingTimeUrls.Clear();
            LoadingTimeUrls.AddRange(list);

            list.Clear();
            // Display loaded assets stats if available
            var debug = editor.Controller?.GetService<IEditorGameDebugViewModelService>();
            if (debug != null)
            {
                foreach (var entry in debug.ContentManagerStats.LoadedAssets)
                {
                    list.Add($"{entry.Url}: Pub:{entry.PublicReferenceCount} Priv:{entry.PrivateReferenceCount}");
                }
            }
            LoadedAssets.Clear();
            LoadedAssets.AddRange(list);
        }
    }

    public DebugEntityHierarchyEditorUserControl()
    {
        InitializeComponent();
    }

    public DebugEntityHierarchyEditorUserControl([NotNull] EntityHierarchyEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        DataContext = new DebugEntityHierarchyEditorViewModel(editor.ServiceProvider, editor);
        Title = $"Scene '{editor.Asset.Url}'";
        InitializeComponent();
    }

    public string Title { get; set; } = "Debug Entity Hierarchy";
}
