// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

global using AssetDependenciesViewModel = Stride.Core.Assets.Presentation.ViewModels.AssetDependenciesViewModel;

namespace Stride.Core.Assets.Editor.ViewModels
{
    /// <summary>
    /// Type reference note: <see cref="Stride.Core.Assets.Presentation.ViewModels.AssetDependenciesViewModel"/> was moved
    /// to <c>Stride.Core.Assets.Presentation.ViewModels</c> in cross-platform architecture so that
    /// <see cref="Stride.Core.Assets.Presentation.ViewModels.AssetViewModel"/> can expose dependency tracking.
    /// This file provides the global alias and namespace parity for <c>Stride.Core.Assets.Editor</c>.
    /// </summary>
    public static class AssetDependenciesViewModelParity
    {
        public static Type UnderlyingType => typeof(Stride.Core.Assets.Presentation.ViewModels.AssetDependenciesViewModel);
    }
}
