// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Materials;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;

namespace Stride.Assets.Editor.ViewModels;

public enum MaterialPreviewShape
{
    Sphere,
    Cube,
    Cylinder,
    Plane,
    Teapot
}

public enum MaterialPreviewEnvironment
{
    Studio,
    Daylight,
    Dark,
    RimLight
}

[AssetEditorViewModel<MaterialViewModel>]
public sealed class MaterialEditorViewModel : AssetEditorViewModel<MaterialViewModel>
{
    private MaterialPreviewShape previewShape = MaterialPreviewShape.Sphere;
    private MaterialPreviewEnvironment previewEnvironment = MaterialPreviewEnvironment.Studio;
    private bool autoRotate = true;

    public MaterialEditorViewModel(MaterialViewModel asset)
        : base(asset)
    {
        SelectPreviewShapeCommand = new AnonymousCommand<MaterialPreviewShape>(ServiceProvider, shape => PreviewShape = shape);
        SelectEnvironmentCommand = new AnonymousCommand<MaterialPreviewEnvironment>(ServiceProvider, env => PreviewEnvironment = env);
        ToggleAutoRotateCommand = new AnonymousCommand(ServiceProvider, () => AutoRotate = !AutoRotate);
        ResetCameraCommand = new AnonymousCommand(ServiceProvider, () => { });
    }

    public MaterialPreviewShape PreviewShape
    {
        get => previewShape;
        set => SetValue(ref previewShape, value);
    }

    public MaterialPreviewEnvironment PreviewEnvironment
    {
        get => previewEnvironment;
        set => SetValue(ref previewEnvironment, value);
    }

    public bool AutoRotate
    {
        get => autoRotate;
        set => SetValue(ref autoRotate, value);
    }

    public ICommandBase SelectPreviewShapeCommand { get; }
    public ICommandBase SelectEnvironmentCommand { get; }
    public ICommandBase ToggleAutoRotateCommand { get; }
    public ICommandBase ResetCameraCommand { get; }

    public string MaterialName => Asset.Name;
    public int LayerCount => Asset.Asset.Layers?.Count ?? 0;
}
