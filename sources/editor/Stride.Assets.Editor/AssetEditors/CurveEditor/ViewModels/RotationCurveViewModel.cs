// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Animations;

namespace Stride.Assets.Editor.ViewModels;

public enum RotationDisplayMode
{
    Quaternion,
    Euler,
    AxisAngle
}

public sealed class RotationCurveViewModel : CurveViewModelBase<Quaternion>
{
    private readonly struct RotationData
    {
        public RotationData(float t, float x, float y, float z, float w)
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

    private readonly List<RotationData> sampleData = new();
    private RotationDisplayMode displayMode = RotationDisplayMode.Euler;

    public RotationCurveViewModel([NotNull] CurveEditorViewModel editor, [NotNull] IComputeCurve<Quaternion> computeCurve, CurveViewModelBase? parent = null, string? name = null)
        : base(editor, parent, computeCurve, name)
    {
    }

    public RotationDisplayMode DisplayMode
    {
        get => displayMode;
        set => SetValue(ref displayMode, value, Refresh);
    }

    public override IEnumerable<CurveRenderSeries> GetRenderSeries()
    {
        var lineCount = DisplayMode == RotationDisplayMode.Euler ? 3 : 4;
        for (var i = 0; i < lineCount; ++i)
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

            Vector3 vector;
            switch (DisplayMode)
            {
                case RotationDisplayMode.Quaternion:
                    sampleData.Add(new RotationData(ft, sample.X, sample.Y, sample.Z, sample.W));
                    break;

                case RotationDisplayMode.Euler:
                    Matrix.RotationQuaternion(ref sample, out var matrix);
                    DecomposeXYZ(matrix, out vector);
                    sampleData.Add(new RotationData(ft, MathUtil.RadiansToDegrees(vector.X), MathUtil.RadiansToDegrees(vector.Y), MathUtil.RadiansToDegrees(vector.Z), float.NaN));
                    break;

                case RotationDisplayMode.AxisAngle:
                    vector = sample.Axis;
                    vector.Normalize();
                    sampleData.Add(new RotationData(ft, vector.X, vector.Y, vector.Z, MathUtil.RadiansToDegrees(sample.Angle)));
                    break;

                default:
                    continue;
            }
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

    private static void DecomposeXYZ(in Matrix matrix, out Vector3 rotation)
    {
        var rotY = MathUtil.IsOne(Math.Abs(matrix.M13)) ? Math.Acos(Math.Max(Math.Abs(matrix.M11), Math.Abs(matrix.M33))) : Math.Asin(-matrix.M13);
        rotation.Y = (float)(MathF.Sign(-matrix.M13) * rotY);
        var test = Math.Cos(rotY);
        if (test > MathUtil.ZeroTolerance)
        {
            rotation.Z = MathF.Atan2(matrix.M12, matrix.M11);
            rotation.X = MathF.Atan2(matrix.M23, matrix.M33);
        }
        else
        {
            if (matrix.M13 >= 0)
            {
                rotation.Z = MathF.Atan2(-matrix.M32, matrix.M22);
                rotation.X = 0.0f;
            }
            else
            {
                rotation.Z = -MathF.Atan2(-matrix.M32, matrix.M22);
                rotation.X = 0.0f;
            }
        }
    }
}
