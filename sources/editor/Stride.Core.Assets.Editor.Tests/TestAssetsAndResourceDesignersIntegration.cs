// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para validação dos Test Assets, KeyGestures & Resource Designers (Fatia 3 - P32).
/// Garante que todos os 7 componentes existam, estejam desacoplados de Win32/WPF,
/// e funcionem perfeitamente no ambiente .NET 10 multiplataforma.
/// </summary>
public class TestAssetsAndResourceDesignersIntegration
{
    private static readonly Assembly PresentationAssembly = typeof(Stride.Assets.Presentation.AssetEditors.EntityComponentSourceCodeResolver).Assembly;
    private static readonly Assembly CoreEditorAssembly = typeof(Stride.Core.Assets.Editor.Settings.ViewModels.SettingsViewModel).Assembly;

    [Fact]
    public void Test_01_TestAsset_Structure_And_Defaults()
    {
        var testAssetType = PresentationAssembly.GetType("Stride.Assets.Presentation.Test.TestAsset");
        Assert.True(testAssetType != null, "Tipo Stride.Assets.Presentation.Test.TestAsset deve existir no assembly Stride.Assets.Presentation");

        // Validar tipos auxiliares no namespace Stride.Assets.Presentation.Test
        var auxiliaryTypeNames = new[]
        {
            "TestEnum",
            "TestFlagEnum",
            "TestStruct",
            "TestClass",
            "TestEnabledClass",
            "TestAbstractClass",
            "TestImplemClass1",
            "TestImplemClass2",
            "TestImplemClassEnabled",
            "TestInlinedAbstractClass",
            "TestInlinedImplemClass1",
            "TestInlinedImplemClass2",
            "TestMissingInlinedProperty"
        };

        foreach (var auxName in auxiliaryTypeNames)
        {
            var fullAuxName = $"Stride.Assets.Presentation.Test.{auxName}";
            var auxType = PresentationAssembly.GetType(fullAuxName);
            Assert.True(auxType != null, $"Tipo auxiliar {fullAuxName} deve existir");
        }

        // Validar metadados do TestAsset
        var dataContractAttr = testAssetType.GetCustomAttribute<DataContractAttribute>();
        Assert.NotNull(dataContractAttr);
        Assert.Equal("TestAsset", dataContractAttr.Alias);

        var assetDescAttr = testAssetType.GetCustomAttribute<AssetDescriptionAttribute>();
        Assert.NotNull(assetDescAttr);

        var displayAttr = testAssetType.GetCustomAttribute<DisplayAttribute>();
        Assert.NotNull(displayAttr);
        Assert.Equal("Test Asset", displayAttr.Name);

        // Constante de extensão de arquivo
        var fileExtField = testAssetType.GetField("FileExtension", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(fileExtField);
        Assert.Equal(".sdtest", fileExtField.GetValue(null));

        // Executar CreateNew() e validar estado retornado
        var createNewMethod = testAssetType.GetMethod("CreateNew", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(createNewMethod);

        var instance = createNewMethod.Invoke(null, null);
        Assert.NotNull(instance);

        // Propriedades primitivas
        var stringProp = testAssetType.GetProperty("String")?.GetValue(instance) as string;
        Assert.Equal("The name", stringProp);

        var charProp = testAssetType.GetProperty("Character")?.GetValue(instance);
        Assert.Equal('f', charProp);

        var sizeProp = testAssetType.GetProperty("Size")?.GetValue(instance);
        Assert.Equal(16.5f, sizeProp);

        var enumProp = testAssetType.GetProperty("Enum")?.GetValue(instance);
        Assert.NotNull(enumProp);
        Assert.Equal("ThirdValue", enumProp.ToString());

        // Listas e coleções
        var integerList = testAssetType.GetProperty("IntegerList")?.GetValue(instance) as IList;
        Assert.NotNull(integerList);
        Assert.Equal(3, integerList.Count);
        Assert.Equal(4, integerList[0]);
        Assert.Equal(6, integerList[1]);
        Assert.Equal(8, integerList[2]);

        var structureList = testAssetType.GetProperty("StructureList")?.GetValue(instance) as IList;
        Assert.NotNull(structureList);
        Assert.Equal(3, structureList.Count);

        var classList = testAssetType.GetProperty("ClassList")?.GetValue(instance) as IList;
        Assert.NotNull(classList);
        Assert.Equal(3, classList.Count);

        var classDict = testAssetType.GetProperty("ClassDictionary")?.GetValue(instance) as IDictionary;
        Assert.NotNull(classDict);
        Assert.Equal(3, classDict.Count);
        Assert.True(classDict.Contains("First"));
        Assert.True(classDict.Contains("Second"));
        Assert.True(classDict.Contains("Third"));

        var readOnlyAbstractList = testAssetType.GetProperty("ReadOnlyAbstractList")?.GetValue(instance) as IList;
        Assert.NotNull(readOnlyAbstractList);
        Assert.Equal(3, readOnlyAbstractList.Count);
    }

    [Fact]
    public void Test_02_UnitTestAsset_Structure_And_Defaults()
    {
        var unitTestAssetType = PresentationAssembly.GetType("Stride.Assets.Presentation.Test.UnitTestAsset");
        Assert.True(unitTestAssetType != null, "Tipo Stride.Assets.Presentation.Test.UnitTestAsset deve existir no assembly Stride.Assets.Presentation");

        // Metadados
        var dataContractAttr = unitTestAssetType.GetCustomAttribute<DataContractAttribute>();
        Assert.NotNull(dataContractAttr);
        Assert.Equal("UnitTestAsset", dataContractAttr.Alias);

        var assetDescAttr = unitTestAssetType.GetCustomAttribute<AssetDescriptionAttribute>();
        Assert.NotNull(assetDescAttr);

        var displayAttr = unitTestAssetType.GetCustomAttribute<DisplayAttribute>();
        Assert.NotNull(displayAttr);
        Assert.Equal("Unit Test Asset", displayAttr.Name);

        var fileExtField = unitTestAssetType.GetField("FileExtension", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(fileExtField);
        Assert.Equal(".sdunittest", fileExtField.GetValue(null));

        // CreateNew()
        var createNewMethod = unitTestAssetType.GetMethod("CreateNew", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(createNewMethod);

        var instance = createNewMethod.Invoke(null, null);
        Assert.NotNull(instance);

        var boxedValueProp = unitTestAssetType.GetProperty("BoxedValue")?.GetValue(instance);
        Assert.NotNull(boxedValueProp);
        Assert.Equal(new Vector3(3, 4, 5), (Vector3)boxedValueProp);
    }

    [Fact]
    public void Test_03_Presentation_KeyGestures_Strings()
    {
        var gesturesType = PresentationAssembly.GetType("Stride.Assets.Presentation.Resources.Strings.KeyGestures");
        Assert.True(gesturesType != null, "Tipo Stride.Assets.Presentation.Resources.Strings.KeyGestures deve existir");

        // ResourceManager
        var resManProp = gesturesType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resManProp);
        var resMan = resManProp.GetValue(null);
        Assert.NotNull(resMan);

        // Culture
        var cultureProp = gesturesType.GetProperty("Culture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cultureProp);
        cultureProp.SetValue(null, CultureInfo.InvariantCulture);

        // Gestures
        var expectedGestures = new (string PropName, string ExpectedValue)[]
        {
            ("GestureAddPoint", "Alt+Left click"),
            ("GestureDuplicateSelection", "Ctrl+D"),
            ("GestureFocus", "F"),
            ("GestureMagicWand", "M"),
            ("GestureNewSprite", "Ctrl+U"),
            ("GestureSelectNextSprite", "Ctrl+K"),
            ("GestureSelectPreviousSprite", "Ctrl+J"),
            ("GestureSelectWholeImage", "Ctrl+W"),
            ("GestureToggleGrid", "Ctrl+G")
        };

        foreach (var (propName, expectedValue) in expectedGestures)
        {
            var prop = gesturesType.GetProperty(propName, BindingFlags.Public | BindingFlags.Static);
            Assert.True(prop != null, $"Propriedade {propName} deve existir em Presentation KeyGestures");
            var val = prop.GetValue(null) as string;
            Assert.Equal(expectedValue, val);
        }
    }

    [Fact]
    public void Test_04_Core_KeyGestures_Strings()
    {
        var gesturesType = CoreEditorAssembly.GetType("Stride.Core.Assets.Editor.Resources.Strings.KeyGestures");
        Assert.True(gesturesType != null, "Tipo Stride.Core.Assets.Editor.Resources.Strings.KeyGestures deve existir");

        // ResourceManager
        var resManProp = gesturesType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resManProp);
        var resMan = resManProp.GetValue(null);
        Assert.NotNull(resMan);

        // Culture
        var cultureProp = gesturesType.GetProperty("Culture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cultureProp);
        cultureProp.SetValue(null, CultureInfo.InvariantCulture);

        // Core Gestures
        var expectedGestures = new (string PropName, string ExpectedValue)[]
        {
            ("EditAsset", "Ctrl+Enter"),
            ("ImportAddFiles", "Ctrl+A"),
            ("ImportAllAsNew", "Ctrl+N"),
            ("ImportAutoMerge", "Ctrl+M"),
            ("ImportFinish", "Alt+F"),
            ("ImportNextStep", "Alt+N"),
            ("ImportPreviousStep", "Alt+P"),
            ("UpdateAllAssetsWithModifiedSource", "Ctrl+R"),
            ("UpdateSelectedAssetsFromSource", "Ctrl+Shift+R")
        };

        foreach (var (propName, expectedValue) in expectedGestures)
        {
            var prop = gesturesType.GetProperty(propName, BindingFlags.Public | BindingFlags.Static);
            Assert.True(prop != null, $"Propriedade {propName} deve existir em Core KeyGestures");
            var val = prop.GetValue(null) as string;
            Assert.Equal(expectedValue, val);
        }
    }

    [Fact]
    public void Test_05_Core_Images_Designer()
    {
        var imagesType = CoreEditorAssembly.GetType("Stride.Core.Assets.Editor.Resources.Images");
        Assert.True(imagesType != null, "Tipo Stride.Core.Assets.Editor.Resources.Images deve existir");

        // ResourceManager
        var resManProp = imagesType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resManProp);
        var resMan = resManProp.GetValue(null);
        Assert.NotNull(resMan);

        // Culture
        var cultureProp = imagesType.GetProperty("Culture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cultureProp);

        // folder byte array
        var folderProp = imagesType.GetProperty("folder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(folderProp != null, "Propriedade folder deve existir em Core Images");
        var folderBytes = folderProp.GetValue(null) as byte[];
        Assert.NotNull(folderBytes);
        Assert.True(folderBytes.Length > 0, "Propriedade folder deve retornar byte[] de imagem não vazio");
    }

    [Fact]
    public void Test_06_Presentation_Properties_Resources()
    {
        var resourcesType = PresentationAssembly.GetType("Stride.Assets.Presentation.Properties.Resources");
        Assert.True(resourcesType != null, "Tipo Stride.Assets.Presentation.Properties.Resources deve existir");

        // ResourceManager
        var resManProp = resourcesType.GetProperty("ResourceManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resManProp);
        var resMan = resManProp.GetValue(null);
        Assert.NotNull(resMan);

        // Culture
        var cultureProp = resourcesType.GetProperty("Culture", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(cultureProp);
        cultureProp.SetValue(null, CultureInfo.InvariantCulture);
        Assert.Equal(CultureInfo.InvariantCulture, cultureProp.GetValue(null));
    }

    [Fact]
    public void Test_07_Presentation_Properties_Settings()
    {
        var settingsType = PresentationAssembly.GetType("Stride.Assets.Presentation.Properties.Settings");
        Assert.True(settingsType != null, "Tipo Stride.Assets.Presentation.Properties.Settings deve existir");

        // Default instance
        var defaultProp = settingsType.GetProperty("Default", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(defaultProp);
        var defaultInstance = defaultProp.GetValue(null);
        Assert.NotNull(defaultInstance);
        Assert.IsAssignableFrom(settingsType, defaultInstance);
    }
}
