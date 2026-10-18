// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Assets.Presentation.ViewModels;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Assets.Entities;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies;
using Stride.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.SceneEditor.ViewModels.AddAssetPolicies
{
    public class AddSceneAssetPolicy : CustomPolicyBase<SceneAsset, SceneViewModel>
    {
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, SceneViewModel asset, AddChildModifiers modifiers, int index, out string message, params object[] messageArgs)
        {
            var currentRoot = (parent as SceneRootViewModel) ?? (parent?.Parent as SceneRootViewModel);
            if (currentRoot == null || index < currentRoot.EntityCount)
            {
                message = "Can only add a scene to another scene";
                return false;
            }
            if (!currentRoot.SceneAsset.CanBeParentOf(asset, out message, true))
            {
                return false;
            }
            message = "Add to this scene";
            return true;
        }

        protected override void ApplyPolicy(EntityHierarchyItemViewModel parent, IReadOnlyCollection<SceneViewModel> assets, int index, Vector3 position)
        {
            var currentRoot = (parent as SceneRootViewModel) ?? (parent?.Parent as SceneRootViewModel);
            var currentScene = currentRoot?.Asset as SceneViewModel;
            if (currentScene == null || currentRoot == null)
                return;

            index -= currentRoot.EntityCount;
            var commonRoots = GetCommonRoots(assets);
            foreach (var scene in commonRoots)
            {
                var rootIndex = currentScene.Children.IndexOf(scene);
                if (rootIndex >= 0 && rootIndex < index)
                    --index;

                scene.Parent?.Children.Remove(scene);
            }
            foreach (var scene in commonRoots)
            {
                currentScene.Children.Insert(index, scene);
                currentRoot.ChildScenes[index].Offset = position;
                currentRoot.ChildScenes[index].RequestLoading(true).Forget();
                ++index;
            }
        }

        private static ISet<SceneViewModel> GetCommonRoots(IReadOnlyCollection<SceneViewModel> items)
        {
            var hashSet = new HashSet<SceneViewModel>(items);
            foreach (var item in items)
            {
                var parent = item.Parent;
                while (parent != null)
                {
                    if (hashSet.Contains(parent))
                    {
                        hashSet.Remove(item);
                        break;
                    }
                    parent = parent.Parent;
                }
            }
            return hashSet;
        }
    }
}
