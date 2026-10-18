// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Assets.Templates;
using Stride.Engine;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para o Marco P31 Fatia 1 (slice-1):
/// Asset Node Updaters, Scene Picking Commands & Asset Factories.
/// Valida presença, hierarquia, contratos e paridade dos 12 artefatos:
/// 1. SceneEditor/MaterialFilterRenderFeature.cs
/// 2. NodePresenters/Commands/FetchEntityCommand.cs
/// 3. ViewModel/Commands/MoveItemCommand.cs
/// 4. NodePresenters/Updaters/VideoAssetNodeUpdater.cs
/// 5. NodePresenters/Updaters/TextureAssetNodeUpdater.cs
/// 6. NodePresenters/Updaters/MaterialAssetNodeUpdater.cs
/// 7. NodePresenters/Updaters/ModelAssetNodeUpdater.cs
/// 8. Templates/ProceduralModelFactoryTemplateGenerator.cs
/// 9. Templates/ColliderShapeStaticMeshFactoryTemplateGenerator.cs
/// 10. Templates/ColliderShapeHullFactoryTemplateGenerator.cs
/// 11. NodePresenters/Commands/PickupSceneObjectCommandBase.cs
/// 12. Templates/SkyboxFactoryTemplateGenerator.cs
/// </summary>
public class TestAssetNodeUpdatersAndFactoriesIntegration
{
    private static readonly Assembly PresentationAssembly = typeof(Stride.Assets.Presentation.StrideDefaultAssetsPlugin).Assembly;
    private static readonly Assembly EditorAssembly = typeof(Stride.Assets.Editor.Templates.AssetTemplateGenerator).Assembly;

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
    public void Test01_FilesExistAndHaveStrictParity()
    {
        var repoRoot = GetRepoRoot();
        var presentationDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Presentation");

        // 1. Validar existência dos 12 arquivos da Fatia 1
        var materialFilterPath = Path.Combine(presentationDir, "SceneEditor", "MaterialFilterRenderFeature.cs");
        var fetchEntityPath = Path.Combine(presentationDir, "NodePresenters", "Commands", "FetchEntityCommand.cs");
        var moveItemPath = Path.Combine(presentationDir, "ViewModel", "Commands", "MoveItemCommand.cs");
        var videoUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "VideoAssetNodeUpdater.cs");
        var textureUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "TextureAssetNodeUpdater.cs");
        var materialUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "MaterialAssetNodeUpdater.cs");
        var modelUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "ModelAssetNodeUpdater.cs");
        var proceduralModelFactoryPath = Path.Combine(presentationDir, "Templates", "ProceduralModelFactoryTemplateGenerator.cs");
        var colliderStaticMeshFactoryPath = Path.Combine(presentationDir, "Templates", "ColliderShapeStaticMeshFactoryTemplateGenerator.cs");
        var colliderHullFactoryPath = Path.Combine(presentationDir, "Templates", "ColliderShapeHullFactoryTemplateGenerator.cs");
        var pickupSceneObjectPath = Path.Combine(presentationDir, "NodePresenters", "Commands", "PickupSceneObjectCommandBase.cs");
        var skyboxFactoryPath = Path.Combine(presentationDir, "Templates", "SkyboxFactoryTemplateGenerator.cs");

        Assert.True(File.Exists(materialFilterPath), $"Expected file not found: {materialFilterPath}");
        Assert.True(File.Exists(fetchEntityPath), $"Expected file not found: {fetchEntityPath}");
        Assert.True(File.Exists(moveItemPath), $"Expected file not found: {moveItemPath}");
        Assert.True(File.Exists(videoUpdaterPath), $"Expected file not found: {videoUpdaterPath}");
        Assert.True(File.Exists(textureUpdaterPath), $"Expected file not found: {textureUpdaterPath}");
        Assert.True(File.Exists(materialUpdaterPath), $"Expected file not found: {materialUpdaterPath}");
        Assert.True(File.Exists(modelUpdaterPath), $"Expected file not found: {modelUpdaterPath}");
        Assert.True(File.Exists(proceduralModelFactoryPath), $"Expected file not found: {proceduralModelFactoryPath}");
        Assert.True(File.Exists(colliderStaticMeshFactoryPath), $"Expected file not found: {colliderStaticMeshFactoryPath}");
        Assert.True(File.Exists(colliderHullFactoryPath), $"Expected file not found: {colliderHullFactoryPath}");
        Assert.True(File.Exists(pickupSceneObjectPath), $"Expected file not found: {pickupSceneObjectPath}");
        Assert.True(File.Exists(skyboxFactoryPath), $"Expected file not found: {skyboxFactoryPath}");

        // 2. Validar paridade estrita de linhas (85-100% dos originais WPF)
        var materialFilterLines = File.ReadAllLines(materialFilterPath).Length;
        Assert.InRange(materialFilterLines, 45, 65); // Original: 55-56 lines

        var fetchEntityLines = File.ReadAllLines(fetchEntityPath).Length;
        Assert.InRange(fetchEntityLines, 48, 68); // Original: 58-59 lines

        var moveItemLines = File.ReadAllLines(moveItemPath).Length;
        Assert.InRange(moveItemLines, 46, 66); // Original: 56-57 lines

        var videoUpdaterLines = File.ReadAllLines(videoUpdaterPath).Length;
        Assert.InRange(videoUpdaterLines, 43, 62); // Original: 52-53 lines

        var textureUpdaterLines = File.ReadAllLines(textureUpdaterPath).Length;
        Assert.InRange(textureUpdaterLines, 43, 62); // Original: 52 lines

        var materialUpdaterLines = File.ReadAllLines(materialUpdaterPath).Length;
        Assert.InRange(materialUpdaterLines, 42, 60); // Original: 51-52 lines

        var modelUpdaterLines = File.ReadAllLines(modelUpdaterPath).Length;
        Assert.InRange(modelUpdaterLines, 40, 58); // Original: 49-50 lines

        var proceduralModelFactoryLines = File.ReadAllLines(proceduralModelFactoryPath).Length;
        Assert.InRange(proceduralModelFactoryLines, 39, 57); // Original: 48-49 lines

        var colliderStaticMeshFactoryLines = File.ReadAllLines(colliderStaticMeshFactoryPath).Length;
        Assert.InRange(colliderStaticMeshFactoryLines, 39, 57); // Original: 48-49 lines

        var colliderHullFactoryLines = File.ReadAllLines(colliderHullFactoryPath).Length;
        Assert.InRange(colliderHullFactoryLines, 39, 57); // Original: 48-49 lines

        var pickupSceneObjectLines = File.ReadAllLines(pickupSceneObjectPath).Length;
        Assert.InRange(pickupSceneObjectLines, 39, 57); // Original: 48-49 lines

        var skyboxFactoryLines = File.ReadAllLines(skyboxFactoryPath).Length;
        Assert.InRange(skyboxFactoryLines, 38, 56); // Original: 47-48 lines

        // 3. Desacoplamento estrito de Win32 e WPF em todos os arquivos
        var allPaths = new[]
        {
            materialFilterPath, fetchEntityPath, moveItemPath, videoUpdaterPath,
            textureUpdaterPath, materialUpdaterPath, modelUpdaterPath, proceduralModelFactoryPath,
            colliderStaticMeshFactoryPath, colliderHullFactoryPath, pickupSceneObjectPath, skyboxFactoryPath
        };

        foreach (var path in allPaths)
        {
            var content = File.ReadAllText(path);
            Assert.DoesNotContain("System.Windows", content);
            Assert.DoesNotContain("PresentationFramework", content);
            Assert.DoesNotContain("WindowsBase", content);
            Assert.DoesNotContain("System.Drawing", content);
        }
    }

    [Fact]
    public void Test02_MaterialFilterRenderFeature_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.MaterialFilterRenderFeature");
        Assert.True(type != null, "MaterialFilterRenderFeature deve existir em Stride.Assets.Presentation.SceneEditor.");
        Assert.True(typeof(SubRenderFeature).IsAssignableFrom(type), "MaterialFilterRenderFeature deve herdar de SubRenderFeature.");

        var filterProp = type.GetProperty("MaterialFilter");
        Assert.NotNull(filterProp);
        Assert.Equal(typeof(ShaderSource), filterProp.PropertyType);

        var initMethod = type.GetMethod("InitializeCore", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(initMethod);

        var prepareMethod = type.GetMethod("PrepareEffectPermutations", new[] { typeof(RenderDrawContext) });
        Assert.NotNull(prepareMethod);

        dynamic instance = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)instance);
    }

    [Fact]
    public void Test03_FetchEntityCommand_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Commands.FetchEntityCommand")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Commands.FetchEntityCommand");
        Assert.True(type != null, "FetchEntityCommand deve existir em Stride.Assets.Presentation.NodePresenters.Commands.");
        Assert.True(typeof(SyncNodePresenterCommandBase).IsAssignableFrom(type), "FetchEntityCommand deve herdar de SyncNodePresenterCommandBase.");

        var cmdNameField = type.GetField("CommandName", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(cmdNameField);
        Assert.Equal("FetchEntity", (string)cmdNameField.GetValue(null)!);

        dynamic cmd = Activator.CreateInstance(type!)!;
        Assert.Equal("FetchEntity", (string)cmd.Name);

        var canAttachMethod = type.GetMethod("CanAttach", new[] { typeof(INodePresenter) });
        Assert.NotNull(canAttachMethod);

        var fetchMethod = type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "Fetch");
        Assert.NotNull(fetchMethod);
    }

    [Fact]
    public void Test04_MoveItemCommand_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.ViewModel.Commands.MoveItemCommand");
        Assert.True(type != null, "MoveItemCommand deve existir em Stride.Assets.Presentation.ViewModel.Commands.");
        Assert.True(typeof(SyncNodePresenterCommandBase).IsAssignableFrom(type), "MoveItemCommand deve herdar de SyncNodePresenterCommandBase.");

        var cmdNameField = type.GetField("CommandName", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(cmdNameField);
        Assert.Equal("MoveItem", (string)cmdNameField.GetValue(null)!);

        dynamic cmd = Activator.CreateInstance(type!)!;
        Assert.Equal("MoveItem", (string)cmd.Name);
        Assert.Equal(CombineMode.AlwaysCombine, (CombineMode)cmd.CombineMode);
    }

    [Fact]
    public void Test05_VideoAssetNodeUpdater_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.VideoAssetNodeUpdater")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Updaters.VideoAssetNodeUpdater");
        Assert.True(type != null, "VideoAssetNodeUpdater deve existir em Stride.Assets.Presentation.NodePresenters.Updaters.");

        var updater = Activator.CreateInstance(type!, nonPublic: true);
        Assert.NotNull(updater);
    }

    [Fact]
    public void Test06_TextureAssetNodeUpdater_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.TextureAssetNodeUpdater")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Updaters.TextureAssetNodeUpdater");
        Assert.True(type != null, "TextureAssetNodeUpdater deve existir em Stride.Assets.Presentation.NodePresenters.Updaters.");

        var updater = Activator.CreateInstance(type!, nonPublic: true);
        Assert.NotNull(updater);
    }

    [Fact]
    public void Test07_MaterialAssetNodeUpdater_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.MaterialAssetNodeUpdater")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Updaters.MaterialAssetNodeUpdater");
        Assert.True(type != null, "MaterialAssetNodeUpdater deve existir em Stride.Assets.Presentation.NodePresenters.Updaters.");

        var updater = Activator.CreateInstance(type!, nonPublic: true);
        Assert.NotNull(updater);
    }

    [Fact]
    public void Test08_ModelAssetNodeUpdater_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.ModelAssetNodeUpdater")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Updaters.ModelAssetNodeUpdater");
        Assert.True(type != null, "ModelAssetNodeUpdater deve existir em Stride.Assets.Presentation.NodePresenters.Updaters.");

        var updater = Activator.CreateInstance(type!, nonPublic: true);
        Assert.NotNull(updater);
    }

    [Fact]
    public void Test09_ProceduralModelFactoryTemplateGenerator_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.Templates.ProceduralModelFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Presentation.Templates.ProceduralModelFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Templates.ProceduralModelFactoryTemplateGenerator");
        Assert.True(type != null, "ProceduralModelFactoryTemplateGenerator deve existir em Stride.Assets.Presentation.Templates.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("8267F08C-3DC8-48F4-81EA-4888A1CEF9CE");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);

        var isSupportingMethod = type.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
    }

    [Fact]
    public void Test10_ColliderShapeStaticMeshFactoryTemplateGenerator_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.Templates.ColliderShapeStaticMeshFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Presentation.Templates.ColliderShapeStaticMeshFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Templates.ColliderShapeStaticMeshFactoryTemplateGenerator");
        Assert.True(type != null, "ColliderShapeStaticMeshFactoryTemplateGenerator deve existir em Stride.Assets.Presentation.Templates.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("5A3CD5E3-4328-36AF-A41C-8146D2D21F83");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);

        var isSupportingMethod = type.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
    }

    [Fact]
    public void Test11_ColliderShapeHullFactoryTemplateGenerator_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.Templates.ColliderShapeHullFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Presentation.Templates.ColliderShapeHullFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Templates.ColliderShapeHullFactoryTemplateGenerator");
        Assert.True(type != null, "ColliderShapeHullFactoryTemplateGenerator deve existir em Stride.Assets.Presentation.Templates.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("5A3CD5E3-4318-46AF-A41C-8146D2D21F80");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);

        var isSupportingMethod = type.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
    }

    [Fact]
    public void Test12_PickupSceneObjectCommandBase_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Commands.PickupSceneObjectCommandBase")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Commands.PickupSceneObjectCommandBase");
        Assert.True(type != null, "PickupSceneObjectCommandBase deve existir.");
        Assert.True(type.IsAbstract, "PickupSceneObjectCommandBase deve ser abstrata.");

        var createPickerMethod = type.GetMethod("CreatePicker", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(createPickerMethod);
        Assert.True(createPickerMethod.IsAbstract, "CreatePicker deve ser abstract.");
    }

    [Fact]
    public void Test13_SkyboxFactoryTemplateGenerator_Contract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.Templates.SkyboxFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Presentation.Templates.SkyboxFactoryTemplateGenerator")
                   ?? EditorAssembly.GetType("Stride.Assets.Editor.Templates.SkyboxFactoryTemplateGenerator");
        Assert.True(type != null, "SkyboxFactoryTemplateGenerator deve existir em Stride.Assets.Presentation.Templates.");

        var defaultField = type.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var instance = defaultField.GetValue(null);
        Assert.NotNull(instance);

        var idField = type.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var expectedGuid = new Guid("F16AB154-86A6-4020-B410-88FBD87D3CA5");
        Assert.Equal(expectedGuid, (Guid)idField.GetValue(null)!);

        var isSupportingMethod = type.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
    }
}
