// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Quantum;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public abstract class KeyFrameCurveViewModel<TValue> : EditableCurveViewModel<TValue>
    where TValue : struct
{
    private readonly ObservableList<KeyFrameControlPointViewModel<TValue>> controlPoints = new();

    protected KeyFrameCurveViewModel([NotNull] CurveEditorViewModel editor, CurveViewModelBase? parent, [NotNull] ComputeAnimationCurve<TValue> computeCurve, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
        KeyFramesNode = CurveNode[nameof(computeCurve.KeyFrames)].Target;
        KeyFramesNode.ItemChanged += KeyFramesContentChanged;
        controlPoints.CollectionChanged += ControlPointsCollectionChanged;
    }

    public override IReadOnlyObservableCollection<ControlPointViewModelBase> ControlPoints => controlPoints;

    protected IObjectNode KeyFramesNode { get; }

    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(KeyFrameCurveViewModel<TValue>));

        controlPoints.CollectionChanged -= ControlPointsCollectionChanged;
        foreach (var point in controlPoints)
        {
            point.PropertyChanged -= ControlPointPropertyChanged;
            point.Destroy();
        }
        controlPoints.Clear();
        KeyFramesNode.ItemChanged -= KeyFramesContentChanged;

        base.Destroy();
    }

    public override bool RemovePoint(ControlPointViewModelBase point)
    {
        if (point is not KeyFrameControlPointViewModel<TValue> kfcp)
            return false;

        var index = new NodeIndex(controlPoints.IndexOf(kfcp));
        if (index.Int == -1)
            return false;

        var kf = KeyFramesNode.Retrieve(index);
        KeyFramesNode.Remove(kf, index);
        return true;
    }

    protected virtual KeyFrameControlPointViewModel<TValue>? CreateControlPoint([NotNull] IObjectNode node)
    {
        var keyNode = node[nameof(AnimationKeyFrame<TValue>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<TValue>.Value)];
        var tangentTypeNode = node[nameof(AnimationKeyFrame<TValue>.TangentType)];
        return CreateKeyFrameControlPoint(keyNode, valueNode, tangentTypeNode);
    }

    protected abstract KeyFrameControlPointViewModel<TValue> CreateKeyFrameControlPoint([NotNull] IMemberNode keyNode, [NotNull] IMemberNode valueNode, [NotNull] IMemberNode tangentTypeNode);

    protected NodeIndex GetInsertIndex(Vector2 point)
    {
        var index = controlPoints.FindIndex(kf => (kf.IsSynchronized ? kf.ActualKey : kf.Key) > point.X);
        return new NodeIndex(index >= 0 ? index : controlPoints.Count);
    }

    protected sealed override void InitializeOverride()
    {
        if (KeyFramesNode.Retrieve() is ICollection<AnimationKeyFrame<TValue>> keyFrames)
        {
            KeyFrameControlPointViewModel<TValue>? previous = null;
            foreach (var keyFrame in keyFrames)
            {
                var node = Editor.Session.AssetNodeContainer.GetOrCreateNode(keyFrame);
                var controlPoint = CreateControlPoint(node);
                if (controlPoint != null)
                {
                    if (previous != null)
                    {
                        previous.Next = controlPoint;
                        controlPoint.Previous = previous;
                    }
                    previous = controlPoint;
                    controlPoints.Add(controlPoint);
                }
            }
        }
    }

    protected virtual void KeyFramesContentChanged(object? sender, ItemChangeEventArgs e)
    {
        if (!IsInitialized)
            return;

        if (!e.Index.IsInt)
            return;

        var index = e.Index.Int;
        KeyFrameControlPointViewModel<TValue>? controlPoint;
        switch (e.ChangeType)
        {
            case ContentChangeType.ValueChange:
            case ContentChangeType.CollectionUpdate:
                return;

            case ContentChangeType.CollectionAdd:
                var node = Editor.Session.AssetNodeContainer.GetNode(e.NewValue);
                if (node != null)
                {
                    controlPoint = CreateControlPoint(node);
                    if (controlPoint != null)
                    {
                        if (index > 0)
                        {
                            controlPoint.Previous = controlPoints[index - 1];
                            controlPoints[index - 1].Next = controlPoint;
                        }
                        if (index < controlPoints.Count)
                        {
                            controlPoint.Next = controlPoints[index];
                            controlPoints[index].Previous = controlPoint;
                        }
                        controlPoints.Insert(index, controlPoint);
                    }
                }
                break;

            case ContentChangeType.CollectionRemove:
                if (index >= 0 && index < controlPoints.Count)
                {
                    controlPoint = controlPoints[index];
                    if (index > 0)
                        controlPoints[index - 1].Next = controlPoint.Next;
                    if (index + 1 < controlPoints.Count)
                        controlPoints[index + 1].Previous = controlPoint.Previous;
                    controlPoints.RemoveAt(index);
                }
                break;

            default:
                break;
        }
    }

    public override IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        var count = controlPoints.Count;
        if (count == 0) yield break;

        var series = new CurveRenderSeries
        {
            Name = DisplayName,
            Color = Color
        };

        var sortedPoints = controlPoints.OrderBy(p => p.ActualPoint.X).ToList();
        for (var i = 0; i < count - 1; ++i)
        {
            var samples = SampleControlPoints(sortedPoints[i], sortedPoints[i + 1]);
            series.Points.AddRange(samples.Select(TransformPoint));
        }
        series.Points.Add(sortedPoints[count - 1].ActualPoint);

        yield return series;
    }

    protected virtual IEnumerable<Vector2> SampleControlPoints([NotNull] KeyFrameControlPointViewModel<TValue> point1, KeyFrameControlPointViewModel<TValue> point2)
    {
        yield return point1.Point;
    }

    protected internal sealed override void UpdateMaxMin()
    {
        base.UpdateMaxMin();

        var minx = MinX;
        var miny = MinY;
        var maxx = MaxX;
        var maxy = MaxY;

        if (double.IsNaN(minx)) minx = double.MaxValue;
        if (double.IsNaN(miny)) miny = double.MaxValue;
        if (double.IsNaN(maxx)) maxx = double.MinValue;
        if (double.IsNaN(maxy)) maxy = double.MinValue;

        foreach (var point in controlPoints)
        {
            var x = point.IsSynchronized ? point.ActualKey : point.Key;
            var y = point.IsSynchronized ? point.ActualValue : point.Value;

            if (!IsValidPoint(x, y))
                continue;

            if (x < minx) minx = x;
            if (x > maxx) maxx = x;
            if (y < miny) miny = y;
            if (y > maxy) maxy = y;
        }

        if (minx < double.MaxValue) MinX = minx;
        if (miny < double.MaxValue) MinY = miny;
        if (maxx > double.MinValue) MaxX = maxx;
        if (maxy > double.MinValue) MaxY = maxy;
    }
}
