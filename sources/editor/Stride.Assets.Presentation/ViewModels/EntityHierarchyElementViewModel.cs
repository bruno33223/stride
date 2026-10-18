// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;

namespace Stride.Assets.Presentation.ViewModels
{
    public abstract class EntityHierarchyElementViewModel : EntityHierarchyItemViewModel, IAssetPropertyProviderViewModel
    {
        private readonly object assetSideInstance;
        private bool isLoaded = true;
        private bool isLoading;
        private bool isLocked;

        protected EntityHierarchyElementViewModel([NotNull] EntityHierarchyViewModel asset, [NotNull] IEnumerable<EntityDesign> childEntities, object assetSideInstance)
            : base(asset, childEntities)
        {
            this.assetSideInstance = assetSideInstance;
            LoadCommand = new AnonymousTaskCommand<bool>(ServiceProvider, recursive => RequestLoading(!IsLoaded, recursive));
            LockCommand = new AnonymousTaskCommand<bool>(ServiceProvider, recursive => RequestLocking(!IsLocked, recursive));

            DependentProperties.Add(nameof(IsLoaded), new[] { nameof(IsSelectable) });
            DependentProperties.Add(nameof(IsLocked), new[] { nameof(IsSelectable) });
        }

        public abstract override AbsoluteId Id { get; }

        /// <summary>
        /// Indicates whether this element is currently loaded in the scene game.
        /// </summary>
        public override bool IsLoaded
        {
            get => isLoaded;
            set
            {
                SetValue(ref isLoaded, value, () =>
                {
                    IsLoading = false;
                    UpdateSelectables();
                });
            }
        }

        /// <summary>
        /// Indicates whether this element is currently being loaded in (or unloaded from) the scene game.
        /// </summary>
        public bool IsLoading
        {
            get => isLoading;
            protected set => SetValue(ref isLoading, value, UpdateCommands);
        }

        /// <summary>
        /// Indicates whether this element is locked in the scene game.
        /// </summary>
        public override bool IsLocked
        {
            get => isLocked;
            set => SetValue(ref isLocked, value, UpdateSelectables);
        }

        /// <summary>
        /// Indicates whether this element can be selected in the scene game.
        /// </summary>
        public bool IsSelectable => IsLoaded && !IsLocked;

        [NotNull]
        public ICommandBase LoadCommand { get; }

        [NotNull]
        public ICommandBase LockCommand { get; }

        /// <inheritdoc/>
        public override EntityHierarchyElementViewModel Owner => this;

        /// <summary>
        /// Gets the number of entity in this object.
        /// </summary>
        public abstract override int EntityCount { get; }

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

        /// <inheritdoc/>
        public override int FindIndexInParent(int indexInEntity, [NotNull] IEnumerable<EntityDesign> entityCollection)
        {
            var indexInParent = 0;
            var i = 0;
            foreach (var entity in entityCollection)
            {
                if (i++ == indexInEntity)
                    break;

                if (string.IsNullOrEmpty(entity.Folder))
                    ++indexInParent;
            }
            return indexInParent;
        }

        /// <summary>
        /// Gets the index of the given entity in this object. This method ignores the concept of folder.
        /// </summary>
        public abstract override int IndexOfEntity([NotNull] EntityViewModel entity);

        /// <summary>
        /// Requests to change the <see cref="IsLoaded"/> property.
        /// </summary>
        public Task RequestLoading(bool load, bool recursive = false)
        {
            if (IsLoading || IsLoaded == load)
                return Task.CompletedTask;

            return OnLoadingRequested(load, recursive);
        }

        /// <summary>
        /// Requests to change the <see cref="IsLocked"/> property.
        /// </summary>
        public Task RequestLocking(bool @lock, bool recursive = false)
        {
            if (!IsLoaded || IsLocked == @lock)
                return Task.CompletedTask;

            return OnLockingRequested(@lock, recursive);
        }

        public virtual void UpdateNodePresenter(INodePresenter node)
        {
        }

        public virtual void FinalizeNodePresenterTree(IAssetNodePresenter root)
        {
        }

        /// <summary>
        /// Called whenever the value of <see cref="IsLoaded"/> property is requested to change.
        /// </summary>
        protected abstract Task OnLoadingRequested(bool load, bool recursive);

        /// <summary>
        /// Called whenever the value of <see cref="IsLocked"/> property is requested to change.
        /// </summary>
        protected abstract Task OnLockingRequested(bool @lock, bool recursive);

        private void UpdateCommands()
        {
            LoadCommand.IsEnabled = !IsLoading;
            LockCommand.IsEnabled = IsLoaded;
        }

        private void UpdateSelectables()
        {
        }

        /// <inheritdoc/>
        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            return Asset.Session.AssetNodeContainer.GetOrCreateNode(assetSideInstance);
        }

        /// <inheritdoc/>
        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return GetNodePath();
        }

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ((IPropertyProviderViewModel)Asset).ShouldConstructMember(member);

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);
    }
}
