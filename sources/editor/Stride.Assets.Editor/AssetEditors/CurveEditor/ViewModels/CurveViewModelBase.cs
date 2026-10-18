// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// Base class for all curves inside a curve editor.
/// </summary>
public abstract class CurveViewModelBase : DispatcherViewModel
{
    private readonly ObservableList<CurveViewModelBase> children = new();
    private AxisBase? xAxis;
    private AxisBase? yAxis;
    private Color color = Color.HotPink;
    private bool isSelected;

    protected CurveViewModelBase([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent)
        : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
    {
        Editor = editor;
        Parent = parent;
        children.CollectionChanged += ChildrenCollectionChanged;
    }

    public virtual IObservableList<CurveViewModelBase> Children => children;

    public Color Color
    {
        get => color;
        protected internal set => SetValue(ref color, value);
    }

    public abstract string DisplayName { get; }

    public bool HasChildren => Children?.Count > 0;

    public bool IsSelected
    {
        get => isSelected;
        set => SetValue(ref isSelected, value);
    }

    public CurveViewModelBase? Parent { get; }

    public double MaxX { get; protected set; }

    public double MaxY { get; protected set; }

    public double MinX { get; protected set; }

    public double MinY { get; protected set; }

    public AxisBase? XAxis
    {
        get => xAxis;
        internal set
        {
            if (xAxis != null)
                xAxis.TransformChanged -= AxisTransformChanged;
            if (SetValue(ref xAxis, value))
            {
                if (xAxis != null)
                    xAxis.TransformChanged += AxisTransformChanged;
                foreach (var child in Children)
                {
                    child.XAxis = value;
                }
            }
        }
    }

    public AxisBase? YAxis
    {
        get => yAxis;
        internal set
        {
            if (yAxis != null)
                yAxis.TransformChanged -= AxisTransformChanged;
            if (SetValue(ref yAxis, value))
            {
                if (yAxis != null)
                    yAxis.TransformChanged += AxisTransformChanged;
                foreach (var child in Children)
                {
                    child.YAxis = value;
                }
            }
        }
    }

    public CurveEditorViewModel Editor { get; }

    internal event EventHandler? AxisTransformChanged;

    public override void Destroy()
    {
        foreach (var child in children)
        {
            child.Destroy();
        }
        children.Clear();

        if (xAxis != null)
            xAxis.TransformChanged -= AxisTransformChanged;
        xAxis = null;
        if (yAxis != null)
            yAxis.TransformChanged -= AxisTransformChanged;
        yAxis = null;

        base.Destroy();
    }

    public void EnsureAxes()
    {
        if (XAxis != null && YAxis != null)
            return;

        XAxis ??= new LinearAxis { Position = AxisPosition.Bottom, Layer = AxisLayer.BelowCurves };
        YAxis ??= new LinearAxis { Position = AxisPosition.Left, Layer = AxisLayer.BelowCurves };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 InverseTransformPoint(Vector2 point)
    {
        return CheckAxes() ? XAxis!.InverseTransform(point.X, point.Y, YAxis!) : point;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 InverseTransformVector(Vector2 vector)
    {
        return CheckAxes() ? XAxis!.InverseTransform(vector.X, vector.Y, YAxis!, isVector: true) : vector;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TransformPoint(Vector2 point)
    {
        return CheckAxes() ? XAxis!.Transform(point.X, point.Y, YAxis!) : point;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TransformVector(Vector2 point)
    {
        return CheckAxes() ? XAxis!.Transform(point.X, point.Y, YAxis!, isVector: true) : point;
    }

    protected internal void UpdateAxisMaxMin()
    {
        if (!CheckAxes()) return;
        XAxis!.Include(MinX);
        XAxis.Include(MaxX);
        YAxis!.Include(MinY);
        YAxis.Include(MaxY);
        foreach (var child in Children)
        {
            child.UpdateAxisMaxMin();
        }
    }

    protected internal virtual void UpdateData()
    {
        foreach (var child in Children)
        {
            child.UpdateData();
        }
    }

    protected internal virtual void UpdateMaxMin()
    {
        MinX = MinY = MaxX = MaxY = double.NaN;
        foreach (var child in Children)
        {
            child.UpdateMaxMin();
        }
    }

    public bool CheckAxes()
    {
        return xAxis != null && yAxis != null;
    }

    public RectangleF GetClippingRect()
    {
        if (!CheckAxes()) return RectangleF.Empty;
        var minX = Math.Min(XAxis!.ScreenMin.X, XAxis.ScreenMax.X);
        var minY = Math.Min(YAxis!.ScreenMin.Y, YAxis.ScreenMax.Y);
        var maxX = Math.Max(XAxis.ScreenMin.X, XAxis.ScreenMax.X);
        var maxY = Math.Max(YAxis.ScreenMin.Y, YAxis.ScreenMax.Y);

        return new RectangleF(minX, minY, maxX - minX, maxY - minY);
    }

    protected bool IsValidPoint(double x, double y)
    {
        return (XAxis?.IsValidValue(x) ?? false) &&
               (YAxis?.IsValidValue(y) ?? false);
    }

    protected virtual void Refresh()
    {
        Editor.InvalidateView();
    }

    public virtual IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        foreach (var child in Children)
        {
            foreach (var series in child.GetRenderSeries())
            {
                yield return series;
            }
        }
    }

    private void ChildrenCollectionChanged(object? sender, [NotNull] NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move)
            return;

        if (e.OldItems != null)
        {
            foreach (CurveViewModelBase child in e.OldItems)
            {
                child.XAxis = null;
                child.YAxis = null;
            }
        }

        if (e.NewItems != null)
        {
            foreach (CurveViewModelBase child in e.NewItems)
            {
                child.XAxis = XAxis;
                child.YAxis = YAxis;
            }
        }
    }

    public virtual void Render(IDrawingContext drawingContext, bool isCurrentCurve)
    {
    }
}

/// <summary>
/// Base class for all computed curves inside a curve editor.
/// </summary>
public abstract class CurveViewModelBase<TValue> : CurveViewModelBase
    where TValue : struct
{
    private readonly string? name;

    protected CurveViewModelBase([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent, [NotNull] IComputeCurve<TValue> computeCurve, string? name = null)
        : base(editor, parent)
    {
        if (computeCurve == null) throw new ArgumentNullException(nameof(computeCurve));

        CurveNode = editor.Session.AssetNodeContainer.GetOrCreateNode(computeCurve);
        this.name = name;
    }

    public override string DisplayName => !string.IsNullOrEmpty(name) ? name : CurveNode.Type.Name;

    public Guid CurveId => CurveNode.Guid;

    public IObjectNode CurveNode { get; }

    public IComputeCurve<TValue>? UnderlyingCurve => CurveNode.Retrieve() as IComputeCurve<TValue>;

    protected static bool GetSamplingParameters([NotNull] AxisBase axis, out double start, out double end, out double increment)
    {
        start = double.IsNaN(axis.ViewMinimum) ? 0 : Math.Max(0, axis.ViewMinimum);
        end = double.IsNaN(axis.ViewMaximum) ? 1 : Math.Min(1, axis.ViewMaximum);

        var screenPixels = (axis.ScreenMax - axis.ScreenMin).X;
        if (!double.IsNaN(screenPixels) && screenPixels > 1)
            increment = (end - start) / screenPixels;
        else
            increment = axis.ActualMajorStep / axis.IntervalLength;

        return !double.IsNaN(increment) && !(increment < float.Epsilon);
    }

    public virtual void Initialize()
    {
    }
}
