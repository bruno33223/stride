using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public abstract class TextureBasePreviewViewModel : AssetPreviewViewModel
{
    private object? textureBasePreview;
    private string previewSelectedMipMap;
    private float spriteScale;

    protected TextureBasePreviewViewModel(SessionViewModel session)
        : base(session)
    {
        // Initialize texture preview
        PreviewAvailableMipMaps = new ObservableList<string>();
        previewSelectedMipMap = "Level 0";
        PreviewZoomInCommand = new AnonymousCommand(ServiceProvider, ZoomIn);
        PreviewZoomOutCommand = new AnonymousCommand(ServiceProvider, ZoomOut);
        PreviewFitOnScreenCommand = new AnonymousCommand(ServiceProvider, FitOnScreen);
        PreviewScaleToRealSizeCommand = new AnonymousCommand(ServiceProvider, ScaleToRealSize);
    }

    public string PreviewSelectedMipMap
    {
        get => previewSelectedMipMap;
        set
        {
            if (value != null)
            {
                SetValue(ref previewSelectedMipMap, value);
                DisplayMipMap(ParseMipMapLevel(value));
            }
        }
    }

    public ObservableList<string> PreviewAvailableMipMaps { get; }

    public float SpriteScale { get => spriteScale; set => SetValue(ref spriteScale, value); }

    public ICommandBase PreviewZoomInCommand { get; }

    public ICommandBase PreviewZoomOutCommand { get; }

    public ICommandBase PreviewFitOnScreenCommand { get; }

    public ICommandBase PreviewScaleToRealSizeCommand { get; }

    protected void AttachPreviewTexture(IAssetPreview preview)
    {
        textureBasePreview = preview;
        if (preview != null)
        {
            var type = preview.GetType();
            var getMipMaps = type.GetMethod("GetAvailableMipMaps");
            if (getMipMaps != null && getMipMaps.Invoke(preview, null) is IEnumerable enumerable)
            {
                PreviewAvailableMipMaps.Clear();
                foreach (var item in enumerable)
                {
                    PreviewAvailableMipMaps.Add($"Level {item}");
                }
            }

            DisplayMipMap(ParseMipMapLevel(PreviewSelectedMipMap));

            var evt = type.GetEvent("SpriteScaleChanged");
            if (evt != null)
            {
                EventHandler handler = (s, e) =>
                {
                    if (type.GetProperty("SpriteScale")?.GetValue(preview) is float sScale)
                        SpriteScale = sScale;
                };
                evt.AddEventHandler(preview, handler);
            }
        }
    }

    private void DisplayMipMap(int level)
    {
        textureBasePreview?.GetType().GetMethod("DisplayMipMap", new[] { typeof(int) })?.Invoke(textureBasePreview, new object[] { level });
    }

    private void ZoomIn()
    {
        textureBasePreview?.GetType().GetMethod("ZoomIn")?.Invoke(textureBasePreview, new object?[] { null });
    }

    private void ZoomOut()
    {
        textureBasePreview?.GetType().GetMethod("ZoomOut")?.Invoke(textureBasePreview, new object?[] { null });
    }

    private void FitOnScreen()
    {
        textureBasePreview?.GetType().GetMethod("FitOnScreen", Type.EmptyTypes)?.Invoke(textureBasePreview, null);
    }

    private void ScaleToRealSize()
    {
        textureBasePreview?.GetType().GetMethod("ScaleToRealSize", Type.EmptyTypes)?.Invoke(textureBasePreview, null);
    }

    private static int ParseMipMapLevel(string level)
    {
        if (level == null)
            return 0;

        int.TryParse(level["Level ".Length..], out var result);
        return result;
    }
}
