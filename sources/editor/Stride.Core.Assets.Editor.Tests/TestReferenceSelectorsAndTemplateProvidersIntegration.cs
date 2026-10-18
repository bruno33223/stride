// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Markup.Xaml.Templates;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Rendering;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Materials.ComputeColors;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestReferenceSelectorsAndTemplateProvidersIntegration
{
    private static DataTemplate DummyTemplate => new();

    private class TestableNodeViewModel : NodeViewModel
    {
        private object? customValue;

        private TestableNodeViewModel() : base(null!, null, "", typeof(object), null!)
        {
        }

        public static TestableNodeViewModel Create(Type type, object? value = null, NodeViewModel? parent = null)
        {
            var node = (TestableNodeViewModel)RuntimeHelpers.GetUninitializedObject(typeof(TestableNodeViewModel));
            typeof(NodeViewModel).GetField("<Type>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, type);
            typeof(NodeViewModel).GetProperty(nameof(Parent))?.SetValue(node, parent);
            typeof(NodeViewModel).GetField("associatedData", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, new Dictionary<string, object>());
            typeof(NodeViewModel).GetField("nodePresenters", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, new List<INodePresenter>());
            node.customValue = value;
            return node;
        }

        protected override object? GetNodeValue() => customValue;

        public void SetAssociatedData(string key, object val)
        {
            var dict = (Dictionary<string, object>)typeof(NodeViewModel).GetField("associatedData", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(this)!;
            dict[key] = val;
        }
    }

    [Fact]
    public void TestRenderStageReferenceTemplateProvider_ExistenceAndBehavior()
    {
        var provider = new RenderStageReferenceTemplateProvider { Template = DummyTemplate };
        Assert.NotNull(provider);
        Assert.Equal(nameof(RenderStageReferenceTemplateProvider), provider.Name);

        // RenderStage node matching
        var stageNode = TestableNodeViewModel.Create(typeof(RenderStage));
        Assert.True(provider.MatchNode(stageNode));

        // Non-RenderStage node matching
        var otherNode = TestableNodeViewModel.Create(typeof(string));
        Assert.False(provider.MatchNode(otherNode));
    }

    [Fact]
    public void TestEntityReferenceTemplateProvider_ContractAndMatch()
    {
        var provider = new EntityReferenceTemplateProvider { Template = DummyTemplate };
        Assert.Equal("EntityReference", provider.Name);

        var entityNode = TestableNodeViewModel.Create(typeof(Entity));
        Assert.True(provider.MatchNode(entityNode));

        var stringNode = TestableNodeViewModel.Create(typeof(string));
        Assert.False(provider.MatchNode(stringNode));
    }

    [Fact]
    public void TestEntityComponentReferenceTemplateProvider_ContractAndMatch()
    {
        var provider = new EntityComponentReferenceTemplateProvider { Template = DummyTemplate };
        Assert.Equal("EntityComponentReference", provider.Name);

        // Standard component
        var componentNode = TestableNodeViewModel.Create(typeof(TransformComponent));
        Assert.True(provider.MatchNode(componentNode));

        // Inside EntityComponentCollection -> should return false
        var collectionNode = TestableNodeViewModel.Create(typeof(EntityComponentCollection));
        var insideCollection = TestableNodeViewModel.Create(typeof(TransformComponent), parent: collectionNode);
        Assert.False(provider.MatchNode(insideCollection));

        // Incompatible type
        var randomNode = TestableNodeViewModel.Create(typeof(int));
        Assert.False(provider.MatchNode(randomNode));
    }

    [Fact]
    public void TestShaderClassNodeMixinReferenceTemplateProvider_ContractAndMatch()
    {
        var provider = new ShaderClassNodeMixinReferenceTemplateProvider { Template = DummyTemplate };
        Assert.Equal("ShaderClassNodeMixinReference", provider.Name);

        // No parent -> false
        var orphanNode = TestableNodeViewModel.Create(typeof(string));
        Assert.False(provider.MatchNode(orphanNode));

        // Parent with ComputeShaderClassColor and string type -> true
        var colorParent = TestableNodeViewModel.Create(typeof(ComputeShaderClassColor), value: new ComputeShaderClassColor());
        var childColorNode = TestableNodeViewModel.Create(typeof(string), parent: colorParent);
        Assert.True(provider.MatchNode(childColorNode));

        // Parent with ComputeShaderClassScalar and string type -> true
        var scalarParent = TestableNodeViewModel.Create(typeof(ComputeShaderClassScalar), value: new ComputeShaderClassScalar());
        var childScalarNode = TestableNodeViewModel.Create(typeof(string), parent: scalarParent);
        Assert.True(provider.MatchNode(childScalarNode));

        // Child with non-string type -> false
        var childIntNode = TestableNodeViewModel.Create(typeof(int), parent: colorParent);
        Assert.False(provider.MatchNode(childIntNode));
    }

    [Fact]
    public void TestScriptVariableReferenceTemplateProvider_ContractAndMatch()
    {
        var provider = new ScriptVariableReferenceTemplateProvider { Template = DummyTemplate };
        Assert.Equal(nameof(ScriptVariableReferenceTemplateProvider), provider.Name);

        // Node without script context -> false
        var node = TestableNodeViewModel.Create(typeof(string));
        Assert.False(provider.MatchNode(node));
    }

    [Fact]
    public void TestContentReferenceTemplateProvider_ContractAndNames()
    {
        var simpleProvider = new ContentReferenceTemplateProvider { Template = DummyTemplate, DynamicThumbnail = false };
        Assert.Equal("SimpleReference", simpleProvider.Name);

        var thumbnailProvider = new ContentReferenceTemplateProvider { Template = DummyTemplate, DynamicThumbnail = true };
        Assert.Equal("ThumbnailReference", thumbnailProvider.Name);

        // Non-content type -> false
        var nonContentNode = TestableNodeViewModel.Create(typeof(int));
        Assert.False(simpleProvider.MatchNode(nonContentNode));
        Assert.False(thumbnailProvider.MatchNode(nonContentNode));
    }

    [Fact]
    public void TestContentReferenceToUrl_ConverterCases()
    {
        var converter = new ContentReferenceToUrl();

        // Null value -> EmptyReference string
        var nullResult = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(ContentReferenceHelper.EmptyReference, nullResult);

        // DifferentValues -> localized string
        var diffResult = converter.Convert(NodeViewModel.DifferentValues, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("(Different values)", diffResult);

        // ConvertBack with unknown URL -> returns null safely without exception
        var backResult = converter.ConvertBack("non_existent/path", typeof(object), null, CultureInfo.InvariantCulture);
        Assert.Null(backResult);
    }

    [Fact]
    public void TestContentReferenceToAsset_ConverterCases()
    {
        var converter = new ContentReferenceToAsset();

        // Null -> null
        var nullResult = converter.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);
        Assert.Null(nullResult);

        // DifferentValues -> null
        var diffResult = converter.Convert(NodeViewModel.DifferentValues, typeof(object), null, CultureInfo.InvariantCulture);
        Assert.Null(diffResult);
    }

    private static string FindDefaultPropertyTemplateProvidersAxaml()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = System.IO.Path.Combine(current, "sources", "editor", "Stride.Core.Assets.Editor.Avalonia", "Views", "DefaultPropertyTemplateProviders.axaml");
            if (System.IO.File.Exists(candidate))
                return candidate;
            var parent = System.IO.Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        throw new System.IO.FileNotFoundException("Could not find DefaultPropertyTemplateProviders.axaml");
    }

    [Fact]
    public void TestDefaultPropertyTemplateProviders_ThumbnailReferenceBinding_HasNoInvalidCast()
    {
        var path = FindDefaultPropertyTemplateProvidersAxaml();
        var content = System.IO.File.ReadAllText(path);

        // Validação da banca: não deve ter o cast inválido ((capvm:AssetViewModel)NodeValue)
        Assert.DoesNotContain("((capvm:AssetViewModel)NodeValue)", content);

        // Deve ter o binding correto com NodeValue direto para ContentReferenceToAsset
        Assert.Contains("DataContext=\"{Binding NodeValue, Converter={caec:ContentReferenceToAsset}}\"", content);
    }
}
