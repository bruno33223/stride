// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets;
using Stride.Assets.Models;
using Stride.Assets.Presentation.Thumbnails;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Storage;
using Stride.Editor.Thumbnails;
using Stride.Graphics;
using Stride.Rendering;
using Xunit;
using FontStyle = Stride.Graphics.Font.FontStyle;

namespace Stride.Core.Assets.Editor.Tests;

public class TestThumbnailCompilersAndCommandsIntegration
{
    private static readonly Type ContextType = typeof(ThumbnailCompilationContext);

    static TestThumbnailCompilersAndCommandsIntegration()
    {
        AssemblyRegistry.Register(typeof(ProceduralModelThumbnailCompiler).Assembly, AssemblyCommonCategories.Assets);
        AssemblyRegistry.Register(typeof(CustomAssetThumbnailCompiler).Assembly, AssemblyCommonCategories.Assets);
    }

    private static ThumbnailCompilerContext CreateTestContext()
    {
        var context = new ThumbnailCompilerContext();
        var gameSettings = new GameSettingsAsset();
        context.SetGameSettingsAsset(gameSettings);
        var dummyGenerator = (ThumbnailGenerator)RuntimeHelpers.GetUninitializedObject(typeof(ThumbnailGenerator));
        context.Properties.Set(ThumbnailGenerator.Key, dummyGenerator);
        return context;
    }

    public class DummyTestAsset : Asset
    {
    }

    private static void AttachDummySession(Package package)
    {
        package.FullPath = new UFile("/test/package/test.sdpkg");
        var container = new StandalonePackage(package);
        var session = new PackageSession();
        typeof(PackageContainer).GetMethod("SetSessionInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.Invoke(container, new object?[] { session });
    }

    [Fact]
    public void TestProceduralModelThumbnailCompiler_ResolutionAndCommandGeneration()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(ProceduralModelAsset), ContextType);
        Assert.NotNull(compiler);
        Assert.Equal("ProceduralModelThumbnailCompiler", compiler.GetType().Name);

        var thumbCompiler = Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.False(thumbCompiler.IsStatic);

        var context = CreateTestContext();
        var proceduralAsset = new ProceduralModelAsset();
        var package = new Package();
        var assetItem = new AssetItem("Models/ProceduralTest", proceduralAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var result = compiler.Prepare(context, assetItem);
        Assert.True(!result.HasErrors, string.Join("; ", result.Messages.Select(m => m.ToString())));
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.Contains("ProceduralModelThumbnailBuildCommand", step.Command.GetType().Name);

        var thumbCommand = Assert.IsAssignableFrom<IThumbnailCommand>(step.Command);
        Assert.NotNull(thumbCommand);

        var baseCommand = Assert.IsAssignableFrom<StrideThumbnailCommand<Model>>(step.Command);
        Assert.NotNull(baseCommand);
        Assert.Equal("__THUMBNAIL__Models/ProceduralTest", baseCommand.Url);
    }

