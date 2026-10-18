// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Media;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<SoundViewModel>]
public sealed class SoundEditorViewModel : AssetEditorViewModel<SoundViewModel>
{
    private bool isPlaying;
    private bool isPaused;
    private bool isLooping;
    private bool isMuted;
    private float volume = 1.0f;
    private double progress;
    private TimeSpan currentTime = TimeSpan.Zero;
    private TimeSpan totalDuration = TimeSpan.FromSeconds(3.5);

    public SoundEditorViewModel(SoundViewModel asset)
        : base(asset)
    {
        PlayCommand = new AnonymousCommand(ServiceProvider, Play);
        PauseCommand = new AnonymousCommand(ServiceProvider, Pause);
        StopCommand = new AnonymousCommand(ServiceProvider, Stop);
        ToggleLoopCommand = new AnonymousCommand(ServiceProvider, () => IsLooping = !IsLooping);
        ToggleMuteCommand = new AnonymousCommand(ServiceProvider, () => IsMuted = !IsMuted);
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
    public ICommandBase ToggleLoopCommand { get; }
    public ICommandBase ToggleMuteCommand { get; }

    public int SampleRate => Asset.Asset.SampleRate;
    public string Channels => Asset.Asset.Spatialized ? "Spatialized 3D" : "Stereo (2 Channels)";
    public int CompressionRatio => Asset.Asset.CompressionRatio;
    public bool StreamFromDisk => Asset.Asset.StreamFromDisk;
    public bool Spatialized => Asset.Asset.Spatialized;
    public string SourceFile => Asset.Asset.Source?.FullPath ?? "(Embedded)";

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
}
