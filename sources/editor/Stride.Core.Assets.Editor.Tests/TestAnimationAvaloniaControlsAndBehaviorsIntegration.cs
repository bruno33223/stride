// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Xaml.Interactivity;
using Xunit;
using Stride.Assets.Models;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAnimationAvaloniaControlsAndBehaviorsIntegration
{
    private static DataTemplate DummyTemplate => new();

    private sealed class MockAnimatedPreviewViewModel : IAnimatedPreviewViewModel
    {
        public bool IsPlaying { get; set; }
        public int PlayCommandExecuteCount { get; private set; }
        public int PauseCommandExecuteCount { get; private set; }

        public ICommandBase PlayCommand { get; }
        public ICommandBase PauseCommand { get; }

        public MockAnimatedPreviewViewModel(bool startPlaying = false)
        {
            var serviceProvider = new ViewModelServiceProvider();
            IsPlaying = startPlaying;
            PlayCommand = new AnonymousCommand(serviceProvider, () =>
            {
                PlayCommandExecuteCount++;
                IsPlaying = true;
            });
            PauseCommand = new AnonymousCommand(serviceProvider, () =>
            {
                PauseCommandExecuteCount++;
                IsPlaying = false;
            });
        }
    }

    private class TestableNodeViewModel : NodeViewModel
    {
        private object? customValue;

        private TestableNodeViewModel() : base(null!, null, "", typeof(object), null!)
        {
        }

        public static TestableNodeViewModel Create(string name, Type type, object? value = null, NodeViewModel? parent = null)
        {
            var node = (TestableNodeViewModel)RuntimeHelpers.GetUninitializedObject(typeof(TestableNodeViewModel));
            typeof(NodeViewModel).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, name);
            typeof(NodeViewModel).GetField("<Type>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, type);
            typeof(NodeViewModel).GetProperty(nameof(Parent))?.SetValue(node, parent);
            typeof(NodeViewModel).GetField("associatedData", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, new Dictionary<string, object>());
            typeof(NodeViewModel).GetField("nodePresenters", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(node, new List<INodePresenter>());
            node.customValue = value;
            return node;
        }

        protected override object? GetNodeValue() => customValue;
    }

    [Fact]
    public void TestSuspendAnimationDuringSliderDragBehaviorLifecycleAndPauseResume()
    {
        var behaviorType = typeof(AnimationPreviewView).Assembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.SuspendAnimationDuringSliderDragBehavior");
        Assert.True(behaviorType != null, "Stride.Assets.Editor.Avalonia.Behaviors.SuspendAnimationDuringSliderDragBehavior must exist.");

        var behaviorObj = Activator.CreateInstance(behaviorType!);
        Assert.NotNull(behaviorObj);
        Assert.IsAssignableFrom<Behavior<Slider>>(behaviorObj);

        var behavior = (Behavior<Slider>)behaviorObj!;
        var slider = new Slider { Minimum = 0, Maximum = 10, Value = 0 };

        var vm = new MockAnimatedPreviewViewModel(startPlaying: true);
        var vmProp = behaviorType!.GetProperty("AnimatedPreviewViewModel");
        Assert.NotNull(vmProp);
        vmProp.SetValue(behavior, vm);

        // Attach behavior to slider
        behavior.Attach(slider);
        Assert.Equal(slider, behavior.AssociatedObject);

        // Verify DragStarted pauses playback
        var dragStartedMethod = behaviorType.GetMethod("OnDragStarted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(dragStartedMethod);
        dragStartedMethod.Invoke(behavior, null);

        Assert.Equal(1, vm.PauseCommandExecuteCount);
        Assert.False(vm.IsPlaying);

        // Verify DragCompleted resumes playback
        var dragCompletedMethod = behaviorType.GetMethod("OnDragCompleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(dragCompletedMethod);
        dragCompletedMethod.Invoke(behavior, null);

        Assert.Equal(1, vm.PlayCommandExecuteCount);
        Assert.True(vm.IsPlaying);

        // Detach behavior
        behavior.Detach();
        Assert.Null(behavior.AssociatedObject);
    }

    [Fact]
    public void TestAnimationFrameBoxTemplateProviderMatchNode()
    {
        var providerType = typeof(AnimationPreviewView).Assembly.GetType("Stride.Assets.Editor.Avalonia.TemplateProviders.AnimationFrameBoxTemplateProvider");
        Assert.True(providerType != null, "Stride.Assets.Editor.Avalonia.TemplateProviders.AnimationFrameBoxTemplateProvider must exist.");

        var providerObj = Activator.CreateInstance(providerType!);
        Assert.NotNull(providerObj);
        Assert.IsAssignableFrom<NodeViewModelTemplateProvider>(providerObj);

        var provider = (NodeViewModelTemplateProvider)providerObj!;
        provider.Template = DummyTemplate;

        Assert.Equal("AnimationFrameBoxTemplateProvider", provider.Name);

        var startNode = TestableNodeViewModel.Create(nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox), typeof(TimeSpan));
        var endNode = TestableNodeViewModel.Create(nameof(AnimationAssetDurationUnchecked.EndAnimationTimeBox), typeof(TimeSpan));
        var otherNode = TestableNodeViewModel.Create("StartAnimationTime", typeof(TimeSpan));
        var randomNode = TestableNodeViewModel.Create("SomeOtherProperty", typeof(int));

        Assert.True(provider.MatchNode(startNode));
        Assert.True(provider.MatchNode(endNode));
        Assert.False(provider.MatchNode(otherNode));
        Assert.False(provider.MatchNode(randomNode));
    }

    [Fact]
    public void TestAnimationFrameTemplateProviderMatchNode()
    {
        var providerType = typeof(AnimationPreviewView).Assembly.GetType("Stride.Assets.Editor.Avalonia.TemplateProviders.AnimationFrameTemplateProvider");
        Assert.True(providerType != null, "Stride.Assets.Editor.Avalonia.TemplateProviders.AnimationFrameTemplateProvider must exist.");

        var providerObj = Activator.CreateInstance(providerType!);
        Assert.NotNull(providerObj);
        Assert.IsAssignableFrom<NodeViewModelTemplateProvider>(providerObj);

        var provider = (NodeViewModelTemplateProvider)providerObj!;
        provider.Template = DummyTemplate;

        Assert.Equal("AnimationFrameTemplateProvider", provider.Name);

        var startNode = TestableNodeViewModel.Create(nameof(AnimationAssetDuration.StartAnimationTime), typeof(TimeSpan));
        var endNode = TestableNodeViewModel.Create(nameof(AnimationAssetDuration.EndAnimationTime), typeof(TimeSpan));
        var boxNode = TestableNodeViewModel.Create("StartAnimationTimeBox", typeof(TimeSpan));
        var randomNode = TestableNodeViewModel.Create("SomeOtherProperty", typeof(int));

        Assert.True(provider.MatchNode(startNode));
        Assert.True(provider.MatchNode(endNode));
        Assert.False(provider.MatchNode(boxNode));
        Assert.False(provider.MatchNode(randomNode));
    }

    [Fact]
    public void TestAnimationPreviewViewControlsAndAnnotation()
    {
        var viewType = typeof(AnimationPreviewView);
        var previewAttr = viewType.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(previewAttr);
        Assert.Equal(typeof(AnimationPreview), previewAttr.AssetPreviewType);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(viewType));

        var view = new AnimationPreviewView();

        var playBtnProp = viewType.GetProperty("PlayButton");
        Assert.NotNull(playBtnProp);
        var playBtn = (Button)playBtnProp.GetValue(view)!;
        Assert.NotNull(playBtn);
        Assert.NotNull(ToolTip.GetTip(playBtn));
        Assert.Contains("Play", ToolTip.GetTip(playBtn)?.ToString());
        Assert.NotNull(playBtn.Content);

        var pauseBtnProp = viewType.GetProperty("PauseButton");
        Assert.NotNull(pauseBtnProp);
        var pauseBtn = (Button)pauseBtnProp.GetValue(view)!;
        Assert.NotNull(pauseBtn);
        Assert.NotNull(ToolTip.GetTip(pauseBtn));
        Assert.Contains("Pause", ToolTip.GetTip(pauseBtn)?.ToString());
        Assert.NotNull(pauseBtn.Content);

        var timeTextBlockProp = viewType.GetProperty("TimeTextBlock");
        Assert.NotNull(timeTextBlockProp);
        var timeTextBlock = (TextBlock)timeTextBlockProp.GetValue(view)!;
        Assert.NotNull(timeTextBlock);

        var timelineSliderProp = viewType.GetProperty("TimelineSlider");
        Assert.NotNull(timelineSliderProp);
        var timelineSlider = (Slider)timelineSliderProp.GetValue(view)!;
        Assert.NotNull(timelineSlider);

        var timeScaleInputProp = viewType.GetProperty("TimeScaleUpDown");
        Assert.NotNull(timeScaleInputProp);
        var timeScaleInput = (NumericUpDown)timeScaleInputProp.GetValue(view)!;
        Assert.NotNull(timeScaleInput);

        var buildingTextBlockProp = viewType.GetProperty("BuildingTextBlock");
        Assert.NotNull(buildingTextBlockProp);
        var buildingTextBlock = (TextBlock)buildingTextBlockProp.GetValue(view)!;
        Assert.NotNull(buildingTextBlock);

        var rootPanelProp = viewType.GetProperty("RootPanel");
        Assert.NotNull(rootPanelProp);
        var rootPanel = (DockPanel)rootPanelProp.GetValue(view)!;
        Assert.NotNull(rootPanel);
    }

    [Fact]
    public void TestParityAndLineCount()
    {
        var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../sources/editor/Stride.Assets.Editor.Avalonia"));

        // 1. SuspendAnimationDuringSliderDragBehavior.cs
        var behaviorFile = Path.Combine(baseDir, "Behaviors/SuspendAnimationDuringSliderDragBehavior.cs");
        Assert.True(File.Exists(behaviorFile), $"SuspendAnimationDuringSliderDragBehavior.cs file must exist at {behaviorFile}");
        var behaviorLines = File.ReadAllLines(behaviorFile).Length;
        Assert.True(behaviorLines >= 60 && behaviorLines <= 85, $"SuspendAnimationDuringSliderDragBehavior.cs should have ~72 lines (60-85), but has {behaviorLines}.");

        // 2. AnimationFrameBoxTemplateProvider.cs
        var boxFile = Path.Combine(baseDir, "TemplateProviders/AnimationFrameBoxTemplateProvider.cs");
        Assert.True(File.Exists(boxFile), $"AnimationFrameBoxTemplateProvider.cs file must exist at {boxFile}");
        var boxLines = File.ReadAllLines(boxFile).Length;
        Assert.True(boxLines >= 15 && boxLines <= 25, $"AnimationFrameBoxTemplateProvider.cs should have ~20 lines (15-25), but has {boxLines}.");

        // 3. AnimationFrameTemplateProvider.cs
        var frameFile = Path.Combine(baseDir, "TemplateProviders/AnimationFrameTemplateProvider.cs");
        Assert.True(File.Exists(frameFile), $"AnimationFrameTemplateProvider.cs file must exist at {frameFile}");
        var frameLines = File.ReadAllLines(frameFile).Length;
        Assert.True(frameLines >= 15 && frameLines <= 25, $"AnimationFrameTemplateProvider.cs should have ~20 lines (15-25), but has {frameLines}.");

        // 4. AnimationPreviewView.cs
        var viewFile = Path.Combine(baseDir, "Views/Preview/AnimationPreviewView.cs");
        Assert.True(File.Exists(viewFile), $"AnimationPreviewView.cs file must exist at {viewFile}");
        var viewLines = File.ReadAllLines(viewFile).Length;
        Assert.True(viewLines >= 100, $"AnimationPreviewView.cs should have at least 100 lines for complete interactive UI, but has {viewLines}.");
    }
}
