using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public class HeightmapPreviewViewModel : TextureBasePreviewViewModel
{
    private object? heightmapPreview;
    private int previewHeightmapLength;
    private int previewHeightmapWidth;

    public HeightmapPreviewViewModel(SessionViewModel session)
        : base(session)
    {
    }

    public int PreviewHeightmapLength { get => previewHeightmapLength; private set => SetValue(ref previewHeightmapLength, value); }

    public int PreviewHeightmapWidth { get => previewHeightmapWidth; private set => SetValue(ref previewHeightmapWidth, value); }

    public override void AttachPreview(IAssetPreview preview)
    {
        heightmapPreview = preview;
        if (preview != null)
        {
            var evt = preview.GetType().GetEvent("NotifyHeightmapLoaded");
            if (evt != null)
            {
                Action handler = UpdateHeightmapInfo;
                evt.AddEventHandler(preview, handler);
            }
        }
        UpdateHeightmapInfo();
        AttachPreviewTexture(preview);
    }

    private void UpdateHeightmapInfo()
    {
        if (heightmapPreview != null)
        {
            var type = heightmapPreview.GetType();
            if (type.GetProperty("Width")?.GetValue(heightmapPreview) is int w)
                PreviewHeightmapWidth = w;
            if (type.GetProperty("Length")?.GetValue(heightmapPreview) is int l)
                PreviewHeightmapLength = l;
        }
    }
}
