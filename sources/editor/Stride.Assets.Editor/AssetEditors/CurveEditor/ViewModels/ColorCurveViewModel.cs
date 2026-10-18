// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public sealed class Color4CurveViewModel : CurveViewModelBase<Color4>
{
    private readonly struct ColorData
    {
        public ColorData(float t, float r, float g, float b, float a)
        {
            T = t;
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public float T { get; }
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public float this[int index] => index switch
        {
            0 => R,
            1 => G,
            2 => B,
            3 => A,
            _ => T
        };
    }

    private readonly List<ColorData> sampleData = new();

    public Color4CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Color4> computeCurve, CurveViewModelBase? parent = null, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public override IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        for (var i = 0; i < 4; ++i)
        {
            var componentIndex = i;
            var series = new CurveRenderSeries
            {
                Name = ((ColorComponent)i).ToString(),
                Color = Editor.GetColor((VectorComponent)i)
            };
            series.Points.AddRange(sampleData.Select(d => TransformPoint(new Vector2(d.T, d[componentIndex]))));
            yield return series;
        }
    }

    protected internal override void UpdateData()
    {
        var curve = UnderlyingCurve;
        if (curve == null || XAxis == null)
            return;

        if (!GetSamplingParameters(XAxis, out var start, out var end, out var increment))
            return;

        curve.UpdateChanges();
        sampleData.Clear();

        for (var t = start; t < end; t += increment)
        {
            var ft = (float)t;
            var sample = curve.Evaluate(ft);
            sampleData.Add(new ColorData(ft, sample.R, sample.G, sample.B, sample.A));
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

        foreach (var data in sampleData)
        {
            var x = data.T;
            if (!XAxis?.IsValidValue(x) ?? true)
                continue;

            if (x < minx) minx = x;
            if (x > maxx) maxx = x;

            for (var i = 0; i < 4; ++i)
            {
                var y = data[i];
                if (!YAxis?.IsValidValue(y) ?? true)
                    continue;

                if (y < miny) miny = y;
                if (y > maxy) maxy = y;
            }
        }

        if (minx < double.MaxValue) MinX = minx;
        if (miny < double.MaxValue) MinY = miny;
        if (maxx > double.MinValue) MaxX = maxx;
        if (maxy > double.MinValue) MaxY = maxy;
    }
}
