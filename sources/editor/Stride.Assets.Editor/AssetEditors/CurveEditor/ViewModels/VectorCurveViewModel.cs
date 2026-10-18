// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public sealed class Vector2CurveViewModel : CurveViewModelBase<Vector2>
{
    private readonly struct VectorData
    {
        public VectorData(float t, float x, float y)
        {
            T = t;
            X = x;
            Y = y;
        }

        public float T { get; }
        public float X { get; }
        public float Y { get; }

        public float this[int index] => index switch
        {
            0 => X,
            1 => Y,
            _ => T
        };
    }

    private readonly List<VectorData> sampleData = new();

    public Vector2CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Vector2> computeCurve, CurveViewModelBase? parent = null, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public override IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        for (var i = 0; i < 2; ++i)
        {
            var componentIndex = i;
            var series = new CurveRenderSeries
            {
                Name = ((VectorComponent)i).ToString(),
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
            sampleData.Add(new VectorData(ft, sample.X, sample.Y));
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

            for (var i = 0; i < 2; ++i)
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

public sealed class Vector3CurveViewModel : CurveViewModelBase<Vector3>
{
    private readonly struct VectorData
    {
        public VectorData(float t, float x, float y, float z)
        {
            T = t;
            X = x;
            Y = y;
            Z = z;
        }

        public float T { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public float this[int index] => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            _ => T
        };
    }

    private readonly List<VectorData> sampleData = new();

    public Vector3CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Vector3> computeCurve, CurveViewModelBase? parent = null, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public override IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        for (var i = 0; i < 3; ++i)
        {
            var componentIndex = i;
            var series = new CurveRenderSeries
            {
                Name = ((VectorComponent)i).ToString(),
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
            sampleData.Add(new VectorData(ft, sample.X, sample.Y, sample.Z));
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

            for (var i = 0; i < 3; ++i)
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

public sealed class Vector4CurveViewModel : CurveViewModelBase<Vector4>
{
    private readonly struct VectorData
    {
        public VectorData(float t, float x, float y, float z, float w)
        {
            T = t;
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float T { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }

        public float this[int index] => index switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            3 => W,
            _ => T
        };
    }

    private readonly List<VectorData> sampleData = new();

    public Vector4CurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Vector4> computeCurve, CurveViewModelBase? parent = null, string? name = null)
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
                Name = ((VectorComponent)i).ToString(),
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
            sampleData.Add(new VectorData(ft, sample.X, sample.Y, sample.Z, sample.W));
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
