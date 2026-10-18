// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Abstract base class for thumbnail presentation data.
/// </summary>
public abstract class ThumbnailData : Stride.Core.Assets.Presentation.ViewModels.ThumbnailData
{
    protected ThumbnailData(ObjectId thumbnailId)
        : base(thumbnailId)
    {
    }
}

/// <summary>
/// Generic resource support for thumbnails.
/// </summary>
public class ResourceThumbnailData : ThumbnailData
{
    private object? resourceKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceThumbnailData"/> class.
    /// </summary>
    /// <param name="thumbnailId">The thumbnail identifier.</param>
    /// <param name="resourceKey">The key used to fetch the resource.</param>
    public ResourceThumbnailData(ObjectId thumbnailId, object resourceKey)
        : base(thumbnailId)
    {
        this.resourceKey = resourceKey;
    }

    /// <summary>
    /// Gets the resource key.
    /// </summary>
    public object? ResourceKey => resourceKey;

    /// <inheritdoc />
    protected override object? BuildImageSource()
    {
        return resourceKey;
    }

    /// <inheritdoc />
    protected override void FreeBuildingResources()
    {
        resourceKey = null;
    }
}

/// <summary>
/// Byte streams bitmap support for thumbnails.
/// </summary>
public class BitmapThumbnailData : ThumbnailData
{
    private static readonly ObjectCache<ObjectId, object> Cache = new(512);
    private Stream? thumbnailBitmapStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitmapThumbnailData"/> class.
    /// </summary>
    /// <param name="thumbnailId">The thumbnail identifier.</param>
    /// <param name="thumbnailBitmapStream">The stream containing bitmap data.</param>
    public BitmapThumbnailData(ObjectId thumbnailId, Stream thumbnailBitmapStream)
        : base(thumbnailId)
    {
        this.thumbnailBitmapStream = thumbnailBitmapStream;
    }

    /// <summary>
    /// Gets the stream containing the thumbnail bitmap data.
    /// </summary>
    public Stream? ThumbnailBitmapStream => thumbnailBitmapStream;

    /// <inheritdoc />
    protected override object? BuildImageSource()
    {
        return BuildAsBitmapImage(thumbnailId, thumbnailBitmapStream);
    }

    /// <inheritdoc />
    protected override void FreeBuildingResources()
    {
        thumbnailBitmapStream?.Dispose();
        thumbnailBitmapStream = null;
    }

    private static object? BuildAsBitmapImage(ObjectId thumbnailId, Stream? thumbnailStream)
    {
        if (thumbnailStream == null)
            return null;

        if (!thumbnailStream.CanRead)
            return null;

        var result = Cache.TryGet(thumbnailId);
        if (result != null)
            return result;

        try
        {
            if (thumbnailStream.CanSeek)
                thumbnailStream.Position = 0;

            var memoryStream = new MemoryStream();
            thumbnailStream.CopyTo(memoryStream);
            var bytes = memoryStream.ToArray();
            Cache.Cache(thumbnailId, bytes);
            return bytes;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
