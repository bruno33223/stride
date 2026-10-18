// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;

using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels.IEditorDesignPartViewModel<,>))]

namespace Stride.Assets.Editor.AssetEditors.AssetCompositeGameEditor.ViewModels
{
    /// <summary>
    /// Forwarding interface alias for <see cref="Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels.IEditorDesignPartViewModel{TAssetPartDesign, TAssetPart}"/>.
    /// </summary>
    public interface IEditorDesignPartViewModel<out TAssetPartDesign, TAssetPart>
        : Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels.IEditorDesignPartViewModel<TAssetPartDesign, TAssetPart>
        where TAssetPartDesign : IAssetPartDesign<TAssetPart>
        where TAssetPart : IIdentifiable
    {
    }
}
