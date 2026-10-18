// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views.Behaviors
{
    public class AxisPaneBehavior : AxisBehavior
    {
        public static readonly StyledProperty<MouseButton> MouseButtonProperty =
            AvaloniaProperty.Register<AxisPaneBehavior, MouseButton>(nameof(MouseButton), MouseButton.Middle);

        private Point previousPosition;

        public MouseButton MouseButton
        {
            get => GetValue(MouseButtonProperty);
            set => SetValue(MouseButtonProperty, value);
        }

        public bool IsPanning { get; private set; }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            if (AssociatedObject == null)
                return;

            AssociatedObject.PointerPressed += OnPointerPressed;
            AssociatedObject.PointerMoved += OnPointerMoved;
            AssociatedObject.PointerReleased += OnPointerReleased;
            AssociatedObject.PointerCaptureLost += OnPointerCaptureLost;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            if (AssociatedObject == null)
                return;

            AssociatedObject.PointerPressed -= OnPointerPressed;
            AssociatedObject.PointerMoved -= OnPointerMoved;
            AssociatedObject.PointerReleased -= OnPointerReleased;
            AssociatedObject.PointerCaptureLost -= OnPointerCaptureLost;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (AssociatedObject == null || IsPanning)
                return;

            CurrentModifiers = e.KeyModifiers;
            var currentPoint = e.GetCurrentPoint(AssociatedObject);
            if (!IsInputValid(currentPoint.Properties, e.KeyModifiers))
                return;

            e.Handled = true;
            AssociatedObject.Focus();
            e.Pointer.Capture(AssociatedObject);
            IsPanning = true;
            previousPosition = currentPoint.Position;
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (AssociatedObject == null || !IsPanning)
                return;

            CurrentModifiers = e.KeyModifiers;
            var currentPoint = e.GetCurrentPoint(AssociatedObject);
            if (IsInputValid(currentPoint.Properties, e.KeyModifiers))
            {
                var position = currentPoint.Position;
                Pan(previousPosition, position);
                previousPosition = position;
                e.Handled = true;
            }
            else
            {
                CancelPanning(e.Pointer);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (IsPanning)
            {
                e.Handled = true;
                CancelPanning(e.Pointer);
            }
        }

        private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            CancelPanning(null);
        }

        public void Pan(Point previousPos, Point currentPos)
        {
            var p0 = new Stride.Core.Mathematics.Vector2((float)previousPos.X, (float)previousPos.Y);
            var p1 = new Stride.Core.Mathematics.Vector2((float)currentPos.X, (float)currentPos.Y);
            if (HasXModifiers())
            {
                XAxis?.Pan(p0, p1);
            }
            if (HasYModifiers())
            {
                YAxis?.Pan(p0, p1);
            }

            if (DrawingView is IDrawingView idv)
            {
                idv.InvalidateDrawing();
            }
            else if (DrawingView is CurveCanvasControl ccc)
            {
                ccc.InvalidateDrawing();
            }
            else
            {
                DrawingView?.InvalidateVisual();
            }
        }

        private void CancelPanning(IPointer? pointer)
        {
            if (!IsPanning)
                return;

            IsPanning = false;
            pointer?.Capture(null);
        }

        private bool IsInputValid(PointerPointProperties props, KeyModifiers modifiers)
        {
            bool isButtonPressed = MouseButton switch
            {
                MouseButton.Left => props.IsLeftButtonPressed,
                MouseButton.Middle => props.IsMiddleButtonPressed,
                MouseButton.Right => props.IsRightButtonPressed,
                MouseButton.XButton1 => props.IsXButton1Pressed,
                MouseButton.XButton2 => props.IsXButton2Pressed,
                _ => false
            };

            return isButtonPressed && (HasXModifiers(modifiers) || HasYModifiers(modifiers));
        }
    }
}
