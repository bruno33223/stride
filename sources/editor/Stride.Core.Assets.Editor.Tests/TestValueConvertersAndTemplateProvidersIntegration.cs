// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Stride.Animations;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;
using Stride.Data;
using Stride.Engine;
using Stride.Rendering.Compositing;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestValueConvertersAndTemplateProvidersIntegration
{
    private static string GetRepoRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(current) && !File.Exists(Path.Combine(current, "stride-env.sh")))
        {
            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        if (string.IsNullOrEmpty(current) || !File.Exists(Path.Combine(current, "stride-env.sh")))
        {
            current = "/media/bruno/SteamGames/apps/stride-source/stride";
        }

        return current;
    }

    [Fact]
    public void TestFileExistenceAndParityForValueConvertersAndTemplateProviders()
    {
        var repoRoot = GetRepoRoot();
        var coreAvaloniaDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Core.Assets.Editor.Avalonia");
        var assetsAvaloniaDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor.Avalonia");

        // 25 Value Converters
        var valueConverterFiles = new (string Path, int ExpectedLines, string Name)[]
        {
            (Path.Combine(assetsAvaloniaDir, "Views", "MethodToDisplayName.cs"), 87, "MethodToDisplayName"),
            (Path.Combine(coreAvaloniaDir, "Converters", "FlagEnumToObservableList.cs"), 83, "FlagEnumToObservableList"),
            (Path.Combine(assetsAvaloniaDir, "Views", "TimeToFrames.cs"), 45, "TimeToFrames"),
            (Path.Combine(coreAvaloniaDir, "Converters", "AssetToExpandedAtInitialization.cs"), 44, "AssetToExpandedAtInitialization"),
            (Path.Combine(assetsAvaloniaDir, "Views", "AvailableVariableReferenceValueConverter.cs"), 43, "AvailableVariableReferenceValueConverter"),
            (Path.Combine(coreAvaloniaDir, "Converters", "AbstractNodeEntryMatchesNodeValue.cs"), 42, "AbstractNodeEntryMatchesNodeValue"),
            (Path.Combine(assetsAvaloniaDir, "Views", "VisualScriptSlotBrush.cs"), 40, "VisualScriptSlotBrush"),
            (Path.Combine(coreAvaloniaDir, "Converters", "AssetFilterViewModelToFullDisplayName.cs"), 38, "AssetFilterViewModelToFullDisplayName"),
            (Path.Combine(coreAvaloniaDir, "Converters", "NameBreakingLine.cs"), 37, "NameBreakingLine"),
            (Path.Combine(assetsAvaloniaDir, "Views", "NodeToCameraSlotIndex.cs"), 33, "NodeToCameraSlotIndex"),
            (Path.Combine(coreAvaloniaDir, "Converters", "TypeToResource.cs"), 32, "TypeToResource"),
            (Path.Combine(coreAvaloniaDir, "Converters", "EnumToTooltip.cs"), 32, "EnumToTooltip"),
            (Path.Combine(assetsAvaloniaDir, "Views", "EntityComponentToTransformLinkInfo.cs"), 30, "EntityComponentToTransformLinkInfo"),
            (Path.Combine(coreAvaloniaDir, "Converters", "AssetViewModelToUrl.cs"), 29, "AssetViewModelToUrl"),
            (Path.Combine(assetsAvaloniaDir, "Views", "UFileToBitmapImage.cs"), 26, "UFileToBitmapImage"),
            (Path.Combine(assetsAvaloniaDir, "Views", "EntityComponentToResource.cs"), 24, "EntityComponentToResource"),
            (Path.Combine(coreAvaloniaDir, "Converters", "DifferentValuesToString.cs"), 22, "DifferentValuesToString"),
            (Path.Combine(coreAvaloniaDir, "Converters", "DifferentValuesToNull.cs"), 22, "DifferentValuesToNull"),
            (Path.Combine(coreAvaloniaDir, "Converters", "DifferentValueToParam.cs"), 22, "DifferentValueToParam"),
            (Path.Combine(assetsAvaloniaDir, "Views", "CollectionContainsItem.cs"), 20, "CollectionContainsItem"),
            (Path.Combine(coreAvaloniaDir, "Converters", "EnumToResource.cs"), 20, "EnumToResource"),
            (Path.Combine(coreAvaloniaDir, "Converters", "IsFirstChild.cs"), 19, "IsFirstChild"),
            (Path.Combine(coreAvaloniaDir, "Converters", "AbstractNodeEntryToType.cs"), 18, "AbstractNodeEntryToType"),
            (Path.Combine(coreAvaloniaDir, "Converters", "AbstractNodeEntryToDisplayName.cs"), 18, "AbstractNodeEntryToDisplayName"),
            (Path.Combine(coreAvaloniaDir, "Converters", "TypeToDisplayName.cs"), 16, "TypeToDisplayName"),
        };

        foreach (var (path, expectedLines, name) in valueConverterFiles)
        {
            Assert.True(File.Exists(path), $"Value Converter file missing: {path} ({name})");
            var lineCount = File.ReadAllLines(path).Length;
            var min = (int)(expectedLines * 0.75);
            var max = (int)(expectedLines * 1.35) + 10;
            Assert.True(lineCount >= min && lineCount <= max,
                $"Value Converter {name} line count {lineCount} out of expected range [{min}, {max}] (original: {expectedLines})");
        }

        // 18 Template Providers
        var templateProviderFiles = new (string Path, int ExpectedLines, string Name)[]
        {
            (Path.Combine(coreAvaloniaDir, "Views", "NodeViewModelTemplateProvider.cs"), 43, "NodeViewModelTemplateProvider"),
            (Path.Combine(assetsAvaloniaDir, "Views", "ComputeCurveTemplateProviders.cs"), 41, "ComputeCurveTemplateProviders"),
            (Path.Combine(coreAvaloniaDir, "Views", "DictionaryStringKeyTemplateProvider.cs"), 40, "DictionaryStringKeyTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "DictionaryEnumKeyTemplateProvider.cs"), 40, "DictionaryEnumKeyTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "ListTemplateProvider.cs"), 34, "ListTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "NullableTemplateProvider.cs"), 27, "NullableTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "ArrayTemplateProvider.cs"), 23, "ArrayTemplateProvider"),
            (Path.Combine(assetsAvaloniaDir, "Views", "GameSettingAddConfigurationTemplateProvider.cs"), 23, "GameSettingAddConfigurationTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "RangedValueTemplateProvider.cs"), 22, "RangedValueTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "CategoryNodeTemplateProvider.cs"), 22, "CategoryNodeTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "DictionaryTemplateProvider.cs"), 21, "DictionaryTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "UnloadableObjectTemplateProvider.cs"), 18, "UnloadableObjectTemplateProvider"),
            (Path.Combine(assetsAvaloniaDir, "Views", "EntityComponentCollectionTemplateProvider.cs"), 18, "EntityComponentCollectionTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "AbstractTypeTemplateProvider.cs"), 17, "AbstractTypeTemplateProvider"),
            (Path.Combine(assetsAvaloniaDir, "Views", "GameSettingsFiltersTemplateProvider.cs"), 17, "GameSettingsFiltersTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "SetTemplateProvider.cs"), 16, "SetTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "ListItemTemplateProvider.cs"), 16, "ListItemTemplateProvider"),
            (Path.Combine(coreAvaloniaDir, "Views", "ArrayItemTemplateProvider.cs"), 16, "ArrayItemTemplateProvider"),
        };

        foreach (var (path, expectedLines, name) in templateProviderFiles)
        {
            Assert.True(File.Exists(path), $"Template Provider file missing: {path} ({name})");
            var lineCount = File.ReadAllLines(path).Length;
            var min = (int)(expectedLines * 0.75);
            var max = (int)(expectedLines * 1.35) + 10;
            Assert.True(lineCount >= min && lineCount <= max,
                $"Template Provider {name} line count {lineCount} out of expected range [{min}, {max}] (original: {expectedLines})");
        }
    }

    [Fact]
    public void TestValueConvertersTypesAndContracts()
    {
        var coreAvaloniaAsm = typeof(DifferentValuesToString).Assembly;
        var assetsAvaloniaAsm = typeof(MethodToDisplayName).Assembly;

        // Verify all 25 Value Converters exist as types
        var expectedTypes = new (Assembly Asm, string FullName)[]
        {
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.MethodToDisplayName"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.FlagEnumToObservableList"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.TimeToFrames"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AssetToExpandedAtInitialization"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.AvailableVariableReferenceValueConverter"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AbstractNodeEntryMatchesNodeValue`1"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AbstractNodeEntryToFontWeight"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AbstractNodeEntryToBrush"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrush`1"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrushConnectorFill"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrushMouseOverConnectorFill"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrushConnectorStroke"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AssetFilterViewModelToFullDisplayName"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.NameBreakingLine"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.NodeToCameraSlotIndex"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.TypeToResource"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.EnumToTooltip"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.EntityComponentToTransformLinkInfo"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AssetViewModelToUrl"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.UFileToBitmapImage"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.EntityComponentToResource"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.DifferentValuesToString"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.DifferentValuesToNull"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.DifferentValueToParam"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.CollectionContainsItem"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.EnumToResource"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.IsFirstChild"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AbstractNodeEntryToType"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.AbstractNodeEntryToDisplayName"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Converters.TypeToDisplayName"),
        };

        foreach (var (asm, fullName) in expectedTypes)
        {
            var type = asm.GetType(fullName);
            Assert.NotNull(type);
            Assert.True(
                typeof(IValueConverter).IsAssignableFrom(type) ||
                typeof(IMultiValueConverter).IsAssignableFrom(type) ||
                type.IsGenericTypeDefinition,
                $"Type {fullName} does not implement IValueConverter or IMultiValueConverter");
        }
    }

    [Fact]
    public void TestTemplateProvidersTypesAndContracts()
    {
        var coreAvaloniaAsm = typeof(NodeViewModelTemplateProvider).Assembly;
        var assetsAvaloniaAsm = typeof(ComputeCurveColor4TemplateProvider).Assembly;

        var expectedTypes = new (Assembly Asm, string FullName)[]
        {
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.NodeViewModelTemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveTemplateProvider`1"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveColor4TemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveFloatTemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveQuaternionTemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveVector2TemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveVector3TemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.ComputeCurveVector4TemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.DictionaryStringKeyTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.DictionaryEnumKeyTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.ListTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.NullableTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.ArrayTemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.GameSettingAddConfigurationTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.RangedValueTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.CategoryNodeTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.DictionaryTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.UnloadableObjectTemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.EntityComponentCollectionTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.AbstractTypeTemplateProvider"),
            (assetsAvaloniaAsm, "Stride.Assets.Editor.Avalonia.Views.GameSettingsFiltersTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.SetTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.ListItemTemplateProvider"),
            (coreAvaloniaAsm, "Stride.Core.Assets.Editor.Avalonia.Views.ArrayItemTemplateProvider"),
        };

        foreach (var (asm, fullName) in expectedTypes)
        {
            var type = asm.GetType(fullName);
            Assert.NotNull(type);
            if (!type.IsGenericTypeDefinition)
            {
                Assert.True(typeof(NodeViewModelTemplateProvider).IsAssignableFrom(type) ||
                            type == typeof(NodeViewModelTemplateProvider));
            }
        }
    }

    [Fact]
    public void TestNameBreakingLineConversion()
    {
        var converter = new NameBreakingLine();
        var input = "MyCoolVariable123";
        var result = (string?)converter.Convert(input, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.NotNull(result);
        Assert.Contains("\u200B", result);
        Assert.Equal("12345", converter.Convert(12345, typeof(string), null, CultureInfo.InvariantCulture)?.ToString());
    }

    [Fact]
    public void TestDifferentValuesConverters()
    {
        var toStringConverter = new DifferentValuesToString();
        Assert.Null(toStringConverter.Convert(NodeViewModel.DifferentValues, typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal("TestValue", toStringConverter.Convert("TestValue", typeof(string), null, CultureInfo.InvariantCulture));

        var toNullConverter = new DifferentValuesToNull();
        Assert.Null(toNullConverter.Convert(NodeViewModel.DifferentValues, typeof(object), null, CultureInfo.InvariantCulture));
        Assert.Equal(42, toNullConverter.Convert(42, typeof(object), null, CultureInfo.InvariantCulture));

        var toParamConverter = new DifferentValueToParam();
        Assert.Equal("Fallback", toParamConverter.Convert(NodeViewModel.DifferentValues, typeof(object), "Fallback", CultureInfo.InvariantCulture));
        Assert.Equal("Actual", toParamConverter.Convert("Actual", typeof(object), "Fallback", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TestCollectionContainsItemConverter()
    {
        var converter = new CollectionContainsItem();
        var list = new List<object> { "Item1", 42, "Item2" };

        var matchResult = converter.Convert(new object?[] { 42, list }, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(true, matchResult);

        var noMatchResult = converter.Convert(new object?[] { "Unknown", list }, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.Equal(false, noMatchResult);
    }

    [Fact]
    public void TestTypeToDisplayNameConverter()
    {
        var converter = new TypeToDisplayName();
        var name = converter.Convert(typeof(string), typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("String", name);

        var none = converter.Convert("NotAType", typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("None", none);
    }

    [Fact]
    public void TestAbstractNodeEntryConverters()
    {
        var toTypeConverter = new AbstractNodeEntryToType();
        var dummyTypeEntry = new AbstractNodeType(typeof(int));
        Assert.Equal(typeof(int), toTypeConverter.Convert(dummyTypeEntry, typeof(Type), null, CultureInfo.InvariantCulture));

        var toDisplayConverter = new AbstractNodeEntryToDisplayName();
        Assert.Equal("Int32", toDisplayConverter.Convert(dummyTypeEntry, typeof(string), null, CultureInfo.InvariantCulture));

        var toWeight = new AbstractNodeEntryToFontWeight();
        var weightMatch = toWeight.Convert(new object?[] { dummyTypeEntry, 10 }, typeof(FontWeight), null, CultureInfo.InvariantCulture);
        Assert.Equal(FontWeight.Bold, weightMatch);

        var weightMismatch = toWeight.Convert(new object?[] { dummyTypeEntry, "string" }, typeof(FontWeight), null, CultureInfo.InvariantCulture);
        Assert.Equal(AvaloniaProperty.UnsetValue, weightMismatch);

        var toBrush = new AbstractNodeEntryToBrush();
        var brushMatch = toBrush.Convert(new object?[] { dummyTypeEntry, 10 }, typeof(IBrush), null, CultureInfo.InvariantCulture);
        Assert.NotNull(brushMatch);
        Assert.IsAssignableFrom<IBrush>(brushMatch);
    }

    [Flags]
    private enum SampleFlags
    {
        None = 0,
        OptionA = 1,
        OptionB = 2,
        OptionC = 4,
    }

    [Fact]
    public void TestFlagEnumToObservableListConverter()
    {
        var converter = new FlagEnumToObservableList();
        // Null node test
        var empty = converter.Convert(new object?[] { null, SampleFlags.OptionA }, typeof(IEnumerable), null, CultureInfo.InvariantCulture);
        Assert.Empty((IEnumerable)empty!);
    }

    [Fact]
    public void TestNullableTemplateProviderContract()
    {
        var provider = new NullableTemplateProvider { Template = new global::Avalonia.Markup.Xaml.Templates.DataTemplate() };
        Assert.False(provider.Struct);
        Assert.Equal("Nullable", provider.Name);

        provider.Struct = true;
        Assert.True(provider.Struct);
        Assert.Equal("NullableStruct", provider.Name);
    }

    [Fact]
    public void TestComputeCurveTemplateProvidersContract()
    {
        var dummyTemplate = new global::Avalonia.Markup.Xaml.Templates.DataTemplate();
        var colorProvider = new ComputeCurveColor4TemplateProvider { Template = dummyTemplate };
        Assert.Contains("Color4", colorProvider.Name);

        var floatProvider = new ComputeCurveFloatTemplateProvider { Template = dummyTemplate };
        Assert.Contains("Single", floatProvider.Name);

        var v3Provider = new ComputeCurveVector3TemplateProvider { Template = dummyTemplate };
        Assert.Contains("Vector3", v3Provider.Name);
    }

    private class DummyAssetsPluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => [];
        public IReadOnlyList<Type> GetPrimitiveTypes() => [];
        public Type? GetAssetViewModelType(Type assetType) => null;
        public Type? GetEditorViewModelType(Type viewModelType) => null;
        public Type? GetEditorViewType(Type editorViewModelType) => null;
        public Type? GetPreviewViewModelType(Type previewType) => null;
        public Type? GetPreviewViewType(Type previewType) => null;
        public void EnsureInitialized(Stride.Core.Diagnostics.ILogger logger) { }
        public object? GetImageForEnum(SessionViewModel? session, object value) => $"icon_{value}";
    }

    [Fact]
    public void TestEnumToResourceDelegation()
    {
        var converter = new EnumToResource();
        Assert.Null(converter.Convert(null, typeof(object), null, CultureInfo.InvariantCulture));

        var serviceProvider = new Stride.Core.Presentation.ViewModels.ViewModelServiceProvider();
        serviceProvider.RegisterService(new DummyAssetsPluginService());

        var sessionField = typeof(SessionViewModel).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        var prevSession = sessionField.GetValue(null);
        try
        {
            var dummySession = (SessionViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
            typeof(Stride.Core.Presentation.ViewModels.ViewModelBase).GetProperty("ServiceProvider")!.SetValue(dummySession, serviceProvider);
            sessionField.SetValue(null, dummySession);

            var result = converter.Convert(SampleFlags.OptionB, typeof(object), null, CultureInfo.InvariantCulture);
            Assert.Equal("icon_OptionB", result);
        }
        finally
        {
            sessionField.SetValue(null, prevSession);
        }
    }
}

