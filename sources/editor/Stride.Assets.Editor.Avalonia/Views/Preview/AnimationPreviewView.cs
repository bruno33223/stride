// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.Avalonia.Behaviors;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// Native Avalonia 12 preview view for animation assets, providing playback transport controls,
/// play/pause buttons, playhead slider with scrub suspend behavior, time readout, time scale control,
/// and preview status messaging.
/// </summary>
[AssetPreviewView<AnimationPreview>]
public class AnimationPreviewView : StridePreviewView
{
    public const string PlayIconData = "M8,5.14V19.14L19,12.14L8,5.14Z";
    public const string PauseIconData = "M14,19H18V5H14M6,19H10V5H6V19Z";

    private AnimationPreviewViewModel? currentViewModel;
    private bool updatingFromViewModel;

    public DockPanel RootPanel { get; }
    public Border TopBar { get; }
    public StackPanel TopControlsPanel { get; }
    public Button PlayButton { get; }
    public Button PauseButton { get; }
    public TextBlock TimeTextBlock { get; }
    public TextBlock TimeScaleLabel { get; }
    public NumericUpDown TimeScaleUpDown { get; }
    public Border BottomBar { get; }
    public StackPanel BottomControlsPanel { get; }
    public Slider TimelineSlider { get; }
    public SuspendAnimationDuringSliderDragBehavior SliderBehavior { get; }
    public ContentPresenter PartStrideView { get; }
    public TextBlock BuildingTextBlock { get; }

    public AnimationPreviewView()
    {
        // 1. Play Button with icon & tooltip
        PlayButton = new Button
        {
            Background = Brushes.Transparent,
            Width = 28,
            Height = 28,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = new PathIcon
            {
                Data = TryParseGeometry(PlayIconData),
                Width = 16,
                Height = 16,
                Foreground = new SolidColorBrush(Color.Parse("#30D158"))
            }
        };
        ToolTip.SetTip(PlayButton, "Play the animation");

        // 2. Pause Button with icon & tooltip
        PauseButton = new Button
        {
            Background = Brushes.Transparent,
            Width = 28,
            Height = 28,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = new PathIcon
            {
                Data = TryParseGeometry(PauseIconData),
                Width = 16,
                Height = 16,
                Foreground = new SolidColorBrush(Color.Parse("#FF9F0A"))
            }
        };
        ToolTip.SetTip(PauseButton, "Pause the animation");

        // 3. Time TextBlock (0.000 / 0.000)
        TimeTextBlock = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0),
            FontFamily = new FontFamily("Consolas, monospace"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
            Text = "0.000 / 0.000"
        };

