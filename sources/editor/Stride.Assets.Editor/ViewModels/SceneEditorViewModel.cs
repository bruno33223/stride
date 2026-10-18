// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Threading;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Editor.AssetEditors.SceneEditor.Services;

namespace Stride.Assets.Editor.ViewModels
{
    /// <summary>
    /// View model of a <see cref="SceneViewModel"/> editor.
    /// </summary>
    [AssetEditorViewModel<SceneViewModel>]
    public sealed class SceneEditorViewModel : EntityHierarchyEditorViewModel, IMultipleAssetEditorViewModel, IAssetEditorViewModel<SceneViewModel>
    {
        private readonly ObservableSet<SceneViewModel> allScenes = new ObservableSet<SceneViewModel>();
        /// <summary>
        /// The scene that was initially opened by this editor.
        /// </summary>
        private SceneViewModel mainScene;
        private readonly AsyncLock loadMutex = new AsyncLock();
        private readonly TaskCompletionSource<int> initialized = new TaskCompletionSource<int>();
        private bool displayCameraPreview;
        private SceneEditorController? controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        public SceneEditorViewModel([NotNull] SceneViewModel asset)
            : this(asset, x => new SceneEditorController(asset.GetRoot(), (SceneEditorViewModel)x))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        public SceneEditorViewModel([NotNull] SceneViewModel asset, [NotNull] Func<AssetEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset)
        {
            mainScene = asset;

            if (controllerFactory != null)
            {
                try
                {
                    controller = controllerFactory(this) as SceneEditorController;
                }
                catch (Exception ex)
                {
                    Logger?.Warning("Could not create SceneEditorController in current context.", ex);
                }
            }

            allScenes.Add(asset);

            if (HierarchyRoot is SceneRootViewModel root)
            {
                root.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
                foreach (var child in root.ChildScenes.BreadthFirst(x => x.ChildScenes))
                {
                    allScenes.Add(child.SceneAsset);
                    child.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
                }
            }
        }

        public override SceneViewModel Asset => (SceneViewModel)base.Asset;

        public bool DisplayCameraPreview
        {
            get => Preview != null ? Preview.IsActive : displayCameraPreview;
            set
            {
                if (Preview != null)
                {
                    SetValue(Preview.IsActive != value, () => Preview.IsActive = value);
                }
                else
                {
                    SetValue(ref displayCameraPreview, value);
                }
            }
        }

        public Task Initialized => initialized.Task;

        /// <summary>
        /// A collection of all scenes opened in this editor.
        /// </summary>
        /// <remarks>
        /// The order of items in this collection is not guaranteed to be consistent with the hierarchy of scenes.
        /// </remarks>
        public IReadOnlyObservableList<SceneViewModel> AllScenes => allScenes;

        /// <inheritdoc />
        IReadOnlyObservableList<AssetViewModel> IMultipleAssetEditorViewModel.OpenedAssets => allScenes;

        private IEditorGameCameraPreviewViewModelService? Preview => Controller?.GetService<IEditorGameCameraPreviewViewModelService>();

        [CanBeNull]
        public new SceneEditorController? Controller => controller ?? (base.Controller as SceneEditorController);

        /// <inheritdoc />
        protected override EntityHierarchyItemViewModel CreateRootPartViewModel()
        {
            return new SceneRootViewModel(this, Asset, loadMutex);
        }

        /// <inheritdoc />
        public new async Task Delete()
        {
            var sceneRoots = SelectedContent.OfType<SceneRootViewModel>().ToList();
            // Mix of scene roots and entities selected
            if (sceneRoots.Count != SelectedContent.Count)
            {
                await base.Delete();
                return;
            }

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                ClearSelection();
                foreach (var sceneRoot in GetCommonRoots(sceneRoots))
                {
                    sceneRoot.Delete();
                }

                UndoRedoService.SetName(transaction, "Remove selected child scenes");
            }
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SceneEditorViewModel));
            var root = HierarchyRoot as SceneRootViewModel;
            if (root != null)
            {
                foreach (var scene in root.ChildScenes.BreadthFirst(x => x.ChildScenes))
                {
                    scene.SaveSettings();
                    scene.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
                }
                root.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
            }

            controller?.Destroy();
            base.Destroy();
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeEditor()
        {
            var root = RootPart as SceneRootViewModel;
            if (root == null)
                return false;

            SceneRootViewModel? mainSceneRoot = null;
            // Register all scenes
            foreach (var sceneRoot in root.Yield().Concat(root.ChildScenes.BreadthFirst(x => x.ChildScenes)))
            {
                var asset = sceneRoot.SceneAsset;
                if (asset == mainScene)
                    mainSceneRoot = sceneRoot;
                allScenes.Add(asset);
                sceneRoot.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
            }
            // Load the main scene (the one that was initially opened by this editor, not necessarily the root scene)
            mainSceneRoot?.RequestLoading(true).Forget();
            // Notify initialization completed
            initialized.TrySetResult(0);
            // Expand the root by default
            root.IsExpanded = true;
            // Set the main scene as currently active (fallback to root)
            ActiveRoot = mainSceneRoot ?? root;
            return true;
        }

        /// <inheritdoc />
        protected override void LoadSettings(SceneSettingsData settings)
        {
            base.LoadSettings(settings);
            DisplayCameraPreview = settings.CameraPreviewVisible;
        }

        /// <inheritdoc />
        protected override void SaveSettings(SceneSettingsData settings)
        {
            base.SaveSettings(settings);
            settings.CameraPreviewVisible = DisplayCameraPreview;
        }

        /// <inheritdoc />
        protected override async Task RefreshEditorProperties()
        {
            // Single element selected (could be a scene)
            if (SelectedContent.Count == 1)
            {
                var sceneRoot = SelectedContent[0] as SceneRootViewModel;
                if (sceneRoot != null)
                {
                    EditorProperties.Name = sceneRoot.Name;
                    EditorProperties.TypeDescription = "Scene";
                    await EditorProperties.GenerateSelectionPropertiesAsync(sceneRoot.Yield());
                    return;
                }
            }
            await base.RefreshEditorProperties();
        }

        protected override void UpdateTransformations(EntityHierarchyItemViewModel element, TransformationTRS transformation)
        {
            if (element is EntityViewModel)
            {
                base.UpdateTransformations(element, transformation);
                return;
            }

            var root = element as SceneRootViewModel;
            if (root == null)
                return;

            // Update properties only when they actually changed
            var oldOffset = root.Offset;
            if (oldOffset != transformation.Position)
                root.Offset = transformation.Position;
        }

        private void ChildScenesCollectionChanged(object? sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems?.Count > 0)
                    {
                        foreach (var root in e.NewItems.Cast<SceneRootViewModel>().BreadthFirst(x => x.ChildScenes))
                        {
                            allScenes.Add(root.SceneAsset);
                            root.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems?.Count > 0)
                    {
                        foreach (var root in e.OldItems.Cast<SceneRootViewModel>().BreadthFirst(x => x.ChildScenes))
                        {
                            root.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
                            allScenes.Remove(root.SceneAsset);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }
    }
}
