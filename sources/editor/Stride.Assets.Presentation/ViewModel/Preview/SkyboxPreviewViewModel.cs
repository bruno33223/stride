using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public class SkyboxPreviewViewModel : AssetPreviewViewModel
{
    private object? skyboxPreview;
    private float glossiness = 0.6f;
    private float metalness = 1.0f;

    public SkyboxPreviewViewModel(SessionViewModel session)
        : base(session)
    {
        ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
    }

    public ICommandBase ResetModelCommand { get; }

    public float Glossiness
    {
        get => glossiness;
        set
        {
            SetValue(ref glossiness, value);
            skyboxPreview?.GetType().GetMethod("SetGlossiness", new[] { typeof(float) })?.Invoke(skyboxPreview, new object[] { value });
        }
    }

    public float Metalness
    {
        get => metalness;
        set
        {
            SetValue(ref metalness, value);
            skyboxPreview?.GetType().GetMethod("SetMetalness", new[] { typeof(float) })?.Invoke(skyboxPreview, new object[] { value });
        }
    }

    public override void AttachPreview(IAssetPreview preview)
    {
        skyboxPreview = preview;
        if (skyboxPreview != null)
        {
            var propG = skyboxPreview.GetType().GetProperty("Glossiness");
            if (propG != null && propG.GetValue(skyboxPreview) is float g)
                SetValue(ref glossiness, g);

            var propM = skyboxPreview.GetType().GetProperty("Metalness");
            if (propM != null && propM.GetValue(skyboxPreview) is float m)
                SetValue(ref metalness, m);
        }
    }

    private void ResetModel()
    {
        skyboxPreview?.GetType().GetMethod("ResetCamera", Type.EmptyTypes)?.Invoke(skyboxPreview, null);
    }
}
