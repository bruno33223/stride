// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels.Preview;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

public class SuspendAnimationDuringSliderDragBehavior : Behavior<Slider>
{
    public static readonly StyledProperty<IAnimatedPreviewViewModel?> AnimatedPreviewViewModelProperty =
        AvaloniaProperty.Register<SuspendAnimationDuringSliderDragBehavior, IAnimatedPreviewViewModel?>(nameof(AnimatedPreviewViewModel));

    public IAnimatedPreviewViewModel? AnimatedPreviewViewModel { get => GetValue(AnimatedPreviewViewModelProperty); set => SetValue(AnimatedPreviewViewModelProperty, value); }

    private bool wasPlaying;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject == null) return;
        AssociatedObject.AddHandler(Thumb.DragStartedEvent, DragStarted);
        AssociatedObject.AddHandler(Thumb.DragCompletedEvent, DragCompleted);
        AssociatedObject.AddHandler(InputElement.PointerPressedEvent, TrackPointerEvent, RoutingStrategies.Tunnel);
        AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, TrackPointerEvent, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.RemoveHandler(Thumb.DragStartedEvent, DragStarted);
            AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, DragCompleted);
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, TrackPointerEvent);
            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, TrackPointerEvent);
        }
        base.OnDetaching();
    }

    private void TrackPointerEvent(object? sender, PointerEventArgs e)
    {
        if (AnimatedPreviewViewModel == null || AssociatedObject == null) return;
        var point = e.GetCurrentPoint(AssociatedObject);
        if (point.Properties.IsLeftButtonPressed)
        {
            wasPlaying = AnimatedPreviewViewModel.IsPlaying;
            if (wasPlaying) AnimatedPreviewViewModel.PauseCommand.Execute();
        }
    }

    public void OnDragStarted() => DragStarted(this, new VectorEventArgs());

    public void OnDragCompleted() => DragCompleted(this, new VectorEventArgs());

    private void DragStarted(object? sender, VectorEventArgs e)
    {
        if (AnimatedPreviewViewModel != null)
        {
            wasPlaying = wasPlaying || AnimatedPreviewViewModel.IsPlaying;
            if (wasPlaying) AnimatedPreviewViewModel.PauseCommand.Execute();
        }
    }

    private void DragCompleted(object? sender, VectorEventArgs e)
    {
        if (AnimatedPreviewViewModel != null && wasPlaying)
        {
            AnimatedPreviewViewModel.PlayCommand.Execute();
        }
        wasPlaying = false;
    }
}
