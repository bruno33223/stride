// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Stride.Assets.Editor.Avalonia.Behaviors
{
    /// <summary>
    /// This behavior allows to create bindings on the <see cref="ScrollViewer.Offset"/>
    /// properties of a <see cref="ScrollViewer"/>.
    /// </summary>
    public class BindScrollOffsetBehavior : Behavior<ScrollViewer>
    {
        public static readonly StyledProperty<double> HorizontalOffsetProperty =
            AvaloniaProperty.Register<BindScrollOffsetBehavior, double>(nameof(HorizontalOffset), default(double));

        public static readonly StyledProperty<double> VerticalOffsetProperty =
            AvaloniaProperty.Register<BindScrollOffsetBehavior, double>(nameof(VerticalOffset), default(double));

        private bool isUpdating;

        static BindScrollOffsetBehavior()
        {
            HorizontalOffsetProperty.Changed.AddClassHandler<BindScrollOffsetBehavior, double>((b, e) => b.OnHorizontalOffsetChanged(e.NewValue.GetValueOrDefault()));
            VerticalOffsetProperty.Changed.AddClassHandler<BindScrollOffsetBehavior, double>((b, e) => b.OnVerticalOffsetChanged(e.NewValue.GetValueOrDefault()));
        }

        public double HorizontalOffset { get => GetValue(HorizontalOffsetProperty); set => SetValue(HorizontalOffsetProperty, value); }

        public double VerticalOffset { get => GetValue(VerticalOffsetProperty); set => SetValue(VerticalOffsetProperty, value); }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.PropertyChanged += OnScrollViewerPropertyChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.PropertyChanged -= OnScrollViewerPropertyChanged;
            }
            base.OnDetaching();
        }

        private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ScrollViewer.OffsetProperty && e.NewValue is Vector offset)
            {
                OnControlOffsetChanged(offset);
            }
        }

        private void OnControlOffsetChanged(Vector offset)
        {
            if (isUpdating) return;
            try
            {
                isUpdating = true;
                SetCurrentValue(HorizontalOffsetProperty, offset.X);
                SetCurrentValue(VerticalOffsetProperty, offset.Y);
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void OnHorizontalOffsetChanged(double value)
        {
            if (AssociatedObject == null || isUpdating) return;
            try
            {
                isUpdating = true;
                AssociatedObject.Offset = new Vector(value, AssociatedObject.Offset.Y);
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void OnVerticalOffsetChanged(double value)
        {
            if (AssociatedObject == null || isUpdating) return;
            try
            {
                isUpdating = true;
                AssociatedObject.Offset = new Vector(AssociatedObject.Offset.X, value);
            }
            finally
            {
                isUpdating = false;
            }
        }
    }
}
