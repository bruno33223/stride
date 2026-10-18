// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public sealed class RotationComponentCurveViewModel : KeyFrameCurveViewModel<Quaternion>
{
    public RotationComponentCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent, [NotNull] ComputeAnimationCurve<Quaternion> computeCurve, VectorComponent component)
        : base(editor, parent, computeCurve)
    {
        Component = component;
        Color = Editor.GetColor(component);
    }

    public VectorComponent Component { get; }

    public override string DisplayName => Component.ToString();

    public override void AddPoint(Vector2 point)
    {
        var realPoint = InverseTransformPoint(point);
        UnderlyingCurve?.UpdateChanges();
        var value = UnderlyingCurve?.Evaluate(realPoint.X) ?? default;
        switch (Component)
        {
            case VectorComponent.X:
                value.X = realPoint.Y;
                break;
            case VectorComponent.Y:
                value.Y = realPoint.Y;
                break;
            case VectorComponent.Z:
                value.Z = realPoint.Y;
                break;
            case VectorComponent.W:
                value.W = realPoint.Y;
                break;
            default:
                throw new NotSupportedException();
        }

        var keyFrame = new AnimationKeyFrame<Quaternion>
        {
            Key = realPoint.X,
            Value = value,
        };
        var index = GetInsertIndex(realPoint);
        KeyFramesNode.Add(keyFrame, index);
    }

    protected override KeyFrameControlPointViewModel<Quaternion> CreateKeyFrameControlPoint(IMemberNode keyNode, IMemberNode valueNode, IMemberNode tangentTypeNode)
    {
        return new RotationComponentControlPointViewModel(this, keyNode, valueNode, tangentTypeNode, Component);
    }

    protected override IEnumerable<Vector2> SampleControlPoints(KeyFrameControlPointViewModel<Quaternion> point1, KeyFrameControlPointViewModel<Quaternion> point2)
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
                VectorComponent.X => sample.X,
                VectorComponent.Y => sample.Y,
                VectorComponent.Z => sample.Z,
                _ => sample.W
            };
            yield return new Vector2(ft, val);
        }
    }
}
