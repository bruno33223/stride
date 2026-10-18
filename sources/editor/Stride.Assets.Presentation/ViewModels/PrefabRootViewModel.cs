// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.ViewModels
{
    [DebuggerDisplay("PrefabRoot = {" + nameof(Name) + "}")]
    public sealed class PrefabRootViewModel : EntityHierarchyRootViewModel, IAssetPropertyProviderViewModel
    {
        private bool isLoaded = true;
        private bool isLoading;
        private bool isLocked;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabRootViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this root.</param>
        public PrefabRootViewModel([NotNull] PrefabViewModel asset)
            : this(null, asset)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabRootViewModel"/> class.
        /// </summary>
        /// <param name="editor">The editor associated with this root.</param>
        /// <param name="asset">The asset related to this root.</param>
        public PrefabRootViewModel(object? editor, [NotNull] PrefabViewModel asset)
            : base(asset)
        {
            Editor = editor;
        }

        /// <summary>
        /// The editor associated with this root.
        /// </summary>
        [CanBeNull]
        public object? Editor { get; }

        /// <summary>
        /// The prefab asset related to this root.
        /// </summary>
        [NotNull]
        public PrefabViewModel PrefabAsset => (PrefabViewModel)Asset;

        /// <inheritdoc/>
        public override AbsoluteId Id => new AbsoluteId(Asset.Id, Guid.Empty);

        /// <inheritdoc/>
        public override string? Name { get => "Prefab root"; set => throw new NotSupportedException($"Cannot change the name of a {nameof(PrefabRootViewModel)} object."); }

        /// <inheritdoc/>
        public override bool IsLoaded { get => isLoaded; set => SetValue(ref isLoaded, value); }

        public bool IsLoading { get => isLoading; set => SetValue(ref isLoading, value); }

        /// <inheritdoc/>
        public override bool IsLocked { get => isLocked; set => SetValue(ref isLocked, value); }

        /// <inheritdoc/>
        public override Task NotifyGameSidePartAdded()
        {
            // Manually notify the game-side scene
            return Task.WhenAll(Children.BreadthFirst(x => x.Children).Select(x => x.NotifyGameSidePartAdded()));
        }

        /// <summary>
        /// Requests to change the <see cref="IsLoaded"/> property.
        /// </summary>
        /// <param name="load"><c>true</c> to load, <c>false</c> to unload.</param>
        /// <param name="recursive"><c>true</c> to also load Children recursively; otherwise, <c>false</c>.</param>
        /// <returns>A task that completes when loading has been requested.</returns>
        public async Task RequestLoading(bool load, bool recursive = false)
        {
            if (IsLoading || IsLoaded == load)
                return;

            IsLoading = true;
            try
            {
                if (load)
                {
                    // Loading/unloading a prefab root always applies to all its content
                    SetEntityIsLoadedRecursively(true);
                    if (Editor != null)
                    {
                        var controllerProp = Editor.GetType().GetProperty("Controller");
                        var ctrl = controllerProp?.GetValue(Editor);
                        if (ctrl != null)
                        {
                            var loadEntitiesMethod = ctrl.GetType().GetMethod("LoadEntities", [typeof(PrefabRootViewModel)]);
                            if (loadEntitiesMethod != null)
                            {
                                var task = loadEntitiesMethod.Invoke(ctrl, [this]) as Task;
                                if (task != null)
                                    await task;
                            }
                        }
                    }
                    await NotifyGameSidePartAdded();
                }
                else
                {
                    // Loading/unloading a prefab root always applies to all its content
                    SetEntityIsLoadedRecursively(false);
                    if (Editor != null)
                    {
                        var controllerProp = Editor.GetType().GetProperty("Controller");
                        var ctrl = controllerProp?.GetValue(Editor);
                        if (ctrl != null)
                        {
                            var unloadEntitiesMethod = ctrl.GetType().GetMethod("UnloadEntities", [typeof(PrefabRootViewModel)]);
                            if (unloadEntitiesMethod != null)
                            {
                                var task = unloadEntitiesMethod.Invoke(ctrl, [this]) as Task;
                                if (task != null)
                                    await task;
                            }
                        }
                    }
                }
                IsLoaded = load;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Requests to change the <see cref="IsLocked"/> property.
        /// </summary>
        /// <param name="lock"><c>true</c> to lock, <c>false</c> to unlock.</param>
        /// <param name="recursive"><c>true</c> to also lock Children recursively; otherwise, <c>false</c>.</param>
        /// <returns>A task that completes when locking has been requested.</returns>
        public Task RequestLocking(bool @lock, bool recursive = false)
        {
            if (!IsLoaded || IsLocked == @lock)
                return Task.CompletedTask;

            IsLocked = @lock;
            // Locking/unlocking a prefab always applies to all its content
            SetEntityIsLockedRecursively(@lock);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the <see cref="EntityHierarchyItemViewModel.IsLoaded"/> property on all entities of the prefab.
        /// </summary>
        /// <param name="isLoaded"></param>
        public void SetEntityIsLoadedRecursively(bool isLoaded)
        {
            // This method is called from RequestLoading, therefore we don't need to request the entities to load.
            // We just need to update their status.
            foreach (var entity in Children.BreadthFirst(x => x.Children).OfType<EntityViewModel>())
            {
                entity.IsLoaded = isLoaded;
            }
        }

        /// <summary>
        /// Updates the <see cref="EntityHierarchyItemViewModel.IsLocked"/> property on all entities of the prefab.
        /// </summary>
        /// <param name="isLocked"></param>
        public void SetEntityIsLockedRecursively(bool isLocked)
        {
            // This method is called from RequestLocking, therefore we don't need to request the entities to lock.
            // We just need to update their status.
            foreach (var entity in Children.BreadthFirst(x => x.Children).OfType<EntityViewModel>())
            {
                entity.IsLocked = isLocked;
            }
        }

        #region IAssetPropertyProviderViewModel

        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        IObjectNode? IPropertyProviderViewModel.GetRootNode()
        {
            return Asset.Session?.AssetNodeContainer?.GetOrCreateNode(Asset.Asset);
        }

        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return new GraphNodePath(Asset.Session.AssetNodeContainer.GetOrCreateNode(Asset.Asset));
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => (Asset as IPropertyProviderViewModel)?.ShouldConstructMember(member) ?? true;

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => (Asset as IPropertyProviderViewModel)?.ShouldConstructItem(collection, index) ?? true;

        #endregion
    }
}
