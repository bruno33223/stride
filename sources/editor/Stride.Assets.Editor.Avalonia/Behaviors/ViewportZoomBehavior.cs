// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Behaviors
{
    public class ViewportZoomBehavior : Behavior<Control>
    {
        public static readonly StyledProperty<KeyModifiers> ModifiersProperty =
            AvaloniaProperty.Register<ViewportZoomBehavior, KeyModifiers>(nameof(Modifiers), defaultValue: KeyModifiers.Control);

        public static readonly StyledProperty<ViewportViewModel?> ViewportProperty =
            AvaloniaProperty.Register<ViewportZoomBehavior, ViewportViewModel?>(nameof(Viewport));

        public KeyModifiers Modifiers
        {
            get => GetValue(ModifiersProperty);
            set => SetValue(ModifiersProperty, value);
        }

        public ViewportViewModel? Viewport
        {
            get => GetValue(ViewportProperty);
            set => SetValue(ViewportProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);
            }
            base.OnDetaching();
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (Viewport == null || (Modifiers != KeyModifiers.None && !e.KeyModifiers.HasFlag(Modifiers)))
                return;

            var position = e.GetPosition(AssociatedObject);
            if (e.Delta.Y > 0)
            {
                Viewport.ZoomIn(position.X, position.Y);
                e.Handled = true;
            }
            else if (e.Delta.Y < 0)
            {
                Viewport.ZoomOut(position.X, position.Y);
                e.Handled = true;
            }
        }

        public void SimulateZoom(Point position, double deltaY, KeyModifiers modifiers)
        {
            if (Viewport == null || (Modifiers != KeyModifiers.None && !modifiers.HasFlag(Modifiers)))
                return;

            if (deltaY > 0)
                Viewport.ZoomIn(position.X, position.Y);
            else if (deltaY < 0)
                Viewport.ZoomOut(position.X, position.Y);
        }
    }
}
