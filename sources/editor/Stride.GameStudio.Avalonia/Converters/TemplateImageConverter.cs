// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Stride.GameStudio.Avalonia.Converters;

public sealed class TemplateImageConverter : IValueConverter
{
    private static readonly ConcurrentDictionary<string, Bitmap> ImageCache = new();
    private static Bitmap? defaultIcon;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Bitmap bitmap)
            return bitmap;

        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            if (ImageCache.TryGetValue(path, out var cachedBitmap))
                return cachedBitmap;

            try
            {
                if (File.Exists(path))
                {
                    var loadedBitmap = new Bitmap(path);
                    return ImageCache.GetOrAdd(path, loadedBitmap);
                }
            }
            catch
            {
                // Fallback to default
            }
        }

        return GetDefaultIcon();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static Bitmap? GetDefaultIcon()
    {
        if (defaultIcon != null)
            return defaultIcon;

        try
        {
            var uri = new Uri("avares://Stride.GameStudio.Avalonia/Assets/default-template-icon.png");
            if (AssetLoader.Exists(uri))
            {
                defaultIcon = new Bitmap(AssetLoader.Open(uri));
            }
        }
        catch
        {
        }
        return defaultIcon;
    }
}
