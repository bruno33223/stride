// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.View.Behaviors;

public static class DragDropHelper
{
    public static DragContainer? GetDragContainer(object? dataObject)
    {
        if (dataObject is DragContainer container)
            return container;

        return null;
    }

    public static IReadOnlyCollection<object>? GetItemsToDrop(DragContainer? dragContainer, object? dataObject)
    {
        if (dragContainer != null)
        {
            return dragContainer.Items;
        }

        if (dataObject is IEnumerable<string> fileStrings)
        {
            return fileStrings.Where(UFile.IsValid).Select(x => new UFile(x)).Cast<object>().ToArray();
        }

        if (dataObject is object[] dataArray)
        {
            return dataArray;
        }

        return null;
    }

    public static bool ShouldDisplayDropAdorner(DisplayDropAdorner rule, IEnumerable<object>? itemsToDrop)
    {
        if (itemsToDrop == null)
            return false;

        return rule switch
        {
            DisplayDropAdorner.Never => false,
            DisplayDropAdorner.InternalOnly => itemsToDrop.All(x => x is not UFile),
            DisplayDropAdorner.ExternalOnly => itemsToDrop.All(x => x is UFile),
            DisplayDropAdorner.Always => true,
            _ => throw new ArgumentOutOfRangeException(nameof(rule)),
        };
    }
}
