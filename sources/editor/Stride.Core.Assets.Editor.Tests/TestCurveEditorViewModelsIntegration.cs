// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Animations;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Solutions;
using Stride.UI;
using Thickness = Stride.Assets.Editor.ViewModels.Thickness;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestCurveEditorViewModelsIntegration
{
    private sealed class TestPluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => [];
        public void EnsureInitialized(ILogger logger) { }
        public Type? GetAssetViewModelType(Type assetType) => null;
        public Type? GetEditorViewModelType(Type viewModelType) => null;
        public Type? GetEditorViewType(Type editorViewModelType) => null;
        public Type? GetPreviewViewModelType(Type previewType) => null;
        public Type? GetPreviewViewType(Type previewType) => null;
        public IReadOnlyList<Type> GetPrimitiveTypes() => [];
    }

    private sealed class TestDebugService : IEditorDebugService
    {
        public IDebugPage? CreateLogDebugPage(Logger logger, string title, bool register = true) => null;
        public IDebugPage? CreateUndoRedoDebugPage(IUndoRedoService actionService, string title, bool register = true) => null;
        public IDebugPage? CreateAssetNodesDebugPage(SessionViewModel session, string title, bool register = true) => null;
        public void RegisterDebugPage(IDebugPage? page) { }
        public void UnregisterDebugPage(IDebugPage? page) { }
    }

    private sealed class TestMainViewModel : IMainViewModel
    {
        public StatusViewModel Status { get; }
        public TestMainViewModel(IViewModelServiceProvider sp) { Status = new StatusViewModel(sp); }
    }

    private sealed class MockEditorDialogService : IEditorDialogService
    {
        public bool HasMainWindow => false;
        public void Exit(int exitCode = 0) { }
        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons, MessageBoxImage image)
            => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
            => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
        public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons, MessageBoxImage image)
            => Task.FromResult(MessageBoxResult.OK);
        public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
            => Task.FromResult(0);
        public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<UFile?>(null);
        public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<IReadOnlyList<UFile>>([]);
        public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null)
            => Task.FromResult<UDirectory?>(null);
        public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
            => Task.FromResult<UFile?>(null);
        public void ShowProgressWindow(WorkProgressViewModel workProgress) { }
        public INewProjectDialog CreateNewProjectDialog(SessionViewModel session) => null!;
        public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null) => null!;
    }

    private class MockDrawingView : IDrawingView
    {
        public IDrawingModel? Model { get; set; }
        public bool InvalidateDrawingCalled { get; private set; }

        public void InvalidateDrawing()
        {
            InvalidateDrawingCalled = true;
        }
    }

    private class MockDrawingContext : IDrawingContext
    {
        public List<RectangleF> DrawnRectangles { get; } = new();

        public void DrawLine(Vector2 p1, Vector2 p2, Color strokeColor) { }

        public void DrawLineSegments(IList<Vector2> points, Color strokeColor, double thickness = 1) { }

        public void DrawRectangle(RectangleF rect, Color fillColor, Color strokeColor)
        {
            DrawnRectangles.Add(rect);
        }

        public void DrawTexts(
            IList<Vector2> points,
            Color color,
            IList<string> texts,
            FontFamily fontFamily,
            double fontSize,
            FontWeight fontWeight,
            HorizontalAlignment hAlign = HorizontalAlignment.Left,
            VerticalAlignment vAlign = VerticalAlignment.Top) { }

        public Size2F MeasureTexts(IList<string> texts, FontFamily fontFamily, double fontSize, FontWeight fontWeight)
        {
            return new Size2F(50, 15);
        }
    }

    private static PackageSession CreateMockPackageSession()
    {
        var session = (PackageSession)RuntimeHelpers.GetUninitializedObject(typeof(PackageSession));

        typeof(PackageSession).GetField("dependenciesLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new object());

        typeof(PackageSession).GetField("packages", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new PackageCollection());

        typeof(PackageSession).GetField("packagesCopy", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, new Dictionary<Package, Package>());

        var upgradesField = typeof(PackageSession).GetField("pendingPackageUpgradesPerPackage", BindingFlags.Instance | BindingFlags.NonPublic);
        if (upgradesField != null)
        {
            upgradesField.SetValue(session, Activator.CreateInstance(upgradesField.FieldType));
        }

        var constraintField = typeof(PackageSession).GetField("constraintProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        if (constraintField != null)
        {
            constraintField.SetValue(session, Activator.CreateInstance(constraintField.FieldType));
        }

        var solution = new Solution { FullPath = "TestSolution.sln" };
        typeof(PackageSession).GetField("VSSolution", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, solution);

        var projectsField = typeof(PackageSession).GetField("<Projects>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        projectsField?.SetValue(session, new ProjectCollection());

        var assemblyContainerField = typeof(PackageSession).GetField("<AssemblyContainer>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        assemblyContainerField?.SetValue(session, new AssemblyContainer());

        var depMgrCtor = typeof(AssetDependencyManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            [typeof(PackageSession)],
            null);
        var depMgr = depMgrCtor!.Invoke([session]);
        typeof(PackageSession).GetField("dependencies", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, depMgr);

        return session;
    }

    private static (SessionViewModel sessionVm, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new UndoRedoService(10));
        sp.RegisterService(new TestPluginService());
        sp.RegisterService(new TestDebugService());
        sp.RegisterService(new MockEditorDialogService());

        var packageSession = CreateMockPackageSession();
        var mainVm = new TestMainViewModel(sp);
        var logger = new LoggerResult();

        var ctor = typeof(SessionViewModel).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IViewModelServiceProvider), typeof(PackageSession), typeof(IMainViewModel), typeof(ILogger)],
            null);

        var sessionVm = (SessionViewModel)ctor!.Invoke([sp, packageSession, mainVm, logger]);
        return (sessionVm, sp);
    }

    private static CurveEditorViewModel CreateCurveEditor()
    {
        var (sessionVm, sp) = CreateTestSession();
        return new CurveEditorViewModel(sp, sessionVm);
    }

    [Fact]
    public void TestCurveEditorImplementsDrawingModelAndExposesDrawingProperties()
    {
        var vm = CreateCurveEditor();

        // TDD Iron Law: CurveEditorViewModel must implement IDrawingModel from CurveEditorViewModel.DrawingModel.cs
        var drawingModel = Assert.IsAssignableFrom<IDrawingModel>(vm);

        // Check drawing layout properties
        vm.AxisTierDistance = 12.0;
        Assert.Equal(12.0, vm.AxisTierDistance);

        vm.Padding = new Thickness(8, 10, 8, 10);
        Assert.Equal(new Thickness(8, 10, 8, 10), vm.Padding);

        // Auto-margins initialized to NaN
        Assert.True(double.IsNaN(vm.CurveAreaMargins.Left));

        // GetColor for vector components
        Assert.Equal(new Color(220, 78, 78), vm.GetColor(VectorComponent.X));
        Assert.Equal(new Color(101, 195, 106), vm.GetColor(VectorComponent.Y));
        Assert.Equal(new Color(63, 116, 209), vm.GetColor(VectorComponent.Z));
        Assert.Equal(new Color(188, 188, 188), vm.GetColor(VectorComponent.W));

        // Attach & Detach IDrawingView
        var view = new MockDrawingView();
        drawingModel.Attach(view);
        vm.InvalidateView();
        Assert.True(view.InvalidateDrawingCalled);

        drawingModel.Detach(view);

        // Render & Update lifecycle via IDrawingModel
        var context = new MockDrawingContext();
        drawingModel.Render(context, 800, 600);
        drawingModel.Update(true);

        Assert.True(vm.Width > 0);
        Assert.True(vm.Height > 0);
    }

    [Fact]
    public void TestMultiDimensionalCurveFactoryCreatesCorrectHierarchies()
    {
        var vm = CreateCurveEditor();

        // Vector2 curve
        var v2Curve = new ComputeAnimationCurveVector2();
        vm.AddCurve(v2Curve, "V2Track");
        var selectedV2 = Assert.IsType<Vector2KeyFrameCurveViewModel>(vm.SelectedCurve);
        Assert.Equal("V2Track", selectedV2.DisplayName);
        Assert.Equal(2, selectedV2.Children.Count);
        Assert.IsType<Vector2ComponentCurveViewModel>(selectedV2.Children[0]);
        Assert.IsType<Vector2ComponentCurveViewModel>(selectedV2.Children[1]);
        Assert.Equal("X", selectedV2.Children[0].DisplayName);
        Assert.Equal("Y", selectedV2.Children[1].DisplayName);

        // Vector3 curve
        var v3Curve = new ComputeAnimationCurveVector3();
        vm.AddCurve(v3Curve, "V3Track");
        var selectedV3 = Assert.IsType<Vector3KeyFrameCurveViewModel>(vm.SelectedCurve);
        Assert.Equal("V3Track", selectedV3.DisplayName);
        Assert.Equal(3, selectedV3.Children.Count);
        Assert.IsType<Vector3ComponentCurveViewModel>(selectedV3.Children[0]);
        Assert.IsType<Vector3ComponentCurveViewModel>(selectedV3.Children[1]);
        Assert.IsType<Vector3ComponentCurveViewModel>(selectedV3.Children[2]);
        Assert.Equal("X", selectedV3.Children[0].DisplayName);
        Assert.Equal("Y", selectedV3.Children[1].DisplayName);
        Assert.Equal("Z", selectedV3.Children[2].DisplayName);

        // Vector4 curve
        var v4Curve = new ComputeAnimationCurveVector4();
        vm.AddCurve(v4Curve, "V4Track");
        var selectedV4 = Assert.IsType<Vector4KeyFrameCurveViewModel>(vm.SelectedCurve);
        Assert.Equal(4, selectedV4.Children.Count);
        Assert.Equal("X", selectedV4.Children[0].DisplayName);
        Assert.Equal("Y", selectedV4.Children[1].DisplayName);
        Assert.Equal("Z", selectedV4.Children[2].DisplayName);
        Assert.Equal("W", selectedV4.Children[3].DisplayName);

        // Quaternion curve
        var rotCurve = new ComputeAnimationCurveQuaternion();
        vm.AddCurve(rotCurve, "RotTrack");
        var selectedRot = Assert.IsType<RotationKeyFrameCurveViewModel>(vm.SelectedCurve);
        Assert.Equal(4, selectedRot.Children.Count);

        // Color4 curve
        var colCurve = new ComputeAnimationCurveColor4();
        vm.AddCurve(colCurve, "ColorTrack");
        var selectedCol = Assert.IsType<Color4KeyFrameCurveViewModel>(vm.SelectedCurve);
        Assert.Equal(4, selectedCol.Children.Count);

        // Float curve
        var flCurve = new ComputeAnimationCurveFloat();
        vm.AddCurve(flCurve, "FloatTrack");
        Assert.IsType<FloatKeyFrameCurveViewModel>(vm.SelectedCurve);
    }

    [Fact]
    public void TestVectorComponentCurvesValidationAndPointOperations()
    {
        var vm = CreateCurveEditor();
        var v2Curve = new ComputeAnimationCurveVector2();
        var v3Curve = new ComputeAnimationCurveVector3();

        // Rejection of invalid components
        Assert.Throws<ArgumentException>(() => new Vector2ComponentCurveViewModel(vm, null, v2Curve, VectorComponent.Z));
        Assert.Throws<ArgumentException>(() => new Vector2ComponentCurveViewModel(vm, null, v2Curve, VectorComponent.W));
        Assert.Throws<ArgumentException>(() => new Vector3ComponentCurveViewModel(vm, null, v3Curve, VectorComponent.W));

        // Valid component creation
        var v2X = new Vector2ComponentCurveViewModel(vm, null, v2Curve, VectorComponent.X);
        Assert.Equal(VectorComponent.X, v2X.Component);
        Assert.Equal("X", v2X.DisplayName);
        Assert.Equal(vm.GetColor(VectorComponent.X), v2X.Color);
    }
}
