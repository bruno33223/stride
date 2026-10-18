// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.View.Behaviors;

public static class DragDropBehavior
{
    /// <summary>
    /// Can't drop here message
    /// </summary>
    public static readonly string InvalidDropAreaMessage = Tr._p("Message", "Can't drop here");

    /// <summary>
    /// Default insert threshold in pixels
    /// </summary>
    public const double InsertThreshold = 4.0;
}
