// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Commands;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for SpriteSheetEditorView.axaml
/// </summary>
[AssetEditorView<SpriteSheetEditorViewModel>]
public partial class SpriteSheetEditorView : UserControl, IAssetEditorView
{
    static SpriteSheetEditorView()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var resources = executingAssembly.GetManifestResourceNames();
        var colorPickerRes = resources.FirstOrDefault(x => x.EndsWith("ColorPicker.cur", StringComparison.Ordinal));
        ColorPickerCursor = LoadCursor(executingAssembly, colorPickerRes, StandardCursorType.Cross);
        var magicWandRes = resources.FirstOrDefault(x => x.EndsWith("MagicWand.cur", StringComparison.Ordinal));
        MagicWandCursor = LoadCursor(executingAssembly, magicWandRes, StandardCursorType.Hand);
        FocusOnRegion = new RoutedCommand("FocusOnRegion", typeof(SpriteSheetEditorView));
        ActivateMagicWand = new RoutedCommand("ActivateMagicWand", typeof(SpriteSheetEditorView));
    }

    private readonly TaskCompletionSource editorInitializationNotifier = new();

    public SpriteSheetEditorView()
    {
        try { InitializeComponent(); } catch (InvalidOperationException) { }
        // Ensure we can give the focus to the editor
        Focusable = true;
    }

    public static RoutedCommand FocusOnRegion { get; }

    public static RoutedCommand ActivateMagicWand { get; }

    public static Cursor ColorPickerCursor { get; }

    public static Cursor MagicWandCursor { get; }

    public Task EditorInitialization => editorInitializationNotifier.Task;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // We give the focus to the editor so shortcuts will work
        if (!IsKeyboardFocusWithin && !(e.Source is GameEngineHost))
            Focus();
    }

    /// <inheritdoc/>
    public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
    {
        var result = await editor.Initialize();
        if (!result)
            editor.Destroy();

        editorInitializationNotifier.SetResult();
        return result;
    }

    private static Cursor LoadCursor(Assembly asm, string? resName, StandardCursorType fallback)
    {
        try
        {
            if (resName != null)
            {
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream != null) return new Cursor(new Bitmap(stream), new PixelPoint(0, 0));
            }
            return new Cursor(fallback);
        }
        catch
        {
            return (Cursor)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Cursor));
        }
    }
}
