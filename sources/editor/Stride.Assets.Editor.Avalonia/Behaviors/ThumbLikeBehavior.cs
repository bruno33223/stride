// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Behaviors
{
    public sealed class ThumbLikeBehavior : Behavior<Control>
    {
        public const double MinimumHorizontalDragDistance = 4.0;
        public const double MinimumVerticalDragDistance = 4.0;

        /// <summary>
        /// Identifies the <see cref="Direction"/> property.
        /// </summary>
        public static readonly StyledProperty<ResizingDirection> DirectionProperty =
            AvaloniaProperty.Register<ThumbLikeBehavior, ResizingDirection>(nameof(Direction));

        /// <summary>
        /// Identifies the <see cref="Target"/> property.
        /// </summary>
        public static readonly StyledProperty<IResizingTarget?> TargetProperty =
            AvaloniaProperty.Register<ThumbLikeBehavior, IResizingTarget?>(nameof(Target));

        /// <summary>
        /// Identifies the <see cref="Reference"/> property.
        /// </summary>
        public static readonly StyledProperty<Control?> ReferenceProperty =
            AvaloniaProperty.Register<ThumbLikeBehavior, Control?>(nameof(Reference));

        /// <summary>
        /// Identifies the <see cref="Modifiers"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyModifiers?> ModifiersProperty =
            AvaloniaProperty.Register<ThumbLikeBehavior, KeyModifiers?>(nameof(Modifiers));

        /// <summary>
        /// Identifies the <see cref="IsEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsEnabledProperty =
            AvaloniaProperty.Register<ThumbLikeBehavior, bool>(nameof(IsEnabled), defaultValue: true);

        private Point previousPosition;
        private Point originPoint;
        private IPointer? capturedPointer;

        /// <summary>
        /// The direction of the resizing.
        /// </summary>
        public ResizingDirection Direction { get => GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

        /// <summary>
        /// The target of the resizing.
        /// </summary>
        public IResizingTarget? Target { get => GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

        /// <summary>
        /// The reference from which deltas are calculated.
        /// When null, <see cref="Behavior{T}.AssociatedObject"/> will be used instead.
        /// </summary>
        public Control? Reference { get => GetValue(ReferenceProperty); set => SetValue(ReferenceProperty, value); }

        public KeyModifiers? Modifiers { get => GetValue(ModifiersProperty); set => SetValue(ModifiersProperty, value); }

        public bool IsEnabled { get => GetValue(IsEnabledProperty); set => SetValue(IsEnabledProperty, value); }

        public bool IsDragging { get; private set; }

        public bool IsInProgress { get; private set; }

        private Control? ActualReference => Reference ?? AssociatedObject;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject == null) return;
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            AssociatedObject.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
                AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
                AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
                AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
            }
            Cancel();
            base.OnDetaching();
        }

        public void Cancel()
        {
            if (!IsInProgress && !IsDragging)
                return;

            ReleasePointerCapture();

            if (IsDragging)
            {
                IsDragging = false;
                var totalMovement = previousPosition - originPoint;
                Target?.OnResizingCompleted(Direction, totalMovement.X, totalMovement.Y);
            }
            IsInProgress = false;
        }

        public void StartResizing(Point origin) => SimulateDragStart(origin);

        public void DeltaResizing(Point newPosition) => SimulateDragDelta(newPosition);

        public void CompleteResizing(Point finalPosition) => SimulateDragComplete(finalPosition);

        public void SimulateDragStart(Point origin)
        {
            originPoint = origin;
            previousPosition = origin;
            IsInProgress = true;
            IsDragging = false;
            Target?.OnResizingStarted(Direction);
        }

        public void SimulateDragDelta(Point newPosition)
        {
            if (!IsInProgress) return;

            if (!IsDragging)
            {
                var dragDelta = newPosition - originPoint;
                if (Math.Abs(dragDelta.X) > MinimumHorizontalDragDistance ||
                    Math.Abs(dragDelta.Y) > MinimumVerticalDragDistance)
                {
                    IsDragging = true;
                }
            }

            if (IsDragging)
            {
                var movement = newPosition - previousPosition;
                if (Math.Abs(movement.X) > double.Epsilon || Math.Abs(movement.Y) > double.Epsilon)
                {
                    previousPosition = newPosition;
                    Target?.OnResizingDelta(Direction, movement.X, movement.Y);
                }
            }
        }

        public void SimulateDragComplete(Point finalPosition)
        {
            if (!IsInProgress) return;

            IsInProgress = false;
            if (IsDragging)
            {
                IsDragging = false;
                var totalMovement = finalPosition - originPoint;
                Target?.OnResizingCompleted(Direction, totalMovement.X, totalMovement.Y);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!IsEnabled || !AreModifiersValid(e.KeyModifiers))
                return;

            var point = e.GetCurrentPoint(ActualReference);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            e.Handled = true;
            capturedPointer = e.Pointer;
            e.Pointer.Capture(AssociatedObject);

            var pos = point.Position;
            SimulateDragStart(pos);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!IsInProgress)
                return;

            var point = e.GetCurrentPoint(ActualReference);
            if (!point.Properties.IsLeftButtonPressed || !AreModifiersValid(e.KeyModifiers))
            {
                Cancel();
                return;
            }

            SimulateDragDelta(point.Position);
            e.Handled = true;
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!IsInProgress)
                return;

            e.Handled = true;
            var pos = e.GetPosition(ActualReference);
            ReleasePointerCapture();
            SimulateDragComplete(pos);
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            Cancel();
        }

        private void ReleasePointerCapture()
        {
            capturedPointer?.Capture(null);
            capturedPointer = null;
        }

        private bool AreModifiersValid(KeyModifiers currentModifiers)
        {
            return Modifiers == null || Modifiers.Value == KeyModifiers.None || currentModifiers.HasFlag(Modifiers.Value);
        }
    }
}
