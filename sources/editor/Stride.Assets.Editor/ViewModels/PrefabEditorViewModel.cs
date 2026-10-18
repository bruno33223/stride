// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;

namespace Stride.Assets.Editor.ViewModels
{
    /// <summary>
    /// View model of a <see cref="PrefabViewModel"/> editor.
    /// </summary>
    [AssetEditorViewModel<PrefabViewModel>]
    public sealed class PrefabEditorViewModel : EntityHierarchyEditorViewModel, IAssetEditorViewModel<PrefabViewModel>
    {
        private IEditorGameController? controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        public PrefabEditorViewModel([NotNull] PrefabViewModel asset)
            : this(asset, x => new PrefabEditorController(asset, (PrefabEditorViewModel)x))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        public PrefabEditorViewModel([NotNull] PrefabViewModel asset, [CanBeNull] Func<PrefabEditorViewModel, IEditorGameController>? controllerFactory)
            : base(asset)
        {
            if (controllerFactory != null)
            {
                try
                {
                    controller = controllerFactory(this);
                }
                catch (Exception ex)
                {
                    GlobalLogger.GetLogger("PrefabEditor").Warning("Could not create PrefabEditorController in current context.", ex);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        public PrefabEditorViewModel([NotNull] PrefabViewModel asset, [NotNull] Func<AssetEditorViewModel, IEditorGameController> controllerFactory)
            : this(asset, controllerFactory != null ? new Func<PrefabEditorViewModel, IEditorGameController>(controllerFactory) : null)
        {
        }

        /// <inheritdoc />
        public override PrefabViewModel Asset => (PrefabViewModel)base.Asset;

        [CanBeNull]
        public new IEditorGameController? Controller => controller ?? base.Controller;

        /// <inheritdoc />
        protected override PrefabRootViewModel CreateRootPartViewModel()
        {
            return new PrefabRootViewModel(this, Asset);
        }

        /// <inheritdoc />
        protected override void LoadSettings(SceneSettingsData settings)
        {
            base.LoadSettings(settings);
            if (Rendering != null)
            {
                Rendering.RenderMode = EditorRenderMode.DefaultEditor;
            }
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeEditor()
        {
            if (!await base.InitializeEditor())
                return false;

            // Always load the whole prefab
            var root = (PrefabRootViewModel)RootPart;
            root.RequestLoading(true).Forget();
            // Expand the root by default
            root.IsExpanded = true;
            return true;
        }
    }
}
