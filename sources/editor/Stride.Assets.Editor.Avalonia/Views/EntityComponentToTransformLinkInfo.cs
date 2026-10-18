// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

public class EntityComponentToTransformLinkInfo : OneWayValueConverter<EntityComponentToTransformLinkInfo>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<EntityComponent> components)
            return AvaloniaProperty.UnsetValue;

        var modelNodeLinkComponent = components.OfType<ModelNodeLinkComponent>().FirstOrDefault();
        if (string.IsNullOrEmpty(modelNodeLinkComponent?.NodeName))
            return AvaloniaProperty.UnsetValue;

        if (modelNodeLinkComponent.Target != null && !string.IsNullOrEmpty(modelNodeLinkComponent.NodeName))
        {
            var entity = modelNodeLinkComponent.Target.Entity;
            return $"{entity.Name}.{modelNodeLinkComponent.NodeName}";
        }
        return modelNodeLinkComponent.NodeName;
    }
}
