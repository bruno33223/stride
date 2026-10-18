// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestPropertyTemplateProviders
{
    private class DummyTemplate : IDataTemplate
    {
        public Control? Build(object? param) => new TextBlock { Text = param?.ToString() };
        public bool Match(object? data) => true;
    }

    [Fact]
    public void TestDataTypeTemplateSelectorMatchAndBuild()
    {
        var selector = new DataTypeTemplateSelector();
        var template = new DummyTemplate();

        selector.TemplateDefinitions.Add(new TemplateDefinition
        {
            DataType = typeof(string),
            DataTemplate = template
        });

        Assert.True(selector.Match("Hello World"));
        Assert.False(selector.Match(12345));

        var control = selector.Build("Hello World");
        Assert.NotNull(control);
        Assert.IsType<TextBlock>(control);
        Assert.Equal("Hello World", ((TextBlock)control).Text);

        var nullControl = selector.Build(12345);
        Assert.Null(nullControl);
    }

    private static DataTemplate DummyTemplateInstance => new DataTemplate();

    [Fact]
    public void TestGenericDictionaryNodeTemplateProvider()
    {
        var provider = new GenericDictionaryNodeTemplateProvider
        {
            Template = DummyTemplateInstance,
            KeyType = typeof(int),
            ValueType = typeof(string)
        };

        Assert.Equal("Dictionary<Int32,String>", provider.Name);
        Assert.Equal(typeof(int), provider.KeyType);
        Assert.Equal(typeof(string), provider.ValueType);
    }

    [Fact]
    public void TestNullableStructTemplateProvider()
    {
        var provider = new NullableStructTemplateProvider { Template = DummyTemplateInstance };
        Assert.Equal("NullableStruct", provider.Name);
    }

    [Fact]
    public void TestObjectTemplateProvider()
    {
        var provider = new ObjectTemplateProvider { Template = DummyTemplateInstance };
        Assert.Equal("Object", provider.Name);
    }

    [Fact]
    public void TestModelComponentMaterialTemplateProvider()
    {
        var provider = new ModelComponentMaterialTemplateProvider { Template = DummyTemplateInstance };
        Assert.Equal("ModelComponentMaterial", provider.Name);
        Assert.True(provider.DynamicThumbnail);
    }

    [Fact]
    public void TestScriptVariableReferenceTemplateProvider()
    {
        var provider = new ScriptVariableReferenceTemplateProvider { Template = DummyTemplateInstance };
        Assert.Equal(nameof(ScriptVariableReferenceTemplateProvider), provider.Name);
    }

    [Fact]
    public void TestModelNodeLinkNameTemplateProvider()
    {
        var provider = new ModelNodeLinkNameTemplateProvider { Template = DummyTemplateInstance };
        Assert.Equal(nameof(ModelNodeNameTemplateProvider), provider.Name);
    }

    [Fact]
    public void TestScriptTextEditorTemplateProvider()
    {
        var provider = new ScriptTextEditorTemplateProvider { Template = DummyTemplateInstance };
        Assert.Equal("ScriptTextEditor", provider.Name);
    }
}
