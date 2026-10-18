// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors
{
    public class ValidateTextBoxAfterSlidingBehavior : Behavior<Slider>
    {
        public static readonly StyledProperty<Control?> TextBoxProperty =
            AvaloniaProperty.Register<ValidateTextBoxAfterSlidingBehavior, Control?>(nameof(TextBox));

        public Control? TextBox { get => GetValue(TextBoxProperty); set => SetValue(TextBoxProperty, value); }

        public System.Action? ValidationAction { get; set; }

        private bool isThumbDragging;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(Thumb.DragStartedEvent, OnDragStarted);
                AssociatedObject.AddHandler(Thumb.DragCompletedEvent, OnDragCompleted);
                AssociatedObject.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                AssociatedObject.PropertyChanged += OnSliderPropertyChanged;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(Thumb.DragStartedEvent, OnDragStarted);
                AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, OnDragCompleted);
                AssociatedObject.RemoveHandler(InputElement.KeyUpEvent, OnKeyUp);
                AssociatedObject.PropertyChanged -= OnSliderPropertyChanged;
            }
            base.OnDetaching();
        }

        private void OnDragStarted(object? sender, VectorEventArgs e)
        {
            isThumbDragging = true;
        }

        private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Slider.ValueProperty)
            {
                if (AssociatedObject == null) return;
                if (!isThumbDragging)
                {
                    ValidateTextBox();
                }
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Down || e.Key == Key.PageDown ||
                e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End)
            {
                ValidateTextBox();
            }
        }

        private void OnDragCompleted(object? sender, VectorEventArgs e)
        {
            ValidateTextBox();
        }

        public void ValidateTextBox()
        {
            ValidationAction?.Invoke();

            if (TextBox != null)
            {
                var validateMethod = TextBox.GetType().GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
                validateMethod?.Invoke(TextBox, null);
            }
        }
    }
}
