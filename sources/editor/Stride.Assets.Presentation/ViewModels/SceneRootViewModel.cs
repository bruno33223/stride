// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Core.Threading;

namespace Stride.Assets.Presentation.ViewModels
{
    public sealed class SceneRootViewModel : EntityHierarchyRootViewModel, IAssetPropertyProviderViewModel
    {
        private readonly ObservableSet<SceneRootViewModel> childScenes = new ObservableSet<SceneRootViewModel>();
        private readonly AsyncLock? loadMutex;
        private readonly Guid sceneId;
        private Vector3 offset;
        private bool isLoaded = true;
        private bool isLoading;
        private bool isLocked;

        public SceneRootViewModel(SceneViewModel asset)
            : this(null, asset, null)
        {
        }

        public SceneRootViewModel(object? editor, SceneViewModel scene, AsyncLock? loadMutex)
            : base(scene)
        {
            this.loadMutex = loadMutex;
            sceneId = Guid.NewGuid();

            if (scene?.Children != null)
            {
                foreach (var childScene in scene.Children)
                {
                    var child = new SceneRootViewModel(editor, childScene, loadMutex) { Parent = this };
                    childScenes.Add(child);
                }
                AddItems(childScenes);
                scene.Children.CollectionChanged += SceneChildrenCollectionChanged;
            }
        }

        public IReadOnlyObservableList<SceneRootViewModel> ChildScenes => childScenes;

        public override AbsoluteId Id => new AbsoluteId(Asset.Id, sceneId);

        public override IEnumerable<EntityViewModel> InnerSubEntities => ChildrenExceptScenes.SelectMany(x => x.InnerSubEntities);

        public override string? Name { get => Asset.Name; set => throw new NotSupportedException($"Cannot change the name of a {nameof(SceneRootViewModel)} object."); }

        public Vector3 Offset { get => offset; set => SetValue(ref offset, value); }

        public override bool IsLoaded { get => isLoaded; set => SetValue(ref isLoaded, value); }

        public bool IsLoading { get => isLoading; set => SetValue(ref isLoading, value); }

        public override bool IsLocked { get => isLocked; set => SetValue(ref isLocked, value); }

        public SceneRootViewModel? ParentScene => Parent as SceneRootViewModel;

        public SceneViewModel SceneAsset => (SceneViewModel)Asset;

        public override int EntityCount => Children.Count(x => !(x is SceneRootViewModel));

        private int ChildScenesStartingIndex => Children.Count - childScenes.Count;

        private IEnumerable<EntityHierarchyItemViewModel> ChildrenExceptScenes => Children.Where(x => !(x is SceneRootViewModel));

        public void Delete()
        {
            ParentScene?.SceneAsset.Children.Remove(SceneAsset);
        }

        public async Task RequestLoading(bool load)
        {
            IsLoading = true;
            await Task.Yield();
            IsLoaded = load;
            IsLoading = false;
        }

        public void SaveSettings()
        {
        }

        private void SceneChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems?.Count > 0)
                    {
                        var sceneIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : childScenes.Count;
                        foreach (var scene in e.NewItems.Cast<SceneViewModel>())
                        {
                            var sceneRoot = new SceneRootViewModel(null, scene, loadMutex) { Parent = this };
                            InsertItem(sceneIndex + ChildScenesStartingIndex, sceneRoot);
                            childScenes.Insert(sceneIndex++, sceneRoot);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems?.Count > 0)
                    {
                        foreach (var scene in e.OldItems.Cast<SceneViewModel>())
                        {
                            var sceneRoot = childScenes.FirstOrDefault(r => r.Asset.Id == scene.Id);
                            if (sceneRoot != null)
                            {
                                RemoveItem(sceneRoot);
                                childScenes.Remove(sceneRoot);
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var root in childScenes.ToList())
                    {
                        RemoveItem(root);
                    }
                    childScenes.Clear();
                    break;
            }
        }

        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        IObjectNode? IPropertyProviderViewModel.GetRootNode()
        {
            return Asset.Session.AssetNodeContainer.GetOrCreateNode(SceneAsset.Asset);
        }

        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return new GraphNodePath(Asset.Session.AssetNodeContainer.GetOrCreateNode(SceneAsset.Asset));
        }

        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ((IPropertyProviderViewModel)Asset).ShouldConstructMember(member);

        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);
    }
}
