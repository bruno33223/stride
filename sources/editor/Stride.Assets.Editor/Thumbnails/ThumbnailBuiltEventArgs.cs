// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// This enum describes the result of a thumbnail build operation.
/// </summary>
public enum ThumbnailBuildResult
{
    /// <summary>
    /// The build has either failed, or not been triggered due to previous failures.
    /// </summary>
    Failed = 0,

    /// <summary>
    /// The build has either been successfully executed, or already up-to-date.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// The build has been cancelled.
    /// </summary>
    Cancelled = 2,
}

/// <summary>
/// An event arguments class containing information about a thumbnail creation.
/// </summary>
[Stride.Core.DataContractIgnore]
public class ThumbnailBuiltEventArgs : Stride.Editor.Thumbnails.ThumbnailBuiltEventArgs
{
}
