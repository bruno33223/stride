// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public abstract class ControlPointViewModelBase : DispatcherViewModel, IResizingTarget
{
    private Vector2 point;
    private bool isSelected;
    private Vector2 startingPoint;

    protected ControlPointViewModelBase([NotNull] CurveViewModelBase curve)
        : base(curve.SafeArgument(nameof(curve)).ServiceProvider)
    {
        Curve = curve;
        DependentProperties.Add(nameof(Point), new[] { nameof(ActualKey), nameof(ActualPoint), nameof(ActualValue) });
    }

    public double ActualKey
    {
        get => Point.X;
        set => Key = value;
    }

    public Vector2 ActualPoint => Curve.TransformPoint(Point);

    public double ActualValue
    {
        get => Point.Y;
        set => Value = value;
    }

    public bool IsSelected
    {
        get => isSelected;
        set => SetValue(ref isSelected, value);
    }

    public virtual AnimationKeyTangentType? TangentType
    {
        get => null;
        set { }
    }

    public bool IsSynchronized { get; protected set; }

    public abstract double Key { get; protected set; }

    public abstract double Value { get; protected set; }

    public Vector2 Point
    {
        get => point;
        protected set => SetValue(ref point, value, () => IsSynchronized = false);
    }

    protected IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    public ControlPointViewModelBase? Next { get; set; }

    public ControlPointViewModelBase? Previous { get; set; }

    public CurveViewModelBase Curve { get; }

    void IResizingTarget.OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        using (var transaction = UndoRedoService.CreateTransaction())
        {
            OnResizingCompleted(direction, horizontalChange, verticalChange);
            UndoRedoService.SetName(transaction, "Move control point");
        }
    }

    void IResizingTarget.OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        OnResizingDelta(direction, horizontalChange, verticalChange);
    }

    void IResizingTarget.OnResizingStarted(ResizingDirection direction)
    {
        SynchronizePoint();
        startingPoint = ActualPoint;
    }

    protected virtual void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        if (Math.Abs(horizontalChange) + Math.Abs(verticalChange) < MathUtil.ZeroToleranceDouble)
            return;

        var vector = Curve.InverseTransformPoint(startingPoint + new Vector2((float)horizontalChange, (float)verticalChange));
        CoerceResizing(ref vector, false);
        Key = vector.X;
        Value = vector.Y;
    }

    protected virtual void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
    {
        var changeVector = Curve.InverseTransformVector(new Vector2((float)horizontalChange, (float)verticalChange));
        CoerceResizing(ref changeVector, true);
        if (direction == ResizingDirection.Center)
        {
            Point = Point + changeVector;
        }
    }

    protected virtual void CoerceResizing(ref Vector2 changeVector, bool isDelta)
    {
    }

    [NotNull]
    protected MemberGraphNodeBinding<TValue> CreateBinding<TValue>(IMemberNode node, string propertyName)
    {
        return new MemberGraphNodeBinding<TValue>(node, propertyName, OnPropertyChanging, OnPropertyChanged, UndoRedoService);
    }

    protected override void OnPropertyChanged([NotNull] params string[] propertyNames)
    {
        base.OnPropertyChanged(propertyNames);

        if (propertyNames.Any(p => string.Equals(p, nameof(Key), StringComparison.Ordinal) || string.Equals(p, nameof(Value), StringComparison.Ordinal)))
        {
            IsSynchronized = false;
            SynchronizePoint();
        }
    }

    public void SynchronizePoint()
    {
        if (IsSynchronized)
        {
            return;
        }

        OnPropertyChanging(nameof(Point));
        point = new Vector2((float)Key, (float)Value);
        IsSynchronized = true;
        OnPropertyChanged(nameof(Point));
    }
}
