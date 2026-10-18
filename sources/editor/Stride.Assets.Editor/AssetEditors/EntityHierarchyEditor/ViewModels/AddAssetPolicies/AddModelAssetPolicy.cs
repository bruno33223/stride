// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies
{
    public class AddModelAssetPolicy<TModelAsset> : CreateComponentPolicyBase<TModelAsset, AssetViewModel<TModelAsset>>
        where TModelAsset : Asset, IModelAsset
    {
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel<TModelAsset> asset)
        {
            return new ModelComponent
            {
                Model = ContentReferenceHelper.CreateReference<Model>(asset)
            };
        }
    }

    public class AddModelAssetPolicy : AddModelAssetPolicy<ModelAsset>
    {
    }
}
