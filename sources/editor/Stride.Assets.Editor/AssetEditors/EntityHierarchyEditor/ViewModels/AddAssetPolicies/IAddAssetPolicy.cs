// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies
{
    public interface IAddAssetPolicy
    {
        bool Accept(Type type);

        bool CanAddOrInsert(EntityHierarchyItemViewModel parent, AssetViewModel asset, AddChildModifiers modifier, int index, out string message, params object[] messageArgs);
    }

    public interface ICreateComponentPolicy : IAddAssetPolicy
    {
        EntityComponent? CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel asset);
    }

    public interface ICreateEntitiesPolicy : IAddAssetPolicy
    {
        AssetCompositeHierarchyData<EntityDesign, Entity>? CreateEntitiesFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel asset);
    }

    public interface ICustomPolicy : IAddAssetPolicy
    {
        void ApplyPolicy(EntityHierarchyItemViewModel parent, IEnumerable<AssetViewModel> assets, int index, Vector3 position);
    }
}
