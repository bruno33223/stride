// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Assets;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Media;
using Stride.Assets.Models;
using Stride.Assets.Physics;
using Stride.Assets.Rendering;
using Stride.Assets.Skyboxes;
using Stride.Assets.Sprite;
using Stride.Assets.SpriteFont;
using Stride.Assets.Textures;
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Assets.Presentation.Thumbnails;
using Stride.Editor.Thumbnails;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestThumbnailInfrastructure
{
    private static readonly Type ContextType = typeof(ThumbnailCompilationContext);

    static TestThumbnailInfrastructure()
    {
        AssemblyRegistry.Register(typeof(ModelThumbnailCompiler).Assembly, AssemblyCommonCategories.Assets);
    }

    private static ThumbnailCompilerContext CreateTestContext()
    {
        var context = new ThumbnailCompilerContext();
        var gameSettings = new GameSettingsAsset();
        context.SetGameSettingsAsset(gameSettings);
        var dummyGenerator = (ThumbnailGenerator)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ThumbnailGenerator));
        context.Properties.Set(ThumbnailGenerator.Key, dummyGenerator);
        return context;
    }

    private static void AttachDummySession(Package package)
    {
        var container = new StandalonePackage(package);
        var session = (PackageSession)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(PackageSession));
        typeof(PackageContainer).GetMethod("SetSessionInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.Invoke(container, new object?[] { session });
    }

    [Fact]
    public void TestCompilerResolution_ModelAsset()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(ModelAsset), ContextType);
        Assert.NotNull(compiler);
        Assert.Equal("ModelThumbnailCompiler", compiler.GetType().Name);
        Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.False(((IThumbnailCompiler)compiler).IsStatic);
    }

    [Fact]
    public void TestCompilerResolution_TextureAsset()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(TextureAsset), ContextType);
        Assert.NotNull(compiler);
        Assert.Equal("TextureThumbnailCompiler", compiler.GetType().Name);
        Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.False(((IThumbnailCompiler)compiler).IsStatic);
    }

    [Fact]
    public void TestCompilerResolution_SceneAsset()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(SceneAsset), ContextType);
        Assert.NotNull(compiler);
        Assert.Equal("SceneThumbnailCompiler", compiler.GetType().Name);
        Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.True(((IThumbnailCompiler)compiler).IsStatic);
    }

    [Fact]
    public void TestCompilerResolution_MaterialAsset()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(MaterialAsset), ContextType);
        Assert.NotNull(compiler);
        Assert.Equal("MaterialThumbnailCompiler", compiler.GetType().Name);
        Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.False(((IThumbnailCompiler)compiler).IsStatic);
    }

    [Fact]
    public void TestCompilerResolution_AllRemainingAssets()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;

        var expectedCompilers = new (Type assetType, string compilerName, bool isStatic)[]
        {
            (typeof(PrefabAsset), "PrefabThumbnailCompiler", false),
            (typeof(AnimationAsset), "AnimationThumbnailCompiler", false),
            (typeof(SpriteFontAsset), "FontThumbnailCompiler", false),
            (typeof(GameSettingsAsset), "GameSettingsThumbnailCompiler", true),
            (typeof(GraphicsCompositorAsset), "GraphicsCompositorThumbnailCompiler", true),
            (typeof(HeightmapAsset), "HeightmapThumbnailCompiler", false),
            (typeof(SkeletonAsset), "SkeletonThumbnailCompiler", true),
            (typeof(SkyboxAsset), "SkyboxThumbnailCompiler", true),
            (typeof(SoundAsset), "SoundThumbnailCompiler", true),
            (typeof(SpriteSheetAsset), "SpriteSheetThumbnailCompiler", false),
            (typeof(UILibraryAsset), "UILibraryThumbnailCompiler", true),
            (typeof(UIPageAsset), "UIPageThumbnailCompiler", false),
            (typeof(VideoAsset), "VideoThumbnailCompiler", true),
        };

        foreach (var (assetType, compilerName, isStatic) in expectedCompilers)
        {
            var compiler = registry.GetCompiler(assetType, ContextType);
            Assert.True(compiler != null, $"Compiler for {assetType.Name} should not be null.");
            Assert.Equal(compilerName, compiler.GetType().Name);
            var thumbCompiler = Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
            Assert.Equal(isStatic, thumbCompiler.IsStatic);
        }
    }

    [Fact]
    public void TestPrepareCommand_SceneThumbnail()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(SceneAsset), ContextType);
        Assert.NotNull(compiler);

        var context = CreateTestContext();
        var sceneAsset = new SceneAsset();
        var package = new Package();
        var assetItem = new AssetItem("Scenes/SceneTest", sceneAsset);
        package.Assets.Add(assetItem);

        var result = compiler.Prepare(context, assetItem);
        Assert.False(result.HasErrors);
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.IsType<StaticThumbnailCommand<SceneAsset>>(step.Command);
    }

    [Fact]
    public void TestPrepareCommand_ModelThumbnail()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(ModelAsset), ContextType);
        Assert.NotNull(compiler);

        var context = CreateTestContext();
        var modelAsset = new ModelAsset();
        var package = new Package();
        var assetItem = new AssetItem("Models/ModelTest", modelAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var result = compiler.Prepare(context, assetItem);
        Assert.True(!result.HasErrors, string.Join("; ", result.Messages.Select(m => m.ToString())));
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.Contains("ModelThumbnailBuildCommand", step.Command.GetType().Name);
    }

    [Fact]
    public void TestPrepareCommand_MaterialThumbnail()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(MaterialAsset), ContextType);
        Assert.NotNull(compiler);

        var context = CreateTestContext();
        var materialAsset = new MaterialAsset();
        var package = new Package();
        var assetItem = new AssetItem("Materials/MaterialTest", materialAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var result = compiler.Prepare(context, assetItem);
        Assert.True(!result.HasErrors, string.Join("; ", result.Messages.Select(m => m.ToString())));
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.Contains("MaterialThumbnailBuildCommand", step.Command.GetType().Name);
    }

    [Fact]
    public void TestPrepareCommand_TextureThumbnail_MissingSourceFallback()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(TextureAsset), ContextType);
        Assert.NotNull(compiler);

        var context = CreateTestContext();
        var textureAsset = new TextureAsset { Source = "non_existent_file.png" };
        var package = new Package();
        var assetItem = new AssetItem("Textures/TextureTest", textureAsset);
        package.Assets.Add(assetItem);

        var result = compiler.Prepare(context, assetItem);
        Assert.True(result.HasErrors, "Should register error for missing source file");
        Assert.True(result.BuildSteps.Count > 0);

        // Fallback static thumbnail command step
        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.IsType<StaticThumbnailCommand<TextureAsset>>(step.Command);
    }

    [Fact]
    public void TestStaticThumbnails_ResourceBytesLoaded()
    {
        var type = Type.GetType("Stride.Assets.Presentation.Resources.Thumbnails.StaticThumbnails, Stride.Assets.Presentation");
        Assert.NotNull(type);

        var properties = new[]
        {
            "AnimationThumbnail",
            "GameSettingsThumbnail",
            "GraphicsCompositorThumbnail",
            "SceneThumbnail",
            "ScriptSourceFileThumbnail",
            "SkeletonThumbnail",
            "SkyboxThumbnail",
            "SoundThumbnail",
            "UILibraryThumbnail",
            "VideoThumbnail"
        };

        foreach (var propName in properties)
        {
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Static);
            Assert.True(prop != null, $"Property {propName} must exist on StaticThumbnails");
            var bytes = prop.GetValue(null) as byte[];
            Assert.True(bytes != null && bytes.Length > 0, $"Property {propName} must return non-empty byte array");
        }
    }
}
