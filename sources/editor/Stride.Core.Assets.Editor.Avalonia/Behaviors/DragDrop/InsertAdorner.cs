// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class InsertAdorner : Control
    {
        private readonly Pen renderPen;

        public Visual AdornedElement { get; }

        public InsertAdorner([NotNull] Visual adornedElement)
        {
            AdornedElement = adornedElement ?? throw new ArgumentNullException(nameof(adornedElement));
            renderPen = new Pen(new SolidColorBrush(Color.FromArgb(255, 173, 173, 173)), 3);
            IsHitTestVisible = false;
        }

        public InsertPosition Position { get; set; }

        public override void Render(DrawingContext context)
        {
            var bounds = Bounds;
            switch (Position)
            {
                case InsertPosition.Before:
                    context.DrawLine(renderPen, new Point(0, 0), new Point(bounds.Width, 0));
                    break;
                case InsertPosition.After:
                    context.DrawLine(renderPen, new Point(0, bounds.Height), new Point(bounds.Width, bounds.Height));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.Render(context);
        }
    }
}
