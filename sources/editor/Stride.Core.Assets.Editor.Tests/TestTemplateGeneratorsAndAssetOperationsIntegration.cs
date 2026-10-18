// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Transactions;
using Stride.Engine;

namespace Stride.Core.Assets.Editor.Tests;

public class TestTemplateGeneratorsAndAssetOperationsIntegration
{
    private static readonly Assembly CoreAssetsEditorAssembly = typeof(Stride.Core.Assets.Editor.ViewModels.SessionViewModel).Assembly;
    private static readonly Assembly AssetsPresentationAssembly = typeof(Stride.Assets.Presentation.SceneEditor.EntityPickingResult).Assembly;
    private static readonly Assembly AssetsEditorAssembly = typeof(Stride.Assets.Editor.Templates.AssetTemplateGenerator).Assembly;

    [Fact]
    public void Test01_VideoFromFileTemplateGenerator_Contracts()
    {
        var type = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.Templates.VideoFromFileTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Presentation.Templates.VideoFromFileTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.VideoFromFileTemplateGenerator");

        Assert.True(type != null, "VideoFromFileTemplateGenerator must exist in Stride.Assets.Presentation.Templates or Stride.Assets.Editor.Templates.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("Id", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("B660A65C-3B61-4AE3-9DC4-E34E62E524C9");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);

        var isSupportingMethod = type.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
    }

    [Fact]
    public void Test02_TemplateGeneratorHelper_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Components.TemplateDescriptions.TemplateGeneratorHelper");
        Assert.True(type != null, "TemplateGeneratorHelper must exist in Stride.Core.Assets.Editor.Components.TemplateDescriptions.");

        var runMethod = type.GetMethod("RunTemplateGeneratorSafe", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(runMethod);
        Assert.True(runMethod.IsGenericMethodDefinition, "RunTemplateGeneratorSafe must be generic.");
    }

    [Fact]
    public void Test03_FixAssetReferenceOperation_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.FixAssetReferenceOperation");
        Assert.True(type != null, "FixAssetReferenceOperation must exist in Stride.Core.Assets.Editor.ViewModel.");

        Assert.True(typeof(DirtyingOperation).IsAssignableFrom(type), "FixAssetReferenceOperation must inherit from DirtyingOperation.");

        var fixMethod = type.GetMethod("FixAssetReferences", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(fixMethod);
    }

    [Fact]
    public void Test04_FuncClipboardMonitor_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.FuncClipboardMonitor`1");
        Assert.True(type != null, "FuncClipboardMonitor<T> must exist in Stride.Core.Assets.Editor.ViewModel.");

        var closedType = type.MakeGenericType(typeof(string));
        var instance = Activator.CreateInstance(closedType);
        Assert.NotNull(instance);

        var getMethod = closedType.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(getMethod);

        var invalidateMethod = closedType.GetMethod("Invalidate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(invalidateMethod);

        var destroyMethod = closedType.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(destroyMethod);

        // Test caching behavior
        int callCount = 0;
        Func<string> evaluator = () =>
        {
            callCount++;
            return $"call_{callCount}";
        };

        var res1 = (string)getMethod.Invoke(instance, new object[] { evaluator })!;
        var res2 = (string)getMethod.Invoke(instance, new object[] { evaluator })!;
        Assert.Equal("call_1", res1);
        Assert.Equal("call_1", res2);
        Assert.Equal(1, callCount);

        // Invalidate and re-evaluate
        invalidateMethod.Invoke(instance, null);
        var res3 = (string)getMethod.Invoke(instance, new object[] { evaluator })!;
        Assert.Equal("call_2", res3);
        Assert.Equal(2, callCount);

        destroyMethod.Invoke(instance, null);
    }

    [Fact]
    public void Test05_AssetsUpgradeOperation_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.AssetsUpgradeOperation");
        Assert.True(type != null, "AssetsUpgradeOperation must exist in Stride.Core.Assets.Editor.ViewModel.");

        Assert.True(typeof(Operation).IsAssignableFrom(type), "AssetsUpgradeOperation must inherit from Operation.");
        Assert.True(typeof(IDirtyingOperation).IsAssignableFrom(type), "AssetsUpgradeOperation must implement IDirtyingOperation.");

        var mockDirtiable = new MockDirtiable();
        var instance = Activator.CreateInstance(type, mockDirtiable) as IDirtyingOperation;
        Assert.NotNull(instance);
        Assert.True(instance.IsDone);
        Assert.Single(instance.Dirtiables);
        Assert.Same(mockDirtiable, instance.Dirtiables[0]);
    }

    [Fact]
    public void Test06_AssetTemplatesViewModel_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Components.AddAssets.AssetTemplatesViewModel");
        Assert.True(type != null, "AssetTemplatesViewModel must exist in Stride.Core.Assets.Editor.Components.AddAssets.");

        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(type), "AssetTemplatesViewModel must inherit from DispatcherViewModel.");

        var templatesProp = type.GetProperty("Templates", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(templatesProp);

        var fileCountProp = type.GetProperty("FileCount", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(fileCountProp);

        var selectedTemplateProp = type.GetProperty("SelectedTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(selectedTemplateProp);
    }

    [Fact]
    public void Test07_AssetReferenceReplacementViewModel_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Components.FixAssetReferences.AssetReferenceReplacementViewModel");
        Assert.True(type != null, "AssetReferenceReplacementViewModel must exist in Stride.Core.Assets.Editor.Components.FixAssetReferences.");

        var clearMethod = type.GetMethod("ClearReference", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(clearMethod);
    }

    [Fact]
    public void Test08_HullAssetFactoryTemplateGenerator_Contracts()
    {
        var type = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.Templates.HullAssetFactoryTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Presentation.Templates.HullAssetFactoryTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.HullAssetFactoryTemplateGenerator");

        Assert.True(type != null, "HullAssetFactoryTemplateGenerator must exist.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("FDBCB65A-9866-4EF2-9A03-AF21F943BDFD");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);
    }

    [Fact]
    public void Test09_HeightmapFactoryTemplateGenerator_Contracts()
    {
        var type = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.Templates.HeightmapFactoryTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Presentation.Templates.HeightmapFactoryTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.HeightmapFactoryTemplateGenerator");

        Assert.True(type != null, "HeightmapFactoryTemplateGenerator must exist.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("127EC64F-6E15-4964-98F4-DB735B39AE09");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);
    }

    [Fact]
    public void Test10_GraphicsCompositorTemplateGenerator_Contracts()
    {
        var type = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.Templates.GraphicsCompositorTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Presentation.Templates.GraphicsCompositorTemplateGenerator")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.GraphicsCompositorTemplateGenerator");

        Assert.True(type != null, "GraphicsCompositorTemplateGenerator must exist.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var dictField = type.GetField("SupportedTemplatesToUrl", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(dictField);
        var dict = dictField.GetValue(null) as Dictionary<Guid, string>;
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey(new Guid("20947F4A-7B50-4716-AC85-D10EFF58CD33")));
        Assert.True(dict.ContainsKey(new Guid("D4EE3BD3-9B06-460E-9175-D6AFB2459463")));
        Assert.True(dict.ContainsKey(new Guid("4BC182D7-69D5-4BE2-9AF3-1C82F67B629D")));
    }

    [Fact]
    public void Test11_AddItemTemplateCollectionViewModel_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.AddItemTemplateCollectionViewModel");
        Assert.True(type != null, "AddItemTemplateCollectionViewModel must exist in Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.");

        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        var instance = Activator.CreateInstance(type, sp) as ViewModelBase;
        Assert.NotNull(instance);

        var rootGroupProp = type.GetProperty("RootGroup", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(rootGroupProp);
        var rootGroup = rootGroupProp.GetValue(instance);
        Assert.NotNull(rootGroup);

        var searchTokenProp = type.GetProperty("SearchToken", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(searchTokenProp);
        searchTokenProp.SetValue(instance, "test_filter");
        Assert.Equal("test_filter", searchTokenProp.GetValue(instance));

        var validateMethod = type.GetMethod("ValidateProperties", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(validateMethod);
        var parameters = new object?[] { null };
        var isValid = (bool)validateMethod.Invoke(instance, parameters)!;
        Assert.True(isValid);
    }

    [Fact]
    public void Test12_BackgroundTaskDescription_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.BackgroundTaskDescription");
        Assert.True(type != null, "BackgroundTaskDescription must exist in Stride.Core.Assets.Editor.ViewModel.");

        Assert.True(typeof(IDisposable).IsAssignableFrom(type), "BackgroundTaskDescription must implement IDisposable.");

        var descProp = type.GetProperty("Description", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(descProp);
    }

    [Fact]
    public void Test13_ColorPaletteViewModel_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.ColorPaletteViewModel");
        Assert.True(type != null, "ColorPaletteViewModel must exist in Stride.Core.Assets.Editor.ViewModel.");

        var colorsField = type.GetField("Colors", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(colorsField);
        var colors = colorsField.GetValue(null) as Dictionary<string, Color3>;
        Assert.NotNull(colors);
        Assert.True(colors.ContainsKey("Silver"));
        Assert.True(colors.ContainsKey("Gold"));
        Assert.True(colors.ContainsKey("Copper"));
        Assert.True(colors.ContainsKey("Titanium"));
    }

    [Fact]
    public void Test14_IDragSourceViewModel_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.IDragSourceViewModel");
        Assert.True(type != null, "IDragSourceViewModel must exist in Stride.Core.Assets.Editor.ViewModel.");

        Assert.True(type.IsInterface, "IDragSourceViewModel must be an interface.");
        Assert.NotNull(type.GetEvent("DragRequested"));
        Assert.NotNull(type.GetMethod("GetItemsToDrag"));
    }

    [Fact]
    public void Test15_KeepOpen_Contracts()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModel.Progress.KeepOpen")
                   ?? CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModels.KeepOpen");
        Assert.True(type != null, "KeepOpen enum must exist.");
        Assert.True(type.IsEnum, "KeepOpen must be an enum.");

        var names = Enum.GetNames(type);
        Assert.Contains("Never", names);
        Assert.Contains("OnWarningsOrErrors", names);
        Assert.Contains("OnErrors", names);
        Assert.Contains("Always", names);
    }

    [Fact]
    public void Test16_MoveDirection_Contracts()
    {
        var type = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Commands.MoveDirection")
                   ?? AssetsEditorAssembly.GetType("Stride.Assets.Presentation.ViewModel.Commands.MoveDirection");
        Assert.True(type != null, "MoveDirection enum must exist in Stride.Assets.Presentation.ViewModel.Commands.");
        Assert.True(type.IsEnum, "MoveDirection must be an enum.");

        var names = Enum.GetNames(type);
        Assert.Contains("Up", names);
        Assert.Contains("Down", names);
    }

    [Fact]
    public void Test17_IResizingTarget_Contracts()
    {
        var type = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.IResizingTarget");
        Assert.True(type != null, "IResizingTarget must exist in Stride.Assets.Presentation.ViewModel.");
        Assert.True(type.IsInterface, "IResizingTarget must be an interface.");

        Assert.NotNull(type.GetMethod("OnResizingCompleted"));
        Assert.NotNull(type.GetMethod("OnResizingDelta"));
        Assert.NotNull(type.GetMethod("OnResizingStarted"));

        var dirType = AssetsPresentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.ResizingDirection");
        Assert.True(dirType != null, "ResizingDirection must exist in Stride.Assets.Presentation.ViewModel.");
        Assert.True(dirType.IsEnum, "ResizingDirection must be an enum.");
        Assert.NotNull(dirType.GetCustomAttribute<FlagsAttribute>());
    }

    [Fact]
    public void Test18_EntityPickingResult_Contracts()
    {
        var type = typeof(Stride.Assets.Presentation.SceneEditor.EntityPickingResult);
        Assert.True(type.IsValueType, "EntityPickingResult must be a struct.");

        Assert.NotNull(type.GetField("Entity"));
        Assert.NotNull(type.GetField("ComponentId"));
        Assert.NotNull(type.GetField("MeshNodeIndex"));
        Assert.NotNull(type.GetField("MaterialIndex"));
        Assert.NotNull(type.GetField("InstanceId"));
        Assert.NotNull(type.GetProperty("Component"));
        Assert.NotNull(type.GetMethod("ToString"));

        var entity = new Entity("TestEntity");
        var result = new Stride.Assets.Presentation.SceneEditor.EntityPickingResult
        {
            Entity = entity,
            ComponentId = 1,
            MeshNodeIndex = 2,
            MaterialIndex = 3,
            InstanceId = 4
        };

        Assert.Equal("ComponentId: 1, MeshNodeIndex: 2, MaterialIndex: 3", result.ToString());
    }

    private class MockDirtiable : IDirtiable
    {
        public bool IsDirty { get; private set; }
        public void UpdateDirtiness(bool value) => IsDirty = value;
    }
}
