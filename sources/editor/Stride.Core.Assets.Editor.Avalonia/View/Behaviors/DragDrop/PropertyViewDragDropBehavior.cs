// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors;

public class PropertyViewDragDropBehavior : Behavior<Control>, IDragDropBehavior
{
    public static readonly StyledProperty<bool> CanDragProperty =
        AvaloniaProperty.Register<PropertyViewDragDropBehavior, bool>(nameof(CanDrag), true);

    public static readonly StyledProperty<bool> CanDropProperty =
        AvaloniaProperty.Register<PropertyViewDragDropBehavior, bool>(nameof(CanDrop), true);

    public static readonly StyledProperty<DisplayDropAdorner> DisplayDropAdornerProperty =
        AvaloniaProperty.Register<PropertyViewDragDropBehavior, DisplayDropAdorner>(nameof(DisplayDropAdorner), DisplayDropAdorner.Never);

    public static readonly StyledProperty<GraphViewModel?> TargetProperty =
        AvaloniaProperty.Register<PropertyViewDragDropBehavior, GraphViewModel?>(nameof(Target));

    public bool CanDrag { get => GetValue(CanDragProperty); set => SetValue(CanDragProperty, value); }
    public bool CanDrop { get => GetValue(CanDropProperty); set => SetValue(CanDropProperty, value); }
    public DisplayDropAdorner DisplayDropAdorner { get => GetValue(DisplayDropAdornerProperty); set => SetValue(DisplayDropAdornerProperty, value); }
    public GraphViewModel? Target { get => GetValue(TargetProperty); set => SetValue(TargetProperty, value); }
}
