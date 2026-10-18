// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Stride.Assets.Entities;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using SessionStateChangedEventArgs = Stride.Core.Assets.Presentation.ViewModels.SessionStateChangedEventArgs;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Serialization;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Presentation.ViewModels
{
    [AssetViewModel<SceneAsset>]
    public sealed class SceneViewModel : EntityHierarchyViewModel, IAssetViewModel<SceneAsset>
    {
        private readonly IObjectNode childrenNode;
        private readonly IMemberNode parentNode;
        private SceneViewModel? parent;

        private bool isInitialized;
        private bool updatingChildren;
        private bool updatingParent;

        public SceneViewModel(ConstructorParameters parameters)
            : base(parameters)
        {
            SetAsDefaultCommand = new AnonymousCommand(ServiceProvider, SetAsDefault);
            UpdateCommands();

            Session.SessionStateChanged += SessionStateChanged;

            var assetNode = Session.AssetNodeContainer.GetNode(Asset);
            childrenNode = assetNode[nameof(SceneAsset.ChildrenIds)].Target!;
            parentNode = assetNode[nameof(SceneAsset.Parent)];
        }

        /// <inheritdoc />
        public new SceneAsset Asset => (SceneAsset)base.Asset;

        public IObservableList<SceneViewModel> Children { get; } = new ObservableSet<SceneViewModel>();

        public SceneViewModel? Parent { get => parent; private set => SetValueUncancellable(ref parent, value, ParentPropertyChanged); }

        public ICommandBase SetAsDefaultCommand { get; }

        public bool CanBeParentOf(SceneViewModel child, out string message, bool checkSameParent = false)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));

            if (child == this)
            {
                message = "Can't be a parent of itself";
                return false;
            }
            if (checkSameParent && child.Parent == this)
            {
                message = "Scene already has this parent";
                return false;
            }
            var scene = Parent;
            while (scene != null)
            {
                if (scene == child)
                {
                    message = "Circular scene hierarchy";
                    return false;
                }
                scene = scene.Parent;
            }
            message = "Can be a parent of this scene";
            return true;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(SceneViewModel));

            parentNode.ValueChanged -= ParentNodeValueChanged;
            childrenNode.ItemChanged -= ChildrenNodeItemChanged;
            Children.CollectionChanged -= ChildrenCollectionChanged;
            Session.SessionStateChanged -= SessionStateChanged;

            base.Destroy();
        }

        public SceneViewModel GetRoot()
        {
            var root = this;
            var hash = new HashSet<AssetId> { root.Id };
            while (root.Parent != null)
            {
                if (!hash.Add(root.Parent.Id))
                {
                    GlobalLogger.GetLogger("Asset").Error($"Cyclic reference detected for scene {root.Name} (Id={root.Id}).");
                    break;
                }
                root = root.Parent;
            }
            return root;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            var invalidIndices = new Stack<int>();
            for (var i = 0; i < Asset.ChildrenIds.Count; i++)
            {
                var childId = Asset.ChildrenIds[i];
                var childScene = Session.GetAssetById(childId) as SceneViewModel;
                if (childScene == null)
                {
                    invalidIndices.Push(i);
                    continue;
                }

                var parentValue = childScene.parentNode.Retrieve();
                var parentReference = AttachedReferenceManager.GetAttachedReference(parentValue);
                if (parentReference?.Id != Id)
                {
                    invalidIndices.Push(i);
                    if (childScene.Parent?.isInitialized == true)
                    {
                        Debug.Assert(childScene.Parent.Children.Contains(childScene));
                    }
                    else
                    {
                        FixupParentChild(childScene, parentReference);
                    }
                }
                else
                {
                    childScene.parent = this;
                    Children.Add(childScene);
                }
            }
            foreach (var index in invalidIndices)
            {
                childrenNode.Remove(Asset.ChildrenIds[index], new NodeIndex(index));
            }

            if (Parent?.isInitialized == true)
            {
                Debug.Assert(Parent.Children.Contains(this));
            }
            else
            {
                var parentValue = parentNode.Retrieve();
                var parentReference = AttachedReferenceManager.GetAttachedReference(parentValue);
                FixupParentChild(this, parentReference);
            }

            Children.CollectionChanged += ChildrenCollectionChanged;
            childrenNode.ItemChanged += ChildrenNodeItemChanged;
            parentNode.ValueChanged += ParentNodeValueChanged;
            isInitialized = true;
        }

        private static void FixupParentChild(SceneViewModel childScene, AttachedReference? parentReference)
        {
            if (parentReference == null)
            {
                return;
            }

            var parent = childScene.Session.GetAssetById(parentReference.Id) as SceneViewModel;
            if (parent == null)
            {
                childScene.Parent = null;
                return;
            }

            if (!parent.isInitialized)
            {
                if (!parent.Asset.ChildrenIds.Contains(childScene.Id))
                {
                    parent.childrenNode.Add(childScene.Id);
                }
                return;
            }

            childScene.parent = parent;
            parent.Children.Add(childScene);
        }

        /// <inheritdoc />
        protected override void UpdateIsDeletedStatus()
        {
            base.UpdateIsDeletedStatus();
            if (IsDeleted)
            {
                Parent?.Children.Remove(this);
            }
        }

        private void ChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (updatingChildren || UndoRedoService.UndoRedoInProgress)
                return;

            try
            {
                updatingChildren = true;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems?.Count > 0)
                        {
                            foreach (var scene in e.OldItems.Cast<SceneViewModel>())
                            {
                                scene.Parent = null;
                                childrenNode.Remove(scene.Id, new NodeIndex(e.OldStartingIndex));
                            }
                        }
                        if (e.NewItems?.Count > 0)
                        {
                            var index = e.NewStartingIndex;
                            foreach (var scene in e.NewItems.Cast<SceneViewModel>())
                            {
                                if (scene.Parent != this)
                                {
                                    scene.Parent?.Children.Remove(scene);
                                    scene.Parent = this;
                                }
                                childrenNode.Add(scene.Id, new NodeIndex(index++));
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems?.Count > 0)
                        {
                            var index = e.NewStartingIndex;
                            foreach (var scene in e.NewItems.Cast<SceneViewModel>())
                            {
                                if (scene.Parent != this)
                                {
                                    scene.Parent?.Children.Remove(scene);
                                    scene.Parent = this;
                                }
                                childrenNode.Add(scene.Id, new NodeIndex(index++));
                            }
                        }
                        break;

                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Reset:
                        throw new NotSupportedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                updatingChildren = false;
            }
        }

        private void ChildrenNodeItemChanged(object? sender, ItemChangeEventArgs e)
        {
            if (updatingChildren)
                return;

            try
            {
                updatingChildren = true;
                switch (e.ChangeType)
                {
                    case ContentChangeType.CollectionUpdate:
                    case ContentChangeType.CollectionAdd:
                    case ContentChangeType.CollectionRemove:
                        AssetId childId;
                        SceneViewModel? childScene;
                        if (e.OldValue != null)
                        {
                            childId = (AssetId)e.OldValue;
                            childScene = (SceneViewModel?)Session.GetAssetById(childId);
                            if (childScene != null)
                            {
                                childScene.Parent = null;
                                Children.Remove(childScene);
                            }
                        }
                        if (e.NewValue != null)
                        {
                            childId = (AssetId)e.NewValue;
                            childScene = (SceneViewModel?)Session.GetAssetById(childId);
                            if (childScene != null)
                            {
                                childScene.Parent?.Children.Remove(childScene);
                                childScene.Parent = this;
                                Children.Insert(e.Index.Int, childScene);
                            }
                        }
                        break;

                    case ContentChangeType.None:
                    case ContentChangeType.ValueChange:
                        throw new InvalidOperationException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                updatingChildren = false;
            }
        }

        private void ParentNodeValueChanged(object? sender, MemberNodeChangeEventArgs e)
        {
            if (updatingParent)
                return;

            try
            {
                updatingParent = true;

                var oldParentReference = AttachedReferenceManager.GetAttachedReference(e.OldValue);
                if (oldParentReference != null)
                {
                    var oldParent = (SceneViewModel?)Session.GetAssetById(oldParentReference.Id);
                    oldParent?.Children.Remove(this);
                }

                var newParentReference = AttachedReferenceManager.GetAttachedReference(e.NewValue);
                if (newParentReference != null)
                {
                    var newParent = (SceneViewModel?)Session.GetAssetById(newParentReference.Id);
                    if (newParent != null)
                        newParent.Children.Add(this);
                    else
                        Parent = null;
                }
            }
            finally
            {
                updatingParent = false;
            }
        }

        private void ParentPropertyChanged()
        {
            if (updatingParent || UndoRedoService.UndoRedoInProgress)
                return;

            try
            {
                updatingParent = true;
                var newValue = parent != null ? AttachedReferenceManager.CreateProxyObject<Scene>(parent.Id, parent.AssetItem.Location) : null;
                parentNode.Update(newValue);
            }
            finally
            {
                updatingParent = false;
            }
        }

        private void SessionStateChanged(object? sender, Stride.Core.Assets.Presentation.ViewModels.SessionStateChangedEventArgs e)
        {
            UpdateCommands();
        }

        private void SetAsDefault()
        {
            if (!SetAsDefaultCommand.IsEnabled)
                return;

            var gameSettings = Session.CurrentProject?.Assets.OfType<GameSettingsViewModel>().FirstOrDefault();
            if (gameSettings == null)
                return;
            gameSettings.DefaultScene = this;
        }

        private void UpdateCommands()
        {
            SetAsDefaultCommand.IsEnabled = Session.CurrentProject?.Assets.OfType<SceneViewModel>().Contains(this) ?? false;
        }
    }
}
