// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.UI;

namespace Stride.Core.Assets.Editor.Tests;

public class TestCurveAxesAndRenderingIntegration
{
    private static readonly Assembly AssetsEditorAssembly = typeof(AxisBase).Assembly;

    private class SpyDrawingContext : IDrawingContext
    {
        public List<(Vector2 P1, Vector2 P2, Color Color)> Lines { get; } = new();
        public List<(IList<Vector2> Points, Color Color, double Thickness)> LineSegments { get; } = new();
        public List<RectangleF> Rectangles { get; } = new();
        public List<(IList<Vector2> Points, IList<string> Texts)> Texts { get; } = new();

        public void DrawLine(Vector2 p1, Vector2 p2, Color strokeColor)
        {
            Lines.Add((p1, p2, strokeColor));
        }

        public void DrawLineSegments(IList<Vector2> points, Color strokeColor, double thickness = 1.0)
        {
            LineSegments.Add((new List<Vector2>(points), strokeColor, thickness));
        }

        public void DrawRectangle(RectangleF rect, Color fillColor, Color strokeColor)
        {
            Rectangles.Add(rect);
        }

        public void DrawTexts(
            IList<Vector2> points,
            Color color,
            IList<string> texts,
            FontFamily fontFamily,
            double fontSize,
            FontWeight fontWeight,
            HorizontalAlignment hAlign = HorizontalAlignment.Left,
            VerticalAlignment vAlign = VerticalAlignment.Top)
        {
            Texts.Add((new List<Vector2>(points), new List<string>(texts)));
        }

        public Size2F MeasureTexts(IList<string> texts, FontFamily fontFamily, double fontSize, FontWeight fontWeight)
        {
            return new Size2F(30.0f, 12.0f);
        }
    }

    [Fact]
    public void TestHorizontalAndVerticalAxisRendererExists()
    {
        var rendererType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.ViewModels.HorizontalAndVerticalAxisRenderer");
        Assert.NotNull(rendererType);

        var ctor = rendererType.GetConstructor(new[] { typeof(CurveEditorViewModel), typeof(IDrawingContext) });
        Assert.NotNull(ctor);

        var renderMethod = rendererType.GetMethod("Render", new[] { typeof(AxisBase), typeof(int) });
        Assert.NotNull(renderMethod);
    }

    [Fact]
    public void TestHorizontalAndVerticalAxisRendererDrawingExecution()
    {
        var editor = (CurveEditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(CurveEditorViewModel));
        typeof(CurveEditorViewModel).GetField("curveArea", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, new RectangleF(50, 50, 700, 500));
        editor.AxisTierDistance = 0;

        var spyContext = new SpyDrawingContext();
        var renderer = new HorizontalAndVerticalAxisRenderer(editor, spyContext);

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 10,
            TickStyle = TickStyle.Outside,
            MajorTickSize = 6,
            MinorTickSize = 3
        };
        xAxis.UpdateTransform(editor.CurveArea);
        xAxis.UpdateIntervals(editor.CurveArea);

        // Pass 0: Minor items
        renderer.Render(xAxis, 0);
        Assert.NotEmpty(spyContext.LineSegments);

        // Pass 1: Major items, labels and axis line
        renderer.Render(xAxis, 1);
        Assert.NotEmpty(spyContext.Lines);
        Assert.NotEmpty(spyContext.Texts);

