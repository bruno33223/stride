// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Media;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Yaml;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAudioViewModelsAndTemplatesIntegration
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

    private static string GetEditorTemplatesMediaDir()
    {
        return Path.Combine(GetRepoRoot(), "sources", "editor", "Stride.Assets.Editor", "Templates", "Assets", "Media");
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
    public void TestSoundTemplateFilesExistAndAreValid()
    {
        var mediaDir = GetEditorTemplatesMediaDir();
        Assert.True(Directory.Exists(mediaDir), $"Templates Media directory does not exist: {mediaDir}");

        var defaultSoundPath = Path.Combine(mediaDir, "DefaultSound.sdtpl");
        var musicSoundPath = Path.Combine(mediaDir, "MusicSound.sdtpl");
        var spatializedSoundPath = Path.Combine(mediaDir, "SpatializedSound.sdtpl");

        Assert.True(File.Exists(defaultSoundPath), $"DefaultSound.sdtpl missing at {defaultSoundPath}");
        Assert.True(File.Exists(musicSoundPath), $"MusicSound.sdtpl missing at {musicSoundPath}");
        Assert.True(File.Exists(spatializedSoundPath), $"SpatializedSound.sdtpl missing at {spatializedSoundPath}");

        var defaultDesc = YamlSerializer.Load<TemplateDescription>(defaultSoundPath) as TemplateAssetFactoryDescription;
        Assert.NotNull(defaultDesc);
        Assert.Equal(new Guid("FE4CC415-19A4-4F6D-9FF1-1DB00D94BD05"), defaultDesc.Id);
        Assert.Equal("SoundAsset", defaultDesc.AssetTypeName);
        Assert.Equal("Media", defaultDesc.Group);
        Assert.Equal("Sound effect", defaultDesc.Name);
        Assert.Equal("DefaultSoundFactory", defaultDesc.FactoryTypeName);
        Assert.True(defaultDesc.ImportSource);

        var musicDesc = YamlSerializer.Load<TemplateDescription>(musicSoundPath) as TemplateAssetFactoryDescription;
        Assert.NotNull(musicDesc);
        Assert.Equal(new Guid("1EB85AB4-B652-4A32-AD7B-9B7423380872"), musicDesc.Id);
        Assert.Equal("SoundAsset", musicDesc.AssetTypeName);
        Assert.Equal("Media", musicDesc.Group);
        Assert.Equal("Music", musicDesc.Name);
        Assert.Equal("MusicSoundFactory", musicDesc.FactoryTypeName);
        Assert.True(musicDesc.ImportSource);

        var spatialDesc = YamlSerializer.Load<TemplateDescription>(spatializedSoundPath) as TemplateAssetFactoryDescription;
        Assert.NotNull(spatialDesc);
        Assert.Equal(new Guid("62124263-A299-42CF-A724-F962AE8773E8"), spatialDesc.Id);
        Assert.Equal("SoundAsset", spatialDesc.AssetTypeName);
        Assert.Equal("Media", spatialDesc.Group);
        Assert.Equal("Spatialized sound", spatialDesc.Name);
        Assert.Equal("SpatializedSoundFactory", spatialDesc.FactoryTypeName);
        Assert.True(spatialDesc.ImportSource);
    }

    [Fact]
    public void TestSoundFromFileTemplateGenerator()
    {
        var asm = typeof(AssetTemplateGenerator).Assembly;
        var generatorType = asm.GetType("Stride.Assets.Editor.Templates.SoundFromFileTemplateGenerator");
        Assert.True(generatorType != null, "Stride.Assets.Editor.Templates.SoundFromFileTemplateGenerator must exist");

        var defaultField = generatorType.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultField);
        var generator = defaultField.GetValue(null) as ITemplateGenerator;
        Assert.NotNull(generator);

        var defaultIdField = generatorType.GetField("DefaultSoundId", BindingFlags.Public | BindingFlags.Static);
        var musicIdField = generatorType.GetField("MusicSoundId", BindingFlags.Public | BindingFlags.Static);
        var spatialIdField = generatorType.GetField("SpatializedSoundId", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(defaultIdField);
        Assert.NotNull(musicIdField);
        Assert.NotNull(spatialIdField);

        var defaultId = (Guid)defaultIdField.GetValue(null)!;
        var musicId = (Guid)musicIdField.GetValue(null)!;
        var spatialId = (Guid)spatialIdField.GetValue(null)!;

        Assert.Equal(new Guid("FE4CC415-19A4-4F6D-9FF1-1DB00D94BD05"), defaultId);
        Assert.Equal(new Guid("1EB85AB4-B652-4A32-AD7B-9B7423380872"), musicId);
        Assert.Equal(new Guid("62124263-A299-42CF-A724-F962AE8773E8"), spatialId);

        var soundTemplate = new TemplateAssetFactoryDescription
        {
            Id = defaultId,
            AssetTypeName = "SoundAsset",
            ImportSource = true
        };
        var musicTemplate = new TemplateAssetFactoryDescription
        {
            Id = musicId,
            AssetTypeName = "SoundAsset",
            ImportSource = true
        };
        var spatialTemplate = new TemplateAssetFactoryDescription
        {
            Id = spatialId,
            AssetTypeName = "SoundAsset",
            ImportSource = true
        };
        var unrelatedTemplate = new TemplateAssetFactoryDescription
        {
            Id = Guid.NewGuid(),
            AssetTypeName = "SoundAsset",
            ImportSource = true
        };

        var isSupportingMethod = generatorType.GetMethod("IsSupportingTemplate", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isSupportingMethod);

        Assert.True((bool)isSupportingMethod.Invoke(generator, [soundTemplate])!);
        Assert.True((bool)isSupportingMethod.Invoke(generator, [musicTemplate])!);
        Assert.True((bool)isSupportingMethod.Invoke(generator, [spatialTemplate])!);
        Assert.False((bool)isSupportingMethod.Invoke(generator, [unrelatedTemplate])!);

        // Verify TemplateManager discovery
        TemplateManager.Register(generator);
        try
        {
            var found = TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(soundTemplate);
            Assert.Same(generator, found);
        }
        finally
        {
            TemplateManager.Unregister(generator);
        }
    }

    [Fact]
    public void TestSoundPreviewViewModelTransportBindingsAndReactivity()
    {
        var (session, _) = CreateTestSession();
        var vm = new SoundPreviewViewModel(session);

        // Verify initial transport defaults
        Assert.Equal(1.0, vm.MasterVolume);
        Assert.Equal(TimeSpan.Zero, vm.CurrentTime);
        Assert.Equal(0.0, vm.CurrentValue);
        Assert.Equal(TimeSpan.Zero, vm.Duration);
        Assert.False(vm.IsAudioValid);
        Assert.NotNull(vm.PlayCommand);
        Assert.NotNull(vm.PauseCommand);

        // Track reactivity
        var propertyChanges = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                propertyChanges.Add(e.PropertyName);
        };

        // MasterVolume binding reactivity
        vm.MasterVolume = 0.65;
        Assert.Equal(0.65, vm.MasterVolume);
        Assert.Contains(nameof(vm.MasterVolume), propertyChanges);

        // CurrentValue binding reactivity (updates CurrentTime and fires both)
        propertyChanges.Clear();
        vm.CurrentValue = 15.5;
        Assert.Equal(15.5, vm.CurrentValue);
        Assert.Equal(TimeSpan.FromSeconds(15.5), vm.CurrentTime);
        Assert.Contains(nameof(vm.CurrentTime), propertyChanges);
        Assert.Contains(nameof(vm.CurrentValue), propertyChanges);

        // IsAudioValid binding reactivity
        propertyChanges.Clear();
        vm.IsAudioValid = true;
        Assert.True(vm.IsAudioValid);
        Assert.Contains(nameof(vm.IsAudioValid), propertyChanges);

        // PlayCommand and PauseCommand state toggling
        vm.PlayCommand.Execute(null);
        Assert.False(vm.PlayCommand.IsEnabled);
        Assert.True(vm.PauseCommand.IsEnabled);

        vm.PauseCommand.Execute(null);
        Assert.True(vm.PlayCommand.IsEnabled);
        Assert.False(vm.PauseCommand.IsEnabled);
    }

    [Fact]
    public void TestSoundPreviewAttachAndAudioUpdate()
    {
        var (session, _) = CreateTestSession();
        var vm = new SoundPreviewViewModel(session);

        var preview = (SoundPreview)RuntimeHelpers.GetUninitializedObject(typeof(SoundPreview));

        vm.AttachPreview(preview);

        // Preview should have received dispatcher and callback should be registered
        Assert.NotNull(preview.UpdateViewModelTime);

        // Simulate audio playback update from game/preview
        preview.UpdateViewModelTime!(true, true, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(180));

        Assert.True(vm.IsAudioValid);
        Assert.Equal(TimeSpan.FromSeconds(30), vm.CurrentTime);
        Assert.Equal(30.0, vm.CurrentValue);
        Assert.Equal(TimeSpan.FromSeconds(180), vm.Duration);
        Assert.False(vm.PlayCommand.IsEnabled);
        Assert.True(vm.PauseCommand.IsEnabled);
    }
}
