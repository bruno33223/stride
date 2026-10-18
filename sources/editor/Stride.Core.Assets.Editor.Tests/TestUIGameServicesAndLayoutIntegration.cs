// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.Game;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Game;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Thickness = Stride.UI.Thickness;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestUIGameServicesAndLayoutIntegration
    {
        private static T SetTestSize<T>(T element, float width, float height, float availableWidth = 1920f, float availableHeight = 1080f) where T : UIElement
        {
            element.Width = width;
            element.Height = height;
            element.HorizontalAlignment = HorizontalAlignment.Left;
            element.VerticalAlignment = VerticalAlignment.Top;
            element.Measure(new Vector3(availableWidth, availableHeight, 0));
            element.Arrange(new Vector3(availableWidth, availableHeight, 0), false);
            return element;
        }
        [Fact]
        public void TestRequiredTypesExistAndHierarchyParity()
        {
            // a) UIEditorGameAdornerService
            Assert.True(typeof(EditorGameServiceBase).IsAssignableFrom(typeof(UIEditorGameAdornerService)));
            Assert.True(typeof(IEditorGameViewModelService).IsAssignableFrom(typeof(UIEditorGameAdornerService)));
            Assert.True(typeof(UIEditorGameAdornerService).IsSealed);

            // b) UIEditorGameCameraService
            Assert.True(typeof(EditorGameCameraService).IsAssignableFrom(typeof(UIEditorGameCameraService)));
            Assert.True(typeof(UIEditorGameCameraService).IsSealed);

            // c) UILayoutHelper
            Assert.True(typeof(UILayoutHelper).IsAbstract && typeof(UILayoutHelper).IsSealed); // static class

            // d) UIEditorController
            Assert.True(typeof(UIEditorController).IsAbstract);
            var baseType = typeof(UIEditorController).BaseType;
            Assert.NotNull(baseType);
            Assert.Contains("AssetCompositeHierarchyEditorController", baseType.Name);
        }

        [Fact]
        public void TestUIEditorGameCameraServiceDefaultsAndReset()
        {
            Assert.Equal(new Vector3(0, 0, 500), UIEditorGameCameraService.DefaultPosition);
            Assert.Equal(0.0f, UIEditorGameCameraService.DefaultPitch);
            Assert.Equal(0.0f, UIEditorGameCameraService.DefaultYaw);

            // Create instance using mock controller
            var controller = new MockEditorGameController();
            var cameraService = new UIEditorGameCameraService(controller);

            // Test ResetCamera with vertical view (pointing up: X=0, Z=0)
            cameraService.ResetCamera(new Vector3(0, 1, 0));
            // When view is vertical, desiredYaw is 0, desiredPitch is MathF.Atan2(1, 0) = Pi/2

            // Test ResetCamera with horizontal view (pointing along -Z)
            cameraService.ResetCamera(new Vector3(0, 0, -1));
            // View along -Z: yaw = Atan2(0, 1) = 0, pitch = Atan2(0, 1) = 0

            // Test ResetCamera with angled view
            cameraService.ResetCamera(new Vector3(1, 0, 0));
            // View along +X: yaw = Atan2(-1, 0) = -Pi/2
        }

        [Fact]
        public void TestUILayoutHelperMoveOperations()
        {
            var resolution = new Vector3(1920, 1080, 100);

            // 1. Move root element with no parent
            var element = SetTestSize(new ContentDecorator
            {
                Margin = new Thickness(50, 50, 0, 0)
            }, 200, 100);
            var delta = new Vector3(30, 20, 0);
            var moved = UILayoutHelper.Move(element, ref delta, magnetDistance: 10f, ref resolution);
            Assert.True(moved);
            Assert.Equal(80f, element.Margin.Left);
            Assert.Equal(70f, element.Margin.Top);

            // 2. Move element inside a ContentControl should return false
            var parentContent = new ContentDecorator();
            var childOfContent = new ContentDecorator { Width = 100, Height = 50 };
            parentContent.Content = childOfContent;
            var contentDelta = new Vector3(10, 10, 0);
            var movedInContent = UILayoutHelper.Move(childOfContent, ref contentDelta, magnetDistance: 10f, ref resolution);
            Assert.False(movedInContent);

            // 3. Move inside Canvas
            var canvas = SetTestSize(new Canvas(), 800, 600);
            var canvasChild = SetTestSize(new ContentDecorator
            {
                Margin = new Thickness(10, 10, 0, 0)
            }, 100, 80);
            canvas.Children.Add(canvasChild);
            var canvasDelta = new Vector3(15, 25, 0);
            var movedCanvas = UILayoutHelper.Move(canvasChild, ref canvasDelta, magnetDistance: 10f, ref resolution);
            Assert.True(movedCanvas);
            Assert.Equal(25f, canvasChild.Margin.Left);
            Assert.Equal(35f, canvasChild.Margin.Top);
            Assert.Equal(HorizontalAlignment.Left, canvasChild.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Top, canvasChild.VerticalAlignment);
        }

        [Fact]
        public void TestUILayoutHelperResizeAndMagnet()
        {
            var resolution = new Vector3(1000, 1000, 100);
            var parent = SetTestSize(new Canvas(), 500, 500);

            var element = SetTestSize(new ContentDecorator
            {
                Margin = new Thickness(100, 100, 0, 0)
            }, 100, 100);
            parent.Children.Add(element);

            // Resize Right
            var deltaRight = new Vector3(40, 0, 0);
            var resizedRight = UILayoutHelper.Resize(element, ResizingDirection.Right, ref deltaRight, magnetDistance: 10f, ref resolution);
            Assert.True(resizedRight);
            Assert.Equal(140f, element.Width);

            // Resize Bottom
            var deltaBottom = new Vector3(0, 30, 0);
            var resizedBottom = UILayoutHelper.Resize(element, ResizingDirection.Bottom, ref deltaBottom, magnetDistance: 10f, ref resolution);
            Assert.True(resizedBottom);
            Assert.Equal(130f, element.Height);

            // Magnet test: Resize Right close to magnet boundary (parent left at 0, sibling or boundary)
            // If delta brings right edge near magnetDistance (10px) to parent right (500), it should snap!
            var magnetElement = SetTestSize(new ContentDecorator
            {
                Margin = new Thickness(0, 0, 0, 0)
            }, 494, 100);
            parent.Children.Add(magnetElement);
            var deltaMagnet = new Vector3(2, 0, 0); // moves right edge to 496, within 10px of 500
            UILayoutHelper.Resize(magnetElement, ResizingDirection.Right, ref deltaMagnet, magnetDistance: 10f, ref resolution);
            Assert.Equal(500f, magnetElement.Width); // Snapped to parent right!
        }

        [Fact]
        public void TestUILayoutHelperStackPanelAndGrid()
        {
            var resolution = new Vector3(1000, 1000, 100);

            // StackPanel Horizontal
            var hStack = SetTestSize(new StackPanel { Orientation = Orientation.Horizontal }, 600, 200);
            var hChild = new ContentDecorator { Width = 80, Height = 80, Margin = new Thickness(10, 10, 0, 0) };
            hStack.Children.Add(hChild);
            var deltaH = new Vector3(5, 5, 0);
            UILayoutHelper.Move(hChild, ref deltaH, 5f, ref resolution);
            Assert.Equal(HorizontalAlignment.Left, hChild.HorizontalAlignment);

            // StackPanel Vertical
            var vStack = SetTestSize(new StackPanel { Orientation = Orientation.Vertical }, 200, 600);
            var vChild = new ContentDecorator { Width = 80, Height = 80, Margin = new Thickness(10, 10, 0, 0) };
            vStack.Children.Add(vChild);
            var deltaV = new Vector3(5, 5, 0);
            UILayoutHelper.Move(vChild, ref deltaV, 5f, ref resolution);
            Assert.Equal(VerticalAlignment.Top, vChild.VerticalAlignment);

            // Grid
            var grid = SetTestSize(new Grid(), 400, 400);
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 200));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Fixed, 200));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 200));
            grid.RowDefinitions.Add(new StripDefinition(StripType.Fixed, 200));
            var gridChild = new ContentDecorator { Width = 50, Height = 50 };
            grid.Children.Add(gridChild);
            var deltaGrid = new Vector3(10, 10, 0);
            var movedGrid = UILayoutHelper.Move(gridChild, ref deltaGrid, 5f, ref resolution);
            Assert.True(movedGrid);
        }

        [Fact]
        public void TestUIEditorGameAdornerServiceAttachedProperties()
        {
            Assert.NotNull(UIEditorGameAdornerService.AssociatedAdornerPropertyKey);
            Assert.NotNull(UIEditorGameAdornerService.AssociatedElementIdPropertyKey);
#if DEBUG
            Assert.NotNull(UIEditorGameAdornerService.AssociatedElementPropertyKey);
#endif
            Assert.Equal("AssociatedAdornerPropertyKey", UIEditorGameAdornerService.AssociatedAdornerPropertyKey.Name);
            Assert.Equal("AssociatedElementIdPropertyKey", UIEditorGameAdornerService.AssociatedElementIdPropertyKey.Name);
        }

        [Fact]
        public void TestAdornersAndCursorEnum()
        {
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.Default));
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.SizeAll));
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.SizeNS));
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.SizeWE));
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.SizeNESW));
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.SizeNWSE));
            Assert.True(Enum.IsDefined(typeof(CursorType), CursorType.No));

            Assert.True(Enum.IsDefined(typeof(MarginEdge), MarginEdge.Left));
            Assert.True(Enum.IsDefined(typeof(MarginEdge), MarginEdge.Top));
            Assert.True(Enum.IsDefined(typeof(MarginEdge), MarginEdge.Right));
            Assert.True(Enum.IsDefined(typeof(MarginEdge), MarginEdge.Bottom));
        }

        [Fact]
        public void TestUIEditorControllerConstantsAndHierarchy()
        {
            Assert.Equal(100.0f, UIEditorController.DesignDensity);
            Assert.Equal("AdornerEntity", UIEditorController.AdornerEntityName);
            Assert.Equal("AdornerRoot", UIEditorController.AdornerRootElementName);
            Assert.Equal("DesignAreaEntity", UIEditorController.DesignAreaEntityName);
            Assert.Equal("DesignAreaRoot", UIEditorController.DesignAreaRootElementName);
            Assert.Equal("UIEntity", UIEditorController.UIEntityName);
        }

        private sealed class MockEditorGameController : IEditorGameController
        {
            public bool IsDisposed => false;
            public IEditorContentLoader Loader => null!;
            public Logger Logger => new LoggerResult();
            public Task GameContentLoaded => Task.CompletedTask;
            public AssetNodeContainer GameSideNodeContainer { get; } = new AssetNodeContainer();

            public Task<bool> StartGame() => Task.FromResult(true);
            public Task<bool> CreateScene() => Task.FromResult(true);
            public void OnHideGame() { }
            public void OnShowGame() { }
            public object FindGameSidePart(AbsoluteId partId) => null!;
            public T GetService<T>() where T : IEditorGameViewModelService => default!;
            public void TriggerActiveRenderStageReevaluation() { }
            public void Destroy() { }
            public bool CheckAccess() => true;
            public void EnsureAccess(bool inDispatcherThread = true) { }
            public Task InvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }
            public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(callback());
            public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }
            public Task<TResult> LowPriorityInvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(callback());
            public Task InvokeTask(Func<Task> task, CancellationToken token = default) => task();
            public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => task();
            public void Invoke(Action callback) => callback();
            public TResult Invoke<TResult>(Func<TResult> callback) => callback();
            public void LowPriorityInvoke(Action callback) => callback();
            public TResult LowPriorityInvoke<TResult>(Func<TResult> callback) => callback();
        }
    }
}
