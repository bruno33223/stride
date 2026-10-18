// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public sealed class FloatKeyFrameCurveViewModel : KeyFrameCurveViewModel<float>
{
    public FloatKeyFrameCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] ComputeAnimationCurve<float> computeCurve, CurveViewModelBase? parent = null, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public override void AddPoint(Vector2 point)
    {
        var realPoint = InverseTransformPoint(point);
        var keyFrame = new AnimationKeyFrame<float>
        {
            Key = realPoint.X,
            Value = realPoint.Y,
        };
        var index = GetInsertIndex(realPoint);
        KeyFramesNode.Add(keyFrame, index);
    }

    [NotNull]
    protected override KeyFrameControlPointViewModel<float> CreateKeyFrameControlPoint(IMemberNode keyNode, IMemberNode valueNode, IMemberNode tangentTypeNode)
    {
        return new FloatControlPointViewModel(this, keyNode, valueNode, tangentTypeNode);
    }

    protected override IEnumerable<Vector2> SampleControlPoints([NotNull] KeyFrameControlPointViewModel<float> point1, KeyFrameControlPointViewModel<float> point2)
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
            yield return new Vector2(ft, UnderlyingCurve.Evaluate(ft));
        }
    }
}
