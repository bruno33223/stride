// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Media;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestVideoEditorReactivity
{
    private static (VideoEditorViewModel vm, VideoAsset asset) CreateTestViewModel()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());

        var videoAsset = new VideoAsset
        {
            Width = 1920,
            Height = 1080,
            VideoDuration = new VideoAssetDuration
            {
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromSeconds(15)
            }
        };

        var assetItem = new AssetItem("video/sample", videoAsset);
        var videoVm = (VideoViewModel)RuntimeHelpers.GetUninitializedObject(typeof(VideoViewModel));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(videoVm, assetItem);
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(videoVm, [sp]);

        var editorVm = new VideoEditorViewModel(videoVm);
        return (editorVm, videoAsset);
    }

    [Fact]
    public void TestStepForwardUpdatesProgressAndRaisesPropertyChanged()
    {
        var (vm, _) = CreateTestViewModel();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        Assert.Equal(0, vm.Progress);

        vm.StepForwardCommand.Execute(null);

        Assert.True(vm.Progress > 0, "Progress should be greater than 0 after StepForward.");
        Assert.Contains(nameof(vm.Progress), changedProperties);
    }

    [Fact]
    public void TestStepBackwardUpdatesProgressAndRaisesPropertyChanged()
    {
        var (vm, _) = CreateTestViewModel();
        vm.Progress = 0.5;

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        vm.StepBackwardCommand.Execute(null);

        Assert.True(vm.Progress < 0.5, "Progress should decrease after StepBackward.");
        Assert.Contains(nameof(vm.Progress), changedProperties);
    }

    [Fact]
    public void TestSetInPointRaisesPropertyChangedForStartTime()
    {
        var (vm, _) = CreateTestViewModel();
        vm.Progress = 0.2;

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        vm.SetInPointCommand.Execute(null);

        Assert.Equal(vm.CurrentTime, vm.StartTime);
        Assert.Contains(nameof(vm.StartTime), changedProperties);
    }

    [Fact]
    public void TestSetOutPointRaisesPropertyChangedForEndTime()
    {
        var (vm, _) = CreateTestViewModel();
        vm.Progress = 0.8;

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        vm.SetOutPointCommand.Execute(null);

        Assert.Equal(vm.CurrentTime, vm.EndTime);
        Assert.Contains(nameof(vm.EndTime), changedProperties);
    }

    [Fact]
    public void TestResetTrimmingRaisesPropertyChangedForStartTimeAndEndTime()
    {
        var (vm, _) = CreateTestViewModel();
        vm.Progress = 0.5;
        vm.SetInPointCommand.Execute(null);
        vm.SetOutPointCommand.Execute(null);

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        vm.ResetTrimmingCommand.Execute(null);

        Assert.Equal(TimeSpan.Zero, vm.StartTime);
        Assert.Equal(TimeSpan.MaxValue, vm.EndTime);
        Assert.Contains(nameof(vm.StartTime), changedProperties);
        Assert.Contains(nameof(vm.EndTime), changedProperties);
    }
}
