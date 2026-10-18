// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Behaviors
{
    public class ViewportPaneBehavior : Behavior<Control>
    {
        public static readonly StyledProperty<KeyModifiers> ModifiersProperty =
            AvaloniaProperty.Register<ViewportPaneBehavior, KeyModifiers>(nameof(Modifiers), defaultValue: KeyModifiers.Control);

        public static readonly StyledProperty<ViewportViewModel?> ViewportProperty =
            AvaloniaProperty.Register<ViewportPaneBehavior, ViewportViewModel?>(nameof(Viewport));

        private Point previousPosition;
        private Point originPoint;
        private IPointer? capturedPointer;

        public KeyModifiers Modifiers { get => GetValue(ModifiersProperty); set => SetValue(ModifiersProperty, value); }

        public ViewportViewModel? Viewport { get => GetValue(ViewportProperty); set => SetValue(ViewportProperty, value); }

        public bool IsPanning { get; private set; }

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
            CancelPanning();
            base.OnDetaching();
        }

        public void StartPanning(Point startPos) => SimulatePanStart(startPos);

        public void MovePanning(Point newPos) => SimulatePanMove(newPos);

        public void EndPanning() => SimulatePanEnd();

        public void SimulatePanStart(Point startPos)
        {
            if (IsPanning) return;

            IsPanning = true;
            originPoint = startPos;
            previousPosition = startPos;
        }

        public void SimulatePanMove(Point newPos)
        {
            if (!IsPanning) return;

            if (Viewport != null)
            {
                var movement = newPos - previousPosition;
                if (Math.Abs(movement.X) > double.Epsilon || Math.Abs(movement.Y) > double.Epsilon)
                {
                    previousPosition = newPos;
                    Viewport.HorizontalOffset += movement.X;
                    Viewport.VerticalOffset += movement.Y;
                }
            }
        }

        public void SimulatePanEnd()
        {
            if (!IsPanning) return;

            IsPanning = false;
            ReleasePointerCapture();
        }

        public void CancelPanning()
        {
            if (!IsPanning) return;

            IsPanning = false;
            ReleasePointerCapture();
            originPoint = default;
            previousPosition = default;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (IsPanning)
                return;

            if (Viewport == null || !IsInputValid(e))
                return;

            e.Handled = true;
            AssociatedObject?.Focus();
            capturedPointer = e.Pointer;
            e.Pointer.Capture(AssociatedObject);

            var point = e.GetPosition(AssociatedObject);
            SimulatePanStart(point);
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!IsPanning)
                return;

            if (Viewport != null && IsInputValid(e))
            {
                var position = e.GetPosition(AssociatedObject);
                SimulatePanMove(position);
                e.Handled = true;
            }
            else
            {
                CancelPanning();
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (IsPanning)
            {
                e.Handled = true;
                SimulatePanEnd();
            }
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            CancelPanning();
        }

        private void ReleasePointerCapture()
        {
            capturedPointer?.Capture(null);
            capturedPointer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsInputValid(PointerEventArgs e)
        {
            var point = e.GetCurrentPoint(AssociatedObject);
            return point.Properties.IsMiddleButtonPressed && (Modifiers == KeyModifiers.None || e.KeyModifiers.HasFlag(Modifiers));
        }
    }
}
