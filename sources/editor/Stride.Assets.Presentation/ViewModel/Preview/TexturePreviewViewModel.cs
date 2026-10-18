using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Preview;
using Stride.Graphics;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public class TexturePreviewViewModel : TextureBasePreviewViewModel
{
    private object? texturePreview;
    private int previewTextureDepth;
    private TextureDimension previewDimension;
    private int previewTextureHeight;
    private int previewTextureWidth;
    private object? selectedCubePreviewMode;
    private float selectedDepth;

    public TexturePreviewViewModel(SessionViewModel session)
        : base(session)
    {
        PreviewPreviousDepthCommand = new AnonymousCommand(ServiceProvider, () =>
        {
            if (PreviewTextureDepth > 0)
                SelectedDepth = (float)(Math.Ceiling(SelectedDepth + PreviewTextureDepth - 1) % PreviewTextureDepth);
        });
        PreviewNextDepthCommand = new AnonymousCommand(ServiceProvider, () =>
        {
            if (PreviewTextureDepth > 0)
                SelectedDepth = (float)(Math.Floor(SelectedDepth + 1) % PreviewTextureDepth);
        });
    }

    public int PreviewTextureDepth { get => previewTextureDepth; private set => SetValue(ref previewTextureDepth, value); }

    public TextureDimension PreviewDimension { get => previewDimension; private set => SetValue(ref previewDimension, value); }

    public int PreviewTextureHeight { get => previewTextureHeight; private set => SetValue(ref previewTextureHeight, value); }

    public int PreviewTextureWidth { get => previewTextureWidth; private set => SetValue(ref previewTextureWidth, value); }

    public object SelectedCubePreviewMode
    {
        get => selectedCubePreviewMode ?? 0;
        set
        {
            SetValue(ref selectedCubePreviewMode, value, () => SetCubePreviewModeInternal(value));
        }
    }

    public float SelectedDepth
    {
        get => selectedDepth;
        set
        {
            SetValue(ref selectedDepth, value, () => SetDepthInternal(value));
        }
    }

    public ICommandBase PreviewPreviousDepthCommand { get; }

    public ICommandBase PreviewNextDepthCommand { get; }

    public override void AttachPreview(IAssetPreview preview)
    {
        texturePreview = preview;
        if (preview != null)
        {
            var evt = preview.GetType().GetEvent("NotifyTextureLoaded");
            if (evt != null)
            {
                Action handler = UpdateTextureInfo;
                evt.AddEventHandler(preview, handler);
            }
        }
        UpdateTextureInfo();
        AttachPreviewTexture(preview);
    }

    private void UpdateTextureInfo()
    {
        if (texturePreview != null)
        {
            var type = texturePreview.GetType();
            if (type.GetProperty("Dimension")?.GetValue(texturePreview) is TextureDimension dim)
                PreviewDimension = dim;
            if (type.GetProperty("TextureWidth")?.GetValue(texturePreview) is int w)
                PreviewTextureWidth = w;
            if (type.GetProperty("TextureHeight")?.GetValue(texturePreview) is int h)
                PreviewTextureHeight = h;
            if (type.GetProperty("TextureDepth")?.GetValue(texturePreview) is int d)
                PreviewTextureDepth = d;
        }
    }

    private void SetCubePreviewModeInternal(object value)
    {
        if (texturePreview != null)
        {
            var method = texturePreview.GetType().GetMethod("SetCubePreviewMode");
            if (method != null)
            {
                var pType = method.GetParameters()[0].ParameterType;
                object val = pType.IsEnum ? Enum.ToObject(pType, Convert.ToInt32(value)) : value;
                method.Invoke(texturePreview, new[] { val });
            }
        }
    }

    private void SetDepthInternal(float depth)
    {
        texturePreview?.GetType().GetMethod("SetDepthToPreview", new[] { typeof(float) })?.Invoke(texturePreview, new object[] { depth });
    }
}
