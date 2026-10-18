// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

[Flags]
public enum SelectionModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4
}

public partial class CurveEditorViewModel : DispatcherViewModel
{
    private readonly ObservableList<CurveViewModelBase> curves = new();
    private bool isControlPointHovered;
    private CurveViewModelBase? selectedCurve;
    private ControlPointViewModelBase? singleSelectedControlPoint;

    public CurveEditorViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] SessionViewModel session)
        : base(serviceProvider.SafeArgument(nameof(serviceProvider)))
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));

        AddPointCommand = new AnonymousCommand<Vector2>(serviceProvider, AddPoint);
        ClearCurveCommand = new AnonymousCommand(serviceProvider, ClearSelectedCurve);
        DeleteSelectedPointsCommand = new AnonymousCommand(serviceProvider, DeleteSelectedPoints);
        FocusCommand = new AnonymousCommand(serviceProvider, Focus);
        NavigateToControlPointCommand = new AnonymousCommand<int>(serviceProvider, NavigateToControlPoint);
        PreviewClickCommand = new AnonymousCommand<Vector2>(serviceProvider, Click);
        RemoveSelectedCurveCommand = new AnonymousCommand(serviceProvider, RemoveSelectedCurve);
        ResetViewCommand = new AnonymousCommand<int>(serviceProvider, ResetAxes);
        SelectCommand = new AnonymousCommand<RectangleF>(serviceProvider, Select);
        SetTangentLinearCommand = new AnonymousCommand(serviceProvider, () => SetSelectedPointsTangentType(AnimationKeyTangentType.Linear));
        SetTangentFlatCommand = new AnonymousCommand(serviceProvider, () => SetSelectedPointsTangentType(AnimationKeyTangentType.Flat));
        SetTangentSmoothCommand = new AnonymousCommand(serviceProvider, () => SetSelectedPointsTangentType(AnimationKeyTangentType.Smooth));
        SetTangentTypeCommand = new AnonymousCommand<AnimationKeyTangentType>(serviceProvider, SetSelectedPointsTangentType);

        SelectedControlPoints.CollectionChanged += SelectedControlPointsCollectionChanged;

        InitializeRendering();
    }

    public IReadOnlyObservableCollection<CurveViewModelBase> Curves => curves;

    public bool IsControlPointHovered
    {
        get => isControlPointHovered;
        set => SetValue(ref isControlPointHovered, value);
    }

    public CurveViewModelBase? SelectedCurve
    {
        get => selectedCurve;
        set => SetValue(ref selectedCurve, value, OnSelectedCurveChanged);
    }

    public ObservableList<object> SelectedControlPoints { get; } = new();

    public SessionViewModel Session { get; }

    public ControlPointViewModelBase? SingleSelectedControlPoint
    {
        get => singleSelectedControlPoint;
        set => SetValue(ref singleSelectedControlPoint, value);
    }

    public SelectionModifiers ActiveModifiers { get; set; } = SelectionModifiers.None;

    public ICommandBase AddPointCommand { get; }

    public ICommandBase ClearCurveCommand { get; }

    public ICommandBase DeleteSelectedPointsCommand { get; }

    public ICommandBase FocusCommand { get; }

    public ICommandBase NavigateToControlPointCommand { get; }

    public ICommandBase PreviewClickCommand { get; }

    public ICommandBase RemoveSelectedCurveCommand { get; }

    public ICommandBase ResetViewCommand { get; }

    public ICommandBase SelectCommand { get; }

    public ICommandBase SetTangentLinearCommand { get; }

    public ICommandBase SetTangentFlatCommand { get; }

    public ICommandBase SetTangentSmoothCommand { get; }

    public ICommandBase SetTangentTypeCommand { get; }

    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    public event Action? ViewInvalidated;

    partial void InitializeRendering();

    public void UpdateBounds(RectangleF bounds)
    {
        CurveArea = bounds;
        CurveAndAxesArea = bounds;
        if (SelectedCurve != null)
        {
            SelectedCurve.XAxis?.UpdateTransform(bounds);
            SelectedCurve.XAxis?.UpdateIntervals(bounds);
            SelectedCurve.YAxis?.UpdateTransform(bounds);
            SelectedCurve.YAxis?.UpdateIntervals(bounds);
        }
    }

    public void AddCurve<TValue>([NotNull] IComputeCurve<TValue> computeCurve, string name)
        where TValue : struct
    {
        if (computeCurve == null) throw new ArgumentNullException(nameof(computeCurve));

        if (SelectCurveIfExists(computeCurve))
            return;

        var curve = CreateCurveHierarchy(computeCurve, name: name);
        AddCurvePrivate(curve);
    }

    public override void Destroy()
    {
        var curvesCopy = curves.ToList();
        curves.Clear();
        SelectedControlPoints.Clear();
        selectedCurve = null;

        foreach (var curve in curvesCopy)
        {
            curve.Destroy();
        }
        base.Destroy();
    }

    private void AddCurvePrivate([NotNull] CurveViewModelBase curve)
    {
        curve.EnsureAxes();
        curves.Add(curve);
        SelectedCurve = curve;
    }

    public void AddPoint(Vector2 point)
    {
        var undoRedo = UndoRedoService;
        using (var transaction = undoRedo?.CreateTransaction())
        {
            var editableCurve = selectedCurve as IEditableCurveViewModel;
            if (editableCurve == null && selectedCurve?.Children.Count > 0)
            {
                editableCurve = selectedCurve.Children.OfType<IEditableCurveViewModel>().FirstOrDefault();
            }
            editableCurve?.AddPoint(point);

            if (transaction != null && undoRedo != null)
            {
                undoRedo.SetName(transaction, "Add control point");
            }
        }
        InvalidateView();
    }

    public void ClearSelectedCurve()
    {
        if (selectedCurve is not IEditableCurveViewModel editableCurve)
            return;

        using (var transaction = UndoRedoService.CreateTransaction())
        {
            DeletePoints(editableCurve.ControlPoints, editableCurve);
            UndoRedoService.SetName(transaction, $"Clear curve '{selectedCurve.DisplayName}'");
        }
        InvalidateView();
    }

    public void ClearPointSelection()
    {
        foreach (var p in SelectedControlPoints.OfType<ControlPointViewModelBase>())
        {
            p.IsSelected = false;
        }
        SelectedControlPoints.Clear();
    }

    public void Click(Vector2 point)
    {
        if (SelectedCurve is not IEditableCurveViewModel editableCurve)
        {
            IsControlPointHovered = false;
            return;
        }

        var radius = editableCurve.ControlPointRadius;
        var clickedPoint = editableCurve.GetClosestPoint(point, 2 * radius);
        if (clickedPoint == null)
        {
            if (ActiveModifiers == SelectionModifiers.None)
                ClearPointSelection();
            IsControlPointHovered = false;
            InvalidateView();
            return;
        }

        if (!clickedPoint.IsSelected)
        {
            var rect = new RectangleF((float)(point.X - 2 * radius), (float)(point.Y - 2 * radius), (float)(4 * radius), (float)(4 * radius));
            Select(rect, true);
        }

        IsControlPointHovered = true;
        InvalidateView();
    }

    public void SetSelectedPointsTangentType(AnimationKeyTangentType tangentType)
    {
        var undoRedo = UndoRedoService;
        using (var transaction = undoRedo?.CreateTransaction())
        {
            foreach (var point in SelectedControlPoints.OfType<ControlPointViewModelBase>())
            {
                point.TangentType = tangentType;
            }

            if (transaction != null && undoRedo != null)
            {
                undoRedo.SetName(transaction, $"Set tangent to {tangentType}");
            }
        }

        InvalidateView();
    }

    public void DeleteSelectedPoints()
    {
        var count = SelectedControlPoints.Count;
        if (count == 0)
            return;

        var editableCurves = new List<IEditableCurveViewModel>();
        if (SelectedCurve is IEditableCurveViewModel single)
            editableCurves.Add(single);
        if (SelectedCurve?.Children.Count > 0)
            editableCurves.AddRange(SelectedCurve.Children.OfType<IEditableCurveViewModel>());

        if (editableCurves.Count == 0)
            return;

        var undoRedo = UndoRedoService;
        using (var transaction = undoRedo?.CreateTransaction())
        {
            foreach (var curve in editableCurves)
            {
                DeletePoints(SelectedControlPoints.OfType<ControlPointViewModelBase>().Where(p => p.Curve == curve), curve);
            }
            if (transaction != null && undoRedo != null)
            {
                undoRedo.SetName(transaction, $"Delete {count} point{(count > 1 ? "s" : "")} from curve '{selectedCurve?.DisplayName}'");
            }
        }
        SelectedControlPoints.Clear();
        InvalidateView();
    }

    private static void DeletePoints([NotNull] IEnumerable<ControlPointViewModelBase> points, IEditableCurveViewModel editableCurve)
    {
        foreach (var point in points.ToList())
        {
            editableCurve.RemovePoint(point);
        }
    }

    public void Focus()
    {
        var count = SelectedControlPoints.Count;
        switch (count)
        {
            case 0:
                ResetAxes();
                break;

            default:
                if (SelectedCurve != null)
                {
                    var center = SelectedControlPoints
                        .Select(s => (s as ControlPointViewModelBase)?.ActualPoint ?? Vector2.Zero)
                        .Aggregate(Vector2.Zero, (c, s) => c + s) / count;
                    SelectedCurve.XAxis?.Center(center);
                    SelectedCurve.YAxis?.Center(center);
                    InvalidateView();
                }
                break;
        }
    }

    public void NavigateToControlPoint(int navigation)
    {
        if (SelectedControlPoints.Count == 0)
            return;

        if (SelectedControlPoints[0] is not ControlPointViewModelBase point)
            return;

        point.IsSelected = false;
        if (navigation == int.MinValue)
        {
            while (point.Previous != null)
            {
                point = point.Previous;
            }
        }
        else if (navigation == int.MaxValue)
        {
            while (point.Next != null)
            {
                point = point.Next;
            }
        }
        else if (navigation == -1)
        {
            point = point.Previous ?? point;
        }
        else if (navigation == 1)
        {
            point = SelectedControlPoints[SelectedControlPoints.Count - 1] as ControlPointViewModelBase ?? point;
            point = point.Next ?? point;
        }
        else
        {
            return;
        }

        ClearPointSelection();
        SelectedControlPoints.Add(point);
        point.IsSelected = true;
        InvalidateView();
    }

    private void OnSelectedCurveChanged()
    {
        ClearPointSelection();
        if (selectedCurve != null)
        {
            selectedCurve.EnsureAxes();
            selectedCurve.UpdateData();
            selectedCurve.UpdateMaxMin();
            selectedCurve.UpdateAxisMaxMin();
            ResetAxes();
        }
        InvalidateView();
    }

    public void RemoveCurve(CurveViewModelBase curve)
    {
        if (curve == null)
            return;

        if (curves.Remove(curve))
        {
            curve.Destroy();
        }
        if (SelectedCurve == curve)
        {
            SelectedCurve = curves.FirstOrDefault();
        }
        InvalidateView();
    }

    public void RemoveSelectedCurve()
    {
        if (SelectedCurve != null)
            RemoveCurve(SelectedCurve);
    }

    public void ResetAxes(int option = 0)
    {
        foreach (var axis in Axes)
        {
            if (axis.IsHorizontal() && option <= 0)
            {
                axis.Reset();
                axis.SetViewMaxMinToActualMaxMin();
            }

            if (axis.IsVertical() && option >= 0)
            {
                axis.Reset();
                axis.SetViewMaxMinToActualMaxMin();
            }
        }

        InvalidateView();
    }

    public void Select(RectangleF selectionRect)
    {
        Select(selectionRect, false);
    }

    public void Select(RectangleF selectionRect, bool isSingleSelection)
    {
        var editableCurves = new List<IEditableCurveViewModel>();
        if (SelectedCurve is IEditableCurveViewModel single)
            editableCurves.Add(single);
        if (SelectedCurve?.Children.Count > 0)
            editableCurves.AddRange(SelectedCurve.Children.OfType<IEditableCurveViewModel>());

        if (editableCurves.Count == 0)
            return;

        IList<ControlPointViewModelBase> selectedPoints = editableCurves
            .SelectMany(c => c.ControlPoints)
            .Where(c => selectionRect.Contains(c.ActualPoint))
            .ToList();

        if (selectedPoints.Count > 1 && isSingleSelection)
        {
            var rectCenter = new Vector2(selectionRect.Left + selectionRect.Width * 0.5f, selectionRect.Top + selectionRect.Height * 0.5f);
            var closest = CurveHelper.GetClosestPoint(selectedPoints, rectCenter);
            if (closest != null)
            {
                selectedPoints = new[] { closest };
            }
        }

        if (ActiveModifiers.HasFlag(SelectionModifiers.Shift))
        {
            foreach (var p in selectedPoints.Where(p => !p.IsSelected))
            {
                SelectedControlPoints.Add(p);
                p.IsSelected = true;
            }
        }
        else if (ActiveModifiers.HasFlag(SelectionModifiers.Control))
        {
            foreach (var p in selectedPoints.Where(p => p.IsSelected))
            {
                SelectedControlPoints.Remove(p);
                p.IsSelected = false;
            }
        }
        else
        {
            ClearPointSelection();
            foreach (var p in selectedPoints)
            {
                SelectedControlPoints.Add(p);
                p.IsSelected = true;
            }
        }

        InvalidateView();
    }

    private void SelectedControlPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SingleSelectedControlPoint = (SelectedControlPoints.Count == 1 ? SelectedControlPoints[0] : null) as ControlPointViewModelBase;
    }

    private bool SelectCurveIfExists<TValue>(IComputeCurve<TValue> computeCurve) where TValue : struct
    {
        var curveNode = Session.AssetNodeContainer.GetNode(computeCurve);
        if (curveNode == null)
            return false;

        var curve = Curves.OfType<CurveViewModelBase<TValue>>().FirstOrDefault(c => c.CurveId == curveNode.Guid);
        if (curve == null)
            return false;

        SelectedCurve = curve;
        return true;
    }
}
