// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public abstract class ColorComponentCurveViewModel<TValue, TKeyFrameControlPointViewModel> : KeyFrameCurveViewModel<TValue>
    where TValue : struct
    where TKeyFrameControlPointViewModel : KeyFrameControlPointViewModel<TValue>
{
    protected ColorComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent, [NotNull] ComputeAnimationCurve<TValue> computeCurve, ColorComponent component)
        : base(editor, parent, computeCurve)
    {
        Component = component;
        Color = Editor.GetColor((VectorComponent)component);
    }

    public ColorComponent Component { get; }

    public sealed override string DisplayName => Component.ToString();

    public sealed override void AddPoint(Vector2 point)
    {
        var realPoint = InverseTransformPoint(point);
        UnderlyingCurve?.UpdateChanges();
        var value = UnderlyingCurve?.Evaluate(realPoint.X) ?? default;
        UpdateComponent(ref value, realPoint);
        var keyFrame = new AnimationKeyFrame<TValue>
        {
            Key = realPoint.X,
            Value = value,
        };
        var index = GetInsertIndex(realPoint);
        KeyFramesNode.Add(keyFrame, index);
    }

    protected abstract void UpdateComponent(ref TValue value, Vector2 realPoint);

    protected sealed override KeyFrameControlPointViewModel<TValue> CreateKeyFrameControlPoint(IMemberNode keyNode, IMemberNode valueNode, IMemberNode tangentTypeNode)
    {
        return (TKeyFrameControlPointViewModel)Activator.CreateInstance(typeof(TKeyFrameControlPointViewModel), this, keyNode, valueNode, tangentTypeNode, Component)!;
    }
}

public sealed class Color4ComponentCurveViewModel : ColorComponentCurveViewModel<Color4, Color4ComponentControlPointViewModel>
{
    public Color4ComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent, [NotNull] ComputeAnimationCurve<Color4> computeCurve, ColorComponent component)
        : base(editor, parent, computeCurve, component)
    {
    }

    protected override void UpdateComponent(ref Color4 value, Vector2 realPoint)
    {
        switch (Component)
        {
            case ColorComponent.R:
                value.R = realPoint.Y;
                break;
            case ColorComponent.G:
                value.G = realPoint.Y;
                break;
            case ColorComponent.B:
                value.B = realPoint.Y;
                break;
            case ColorComponent.A:
                value.A = realPoint.Y;
                break;
            default:
                throw new NotSupportedException();
        }
    }

    protected override IEnumerable<Vector2> SampleControlPoints([NotNull] KeyFrameControlPointViewModel<Color4> point1, KeyFrameControlPointViewModel<Color4> point2)
    {
        if (!point1.IsSynchronized || !point2.IsSynchronized || UnderlyingCurve == null || XAxis == null)
        {
            yield return point1.Point;
            yield break;
        }

        if (!GetSamplingParameters(XAxis, out var start, out var end, out var increment))
        {
            yield return point1.Point;
            yield break;
        }

        start = Math.Max(start, point1.ActualKey);
        end = Math.Min(end, point2.ActualKey);

        UnderlyingCurve.UpdateChanges();
        for (var t = start; t < end; t += increment)
        {
            var ft = (float)t;
            var sample = UnderlyingCurve.Evaluate(ft);
            var val = Component switch
            {
                ColorComponent.R => sample.R,
                ColorComponent.G => sample.G,
                ColorComponent.B => sample.B,
                _ => sample.A
            };
            yield return new Vector2(ft, val);
        }
    }
}
