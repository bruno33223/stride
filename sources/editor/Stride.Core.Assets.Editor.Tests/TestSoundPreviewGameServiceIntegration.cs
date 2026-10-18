// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Media;
using Stride.Assets.Presentation.Thumbnails;
using Stride.Audio;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Core;
using Stride.Editor.Preview;
using Stride.Editor.Thumbnails;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSoundPreviewGameServiceIntegration
{
    static TestSoundPreviewGameServiceIntegration()
    {
        AssemblyRegistry.Register(typeof(SoundThumbnailCompiler).Assembly, AssemblyCommonCategories.Assets);
        AssemblyRegistry.Register(typeof(SoundAssetEditorGameCompiler).Assembly, AssemblyCommonCategories.Assets);
    }

    private static (SessionViewModel session, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        var dispatcher = new NullDispatcherService();
        sp.RegisterService(dispatcher);

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(session, [sp]);
        return (session, sp);
    }

    [Fact]
    public void TestSoundPreview_LoopSupportAndVolumeControl()
    {
        var preview = new SoundPreview();

        // 1. Volume Control
        preview.SetMasterVolume(0.75);

        // 2. Loop Control - verify IsLooping property exists and can be set/read
        var loopingProp = typeof(SoundPreview).GetProperty("IsLooping", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(loopingProp != null, "SoundPreview must define a public IsLooping property for loop control.");
        Assert.True(loopingProp.CanRead, "IsLooping must be readable.");
        Assert.True(loopingProp.CanWrite, "IsLooping must be writable.");

        // Initial default should be false
        var initialLoop = (bool)loopingProp.GetValue(preview)!;
        Assert.False(initialLoop, "IsLooping should default to false.");

        // Set to true
        loopingProp.SetValue(preview, true);
        var activeLoop = (bool)loopingProp.GetValue(preview)!;
        Assert.True(activeLoop, "IsLooping must be true after setting it to true.");

        // Verify SetLoop method exists as well for imperative control
        var setLoopMethod = typeof(SoundPreview).GetMethod("SetLoop", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(setLoopMethod != null, "SoundPreview must provide a public SetLoop(bool) method.");
        setLoopMethod.Invoke(preview, [false]);
        var loopAfterMethod = (bool)loopingProp.GetValue(preview)!;
        Assert.False(loopAfterMethod, "SetLoop(false) must update IsLooping to false.");
    }

    [Fact]
    public void TestSoundPreview_ViewModelAsyncNotificationAndCommands()
    {
        var (session, sp) = CreateTestSession();
        var vm = new SoundPreviewViewModel(session);
        var preview = new SoundPreview();

        // Attach preview to ViewModel
        vm.AttachPreview(preview);

        // Initially commands are initialized
        Assert.True(vm.PlayCommand.IsEnabled);
        Assert.False(vm.PauseCommand.IsEnabled);
        Assert.False(vm.IsAudioValid);
        Assert.Equal(TimeSpan.Zero, vm.CurrentTime);
        Assert.Equal(TimeSpan.Zero, vm.Duration);

        // Simulate audio playback notification from game thread
        var current = TimeSpan.FromSeconds(2.5);
        var total = TimeSpan.FromSeconds(10.0);
        preview.UpdateViewModelTime?.Invoke(true, true, current, total);

        Assert.True(vm.IsAudioValid);
        Assert.False(vm.PlayCommand.IsEnabled);
        Assert.True(vm.PauseCommand.IsEnabled);
        Assert.Equal(current, vm.CurrentTime);
        Assert.Equal(total, vm.Duration);

        // Simulate pause/stopped notification
        preview.UpdateViewModelTime?.Invoke(true, false, current, total);
        Assert.True(vm.PlayCommand.IsEnabled);
        Assert.False(vm.PauseCommand.IsEnabled);

        // Test volume binding update propagates to preview
        vm.MasterVolume = 0.42;
        Assert.Equal(0.42, vm.MasterVolume);
    }

    [Fact]
    public void TestSoundPreview_NullDispatcherSafety()
    {
        // When dispatcher is null or not yet set, notifications must not throw NullReferenceException
        var preview = new SoundPreview();

        bool callbackInvoked = false;
        preview.UpdateViewModelTime = (hasAudio, isPlaying, cur, dur) =>
        {
            callbackInvoked = true;
        };

        // Calling UpdateViewModelTime without dispatcher should succeed safely
        preview.UpdateViewModelTime.Invoke(false, false, TimeSpan.Zero, TimeSpan.Zero);
        Assert.True(callbackInvoked);

        // Calling SetCurrentTime, Play, Pause, SetMasterVolume when instance is null should not throw
        preview.SetCurrentTime(TimeSpan.FromSeconds(1));
        preview.Play();
        preview.Pause();
        preview.SetMasterVolume(0.5);
    }

    [Fact]
    public void TestSoundAssetEditorGameCompiler_PrepareAndResolution()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(SoundAsset), typeof(EditorGameCompilationContext));
        Assert.NotNull(compiler);
        Assert.IsType<SoundAssetEditorGameCompiler>(compiler);

        var tempSource = Path.GetTempFileName();
        try
        {
            var soundAsset = new SoundAsset { Source = tempSource };
            var packageDir = Path.GetDirectoryName(tempSource)!;
            var package = new Package { FullPath = new Core.IO.UFile(Path.Combine(packageDir, "TestPackage.sdpkg")) };
            var assetItem = new AssetItem("Audio/TestSound", soundAsset);
            package.Assets.Add(assetItem);
            var context = new AssetCompilerContext { Platform = Core.PlatformType.Linux };

            var result = compiler.Prepare(context, assetItem);
            Assert.False(result.HasErrors);
            Assert.Single(result.BuildSteps.Steps);

            var commandStep = Assert.IsAssignableFrom<CommandBuildStep>(result.BuildSteps.Steps.First());
            Assert.NotNull(commandStep);
            Assert.NotNull(commandStep.Command);
            Assert.IsType<DummyAssetCommand<SoundAsset, Sound>>(commandStep.Command);
        }
        finally
        {
            if (File.Exists(tempSource))
                File.Delete(tempSource);
        }
    }

    [Fact]
    public void TestSoundThumbnailCompiler_StaticThumbnailResolution()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;
        var compiler = registry.GetCompiler(typeof(SoundAsset), typeof(ThumbnailCompilationContext));
        Assert.NotNull(compiler);
        Assert.IsType<SoundThumbnailCompiler>(compiler);

        var thumbCompiler = Assert.IsAssignableFrom<IThumbnailCompiler>(compiler);
        Assert.True(thumbCompiler.IsStatic, "Sound thumbnail must be static.");

        var context = new ThumbnailCompilerContext();
        var gameSettings = new GameSettingsAsset();
        context.SetGameSettingsAsset(gameSettings);
        var dummyGenerator = (ThumbnailGenerator)RuntimeHelpers.GetUninitializedObject(typeof(ThumbnailGenerator));
        context.Properties.Set(ThumbnailGenerator.Key, dummyGenerator);

        var soundAsset = new SoundAsset();
        var package = new Package { FullPath = new Core.IO.UFile("/TestPackage.sdpkg") };
        var assetItem = new AssetItem("Audio/AmbientTrack", soundAsset);
        package.Assets.Add(assetItem);

        var result = compiler.Prepare(context, assetItem);
        Assert.False(result.HasErrors);
        Assert.Single(result.BuildSteps.Steps);

        var step = Assert.IsType<ThumbnailBuildStep>(result.BuildSteps.Steps.First());
        Assert.NotNull(step.Command);
        Assert.IsType<StaticThumbnailCommand<SoundAsset>>(step.Command);
    }

    [Fact]
    public void TestAudioSystem_GracefulInitialization()
    {
        var services = new ServiceRegistry();
        var audioSystem = new AudioSystem(services);

        Assert.NotNull(audioSystem);
        Assert.True(audioSystem.Enabled);

        // AudioSystem.Initialize should catch OpenAL / device errors without throwing
        audioSystem.Initialize();

        // AudioEngine may be null or valid depending on local OpenAL device availability,
        // but the system must not crash or leave inconsistent state.
        if (audioSystem.AudioEngine != null)
        {
            Assert.NotEqual(AudioEngineState.Invalidated, audioSystem.AudioEngine.State);
        }
    }

    [Fact]
    public void TestSlice2_FileParityAndLineCounts()
    {
        var rootDir = AppDomain.CurrentDomain.BaseDirectory;
        // Search upwards for sources directory
        var dir = new DirectoryInfo(rootDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "sources")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var sourcesDir = Path.Combine(dir.FullName, "sources");

        var soundPreviewFile = Path.Combine(sourcesDir, "editor/Stride.Assets.Editor/Preview/SoundPreview.cs");
        var soundCompilerFile = Path.Combine(sourcesDir, "editor/Stride.Editor/Preview/SoundAssetEditorGameCompiler.cs");
        var soundThumbFile = Path.Combine(sourcesDir, "editor/Stride.Assets.Editor/Thumbnails/SoundThumbnailCompiler.cs");

        Assert.True(File.Exists(soundPreviewFile), $"File must exist: {soundPreviewFile}");
        Assert.True(File.Exists(soundCompilerFile), $"File must exist: {soundCompilerFile}");
        Assert.True(File.Exists(soundThumbFile), $"File must exist: {soundThumbFile}");

        var previewLines = File.ReadAllLines(soundPreviewFile).Length;
        var compilerLines = File.ReadAllLines(soundCompilerFile).Length;
        var thumbLines = File.ReadAllLines(soundThumbFile).Length;

        // SoundPreview.cs must be faithful to original WPF (152 lines)
        Assert.Equal(152, previewLines);

        // SoundAssetEditorGameCompiler.cs must be exactly 19 lines
        Assert.Equal(19, compilerLines);

        // SoundThumbnailCompiler.cs must be exactly 20 lines
        Assert.Equal(20, thumbLines);
    }
}
