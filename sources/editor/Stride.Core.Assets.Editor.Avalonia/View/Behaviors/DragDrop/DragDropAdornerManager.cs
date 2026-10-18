// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Stride.Core.Presentation.Services;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors;

/// <summary>
/// Manages activation and deactivation of drag-and-drop adorners in Avalonia.
/// </summary>
public static class DragDropAdornerManager
{
    private static readonly DispatcherTimer DragLeaveTimer;
    private static readonly Dictionary<Control, Control> ActiveAdorners = new();

    static DragDropAdornerManager()
    {
        DragLeaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        DragLeaveTimer.Tick += (s, e) => Deactivate();
    }

    public static void ShowInsertAdorner(Control target, InsertPosition position)
    {
        var layer = AdornerLayer.GetAdornerLayer(target);
        if (layer == null) return;

        DragLeaveTimer.Stop();
        DragLeaveTimer.Start();
    }

    public static void SetAdornerState(Control element, bool isAccepted)
    {
        var layer = AdornerLayer.GetAdornerLayer(element);
        if (layer == null) return;

        DragLeaveTimer.Stop();
        DragLeaveTimer.Start();
    }

    public static void Deactivate()
    {
        DragLeaveTimer.Stop();
        ActiveAdorners.Clear();
    }
}
