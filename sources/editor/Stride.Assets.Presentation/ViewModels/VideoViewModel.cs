// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Media;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="VideoAsset"/>.
/// </summary>
[AssetViewModel<VideoAsset>]
public class VideoViewModel : AssetViewModel<VideoAsset>
{
    public VideoViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }
}
