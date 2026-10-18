// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels
{
    [AssetViewModel<AnimationAsset>]
    public class AnimationViewModel : ImportedAssetViewModel<AnimationAsset>
    {
        public AnimationViewModel(ConstructorParameters parameters)
            : base(parameters)
        {
        }

        protected override IAssetImporter GetImporter()
        {
            return AssetRegistry.FindImporterForFile(Asset.Source).OfType<ModelAssetImporter>().FirstOrDefault();
        }

        protected override void UpdateAssetFromSource(AnimationAsset assetToMerge)
        {
            AssetRootNode[nameof(AnimationAsset.AnimationTimeMaximum)].Update(assetToMerge.AnimationTimeMaximum);
            AssetRootNode[nameof(AnimationAsset.AnimationTimeMinimum)].Update(assetToMerge.AnimationTimeMinimum);
        }
    }
}

