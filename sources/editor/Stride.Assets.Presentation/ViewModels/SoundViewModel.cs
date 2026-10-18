// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Media;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="SoundAsset"/>.
/// </summary>
[AssetViewModel<SoundAsset>]
public class SoundViewModel : AssetViewModel<SoundAsset>
{
    public SoundViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }
}
