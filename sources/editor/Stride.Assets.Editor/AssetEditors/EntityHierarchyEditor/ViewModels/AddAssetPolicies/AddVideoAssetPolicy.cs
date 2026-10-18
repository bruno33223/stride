// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Assets.Media;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;
using Stride.Video;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies
{
    public class AddVideoAssetPolicy : CreateComponentPolicyBase<VideoAsset, AssetViewModel<VideoAsset>>
    {
        protected override EntityComponent CreateComponentFromAsset(EntityHierarchyItemViewModel parent, AssetViewModel<VideoAsset> asset)
        {
            return new VideoComponent
            {
                Source = ContentReferenceHelper.CreateReference<Video.Video>(asset)
            };
        }
    }
}