    [Fact]
    public void TestPrecompiledFontThumbnailCompiler_ResolutionAndObsoleteAndCommandGeneration()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(PrecompiledSpriteFontAsset), ContextType);
        Assert.NotNull(compiler);
        Assert.Equal("PrecompiledFontThumbnailCompiler", compiler.GetType().Name);

        var obsoleteAttr = compiler.GetType().GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(obsoleteAttr);

        var thumbCompiler = Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.False(thumbCompiler.IsStatic);

        var context = CreateTestContext();
        var fontAsset = new PrecompiledSpriteFontAsset
        {
            FontName = "Arial",
            Size = 16,
            Style = FontStyle.Bold
        };
        var package = new Package();
        var assetItem = new AssetItem("Fonts/PrecompiledArial", fontAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var result = compiler.Prepare(context, assetItem);
        Assert.True(!result.HasErrors, string.Join("; ", result.Messages.Select(m => m.ToString())));
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.Contains("PrecompiledFontBuildCommand", step.Command.GetType().Name);

        var thumbCommand = Assert.IsAssignableFrom<IThumbnailCommand>(step.Command);
        Assert.NotNull(thumbCommand);

        var baseCommand = Assert.IsAssignableFrom<ThumbnailFromTextureCommand<Graphics.SpriteFont>>(step.Command);
        Assert.NotNull(baseCommand);
    }

    [Fact]
    public void TestPrecompiledFontThumbnailCompiler_TitleTextFormatting()
    {
        var context = CreateTestContext();
        var fontAsset = new PrecompiledSpriteFontAsset
        {
            FontName = "Very Long Font Name That Should Split",
            Size = 24,
            Style = FontStyle.Italic
        };
        var package = new Package();
        var assetItem = new AssetItem("Fonts/PrecompiledLongName", fontAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var compiler = new PrecompiledFontThumbnailCompiler();
        var result = compiler.Prepare(context, assetItem);
        Assert.False(result.HasErrors);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        var command = step.Command;
        Assert.NotNull(command);

        var method = command.GetType().GetMethod("BuildTitleText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        var title = method.Invoke(command, null) as string;
        Assert.NotNull(title);
        Assert.StartsWith("Precompiled\n", title);
        Assert.Contains("24 Italic", title);
    }

    [Fact]
    public void TestCustomAssetThumbnailCompiler_FallbackStaticThumbnail()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(Asset), ContextType);
        Assert.NotNull(compiler);
        Assert.IsType<CustomAssetThumbnailCompiler>(compiler);

        var customCompiler = (CustomAssetThumbnailCompiler)compiler;
        Assert.True(customCompiler.IsStatic);

        var context = CreateTestContext();
        var dummyAsset = new DummyTestAsset();
        var package = new Package();
        var assetItem = new AssetItem("Custom/DummyCustomAsset", dummyAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var result = customCompiler.Prepare(context, assetItem);
        Assert.True(!result.HasErrors, string.Join("; ", result.Messages.Select(m => m.ToString())));
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsAssignableFrom<BuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step);
        var command = (step as CommandBuildStep)?.Command ?? (step as ThumbnailBuildStep)?.Command;
        Assert.NotNull(command);
        Assert.IsAssignableFrom<IThumbnailCommand>(command);
    }

    [Fact]
    public void TestStaticThumbnailCompilerAndCommand_ExecutionAndParameters()
    {
        var dummyBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header magic
        var staticCompiler = new StaticThumbnailCompiler<Asset>(dummyBytes)
        {
            Priority = 500
        };
        Assert.True(staticCompiler.IsStatic);
        Assert.Equal(500, staticCompiler.Priority);

        var context = CreateTestContext();
        var dummyAsset = new DummyTestAsset();
        var package = new Package();
        var assetItem = new AssetItem("Custom/StaticTestAsset", dummyAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var result = staticCompiler.Prepare(context, assetItem);
        Assert.False(result.HasErrors);
        Assert.True(result.BuildSteps.Count > 0);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        var staticCommand = Assert.IsType<StaticThumbnailCommand<Asset>>(step.Command);
        Assert.NotNull(staticCommand);

        var iThumbCommand = Assert.IsAssignableFrom<IThumbnailCommand>(staticCommand);
        iThumbCommand.DependencyBuildStatus = LogMessageType.Warning;
        Assert.Equal(LogMessageType.Warning, iThumbCommand.DependencyBuildStatus);

        var fieldData = staticCommand.GetType().GetField("staticImageData", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(fieldData);
        Assert.Equal(dummyBytes, fieldData.GetValue(staticCommand));
        Assert.Equal(context.ThumbnailResolution, staticCommand.Parameters.ThumbnailSize);
    }

    [Fact]
    public void TestThumbnailListCompiler_StaticAndDynamicAssetCompilation()
    {
        var dummyGenerator = (ThumbnailGenerator)RuntimeHelpers.GetUninitializedObject(typeof(ThumbnailGenerator));
        var registry = BuildDependencyManager.AssetCompilerRegistry;

        bool builtInvoked = false;
        void BuiltHandler(object? sender, ThumbnailBuiltEventArgs e)
        {
            builtInvoked = true;
        }

        var listCompiler = new ThumbnailListCompiler(dummyGenerator, BuiltHandler, registry);
        var gameSettings = new GameSettingsAsset();

        var proceduralAsset = new ProceduralModelAsset();
        var package = new Package();
        var assetItem = new AssetItem("Models/ProceduralTest2", proceduralAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        // Compile as static thumbnail
        var staticSteps = listCompiler.Compile(assetItem, gameSettings, staticThumbnail: true);
        Assert.NotNull(staticSteps);
        Assert.True(staticSteps.Count > 0);

        // Compile as dynamic thumbnail
        var dynamicSteps = listCompiler.Compile(assetItem, gameSettings, staticThumbnail: false);
        Assert.NotNull(dynamicSteps);
        Assert.True(dynamicSteps.Count > 0);
    }

    [Fact]
    public void TestThumbnailCompilerContextAndCompilationContext_LifecycleAndProperties()
    {
        using var context = new ThumbnailCompilerContext();
        Assert.Equal(new Int2(128, 128), context.ThumbnailResolution);

        var gameSettings = new GameSettingsAsset();
        context.SetGameSettingsAsset(gameSettings);
        Assert.Same(gameSettings, context.GetGameSettingsAsset());

        bool eventRaised = false;
        context.ThumbnailBuilt += (s, e) => { eventRaised = true; };
        var assetItem = new AssetItem("test/url", new GameSettingsAsset());
        context.NotifyThumbnailBuilt(assetItem, ThumbnailBuildResult.Succeeded, true, Stream.Null, ObjectId.Empty);
        Assert.True(eventRaised);
    }

    [Fact]
    public void TestThumbnailCommandParameters_InitializationAndProperties()
    {
        var asset = new ProceduralModelAsset();
        var res = new Int2(256, 256);
        var parameters = new ThumbnailCommandParameters(asset, "test/thumbnail/url", res)
        {
            ColorSpace = ColorSpace.Linear,
            RenderingMode = RenderingMode.HDR
        };

        Assert.Same(asset, parameters.Asset);
        Assert.Equal("test/thumbnail/url", parameters.ThumbnailUrl);
        Assert.Equal(res, parameters.ThumbnailSize);
        Assert.Equal(ColorSpace.Linear, parameters.ColorSpace);
        Assert.Equal(RenderingMode.HDR, parameters.RenderingMode);
    }

    [Fact]
    public void TestThumbnailHierarchyAndContracts_InterfacesAndInheritance()
    {
        Assert.True(typeof(IThumbnailCompiler).IsInterface);
        Assert.True(typeof(IAssetCompiler).IsAssignableFrom(typeof(IThumbnailCompiler)));

        Assert.True(typeof(IThumbnailCommand).IsInterface);

        var compilerPropPriority = typeof(IThumbnailCompiler).GetProperty("Priority");
        Assert.NotNull(compilerPropPriority);
        Assert.Equal(typeof(int), compilerPropPriority.PropertyType);

        var compilerPropIsStatic = typeof(IThumbnailCompiler).GetProperty("IsStatic");
        Assert.NotNull(compilerPropIsStatic);
        Assert.Equal(typeof(bool), compilerPropIsStatic.PropertyType);

        var commandPropDepStatus = typeof(IThumbnailCommand).GetProperty("DependencyBuildStatus");
        Assert.NotNull(commandPropDepStatus);
        Assert.Equal(typeof(LogMessageType), commandPropDepStatus.PropertyType);
    }

    [Fact]
    public void TestStrideThumbnailCommands_InheritanceAndLifecycleContracts()
    {
        Assert.True(typeof(ThumbnailCommand).IsSubclassOf(typeof(Command)));
        Assert.True(typeof(IThumbnailCommand).IsAssignableFrom(typeof(StrideThumbnailCommand<Model>)));
        Assert.True(typeof(IThumbnailCommand).IsAssignableFrom(typeof(StaticThumbnailCommand<Asset>)));

        Assert.True(typeof(StrideThumbnailCommand<Model>).IsSubclassOf(typeof(ThumbnailCommand)));
        Assert.True(typeof(ThumbnailFromEntityCommand<Model>).IsSubclassOf(typeof(StrideThumbnailCommand<Model>)));
        Assert.True(typeof(ThumbnailFromTextureCommand<Graphics.SpriteFont>).IsSubclassOf(typeof(StrideThumbnailCommand<Graphics.SpriteFont>)));
    }
}
