// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.BuildEngine;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Special <see cref="BuildStep"/> that executes thumbnail build commands.
/// </summary>
[Stride.Core.DataContractIgnore]
public class ThumbnailBuildStep : Stride.Editor.Thumbnails.ThumbnailBuildStep
{
    public ThumbnailBuildStep(Command command)
        : base(command)
    {
    }
}
