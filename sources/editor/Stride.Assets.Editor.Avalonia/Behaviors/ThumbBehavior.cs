// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Behaviors
{
    public class ThumbBehavior : Behavior<Thumb>
    {
        public static readonly StyledProperty<ResizingDirection> DirectionProperty =
            AvaloniaProperty.Register<ThumbBehavior, ResizingDirection>(nameof(Direction));

        public static readonly StyledProperty<IResizingTarget?> TargetProperty =
            AvaloniaProperty.Register<ThumbBehavior, IResizingTarget?>(nameof(Target));

        /// <summary>
        /// The direction of the resizing.
        /// </summary>
        public ResizingDirection Direction { get => GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

        /// <summary>
        /// The target of the resizing.
        /// </summary>
        public IResizingTarget? Target { get => GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

        public bool IsDragging { get; private set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.DragStarted += OnDragStarted;
                AssociatedObject.DragDelta += OnDragDelta;
                AssociatedObject.DragCompleted += OnDragCompleted;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.DragCompleted -= OnDragCompleted;
                AssociatedObject.DragDelta -= OnDragDelta;
                AssociatedObject.DragStarted -= OnDragStarted;
            }
            base.OnDetaching();
        }

        public virtual void OnDragCompleted(object? sender, VectorEventArgs e)
        {
            if (IsDragging) Target?.OnResizingCompleted(Direction, e.Vector.X, e.Vector.Y);
            IsDragging = false;
        }

        public virtual void OnDragDelta(object? sender, VectorEventArgs e)
        {
            if (IsDragging) Target?.OnResizingDelta(Direction, e.Vector.X, e.Vector.Y);
        }

        public virtual void OnDragStarted(object? sender, VectorEventArgs e)
        {
            IsDragging = true;
            Target?.OnResizingStarted(Direction);
        }

        public void SimulateDragStart() => OnDragStarted(AssociatedObject, new VectorEventArgs());

        public void SimulateDragDelta(double horizontalChange, double verticalChange) =>
            OnDragDelta(AssociatedObject, new VectorEventArgs { Vector = new Vector(horizontalChange, verticalChange) });

        public void SimulateDragComplete(double horizontalChange, double verticalChange) =>
            OnDragCompleted(AssociatedObject, new VectorEventArgs { Vector = new Vector(horizontalChange, verticalChange) });
    }
}
