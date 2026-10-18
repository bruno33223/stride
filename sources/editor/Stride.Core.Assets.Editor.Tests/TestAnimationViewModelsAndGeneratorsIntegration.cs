// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.Quantum;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Yaml;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAnimationViewModelsAndGeneratorsIntegration
{
    private static readonly Assembly EditorAssembly = typeof(AssetTemplateGenerator).Assembly;

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

    private static (SessionViewModel session, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        return (session, sp);
    }

    [Fact]
    public void TestAnimationViewModel_TypeAndHierarchy()
    {
        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.AnimationViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.ViewModels.AnimationViewModel must exist in Stride.Assets.Editor assembly.");

        var importedBaseType = typeof(AssetViewModel).Assembly.GetType("Stride.Core.Assets.Presentation.ViewModels.ImportedAssetViewModel`1");
        Assert.True(importedBaseType != null, "ImportedAssetViewModel<T> must exist in Stride.Core.Assets.Presentation.");

        var genericImported = importedBaseType.MakeGenericType(typeof(AnimationAsset));
        Assert.True(genericImported.IsAssignableFrom(vmType),
            "AnimationViewModel must inherit from ImportedAssetViewModel<AnimationAsset>.");

        var attr = vmType.GetCustomAttribute<AssetViewModelAttribute<AnimationAsset>>(inherit: false);
        Assert.NotNull(attr);

        var ctor = vmType.GetConstructor([typeof(AssetViewModel.ConstructorParameters)]);
        Assert.NotNull(ctor);

        var getImporterMethod = vmType.GetMethod("GetImporter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(getImporterMethod);

        var updateFromSourceMethod = vmType.GetMethod("UpdateAssetFromSource", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(AnimationAsset)], null);
        Assert.NotNull(updateFromSourceMethod);
    }

    [Fact]
    public void TestAnimationViewModel_UpdateAssetFromSourceAndImporter()
    {
        var vmType = EditorAssembly.GetType("Stride.Assets.Editor.ViewModels.AnimationViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.ViewModels.AnimationViewModel must exist.");

        var (session, sp) = CreateTestSession();
        var animAsset = new AnimationAsset
        {
            AnimationTimeMinimum = TimeSpan.FromSeconds(1),
            AnimationTimeMaximum = TimeSpan.FromSeconds(5),
            Source = "test_source.fbx"
        };
        var assetItem = new AssetItem("animations/test_anim", animAsset);

        var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
        container.RegisterGraph(graph);

        var vm = (AssetViewModel)RuntimeHelpers.GetUninitializedObject(vmType);
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, assetItem);
        typeof(AssetViewModel).GetField("<PropertyGraph>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, graph);
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(vm, [sp]);

        // Test UpdateAssetFromSource
        var updateFromSourceMethod = vmType.GetMethod("UpdateAssetFromSource", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(AnimationAsset)], null);
        Assert.NotNull(updateFromSourceMethod);

        var assetToMerge = new AnimationAsset
        {
            AnimationTimeMinimum = TimeSpan.FromSeconds(0.25),
            AnimationTimeMaximum = TimeSpan.FromSeconds(12.75)
        };

        updateFromSourceMethod.Invoke(vm, [assetToMerge]);

        Assert.Equal(TimeSpan.FromSeconds(0.25), vm.AssetRootNode?[nameof(AnimationAsset.AnimationTimeMinimum)]?.Retrieve());
        Assert.Equal(TimeSpan.FromSeconds(12.75), vm.AssetRootNode?[nameof(AnimationAsset.AnimationTimeMaximum)]?.Retrieve());
        Assert.Equal(TimeSpan.FromSeconds(0.25), animAsset.AnimationTimeMinimum);
        Assert.Equal(TimeSpan.FromSeconds(12.75), animAsset.AnimationTimeMaximum);

        // Test GetImporter returns null or ModelAssetImporter depending on registry
        var getImporterMethod = vmType.GetMethod("GetImporter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(getImporterMethod);
        var importer = getImporterMethod.Invoke(vm, null);
        if (importer != null)
        {
            Assert.True(importer is ModelAssetImporter, "Importer should be of type ModelAssetImporter.");
        }
    }

    [Fact]
    public void TestSkeletonFromFileTemplateGenerator_ContractAndRegistration()
    {
        var genType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.SkeletonFromFileTemplateGenerator");
        Assert.True(genType != null, "Stride.Assets.Editor.Templates.SkeletonFromFileTemplateGenerator must exist.");

        Assert.True(typeof(AssetFromFileTemplateGenerator).IsAssignableFrom(genType),
            "SkeletonFromFileTemplateGenerator must inherit from AssetFromFileTemplateGenerator.");

        var defaultField = genType.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var generator = defaultField.GetValue(null) as ITemplateGenerator;
        Assert.NotNull(generator);

        var idField = genType.GetField("Id", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var id = (Guid)idField.GetValue(null)!;
        Assert.Equal(new Guid("7DA3597C-7AC4-450E-B60B-21DDB6296208"), id);

        var desc = new TemplateAssetFactoryDescription { Id = id, AssetTypeName = "SkeletonAsset" };
        var otherDesc = new TemplateAssetFactoryDescription { Id = Guid.NewGuid(), AssetTypeName = "SkeletonAsset" };

        var isSupportingMethod = genType.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
        Assert.True((bool)isSupportingMethod.Invoke(generator, [desc])!);
        Assert.False((bool)isSupportingMethod.Invoke(generator, [otherDesc])!);

        // Test registration via StrideTemplates.Register
        Stride.Assets.Editor.Templates.StrideTemplates.Register();
        var registeredGen = TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(desc);
        Assert.NotNull(registeredGen);
        Assert.Same(generator, registeredGen);
    }

    [Fact]
    public void TestAnimationFromFileTemplateGenerator_ContractAndRegistration()
    {
        var genType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.AnimationFromFileTemplateGenerator");
        Assert.True(genType != null, "Stride.Assets.Editor.Templates.AnimationFromFileTemplateGenerator must exist.");

        Assert.True(typeof(AssetFromFileTemplateGenerator).IsAssignableFrom(genType),
            "AnimationFromFileTemplateGenerator must inherit from AssetFromFileTemplateGenerator.");

        var defaultField = genType.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var generator = defaultField.GetValue(null) as ITemplateGenerator;
        Assert.NotNull(generator);

        var idField = genType.GetField("Id", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var id = (Guid)idField.GetValue(null)!;
        Assert.Equal(new Guid("24E65D4C-0207-4B62-B8F2-BAB33F44B4FA"), id);

        var desc = new TemplateAssetFactoryDescription { Id = id, AssetTypeName = "AnimationAsset" };
        var otherDesc = new TemplateAssetFactoryDescription { Id = Guid.NewGuid(), AssetTypeName = "AnimationAsset" };

        var isSupportingMethod = genType.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);
        Assert.True((bool)isSupportingMethod.Invoke(generator, [desc])!);
        Assert.False((bool)isSupportingMethod.Invoke(generator, [otherDesc])!);

        // Test registration via StrideTemplates.Register
        Stride.Assets.Editor.Templates.StrideTemplates.Register();
        var registeredGen = TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(desc);
        Assert.NotNull(registeredGen);
        Assert.Same(generator, registeredGen);
    }

    [Fact]
    public void TestAnimationTemplateFilesAndScriptsIntegrity()
    {
        var repoRoot = GetRepoRoot();
        var templatesDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor", "Templates", "Assets");

        var scriptsAnimDir = Path.Combine(templatesDir, "Scripts", "Animation");
        Assert.True(Directory.Exists(scriptsAnimDir), $"Directory {scriptsAnimDir} must exist.");

        // Check AnimationBlend.cs
        var blendScriptPath = Path.Combine(scriptsAnimDir, "AnimationBlend.cs");
        Assert.True(File.Exists(blendScriptPath), $"AnimationBlend.cs must exist at {blendScriptPath}.");
        var blendScriptContent = File.ReadAllText(blendScriptPath);
        var blendScriptLines = File.ReadAllLines(blendScriptPath);
        Assert.InRange(blendScriptLines.Length, 80, 90);
        Assert.Contains("##Namespace##", blendScriptContent);
        Assert.Contains("##Scriptname##", blendScriptContent);
        Assert.Contains("IBlendTreeBuilder", blendScriptContent);
        Assert.Contains("BuildBlendTree", blendScriptContent);

        // Check AnimationBlend.sdtpl
        var blendTplPath = Path.Combine(scriptsAnimDir, "AnimationBlend.sdtpl");
        Assert.True(File.Exists(blendTplPath), $"AnimationBlend.sdtpl must exist at {blendTplPath}.");
        var blendTpl = YamlSerializer.Load<TemplateDescription>(blendTplPath) as TemplateAssetDescription;
        Assert.NotNull(blendTpl);
        Assert.Equal(new Guid("C8387DE6-BC49-4F0F-9DB1-37109621BC8F"), blendTpl.Id);
        Assert.Equal("ScriptSourceFileAsset", blendTpl.AssetTypeName);
        Assert.Equal("AnimationBlend", blendTpl.DefaultOutputName);

        // Check AnimationStart.cs
        var startScriptPath = Path.Combine(scriptsAnimDir, "AnimationStart.cs");
        Assert.True(File.Exists(startScriptPath), $"AnimationStart.cs must exist at {startScriptPath}.");
        var startScriptContent = File.ReadAllText(startScriptPath);
        var startScriptLines = File.ReadAllLines(startScriptPath);
        Assert.InRange(startScriptLines.Length, 50, 60);
        Assert.Contains("##Namespace##", startScriptContent);
        Assert.Contains("##Scriptname##", startScriptContent);
        Assert.Contains("StartupScript", startScriptContent);
        Assert.Contains("PlayAnimation", startScriptContent);

        // Check AnimationStart.sdtpl
        var startTplPath = Path.Combine(scriptsAnimDir, "AnimationStart.sdtpl");
        Assert.True(File.Exists(startTplPath), $"AnimationStart.sdtpl must exist at {startTplPath}.");
        var startTpl = YamlSerializer.Load<TemplateDescription>(startTplPath) as TemplateAssetDescription;
        Assert.NotNull(startTpl);
        Assert.Equal(new Guid("182719A2-FA85-4839-8145-CD1D88A6AB50"), startTpl.Id);
        Assert.Equal("ScriptSourceFileAsset", startTpl.AssetTypeName);
        Assert.Equal("AnimationStart", startTpl.DefaultOutputName);

        // Check Templates/Assets/Animations/
        var animDir = Path.Combine(templatesDir, "Animations");
        Assert.True(Directory.Exists(animDir), $"Directory {animDir} must exist.");

        var skelTplPath = Path.Combine(animDir, "DefaultSkeleton.sdtpl");
        Assert.True(File.Exists(skelTplPath), $"DefaultSkeleton.sdtpl must exist at {skelTplPath}.");
        var skelTpl = YamlSerializer.Load<TemplateDescription>(skelTplPath) as TemplateAssetFactoryDescription;
        Assert.NotNull(skelTpl);
        Assert.Equal(new Guid("7DA3597C-7AC4-450E-B60B-21DDB6296208"), skelTpl.Id);
        Assert.Equal("SkeletonAsset", skelTpl.AssetTypeName);
        Assert.True(skelTpl.ImportSource);

        var animTplPath = Path.Combine(animDir, "DefaultAnimation.sdtpl");
        Assert.True(File.Exists(animTplPath), $"DefaultAnimation.sdtpl must exist at {animTplPath}.");
        var animTpl = YamlSerializer.Load<TemplateDescription>(animTplPath) as TemplateAssetFactoryDescription;
        Assert.NotNull(animTpl);
        Assert.Equal(new Guid("24E65D4C-0207-4B62-B8F2-BAB33F44B4FA"), animTpl.Id);
        Assert.Equal("AnimationAsset", animTpl.AssetTypeName);
        Assert.True(animTpl.ImportSource);
    }
}
