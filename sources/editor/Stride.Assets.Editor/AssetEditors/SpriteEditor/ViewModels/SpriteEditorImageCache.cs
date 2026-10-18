// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.TextureConverter;

namespace Stride.Assets.Editor.ViewModels;

public class SpriteEditorImageCache : IDisposable
{
    private const int CacheSize = 30;

    private sealed class CacheEntry : IDisposable
    {
        public required UFile File { get; init; }
        public required DateTime LastWriteTime { get; init; }
        public required TexImage Image { get; init; }

        public void Dispose()
        {
            Image.Dispose();
        }
    }

    private readonly List<CacheEntry> cache = new(CacheSize);
    private readonly object lockObject = new();
    private bool isDisposed;

    public void Dispose()
    {
        lock (lockObject)
        {
            if (isDisposed)
                return;

            isDisposed = true;
            foreach (var entry in cache)
            {
                entry.Dispose();
            }
            cache.Clear();
        }
    }

    public Rectangle? FindSpriteRegion(UFile? source, Vector2 initialPoint, bool useTransparency, Color maskColor)
    {
        if (source == null)
            return null;

        lock (lockObject)
        {
            if (isDisposed)
                return null;

            var entry = GetOrCreateEntryLocked(source);
            if (entry == null)
                return null;

            var pixelPoint = new Int2((int)initialPoint.X, (int)initialPoint.Y);

            try
            {
                using var texTool = new TextureTool();
                return useTransparency
                    ? texTool.FindSpriteRegion(entry.Image, pixelPoint)
                    : texTool.FindSpriteRegion(entry.Image, pixelPoint, maskColor, 0x00ffffff);
            }
            catch
            {
                return null;
            }
        }
    }

    public Size2? GetPixelSize(UFile? source)
    {
        if (source == null)
            return null;

        lock (lockObject)
        {
            if (isDisposed)
                return null;

            var entry = GetOrCreateEntryLocked(source);
            if (entry == null)
                return null;

            return new Size2(entry.Image.Width, entry.Image.Height);
        }
    }

    public object? RetrieveImage(UFile? filePath)
    {
        if (filePath == null)
            return null;

        lock (lockObject)
        {
            if (isDisposed)
                return null;

            var entry = GetOrCreateEntryLocked(filePath);
            return entry?.Image;
        }
    }

    public Color? PickPixelColor(UFile? source, Vector2 initialPoint)
    {
        if (source == null)
            return null;

        lock (lockObject)
        {
            if (isDisposed)
                return null;

            var entry = GetOrCreateEntryLocked(source);
            if (entry == null)
                return null;

            var pixelPoint = new Int2((int)initialPoint.X, (int)initialPoint.Y);

            try
            {
                using var texTool = new TextureTool();
                return texTool.PickColor(entry.Image, pixelPoint);
            }
            catch
            {
                return null;
            }
        }
    }

    private CacheEntry? GetOrCreateEntryLocked(UFile filePath)
    {
        if (filePath == null || !File.Exists(filePath) || isDisposed)
            return null;

        var lastWrite = File.GetLastWriteTime(filePath);
        var entry = cache.FirstOrDefault(x => x.File == filePath);
        if (entry != null)
        {
            if (lastWrite == entry.LastWriteTime)
            {
                // LRU: move re-used entry to the end
                cache.Remove(entry);
                cache.Add(entry);
                return entry;
            }
            cache.Remove(entry);
            entry.Dispose();
        }

        TexImage? texImage = null;
        try
        {
            using var texTool = new TextureTool();
            texImage = texTool.Load(filePath, false);
            texTool.Decompress(texImage, texImage.Format.IsSRgb);
            if (texImage.Format == PixelFormat.R16G16B16A16_UNorm)
                texTool.Convert(texImage, PixelFormat.R8G8B8A8_UNorm);

            var newEntry = new CacheEntry
            {
                File = filePath,
                LastWriteTime = lastWrite,
                Image = texImage,
            };

            cache.Add(newEntry);
            if (cache.Count > CacheSize)
            {
                var oldest = cache[0];
                cache.RemoveAt(0);
                oldest.Dispose();
            }

            return newEntry;
        }
        catch
        {
            texImage?.Dispose();
            return null;
        }
    }
}
