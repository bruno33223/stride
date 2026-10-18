// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views.Behaviors
{
    public abstract class AxisBehavior : Behavior<Control>
    {
        public static readonly StyledProperty<Control?> DrawingViewProperty =
            AvaloniaProperty.Register<AxisBehavior, Control?>(nameof(DrawingView));
        public static readonly StyledProperty<AxisBase?> XAxisProperty =
            AvaloniaProperty.Register<AxisBehavior, AxisBase?>(nameof(XAxis));
        public static readonly StyledProperty<AxisBase?> YAxisProperty =
            AvaloniaProperty.Register<AxisBehavior, AxisBase?>(nameof(YAxis));
        public static readonly StyledProperty<KeyModifiers> XModifiersProperty =
            AvaloniaProperty.Register<AxisBehavior, KeyModifiers>(nameof(XModifiers), KeyModifiers.None);
        public static readonly StyledProperty<KeyModifiers> YModifiersProperty =
            AvaloniaProperty.Register<AxisBehavior, KeyModifiers>(nameof(YModifiers), KeyModifiers.None);

        public Control? DrawingView { get => GetValue(DrawingViewProperty); set => SetValue(DrawingViewProperty, value); }

        public AxisBase? XAxis { get => GetValue(XAxisProperty); set => SetValue(XAxisProperty, value); }

        public AxisBase? YAxis { get => GetValue(YAxisProperty); set => SetValue(YAxisProperty, value); }

        public KeyModifiers XModifiers { get => GetValue(XModifiersProperty); set => SetValue(XModifiersProperty, value); }

        public KeyModifiers YModifiers { get => GetValue(YModifiersProperty); set => SetValue(YModifiersProperty, value); }

        public KeyModifiers CurrentModifiers { get; set; } = KeyModifiers.None;

        public bool HasXModifiers() => HasXModifiers(CurrentModifiers);

        public bool HasYModifiers() => HasYModifiers(CurrentModifiers);

        public bool HasXModifiers(KeyModifiers currentModifiers)
        {
            return XModifiers == KeyModifiers.None ? currentModifiers == KeyModifiers.None : currentModifiers.HasFlag(XModifiers);
        }

        public bool HasYModifiers(KeyModifiers currentModifiers)
        {
            return YModifiers == KeyModifiers.None ? currentModifiers == KeyModifiers.None : currentModifiers.HasFlag(YModifiers);
        }
    }
}
