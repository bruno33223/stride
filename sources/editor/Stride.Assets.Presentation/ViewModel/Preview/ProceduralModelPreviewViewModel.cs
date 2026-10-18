using Stride.Core.Assets.Editor.ViewModels;
// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.ViewModel.Preview;

public class ProceduralModelPreviewViewModel : AssetPreviewViewModel
{
    private object? modelPreview;

    public ProceduralModelPreviewViewModel(SessionViewModel session)
        : base(session)
    {
        ResetModelCommand = new AnonymousCommand(ServiceProvider, ResetModel);
    }

    public ICommandBase ResetModelCommand { get; }

    public override void AttachPreview(IAssetPreview preview)
    {
        modelPreview = preview;
    }

    private void ResetModel()
    {
        modelPreview?.GetType().GetMethod("ResetCamera", Type.EmptyTypes)?.Invoke(modelPreview, null);
    }
}
