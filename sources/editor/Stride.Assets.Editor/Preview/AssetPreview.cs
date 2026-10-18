// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.Assets.Editor.Preview.ViewModel;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Preview;

/// <summary>
/// A base implementation for the <see cref="IAssetPreview"/>.
/// </summary>
public abstract class AssetPreview : Stride.Editor.Preview.AssetPreview, IAssetPreview
{
    new public IAssetPreviewViewModel? PreviewViewModel
    {
        get => base.PreviewViewModel as IAssetPreviewViewModel;
        protected set => base.PreviewViewModel = value;
    }

    public async Task<object?> Initialize(AssetViewModel asset, IPreviewBuilder builder)
    {
        return await base.Initialize(asset, (Stride.Editor.Preview.IPreviewBuilder)builder);
    }
}

/// <summary>
/// A specialization of the <see cref="AssetPreview"/> class that specifies the related asset type as a generic argument.
/// </summary>
/// <typeparam name="T">The type of asset this class manages to preview.</typeparam>
public abstract class AssetPreview<T> : AssetPreview where T : Asset
{
    protected T Asset => (T)AssetItem.Asset;
}