        // 4. Time Scale Label and NumericUpDown
        TimeScaleLabel = new TextBlock
        {
            Text = "Time scale:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(6, 0, 4, 0),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#AAAAAA"))
        };

        TimeScaleUpDown = new NumericUpDown
        {
            Minimum = 0.0m,
            Maximum = 100.0m,
            Increment = 0.1m,
            FormatString = "0.000",
            Value = 1.0m,
            Width = 80,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center
        };
        TimeScaleUpDown.ValueChanged += OnTimeScaleValueChanged;

        // 5. Top Controls StackPanel & Border
        TopControlsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(4),
            Spacing = 4,
            IsVisible = true
        };
        TopControlsPanel.Children.Add(PlayButton);
        TopControlsPanel.Children.Add(PauseButton);
        TopControlsPanel.Children.Add(TimeTextBlock);
        TopControlsPanel.Children.Add(TimeScaleLabel);
        TopControlsPanel.Children.Add(TimeScaleUpDown);

        TopBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252528")),
            Child = TopControlsPanel
        };
        DockPanel.SetDock(TopBar, Dock.Top);

        // 6. Timeline Slider and Suspend Behavior
        TimelineSlider = new Slider
        {
            Minimum = 0.0,
            Maximum = 0.0,
            Value = 0.0,
            SmallChange = 0.0333,
            LargeChange = 0.1,
            Height = 22,
            Margin = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        TimelineSlider.ValueChanged += OnTimelineSliderValueChanged;

        SliderBehavior = new SuspendAnimationDuringSliderDragBehavior();
        Interaction.GetBehaviors(TimelineSlider).Add(SliderBehavior);

        // 7. Bottom Controls Panel & Border
        BottomControlsPanel = new StackPanel
        {
            Margin = new Thickness(6, 2),
            IsVisible = false
        };
        BottomControlsPanel.Children.Add(TimelineSlider);

        BottomBar = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252528")),
            Child = BottomControlsPanel
        };
        DockPanel.SetDock(BottomBar, Dock.Bottom);

        // 8. Building TextBlock & Center Viewport Presenter
        BuildingTextBlock = new TextBlock
        {
            Text = "Building preview animation asset...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            IsVisible = true
        };

        PartStrideView = new ContentPresenter
        {
            Name = "PART_StrideView",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var centerGrid = new Grid();
        centerGrid.Children.Add(PartStrideView);
        centerGrid.Children.Add(BuildingTextBlock);

        // 9. Root Panel
        RootPanel = new DockPanel
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1E")),
            LastChildFill = true
        };
        RootPanel.Children.Add(TopBar);
        RootPanel.Children.Add(BottomBar);
        RootPanel.Children.Add(centerGrid);

        VisualChildren.Add(RootPanel);
        LogicalChildren.Add(RootPanel);
        Template = new FuncControlTemplate<AnimationPreviewView>((_, _) => RootPanel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DataContextProperty)
        {
            OnDataContextChanged(change.OldValue, change.NewValue);
        }
    }

    private void OnDataContextChanged(object? oldValue, object? newValue)
    {
        if (currentViewModel != null)
        {
            currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            currentViewModel = null;
        }

        if (newValue is AnimationPreviewViewModel vm)
        {
            currentViewModel = vm;
            currentViewModel.PropertyChanged += OnViewModelPropertyChanged;

            PlayButton.Command = vm.PlayCommand;
            PauseButton.Command = vm.PauseCommand;
            SliderBehavior.AnimatedPreviewViewModel = vm;

            SyncFromViewModel();
        }
        else
        {
            PlayButton.Command = null;
            PauseButton.Command = null;
            SliderBehavior.AnimatedPreviewViewModel = null;
            UpdateVisibility(false);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (currentViewModel == null || updatingFromViewModel)
            return;

        switch (e.PropertyName)
        {
            case nameof(AnimationPreviewViewModel.IsValid):
            case nameof(AnimationPreviewViewModel.CurrentTime):
            case nameof(AnimationPreviewViewModel.Duration):
            case nameof(AnimationPreviewViewModel.TimeScale):
                SyncFromViewModel();
                break;
        }
    }

    private void SyncFromViewModel()
    {
        if (currentViewModel == null)
            return;

        updatingFromViewModel = true;
        try
        {
            var isValid = currentViewModel.IsValid;
            UpdateVisibility(isValid);

            TimeScaleUpDown.Value = (decimal)currentViewModel.TimeScale;

            if (isValid)
            {
                TimelineSlider.Maximum = Math.Max(0.0, currentViewModel.Duration);
                TimelineSlider.Value = currentViewModel.CurrentTime;
                TimeTextBlock.Text = $"{currentViewModel.CurrentTime:0.000} / {currentViewModel.Duration:0.000}";
            }
            else
            {
                TimeTextBlock.Text = "0.000 / 0.000";
            }
        }
        finally
        {
            updatingFromViewModel = false;
        }
    }

    private void UpdateVisibility(bool isValid)
    {
        BottomControlsPanel.IsVisible = isValid;
        BuildingTextBlock.IsVisible = !isValid;
    }

    private void OnTimelineSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!updatingFromViewModel && currentViewModel != null && currentViewModel.IsValid)
        {
            currentViewModel.CurrentTime = (float)TimelineSlider.Value;
            TimeTextBlock.Text = $"{currentViewModel.CurrentTime:0.000} / {currentViewModel.Duration:0.000}";
        }
    }

    private void OnTimeScaleValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (!updatingFromViewModel && currentViewModel != null && TimeScaleUpDown.Value.HasValue)
        {
            currentViewModel.TimeScale = (float)TimeScaleUpDown.Value.Value;
        }
    }

    private static Geometry? TryParseGeometry(string data)
    {
        try
        {
            return StreamGeometry.Parse(data);
        }
        catch
        {
            return null;
        }
    }
}
