// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public class AssetViewModelToUrl : ValueConverterBase<AssetViewModelToUrl>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var asset = value as AssetViewModel;
        return asset != null && asset.Id != AssetId.Empty ? asset.Url : ContentReferenceHelper.EmptyReference;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var url = value as string;
        var asset = SessionViewModel.Instance?.AllAssets.FirstOrDefault(x => x.Url == url);
        return asset;
    }
}
