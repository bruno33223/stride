// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Point = Avalonia.Point;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Animations;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Avalonia.Views.Behaviors;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestCurveControlPointsAndBehaviorsIntegration
{
    private class DummyCurveViewModel : CurveViewModelBase
    {
        public DummyCurveViewModel(CurveEditorViewModel editor) : base(editor, null)
        {
        }

        public override string DisplayName => "DummyCurve";

        public override IEnumerable<CurveRenderSeries> GetRenderSeries() => Enumerable.Empty<CurveRenderSeries>();
    }

    private static (CurveEditorViewModel editor, DummyCurveViewModel curve) CreateTestContext()
    {
        var services = new ViewModelServiceProvider();
        services.RegisterService(new NullDispatcherService());
        services.RegisterService(new UndoRedoService(50));

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        var editor = new CurveEditorViewModel(services, session);
        var curve = new DummyCurveViewModel(editor);
        return (editor, curve);
    }

    [Fact]
    public void TestVector2ComponentControlPointViewModel()
    {
        var (_, curve) = CreateTestContext();
        var container = new NodeContainer();
        var keyframe = new AnimationKeyFrame<Vector2>
        {
            Key = 1.0f,
            Value = new Vector2(10.0f, 20.0f),
            TangentType = AnimationKeyTangentType.Linear
        };

        var node = container.GetOrCreateNode(keyframe);
        var keyNode = node[nameof(AnimationKeyFrame<Vector2>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<Vector2>.Value)];
        var tangentNode = node[nameof(AnimationKeyFrame<Vector2>.TangentType)];

        // Component X
        var cpX = new Vector2ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.X);
        Assert.Equal(VectorComponent.X, cpX.Component);
        Assert.Equal(1.0, cpX.ActualKey);
        Assert.Equal(10.0, cpX.ActualValue);

        cpX.ActualValue = 99.0;
        Assert.Equal(99.0f, keyframe.Value.X);
        Assert.Equal(20.0f, keyframe.Value.Y);

        // Component Y
        var cpY = new Vector2ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Y);
        Assert.Equal(VectorComponent.Y, cpY.Component);
        Assert.Equal(20.0, cpY.ActualValue);

        cpY.ActualValue = 88.0;
        Assert.Equal(99.0f, keyframe.Value.X);
        Assert.Equal(88.0f, keyframe.Value.Y);

        // Unsupported components for Vector2
        Assert.Throws<ArgumentException>(() => new Vector2ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Z));
        Assert.Throws<ArgumentException>(() => new Vector2ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.W));
    }

    [Fact]
    public void TestVector3ComponentControlPointViewModel()
    {
        var (_, curve) = CreateTestContext();
        var container = new NodeContainer();
        var keyframe = new AnimationKeyFrame<Vector3>
        {
            Key = 2.0f,
            Value = new Vector3(1.0f, 2.0f, 3.0f),
            TangentType = AnimationKeyTangentType.Linear
        };

        var node = container.GetOrCreateNode(keyframe);
        var keyNode = node[nameof(AnimationKeyFrame<Vector3>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<Vector3>.Value)];
        var tangentNode = node[nameof(AnimationKeyFrame<Vector3>.TangentType)];

        var cpX = new Vector3ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.X);
        var cpY = new Vector3ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Y);
        var cpZ = new Vector3ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Z);

        Assert.Equal(1.0, cpX.ActualValue);
        Assert.Equal(2.0, cpY.ActualValue);
        Assert.Equal(3.0, cpZ.ActualValue);

        cpX.ActualValue = 11.0;
        cpY.ActualValue = 22.0;
        cpZ.ActualValue = 33.0;

        Assert.Equal(new Vector3(11.0f, 22.0f, 33.0f), keyframe.Value);

        // Unsupported component for Vector3
        Assert.Throws<ArgumentException>(() => new Vector3ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.W));
    }

    [Fact]
    public void TestVector4ComponentControlPointViewModel()
    {
        var (_, curve) = CreateTestContext();
        var container = new NodeContainer();
        var keyframe = new AnimationKeyFrame<Vector4>
        {
            Key = 3.0f,
            Value = new Vector4(1.0f, 2.0f, 3.0f, 4.0f),
            TangentType = AnimationKeyTangentType.Linear
        };

        var node = container.GetOrCreateNode(keyframe);
        var keyNode = node[nameof(AnimationKeyFrame<Vector4>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<Vector4>.Value)];
        var tangentNode = node[nameof(AnimationKeyFrame<Vector4>.TangentType)];

        var cpX = new Vector4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.X);
        var cpY = new Vector4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Y);
        var cpZ = new Vector4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Z);
        var cpW = new Vector4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.W);

        Assert.Equal(1.0, cpX.ActualValue);
        Assert.Equal(2.0, cpY.ActualValue);
        Assert.Equal(3.0, cpZ.ActualValue);
        Assert.Equal(4.0, cpW.ActualValue);

        cpX.ActualValue = 101.0;
        cpY.ActualValue = 102.0;
        cpZ.ActualValue = 103.0;
        cpW.ActualValue = 104.0;

        Assert.Equal(new Vector4(101.0f, 102.0f, 103.0f, 104.0f), keyframe.Value);
    }

    [Fact]
    public void TestColor4ComponentControlPointViewModel()
    {
        var (_, curve) = CreateTestContext();
        var container = new NodeContainer();
        var keyframe = new AnimationKeyFrame<Color4>
        {
            Key = 0.5f,
            Value = new Color4(0.1f, 0.2f, 0.3f, 0.4f),
            TangentType = AnimationKeyTangentType.Linear
        };

        var node = container.GetOrCreateNode(keyframe);
        var keyNode = node[nameof(AnimationKeyFrame<Color4>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<Color4>.Value)];
        var tangentNode = node[nameof(AnimationKeyFrame<Color4>.TangentType)];

        var cpR = new Color4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, ColorComponent.R);
        var cpG = new Color4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, ColorComponent.G);
        var cpB = new Color4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, ColorComponent.B);
        var cpA = new Color4ComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, ColorComponent.A);

        Assert.Equal(0.1f, (float)cpR.ActualValue, precision: 4);
        Assert.Equal(0.2f, (float)cpG.ActualValue, precision: 4);
        Assert.Equal(0.3f, (float)cpB.ActualValue, precision: 4);
        Assert.Equal(0.4f, (float)cpA.ActualValue, precision: 4);

        cpR.ActualValue = 0.9f;
        cpG.ActualValue = 0.8f;
        cpB.ActualValue = 0.7f;
        cpA.ActualValue = 0.6f;

        Assert.Equal(new Color4(0.9f, 0.8f, 0.7f, 0.6f), keyframe.Value);
    }

    [Fact]
    public void TestRotationComponentControlPointViewModel()
    {
        var (_, curve) = CreateTestContext();
        var container = new NodeContainer();
        var keyframe = new AnimationKeyFrame<Quaternion>
        {
            Key = 0.0f,
            Value = new Quaternion(0.1f, 0.2f, 0.3f, 0.4f),
            TangentType = AnimationKeyTangentType.Linear
        };

        var node = container.GetOrCreateNode(keyframe);
        var keyNode = node[nameof(AnimationKeyFrame<Quaternion>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<Quaternion>.Value)];
        var tangentNode = node[nameof(AnimationKeyFrame<Quaternion>.TangentType)];

        var cpX = new RotationComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.X);
        var cpY = new RotationComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Y);
        var cpZ = new RotationComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.Z);
        var cpW = new RotationComponentControlPointViewModel(curve, keyNode, valueNode, tangentNode, VectorComponent.W);

        Assert.Equal(0.1f, (float)cpX.ActualValue, precision: 4);
        Assert.Equal(0.2f, (float)cpY.ActualValue, precision: 4);
        Assert.Equal(0.3f, (float)cpZ.ActualValue, precision: 4);
        Assert.Equal(0.4f, (float)cpW.ActualValue, precision: 4);

        cpX.ActualValue = 0.5f;
        cpY.ActualValue = 0.6f;
        cpZ.ActualValue = 0.7f;
        cpW.ActualValue = 0.8f;

        Assert.Equal(new Quaternion(0.5f, 0.6f, 0.7f, 0.8f), keyframe.Value);
    }

    [Fact]
    public void TestFloatControlPointViewModel()
    {
        var (_, curve) = CreateTestContext();
        var container = new NodeContainer();
        var keyframe = new AnimationKeyFrame<float>
        {
            Key = 1.25f,
            Value = 42.5f,
            TangentType = AnimationKeyTangentType.Flat
        };

        var node = container.GetOrCreateNode(keyframe);
        var keyNode = node[nameof(AnimationKeyFrame<float>.Key)];
        var valueNode = node[nameof(AnimationKeyFrame<float>.Value)];
        var tangentNode = node[nameof(AnimationKeyFrame<float>.TangentType)];

        var cp = new FloatControlPointViewModel(curve, keyNode, valueNode, tangentNode);
        Assert.Equal(1.25, cp.ActualKey);
        Assert.Equal(42.5, cp.ActualValue);
        Assert.Equal(AnimationKeyTangentType.Flat, cp.TangentType);

        cp.ActualValue = 84.0;
        Assert.Equal(84.0f, keyframe.Value);

        cp.TangentType = AnimationKeyTangentType.Smooth;
        Assert.Equal(AnimationKeyTangentType.Smooth, keyframe.TangentType);
    }

    [Fact]
    public void TestAxisBehaviorModifiersAndDefaults()
    {
        var behavior = new AxisPaneBehavior();
        Assert.Null(behavior.DrawingView);
        Assert.Null(behavior.XAxis);
        Assert.Null(behavior.YAxis);
        Assert.Equal(KeyModifiers.None, behavior.XModifiers);
        Assert.Equal(KeyModifiers.None, behavior.YModifiers);
        Assert.Equal(MouseButton.Middle, behavior.MouseButton);

        // This call will fail in RED stage because HasXModifiers throws NotImplementedException
        behavior.CurrentModifiers = KeyModifiers.None;
        Assert.True(behavior.HasXModifiers());
        Assert.True(behavior.HasYModifiers());

        behavior.XModifiers = KeyModifiers.Control;
        Assert.False(behavior.HasXModifiers());

        behavior.CurrentModifiers = KeyModifiers.Control;
        Assert.True(behavior.HasXModifiers());
    }

    [Fact]
    public void TestAxisPaneBehaviorPanningIntegration()
    {
        var xAxis = new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 100 };
        var yAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100 };
        var bounds = new RectangleF(0, 0, 500, 500);
        xAxis.UpdateTransform(bounds);
        yAxis.UpdateTransform(bounds);

        var canvas = new Canvas();
        var behavior = new AxisPaneBehavior
        {
            XAxis = xAxis,
            YAxis = yAxis,
            DrawingView = canvas,
            XModifiers = KeyModifiers.None,
            YModifiers = KeyModifiers.None
        };

        // Panning call - will fail in RED stage
        behavior.Pan(new Point(100, 100), new Point(150, 120));

        // When pan is implemented, axis actual limits change
        Assert.NotEqual(0.0, xAxis.ActualMinimum);
    }

    [Fact]
    public void TestAxisZoomBehaviorZoomingIntegration()
    {
        var xAxis = new LinearAxis { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 100 };
        var yAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100 };
        var bounds = new RectangleF(0, 0, 500, 500);
        xAxis.UpdateTransform(bounds);
        yAxis.UpdateTransform(bounds);

        var canvas = new Canvas();
        var behavior = new AxisZoomBehavior
        {
            XAxis = xAxis,
            YAxis = yAxis,
            DrawingView = canvas,
            XModifiers = KeyModifiers.None,
            YModifiers = KeyModifiers.None
        };

        // Will fail in RED stage
        behavior.ZoomFactorIn();
        Assert.Equal(1.25, behavior.ZoomFactor);

        behavior.ZoomFactorOut();
        Assert.Equal(0.8, behavior.ZoomFactor);

        var invPoint = behavior.InverseTransform(new Point(250, 250));
        Assert.True(invPoint.X >= 0 && invPoint.Y >= 0);
    }

    [Fact]
    public void TestCurveCanvasControlDrawingViewIntegration()
    {
        var canvas = new CurveCanvasControl();
        // CurveCanvasControl should provide InvalidateDrawing() to integrate with behaviors
        var invMethod = typeof(CurveCanvasControl).GetMethod("InvalidateDrawing");
        Assert.NotNull(invMethod);
        invMethod.Invoke(canvas, null);

        var behavior = new AxisPaneBehavior
        {
            DrawingView = canvas
        };
        Assert.Same(canvas, behavior.DrawingView);
    }
}
