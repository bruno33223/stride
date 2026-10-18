// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Annotations;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public class EnumToTooltip : OneWayValueConverter<EnumToTooltip>
{
    /// <inheritdoc />
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Value can be null when the control is removed from the visual tree and the related property is unbound.
        if (value == null || value == AvaloniaProperty.UnsetValue)
            return null;

        var stringValue = value.ToString();
        if (stringValue == null)
            return null;

        var members = value.GetType().GetMember(stringValue);
        if (members.Length == 0)
            return stringValue;

        var member = members[0];
        var attribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<TranslationAttribute>(member);
        var assembly = value.GetType().Assembly;
        return attribute != null
            ? (string.IsNullOrEmpty(attribute.Context)
                ? TranslationManager.Instance.GetString(attribute.Text, assembly)
                : TranslationManager.Instance.GetParticularString(attribute.Context, attribute.Text, assembly))
            : stringValue;
    }
}
