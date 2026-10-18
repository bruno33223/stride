// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Media;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<VideoViewModel>]
public sealed class VideoEditorViewModel : AssetEditorViewModel<VideoViewModel>
{
    private bool isPlaying;
    private bool isPaused;
    private bool isLooping;
    private bool isMuted;
    private float volume = 1.0f;
    private double progress;
    private TimeSpan currentTime = TimeSpan.Zero;
    private TimeSpan totalDuration = TimeSpan.FromSeconds(15.0);

    public VideoEditorViewModel(VideoViewModel asset)
        : base(asset)
    {
        PlayCommand = new AnonymousCommand(ServiceProvider, Play);
        PauseCommand = new AnonymousCommand(ServiceProvider, Pause);
        StopCommand = new AnonymousCommand(ServiceProvider, Stop);
        StepForwardCommand = new AnonymousCommand(ServiceProvider, StepForward);
        StepBackwardCommand = new AnonymousCommand(ServiceProvider, StepBackward);
        ToggleLoopCommand = new AnonymousCommand(ServiceProvider, () => IsLooping = !IsLooping);
        ToggleMuteCommand = new AnonymousCommand(ServiceProvider, () => IsMuted = !IsMuted);
        SetInPointCommand = new AnonymousCommand(ServiceProvider, SetInPoint);
        SetOutPointCommand = new AnonymousCommand(ServiceProvider, SetOutPoint);
        ResetTrimmingCommand = new AnonymousCommand(ServiceProvider, ResetTrimming);
    }

    public bool IsPlaying
    {
        get => isPlaying;
        private set => SetValue(ref isPlaying, value);
    }

    public bool IsPaused
    {
        get => isPaused;
        private set => SetValue(ref isPaused, value);
    }

    public bool IsLooping
    {
        get => isLooping;
        set => SetValue(ref isLooping, value);
    }

    public bool IsMuted
    {
        get => isMuted;
        set => SetValue(ref isMuted, value);
    }

    public float Volume
    {
        get => volume;
        set => SetValue(ref volume, Math.Clamp(value, 0f, 1f));
    }

    public double Progress
    {
        get => progress;
        set
        {
            if (SetValue(ref progress, Math.Clamp(value, 0.0, 1.0)))
            {
                CurrentTime = TimeSpan.FromSeconds(progress * TotalDuration.TotalSeconds);
            }
        }
    }

    public TimeSpan CurrentTime
    {
        get => currentTime;
        set => SetValue(ref currentTime, value);
    }

    public TimeSpan TotalDuration
    {
        get => totalDuration;
        set => SetValue(ref totalDuration, value);
    }

    public ICommandBase PlayCommand { get; }
    public ICommandBase PauseCommand { get; }
    public ICommandBase StopCommand { get; }
    public ICommandBase StepForwardCommand { get; }
    public ICommandBase StepBackwardCommand { get; }
    public ICommandBase ToggleLoopCommand { get; }
    public ICommandBase ToggleMuteCommand { get; }
    public ICommandBase SetInPointCommand { get; }
    public ICommandBase SetOutPointCommand { get; }
    public ICommandBase ResetTrimmingCommand { get; }

    public float Width => Asset.Asset.Width;
    public float Height => Asset.Asset.Height;
    public bool IsSizeInPercentage => Asset.Asset.IsSizeInPercentage;
    public TimeSpan StartTime => Asset.Asset.VideoDuration.StartTime;
    public TimeSpan EndTime => Asset.Asset.VideoDuration.EndTime;
    public bool IsAudioChannelMono => Asset.Asset.IsAudioChannelMono;
    public string SourceFile => Asset.Asset.Source?.FullPath ?? "(No source)";

    public string ResolutionString => IsSizeInPercentage ? $"{Width:0}% × {Height:0}%" : $"{Width:0} × {Height:0} px";

    private void Play()
    {
        IsPlaying = true;
        IsPaused = false;
    }

    private void Pause()
    {
        if (IsPlaying)
        {
            IsPlaying = false;
            IsPaused = true;
        }
    }

    private void Stop()
    {
        IsPlaying = false;
        IsPaused = false;
        Progress = 0;
        CurrentTime = TimeSpan.Zero;
    }

    private void StepForward()
    {
        var frame = TimeSpan.FromSeconds(1.0 / 30.0);
        CurrentTime = CurrentTime + frame > TotalDuration ? TotalDuration : CurrentTime + frame;
        if (TotalDuration.TotalSeconds > 0)
            Progress = CurrentTime.TotalSeconds / TotalDuration.TotalSeconds;
    }

    private void StepBackward()
    {
        var frame = TimeSpan.FromSeconds(1.0 / 30.0);
        CurrentTime = CurrentTime - frame < TimeSpan.Zero ? TimeSpan.Zero : CurrentTime - frame;
        if (TotalDuration.TotalSeconds > 0)
            Progress = CurrentTime.TotalSeconds / TotalDuration.TotalSeconds;
    }

    private void SetInPoint()
    {
        Asset.Asset.VideoDuration.StartTime = CurrentTime;
        OnPropertyChanged(nameof(StartTime));
    }

    private void SetOutPoint()
    {
        Asset.Asset.VideoDuration.EndTime = CurrentTime;
        OnPropertyChanged(nameof(EndTime));
    }

    private void ResetTrimming()
    {
        Asset.Asset.VideoDuration.StartTime = TimeSpan.Zero;
        Asset.Asset.VideoDuration.EndTime = TimeSpan.MaxValue;
        OnPropertyChanged(nameof(StartTime));
        OnPropertyChanged(nameof(EndTime));
    }
}
