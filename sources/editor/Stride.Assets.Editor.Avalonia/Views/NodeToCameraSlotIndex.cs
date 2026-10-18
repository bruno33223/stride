// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.Avalonia.Views;

public class NodeToCameraSlotIndex : OneWayMultiValueConverter<NodeToCameraSlotIndex>
{
    /// <inheritdoc/>
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not SceneCameraSlotId slotIndex)
            return "(Invalid camera index)";

        var slots = values[1] as IList<AbstractNodeEntry>;
        if (slots != null)
        {
            foreach (var slot in slots.OfType<AbstractNodeValue>().Where(x => x.Value is SceneCameraSlotId))
            {
                if (slot.Value is SceneCameraSlotId slotId && slotId.Id == slotIndex.Id)
                    return slot.DisplayValue;
            }
        }

        return "(Invalid camera index)";
    }
}
