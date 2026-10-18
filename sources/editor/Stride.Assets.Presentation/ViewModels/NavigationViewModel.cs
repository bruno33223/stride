// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Assets.Navigation;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels
{
    /// <summary>
    /// View model for <see cref="NavigationMeshAsset"/>.
    /// </summary>
    [AssetViewModel<NavigationMeshAsset>]
    public class NavigationViewModel : AssetViewModel<NavigationMeshAsset>
    {
        public NavigationViewModel(ConstructorParameters parameters)
            : base(parameters)
        {
        }
    }
}

namespace Stride.Assets.Presentation.ViewModel
{
    /// <summary>
    /// Legacy compatibility view model for <see cref="NavigationMeshAsset"/>.
    /// </summary>
    public class NavigationViewModel : ViewModels.NavigationViewModel
    {
        public NavigationViewModel(ConstructorParameters parameters)
            : base(parameters)
        {
        }
    }
}
