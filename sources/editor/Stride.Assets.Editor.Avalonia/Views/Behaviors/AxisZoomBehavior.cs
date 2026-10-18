// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views.Behaviors
{
    public class AxisZoomBehavior : AxisBehavior
    {
        public double ZoomFactor { get; set; } = 1.0;

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            if (AssociatedObject != null) AssociatedObject.PointerWheelChanged += OnPointerWheelChanged;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            if (AssociatedObject != null) AssociatedObject.PointerWheelChanged -= OnPointerWheelChanged;
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (AssociatedObject == null) return;
            CurrentModifiers = e.KeyModifiers;
            Zoom(e.Delta.Y, e.GetPosition(AssociatedObject));
            e.Handled = true;
        }

        public void Zoom(double delta, Point position)
        {
            var hasX = HasXModifiers();
            var hasY = HasYModifiers();
            if (!hasX && !hasY) return;

            if (delta > 0) ZoomFactorIn();
            if (delta < 0) ZoomFactorOut();

            var current = InverseTransform(position);
            if (hasX) XAxis?.ZoomAt(ZoomFactor, current.X);
            if (hasY) YAxis?.ZoomAt(ZoomFactor, current.Y);

            if (DrawingView is IDrawingView idv) idv.InvalidateDrawing();
            else if (DrawingView is CurveCanvasControl ccc) ccc.InvalidateDrawing();
            else DrawingView?.InvalidateVisual();
        }

        public Point InverseTransform(Point point)
        {
            if (XAxis != null)
            {
                var inv = XAxis.InverseTransform(point.X, point.Y, YAxis);
                return new Point(inv.X, inv.Y);
            }
            if (YAxis != null) return new Point(0, YAxis.InverseTransform(point.Y));
            return new Point();
        }

        public void ZoomFactorIn()
        {
            ZoomFactor = 1.25;
        }

        public void ZoomFactorOut()
        {
            ZoomFactor = 0.8;
        }
    }
}
