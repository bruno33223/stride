// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies
{
    public abstract class AddAssetPolicyBase<TAsset, TAssetViewModel> : IAddAssetPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        public bool Accept(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return typeof(TAsset) == type;
        }

        public bool CanAddOrInsert(EntityHierarchyItemViewModel parent, AssetViewModel asset, AddChildModifiers modifier, int index, out string message, params object[] messageArgs)
        {
            return CanAddOrInsert(parent, (TAssetViewModel)asset, modifier, index, out message, messageArgs);
        }

        protected abstract bool CanAddOrInsert(EntityHierarchyItemViewModel parent, TAssetViewModel asset, AddChildModifiers modifier, int index, out string message, params object[] messageArgs);
    }

    public abstract class CreateComponentPolicyBase<TAsset, TAssetViewModel> : AddAssetPolicyBase<TAsset, TAssetViewModel>, ICreateComponentPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        protected override bool CanAddOrInsert(EntityHierarchyItemViewModel parent, TAssetViewModel asset, AddChildModifiers modifier, int index, out string message, params object[] messageArgs)
        {
            message = string.Format("Add the selection into {0}", messageArgs);
            return true;
        }

        public EntityComponent? CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel asset)
        {
            return CreateComponentFromAsset(parent, (TAssetViewModel)asset);
        }

        protected abstract EntityComponent? CreateComponentFromAsset(EntityHierarchyItemViewModel parent, TAssetViewModel asset);
    }

    public abstract class CreateEntitiesPolicyBase<TAsset, TAssetViewModel> : AddAssetPolicyBase<TAsset, TAssetViewModel>, ICreateEntitiesPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        public AssetCompositeHierarchyData<EntityDesign, Entity>? CreateEntitiesFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel asset)
        {
            return CreateEntitiesFromAsset(parent, (TAssetViewModel)asset);
        }

        protected abstract AssetCompositeHierarchyData<EntityDesign, Entity>? CreateEntitiesFromAsset(EntityHierarchyItemViewModel parent, TAssetViewModel asset);
    }

    public abstract class CustomPolicyBase<TAsset, TAssetViewModel> : AddAssetPolicyBase<TAsset, TAssetViewModel>, ICustomPolicy
        where TAsset : Asset
        where TAssetViewModel : AssetViewModel, IAssetViewModel<TAsset>
    {
        public void ApplyPolicy(EntityHierarchyItemViewModel parent, IEnumerable<AssetViewModel> assets, int index, Vector3 position)
        {
            ApplyPolicy(parent, assets.Cast<TAssetViewModel>().ToList(), index, position);
        }

        protected abstract void ApplyPolicy(EntityHierarchyItemViewModel parent, IReadOnlyCollection<TAssetViewModel> assets, int index, Vector3 position);
    }
}
