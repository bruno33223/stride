// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Assets.Editor.Avalonia.Views;

public class UFileToBitmapImage : OneWayMultiValueConverter<UFileToBitmapImage>
{
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
            return null;

        var filePath = values[0] as UFile;
        var spriteCache = values[1] as SpriteEditorImageCache;
        if (filePath == null || spriteCache == null)
            return null;

        return spriteCache.RetrieveImage(filePath);
    }
}
