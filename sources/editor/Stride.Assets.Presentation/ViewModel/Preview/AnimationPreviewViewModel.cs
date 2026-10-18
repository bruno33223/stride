// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public sealed class AnimationPreviewViewModel : AssetPreviewViewModel, IAnimatedPreviewViewModel
{
    private object? animationPreview;
    private float timeScale = 1.0f;
    private float currentTime;
    private float duration;
    private bool isValid;
    private volatile bool updatingFromGame;

    public AnimationPreviewViewModel(SessionViewModel session)
        : base(session)
    {
        PlayCommand = new AnonymousCommand(ServiceProvider, Play);
        PauseCommand = new AnonymousCommand(ServiceProvider, Pause);
    }

    public float TimeScale { get { return timeScale; } set { SetValue(ref timeScale, value, () => SetTimeScale(value)); } }

    public float CurrentTime { get { return currentTime; } set { SetValue(ref currentTime, value); if (!updatingFromGame) SetCurrentTime(value); } }

    public float Duration { get { return duration; } private set { SetValue(ref duration, value); } }

    public bool IsValid { get { return isValid; } private set { SetValue(ref isValid, value); } }

    public ICommandBase PlayCommand { get; }

    public ICommandBase PauseCommand { get; }

    public bool IsPlaying => GetIsPlaying();

    public override void AttachPreview(IAssetPreview preview)
    {
        animationPreview = preview;
        SetTimeScale(timeScale);
        currentTime = 0.0f;
        SetUpdateViewModelTime();

        // Automatically play the animation
        Play();
    }

    public static AssetItem? FindModelForPreview(AssetItem assetItem)
    {
        var animationAsset = (AnimationAsset)assetItem.Asset;
        var previewModelAsset = (animationAsset?.PreviewModel != null ? assetItem.Package.FindAssetFromProxyObject(animationAsset.PreviewModel) : null);
        return (previewModelAsset?.Asset as ModelAsset)?.Skeleton != null ? previewModelAsset : null;
    }

    private void UpdateViewModelTime(bool isTimeValid, float current, float animDuration)
    {
        if (updatingFromGame)
            return;

        updatingFromGame = true;
        Dispatcher.LowPriorityInvokeAsync(() =>
        {
            IsValid = isTimeValid;
            CurrentTime = current;
            Duration = animDuration;
            updatingFromGame = false;
        });
    }

    private void Play()
    {
        animationPreview?.GetType().GetMethod("Play", Type.EmptyTypes)?.Invoke(animationPreview, null);
        PlayCommand.IsEnabled = false;
        PauseCommand.IsEnabled = true;
    }

    private void Pause()
    {
        animationPreview?.GetType().GetMethod("Pause", Type.EmptyTypes)?.Invoke(animationPreview, null);
        PlayCommand.IsEnabled = true;
        PauseCommand.IsEnabled = false;
    }

    private void SetTimeScale(float value)
    {
        animationPreview?.GetType().GetMethod("SetTimeScale", new[] { typeof(float) })?.Invoke(animationPreview, new object[] { value });
    }

    private void SetCurrentTime(float value)
    {
        animationPreview?.GetType().GetMethod("SetCurrentTime", new[] { typeof(float) })?.Invoke(animationPreview, new object[] { value });
    }

    private bool GetIsPlaying()
    {
        return (bool)(animationPreview?.GetType().GetProperty("IsPlaying")?.GetValue(animationPreview) ?? false);
    }

    private void SetUpdateViewModelTime()
    {
        if (animationPreview == null)
            return;

        var prop = animationPreview.GetType().GetProperty("UpdateViewModelTime");
        if (prop != null)
        {
            Action<bool, float, float> action = UpdateViewModelTime;
            prop.SetValue(animationPreview, action);
        }
    }
}
