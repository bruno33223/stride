// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public sealed class FloatCurveViewModel : CurveViewModelBase<float>
{
    private readonly List<Vector2> samplePoints = new();

    public FloatCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<float> computeCurve, CurveViewModelBase? parent = null, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public override IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        var series = new CurveRenderSeries
        {
            Name = DisplayName,
            Color = Color
        };
        series.Points.AddRange(samplePoints.Select(TransformPoint));
        yield return series;
    }

    protected internal override void UpdateData()
    {
        var curve = UnderlyingCurve;
        if (curve == null || XAxis == null)
            return;

        if (!GetSamplingParameters(XAxis, out var start, out var end, out var increment))
            return;

        curve.UpdateChanges();
        samplePoints.Clear();

        for (var t = start; t < end; t += increment)
        {
            var ft = (float)t;
            samplePoints.Add(new Vector2(ft, curve.Evaluate(ft)));
        }
    }

    protected internal override void UpdateMaxMin()
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

        foreach (var point in samplePoints)
        {
            var x = point.X;
            var y = point.Y;

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
