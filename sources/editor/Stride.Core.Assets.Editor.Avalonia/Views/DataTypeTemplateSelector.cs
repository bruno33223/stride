// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public class DataTypeTemplateSelector : AvaloniaObject, IDataTemplate
{
    [Content]
    public TemplateDefinitionCollection TemplateDefinitions { get; } = new TemplateDefinitionCollection();

    public virtual Control? Build(object? param)
    {
        if (param == null || TemplateDefinitions.Count == 0)
            return null;

        var template = TemplateDefinitions.FirstOrDefault(t => t.DataType?.IsInstanceOfType(param) == true);
        return template?.DataTemplate?.Build(param);
    }

    public virtual bool Match(object? data)
    {
        if (data == null || TemplateDefinitions.Count == 0)
            return false;

        return TemplateDefinitions.Any(t => t.DataType?.IsInstanceOfType(data) == true);
    }
}

public class TemplateDefinitionCollection : Collection<TemplateDefinition> { }

public class TemplateDefinition : AvaloniaObject
{
    public static readonly StyledProperty<Type?> DataTypeProperty =
        AvaloniaProperty.Register<TemplateDefinition, Type?>(nameof(DataType));

    public static readonly StyledProperty<IDataTemplate?> DataTemplateProperty =
        AvaloniaProperty.Register<TemplateDefinition, IDataTemplate?>(nameof(DataTemplate));

    public Type? DataType
    {
        get => GetValue(DataTypeProperty);
        set => SetValue(DataTypeProperty, value);
    }

    public IDataTemplate? DataTemplate
    {
        get => GetValue(DataTemplateProperty);
        set => SetValue(DataTemplateProperty, value);
    }
}
