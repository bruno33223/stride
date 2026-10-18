// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Assets.Editor.Avalonia.Views.Preview;

/// <summary>
/// Native Avalonia 12 preview view for sound assets, providing playback transport controls,
/// scrubbing playhead slider, volume control, time readout, and building status message.
/// </summary>
[AssetPreviewView<SoundPreview>]
public class SoundPreviewView : StridePreviewView
{
    public const string PlayIconData = "M8,5.14V19.14L19,12.14L8,5.14Z";
    public const string PauseIconData = "M14,19H18V5H14M6,19H10V5H6V19Z";

    private SoundPreviewViewModel? currentViewModel;
    private bool updatingFromViewModel;

    public DockPanel RootPanel { get; }
    public StackPanel TopControlsPanel { get; }
    public Button PlayButton { get; }
    public Button PauseButton { get; }
    public Slider VolumeSlider { get; }
    public Slider PlayheadSlider { get; }
    public TextBlock TimeTextBlock { get; }
    public TextBlock BuildingTextBlock { get; }

    public SoundPreviewView()
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
        ToolTip.SetTip(PlayButton, "Play the sound");

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
        ToolTip.SetTip(PauseButton, "Pause the sound");

        // 3. Volume Slider
        VolumeSlider = new Slider
        {
            Minimum = 0.0,
            Maximum = 1.0,
            Value = 1.0,
            Width = 60,
            Height = 16,
            Margin = new Thickness(5),
            VerticalAlignment = VerticalAlignment.Center
        };
        VolumeSlider.ValueChanged += OnVolumeSliderValueChanged;

        // 4. Top Controls Panel
        TopControlsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(4),
            Spacing = 4,
            IsVisible = false
        };
        DockPanel.SetDock(TopControlsPanel, Dock.Top);
        TopControlsPanel.Children.Add(PlayButton);
        TopControlsPanel.Children.Add(PauseButton);
        TopControlsPanel.Children.Add(VolumeSlider);

        // 5. Playhead Slider with scrubbing
        PlayheadSlider = new Slider
        {
            Minimum = 0.0,
            Maximum = 0.0,
            Value = 0.0,
            Height = 20,
            Margin = new Thickness(15, 5),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };
        PlayheadSlider.ValueChanged += OnPlayheadSliderValueChanged;

        // 6. Time TextBlock (mm:ss:fff)
        TimeTextBlock = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Consolas, monospace"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
            IsVisible = false
        };

        // 7. Building Status Message TextBlock
        BuildingTextBlock = new TextBlock
        {
            Text = "Building preview sound asset...",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#888888")),
            IsVisible = true
        };

        // 8. Center Container Panel
        var centerStackPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };
        centerStackPanel.Children.Add(PlayheadSlider);
        centerStackPanel.Children.Add(TimeTextBlock);
        centerStackPanel.Children.Add(BuildingTextBlock);

        var centerBorder = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            Child = centerStackPanel
        };

        // 9. Root Panel
        RootPanel = new DockPanel
        {
            Background = new SolidColorBrush(Color.Parse("#1A1A1E")),
            LastChildFill = true
        };
        RootPanel.Children.Add(TopControlsPanel);
        RootPanel.Children.Add(centerBorder);

        VisualChildren.Add(RootPanel);
        LogicalChildren.Add(RootPanel);
        Template = new FuncControlTemplate<SoundPreviewView>((_, _) => RootPanel);
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

        if (newValue is SoundPreviewViewModel vm)
        {
            currentViewModel = vm;
            currentViewModel.PropertyChanged += OnViewModelPropertyChanged;

            PlayButton.Command = vm.PlayCommand;
            PauseButton.Command = vm.PauseCommand;

            SyncFromViewModel();
        }
        else
        {
            PlayButton.Command = null;
            PauseButton.Command = null;
            UpdateVisibility(false);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (currentViewModel == null || updatingFromViewModel)
            return;

        switch (e.PropertyName)
        {
            case nameof(SoundPreviewViewModel.IsAudioValid):
            case nameof(SoundPreviewViewModel.Duration):
            case nameof(SoundPreviewViewModel.CurrentTime):
            case nameof(SoundPreviewViewModel.CurrentValue):
            case nameof(SoundPreviewViewModel.MasterVolume):
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
            var isValid = currentViewModel.IsAudioValid;
            UpdateVisibility(isValid);

            VolumeSlider.Value = currentViewModel.MasterVolume;

            if (isValid)
            {
                PlayheadSlider.Maximum = Math.Max(0.0, currentViewModel.Duration.TotalSeconds);
                PlayheadSlider.Value = currentViewModel.CurrentValue;
                TimeTextBlock.Text = $"{currentViewModel.CurrentTime:mm\\:ss\\:fff} / {currentViewModel.Duration:mm\\:ss\\:fff}";
            }
        }
        finally
        {
            updatingFromViewModel = false;
        }
    }

    private void UpdateVisibility(bool isAudioValid)
    {
        TopControlsPanel.IsVisible = isAudioValid;
        PlayheadSlider.IsVisible = isAudioValid;
        TimeTextBlock.IsVisible = isAudioValid;
        BuildingTextBlock.IsVisible = !isAudioValid;
    }

    private void OnVolumeSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!updatingFromViewModel && currentViewModel != null)
        {
            currentViewModel.MasterVolume = VolumeSlider.Value;
        }
    }

    private void OnPlayheadSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!updatingFromViewModel && currentViewModel != null && currentViewModel.IsAudioValid)
        {
            currentViewModel.CurrentValue = PlayheadSlider.Value;
            TimeTextBlock.Text = $"{currentViewModel.CurrentTime:mm\\:ss\\:fff} / {currentViewModel.Duration:mm\\:ss\\:fff}";
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
