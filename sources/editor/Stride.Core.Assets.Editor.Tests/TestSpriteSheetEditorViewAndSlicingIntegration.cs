// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSpriteSheetEditorViewAndSlicingIntegration
{
    private static string FindRepoRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, "sources", "editor")))
                return current;

            var parent = Directory.GetParent(current)?.FullName;
            if (parent == current || string.IsNullOrEmpty(parent))
                break;

            current = parent;
        }

        throw new DirectoryNotFoundException("Não foi possível localizar o diretório raiz do repositório Stride.");
    }

    [Fact]
    public void TestSpriteSheetEditorViewTypeAndAnnotations()
    {
        var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView).Assembly;
        var viewType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView");

        Assert.NotNull(viewType);
        Assert.True(typeof(UserControl).IsAssignableFrom(viewType));
        Assert.True(typeof(IAssetEditorView).IsAssignableFrom(viewType));

        var attr = viewType.GetCustomAttribute<AssetEditorViewAttribute<SpriteSheetEditorViewModel>>(inherit: false);
        Assert.NotNull(attr);
    }

    [Fact]
    public void TestSpriteSheetEditorViewCommandsAndProperties()
    {
        var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView).Assembly;
        var viewType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView");
        Assert.NotNull(viewType);

        // Comandos estáticos FocusOnRegion e ActivateMagicWand
        var focusProp = viewType.GetProperty("FocusOnRegion", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(focusProp);
        Assert.True(typeof(ICommand).IsAssignableFrom(focusProp.PropertyType));
        var focusCommand = focusProp.GetValue(null);
        Assert.NotNull(focusCommand);

        var wandProp = viewType.GetProperty("ActivateMagicWand", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(wandProp);
        Assert.True(typeof(ICommand).IsAssignableFrom(wandProp.PropertyType));
        var wandCommand = wandProp.GetValue(null);
        Assert.NotNull(wandCommand);

        // Propriedades estáticas de Cursors
        var colorPickerProp = viewType.GetProperty("ColorPickerCursor", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(colorPickerProp);
        Assert.Equal(typeof(Cursor), colorPickerProp.PropertyType);
        var colorPickerCursor = colorPickerProp.GetValue(null);
        Assert.NotNull(colorPickerCursor);

        var magicWandProp = viewType.GetProperty("MagicWandCursor", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(magicWandProp);
        Assert.Equal(typeof(Cursor), magicWandProp.PropertyType);
        var magicWandCursor = magicWandProp.GetValue(null);
        Assert.NotNull(magicWandCursor);

        // Propriedade de instância EditorInitialization
        var editorInitProp = viewType.GetProperty("EditorInitialization", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(editorInitProp);
        Assert.Equal(typeof(Task), editorInitProp.PropertyType);
    }

    [Fact]
    public async Task TestSpriteSheetEditorViewInitializeEditorLifecycle()
    {
        var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView).Assembly;
        var viewType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView");
        Assert.NotNull(viewType);

        var instance = Activator.CreateInstance(viewType);
        Assert.NotNull(instance);

        var initMethod = viewType.GetMethod("InitializeEditor", [typeof(IAssetEditorViewModel)]);
        Assert.NotNull(initMethod);
        Assert.Equal(typeof(Task<bool>), initMethod.ReturnType);

        var editorInitProp = viewType.GetProperty("EditorInitialization", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(editorInitProp);

        var initTask = editorInitProp.GetValue(instance) as Task;
        Assert.NotNull(initTask);
        Assert.False(initTask.IsCompleted);

        var mockEditor = new MockAssetEditorViewModel(shouldSucceed: true);
        var taskResult = (Task<bool>)initMethod.Invoke(instance, [mockEditor])!;
        var success = await taskResult;

        Assert.True(success);
        Assert.True(mockEditor.InitializeCalled);
        Assert.False(mockEditor.DestroyCalled);
        Assert.True(initTask.IsCompleted);

        // Testar caso de falha de inicialização (deve chamar editor.Destroy())
        var instanceFail = Activator.CreateInstance(viewType);
        Assert.NotNull(instanceFail);
        var mockEditorFail = new MockAssetEditorViewModel(shouldSucceed: false);
        var failResult = await (Task<bool>)initMethod.Invoke(instanceFail, [mockEditorFail])!;

        Assert.False(failResult);
        Assert.True(mockEditorFail.InitializeCalled);
        Assert.True(mockEditorFail.DestroyCalled);
    }

    [Fact]
    public void TestSpriteSheetEditorViewAxamlIntegrityAndLineParity()
    {
        var repoRoot = FindRepoRoot();
        var axamlPath = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor.Avalonia", "Views", "SpriteSheetEditorView.axaml");
        var csPath = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor.Avalonia", "Views", "SpriteSheetEditorView.axaml.cs");

        Assert.True(File.Exists(axamlPath), $"Arquivo AXAML não encontrado em: {axamlPath}");
        Assert.True(File.Exists(csPath), $"Arquivo C# code-behind não encontrado em: {csPath}");

        var axamlLines = File.ReadAllLines(axamlPath);
        Assert.Equal(393, axamlLines.Length);

        var axamlText = File.ReadAllText(axamlPath);
        Assert.Contains("x:Class=\"Stride.Assets.Editor.Avalonia.Views.SpriteSheetEditorView\"", axamlText);
        Assert.Contains("x:DataType=\"vm:SpriteSheetEditorViewModel\"", axamlText);
        Assert.Contains("Viewport.ZoomInCommand", axamlText);
        Assert.Contains("Viewport.ZoomOutCommand", axamlText);
        Assert.Contains("Viewport.ScaleToRealSizeCommand", axamlText);
        Assert.Contains("Viewport.FitOnScreenCommand", axamlText);
        Assert.Contains("FocusOnRegionCommand", axamlText);
        Assert.Contains("UseWholeImageCommand", axamlText);
        Assert.Contains("ToggleUseMagicWandCommand", axamlText);
        Assert.Contains("SelectionHighlightEnabled", axamlText);

        // Paridade estrita de linhas do code-behind com WPF (~85 linhas: entre 75 e 95 linhas)
        var csLines = File.ReadAllLines(csPath);
        Assert.InRange(csLines.Length, 75, 95);
    }

    [Fact]
    public void TestSpritePreviewViewsParityAndAnnotations()
    {
        var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.SpriteSheetPreviewView).Assembly;

        // SpriteSheetPreviewView
        var sheetPreviewViewType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.Preview.SpriteSheetPreviewView");
        Assert.NotNull(sheetPreviewViewType);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(sheetPreviewViewType));
        var sheetAttr = sheetPreviewViewType.GetCustomAttribute<AssetPreviewViewAttribute<SpriteSheetPreview>>(inherit: false);
        Assert.NotNull(sheetAttr);

        // SpriteFontPreviewView
        var fontPreviewViewType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.Preview.SpriteFontPreviewView");
        Assert.NotNull(fontPreviewViewType);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(fontPreviewViewType));

        var fontAttr = fontPreviewViewType.GetCustomAttribute<AssetPreviewViewAttribute<SpriteFontPreview>>(inherit: false);
        Assert.NotNull(fontAttr);

        var precompiledFontAttr = fontPreviewViewType.GetCustomAttribute<AssetPreviewViewAttribute<PrecompiledSpriteFontPreview>>(inherit: false);
        Assert.NotNull(precompiledFontAttr);
    }

    [Fact]
    public void TestSpriteSheetListBoxSelectionSynchronization()
    {
        var repoRoot = FindRepoRoot();
        var axamlPath = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor.Avalonia", "Views", "SpriteSheetEditorView.axaml");
        var axamlText = File.ReadAllText(axamlPath);

        // O ListBox de sprites deve ter suporte a seleção múltipla e estar conectado a SelectedSprites e CurrentSprite
        Assert.Contains("SelectionMode=\"Multiple\"", axamlText);
        Assert.Contains("SelectedItems=\"{Binding SelectedSprites}\"", axamlText);
        Assert.Contains("SelectedItem=\"{Binding CurrentSprite, Mode=TwoWay}\"", axamlText);
    }

    private sealed class MockAssetEditorViewModel : IAssetEditorViewModel
    {
        private readonly bool shouldSucceed;

        public AssetViewModel Asset => null!;
        public bool InitializeCalled { get; private set; }
        public bool DestroyCalled { get; private set; }

        public MockAssetEditorViewModel(bool shouldSucceed)
        {
            this.shouldSucceed = shouldSucceed;
        }

        public Task<bool> Initialize()
        {
            InitializeCalled = true;
            return Task.FromResult(shouldSucceed);
        }

        public void Destroy()
        {
            DestroyCalled = true;
        }
    }
}
