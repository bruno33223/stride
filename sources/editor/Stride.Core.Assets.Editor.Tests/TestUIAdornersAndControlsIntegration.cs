// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Xunit;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Core.Mathematics;
using Stride.UI;
using Stride.UI.Controls;
using Thickness = Stride.UI.Thickness;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestUIAdornersAndControlsIntegration
    {
        private static readonly Assembly AssetsEditorAssembly = typeof(UIEditorBaseViewModel).Assembly;
        private static readonly Assembly AvaloniaViewsAssembly = typeof(ThicknessEditor).Assembly;

        [Fact]
        public void TestAdornerTypesAndEnumsParity()
        {
            var adornerBaseType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.AdornerBase`1");
            Assert.NotNull(adornerBaseType);

            var iAdornerBaseType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.IAdornerBase`1");
            Assert.NotNull(iAdornerBaseType);

            var iResizingAdornerType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.IResizingAdorner");
            Assert.NotNull(iResizingAdornerType);

            var sizingAdornerType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.SizingAdorner");
            Assert.NotNull(sizingAdornerType);

            var moveAdornerType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.MoveAdorner");
            Assert.NotNull(moveAdornerType);

            var marginAdornerType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.MarginAdorner");
            Assert.NotNull(marginAdornerType);

            var borderAdornerType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.BorderAdorner");
            Assert.NotNull(borderAdornerType);

            var highlightAdornerType = AssetsEditorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners.HighlightAdorner");
            Assert.NotNull(highlightAdornerType);

            // Verify CursorType enum values
            Assert.Equal(0, (int)CursorType.Default);
            Assert.Equal(1, (int)CursorType.No);
            Assert.Equal(2, (int)CursorType.SizeAll);
            Assert.Equal(3, (int)CursorType.SizeNS);
            Assert.Equal(4, (int)CursorType.SizeWE);
            Assert.Equal(5, (int)CursorType.SizeNESW);
            Assert.Equal(6, (int)CursorType.SizeNWSE);

            // Verify MarginEdge enum values
            Assert.Equal(0, (int)MarginEdge.Left);
            Assert.Equal(1, (int)MarginEdge.Top);
            Assert.Equal(2, (int)MarginEdge.Back);
            Assert.Equal(3, (int)MarginEdge.Right);
            Assert.Equal(4, (int)MarginEdge.Bottom);
            Assert.Equal(5, (int)MarginEdge.Front);
        }

        [Fact]
        public void TestAdornersAndThicknessEditorParityAndLineCount()
        {
            // Verify ThicknessEditor line count parity (must match the original WPF 118 lines parity target)
            var thicknessEditorFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../sources/editor/Stride.Assets.Editor.Avalonia/Views/ThicknessEditor.cs"));
            Assert.True(File.Exists(thicknessEditorFile), $"ThicknessEditor.cs file must exist at {thicknessEditorFile}");
            var lineCount = File.ReadAllLines(thicknessEditorFile).Length;
            
            // Expected 119 lines (85-100% parity with WPF 119-line implementation, specifically >= 115 and <= 130)
            Assert.InRange(lineCount, 115, 130);
        }

        [Fact]
        public void TestThicknessEditorAvaloniaProperties()
        {
            var editor = new ThicknessEditor();

            // Initial value is null
            Assert.Null(editor.Value);
            Assert.Null(editor.Left);
            Assert.Null(editor.Top);
            Assert.Null(editor.Back);
            Assert.Null(editor.Right);
            Assert.Null(editor.Bottom);
            Assert.Null(editor.Front);

            // Test setting Value updates individual component properties
            var expectedThickness = new Thickness(10, 20, 30, 40, 50, 60);
            editor.Value = expectedThickness;

            Assert.Equal(10f, editor.Left);
            Assert.Equal(20f, editor.Top);
            Assert.Equal(30f, editor.Back);
            Assert.Equal(40f, editor.Right);
            Assert.Equal(50f, editor.Bottom);
            Assert.Equal(60f, editor.Front);

            // Test setting individual component updates Value
            editor.Left = 15f;
            Assert.NotNull(editor.Value);
            Assert.Equal(15f, editor.Value.Value.Left);
            Assert.Equal(20f, editor.Value.Value.Top);

            editor.Top = 25f;
            Assert.Equal(25f, editor.Value.Value.Top);

            editor.Back = 35f;
            Assert.Equal(35f, editor.Value.Value.Back);

            editor.Right = 45f;
            Assert.Equal(45f, editor.Value.Value.Right);

            editor.Bottom = 55f;
            Assert.Equal(55f, editor.Value.Value.Bottom);

            editor.Front = 65f;
            Assert.Equal(65f, editor.Value.Value.Front);

            // Test SetVectorFromValue (UniformCuboid)
            editor.SetVectorFromValue(8f);
            Assert.NotNull(editor.Value);
            Assert.Equal(new Thickness(8f, 8f, 8f, 8f, 8f, 8f), editor.Value.Value);
            Assert.Equal(8f, editor.Left);
            Assert.Equal(8f, editor.Right);
            Assert.Equal(8f, editor.Top);
            Assert.Equal(8f, editor.Bottom);
            Assert.Equal(8f, editor.Back);
            Assert.Equal(8f, editor.Front);

            // Test ResetValue
            editor.DefaultValue = new Thickness(1, 2, 3, 4, 5, 6);
            editor.ResetValue();
            Assert.Equal(new Thickness(1, 2, 3, 4, 5, 6), editor.Value);
        }

        [Fact]
        public void TestThicknessEditorPropertyRegistrationOptions()
        {
            Assert.Equal(BindingMode.TwoWay, ThicknessEditor.LeftProperty.GetMetadata(typeof(ThicknessEditor)).DefaultBindingMode);
            Assert.Equal(BindingMode.TwoWay, ThicknessEditor.TopProperty.GetMetadata(typeof(ThicknessEditor)).DefaultBindingMode);
            Assert.Equal(BindingMode.TwoWay, ThicknessEditor.BackProperty.GetMetadata(typeof(ThicknessEditor)).DefaultBindingMode);
            Assert.Equal(BindingMode.TwoWay, ThicknessEditor.RightProperty.GetMetadata(typeof(ThicknessEditor)).DefaultBindingMode);
            Assert.Equal(BindingMode.TwoWay, ThicknessEditor.BottomProperty.GetMetadata(typeof(ThicknessEditor)).DefaultBindingMode);
            Assert.Equal(BindingMode.TwoWay, ThicknessEditor.FrontProperty.GetMetadata(typeof(ThicknessEditor)).DefaultBindingMode);
        }
    }
}
