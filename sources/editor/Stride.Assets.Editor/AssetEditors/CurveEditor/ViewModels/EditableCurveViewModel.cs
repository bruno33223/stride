// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public interface IEditableCurveViewModel
{
    [ItemNotNull, NotNull]
    IReadOnlyObservableCollection<ControlPointViewModelBase> ControlPoints { get; }

    double ControlPointRadius { get; }

    void AddPoint(Vector2 point);

    ControlPointViewModelBase? GetClosestPoint(Vector2 position, double maximumDistance = double.PositiveInfinity);

    bool RemovePoint(ControlPointViewModelBase point);
}

public abstract class EditableCurveViewModel<TValue> : CurveViewModelBase<TValue>, IEditableCurveViewModel, IResizingTarget
    where TValue : struct
{
    private Color controlPointColor = Color.CadetBlue;
    private double controlPointRadius = 4;

    protected EditableCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent, [NotNull] IComputeCurve<TValue> computeCurve, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public abstract IReadOnlyObservableCollection<ControlPointViewModelBase> ControlPoints { get; }

    public Color ControlPointColor
    {
        get => controlPointColor;
        protected internal set => SetValue(ref controlPointColor, value);
    }

    public double ControlPointRadius
    {
        get => controlPointRadius;
        protected internal set => SetValue(ref controlPointRadius, value);
    }

    public bool IsInitialized { get; private set; }

    public bool IsInitializing { get; private set; }

    public abstract void AddPoint(Vector2 point);

    public ControlPointViewModelBase? GetClosestPoint(Vector2 position, double maximumDistance)
    {
        return CurveHelper.GetClosestPoint(ControlPoints, position, maximumDistance);
    }

    public sealed override void Initialize()
    {
        IsInitializing = true;
        try
        {
            InitializeOverride();
            IsInitialized = true;
        }
        finally
        {
            IsInitializing = false;
        }
    }

    public abstract bool RemovePoint(ControlPointViewModelBase point);

    void IResizingTarget.OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        using (var transaction = Editor.UndoRedoService.CreateTransaction())
        {
            OnResizingCompleted(direction, horizontalChange, verticalChange);
            Editor.UndoRedoService.SetName(transaction, "Move control points");
        }
    }

    void IResizingTarget.OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        OnResizingDelta(direction, horizontalChange, verticalChange);
    }

    void IResizingTarget.OnResizingStarted(ResizingDirection direction)
    {
        OnResizingStarted(direction);
    }

    protected void ControlPointsCollectionChanged(object? sender, [NotNull] NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Move)
            return;

        if (e.OldItems != null)
        {
            foreach (ControlPointViewModelBase c in e.OldItems)
            {
                c.PropertyChanged -= ControlPointPropertyChanged;
                c.Destroy();
            }
        }

        if (e.NewItems != null)
        {
            foreach (ControlPointViewModelBase c in e.NewItems)
            {
                c.PropertyChanged += ControlPointPropertyChanged;
            }
        }

        Refresh();
    }

    protected void ControlPointPropertyChanged(object? sender, [NotNull] PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ControlPointViewModelBase.ActualPoint) || e.PropertyName == nameof(ControlPointViewModelBase.IsSelected))
        {
            Refresh();
        }
    }

    protected virtual void InitializeOverride()
    {
    }

    protected virtual void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        var selectedPoints = ControlPoints.Where(c => c.IsSelected).ToList();
        foreach (IResizingTarget c in selectedPoints)
        {
            c.OnResizingCompleted(direction, horizontalChange, verticalChange);
        }
    }

    protected virtual void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        var selectedPoints = ControlPoints.Where(c => c.IsSelected).ToList();
        foreach (IResizingTarget c in selectedPoints)
        {
            c.OnResizingDelta(direction, horizontalChange, verticalChange);
        }
    }

    protected virtual void OnResizingStarted(ResizingDirection direction)
    {
        var selectedPoints = ControlPoints.Where(c => c.IsSelected).ToList();
        foreach (IResizingTarget c in selectedPoints)
        {
            c.OnResizingStarted(direction);
        }
    }

    protected override void Refresh()
    {
        if (!IsInitialized || IsInitializing)
            return;

        base.Refresh();
    }
}
