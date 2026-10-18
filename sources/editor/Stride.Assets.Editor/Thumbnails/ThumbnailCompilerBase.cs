// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Assets;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Base class for thumbnail compilers.
/// </summary>
/// <typeparam name="T">The type of asset.</typeparam>
public abstract class ThumbnailCompilerBase<T> : Stride.Editor.Thumbnails.ThumbnailCompilerBase<T> where T : Asset
{
}
