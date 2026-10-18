// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets;

namespace Stride.Core.Assets.Editor.Services;

public class ThumbnailCompletedArgs : EventArgs
{
    public ThumbnailCompletedArgs(AssetId assetId, ThumbnailData? data)
    {
        AssetId = assetId;
        Data = data;
    }

    public ThumbnailCompletedArgs(AssetId assetId, Stride.Core.Assets.Presentation.ViewModels.ThumbnailData? data)
    {
        AssetId = assetId;
        Data = data;
    }

    public AssetId AssetId { get; private set; }

    public Stride.Core.Assets.Presentation.ViewModels.ThumbnailData? Data { get; private set; }
}