        // Render Vertical Axis
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = -5,
            Maximum = 5,
            TickStyle = TickStyle.Crossing,
            MajorTickSize = 8,
            MinorTickSize = 4
        };
        yAxis.UpdateTransform(editor.CurveArea);
        yAxis.UpdateIntervals(editor.CurveArea);

        renderer.Render(yAxis, 0);
        renderer.Render(yAxis, 1);
        Assert.True(spyContext.Texts.Count >= 2);
    }

    [Fact]
    public void TestAxisBaseMeasure()
    {
        var axis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 100,
            MajorTickSize = 6,
            AxisDistance = 2,
            AxisTickToLabelDistance = 4
        };
        axis.UpdateActualMaxMin();
        axis.UpdateIntervals(new RectangleF(0, 0, 800, 600));

        var spy = new SpyDrawingContext();
        var measured = axis.Measure(spy);

        Assert.True(measured.Height > 0);
    }

    [Fact]
    public void TestAxisChangedEventArgsComplete()
    {
        var zoomArgs = new AxisChangedEventArgs(AxisChangeTypes.Zoom, -1.5, 2.5);
        Assert.Equal(AxisChangeTypes.Zoom, zoomArgs.ChangeType);
        Assert.Equal(-1.5, zoomArgs.DeltaMinimum);
        Assert.Equal(2.5, zoomArgs.DeltaMaximum);

        var panArgs = new AxisChangedEventArgs(AxisChangeTypes.Pan, 10.0, 10.0);
        Assert.Equal(AxisChangeTypes.Pan, panArgs.ChangeType);
        Assert.Equal(10.0, panArgs.DeltaMinimum);
        Assert.Equal(10.0, panArgs.DeltaMaximum);

        var resetArgs = new AxisChangedEventArgs(AxisChangeTypes.Reset, 0.0, 0.0);
        Assert.Equal(AxisChangeTypes.Reset, resetArgs.ChangeType);
    }

    [Fact]
    public void TestLinearAxisTransforms()
    {
        var axis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 100
        };

        var bounds = new RectangleF(0, 0, 800, 600);
        axis.UpdateTransform(bounds);
        axis.UpdateIntervals(bounds);

        Assert.True(axis.IsXyAxis);
        Assert.True(axis.IsHorizontal());
        Assert.False(axis.IsVertical());

        var t0 = axis.Transform(0);
        var t100 = axis.Transform(100);
        Assert.True(t100 > t0);

        var inv0 = axis.InverseTransform(t0);
        var inv100 = axis.InverseTransform(t100);
        Assert.InRange(Math.Abs(inv0 - 0.0), 0, 1e-5);
        Assert.InRange(Math.Abs(inv100 - 100.0), 0, 1e-5);
    }

    [Fact]
    public void TestLogarithmicAxisTransforms()
    {
        var logAxis = new LogarithmicAxis
        {
            Position = AxisPosition.Left,
            Minimum = 1,
            Maximum = 1000
        };

        var bounds = new RectangleF(0, 0, 800, 600);
        logAxis.UpdateTransform(bounds);
        logAxis.UpdateIntervals(bounds);

        Assert.True(logAxis.IsXyAxis);
        Assert.False(logAxis.IsHorizontal());
        Assert.True(logAxis.IsVertical());

        var t10 = logAxis.Transform(10);
        var inv10 = logAxis.InverseTransform(t10);
        Assert.InRange(Math.Abs(inv10 - 10.0), 0, 1e-4);
    }

    [Fact]
    public void TestAxisPanAndZoomInteractions()
    {
        var axis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 0,
            Maximum = 100
        };

        var bounds = new RectangleF(0, 0, 500, 300);
        axis.UpdateTransform(bounds);
        axis.UpdateIntervals(bounds);

        AxisChangedEventArgs? lastChanged = null;
        axis.AxisChanged += (_, e) => lastChanged = e;

        // Pan
        axis.Pan(50.0);
        Assert.NotNull(lastChanged);
        Assert.Equal(AxisChangeTypes.Pan, lastChanged.ChangeType);

        // Zoom at center
        lastChanged = null;
        axis.ZoomAtCenter(2.0);
        Assert.NotNull(lastChanged);
        Assert.Equal(AxisChangeTypes.Zoom, lastChanged.ChangeType);

        // Reset
        lastChanged = null;
        axis.Reset();
        Assert.NotNull(lastChanged);
        Assert.Equal(AxisChangeTypes.Reset, lastChanged.ChangeType);
    }

    [Fact]
    public void TestCreateTickValuesAndFormatting()
    {
        var ticks = AxisBase.CreateTickValues(0, 10, 2);
        Assert.Equal(6, ticks.Count);
        Assert.Equal(0.0, ticks[0]);
        Assert.Equal(2.0, ticks[1]);
        Assert.Equal(4.0, ticks[2]);
        Assert.Equal(6.0, ticks[3]);
        Assert.Equal(8.0, ticks[4]);
        Assert.Equal(10.0, ticks[5]);

        var axis = new LinearAxis { StringFormat = "0.00" };
        Assert.Equal("5.50", axis.FormatValue(5.5));
    }
}
