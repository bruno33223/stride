// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Rendering;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.AssetEditors.GraphicsCompositorEditor.ViewModels;

/// <summary>
/// View model for <see cref="SceneCameraSlot"/>.
/// </summary>
public class GraphicsCompositorCameraSlotsViewModel : GraphicsCompositorItemViewModel, IDisposable
{
    private readonly IObjectNode cameraSlotNode;
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;

    public GraphicsCompositorCameraSlotsViewModel([NotNull] GraphicsCompositorEditorViewModel editor, SceneCameraSlot cameraSlot)
        : base(editor)
    {
        CameraSlot = cameraSlot;
        cameraSlotNode = editor.Session.AssetNodeContainer.GetOrCreateNode(cameraSlot);

        nameNodeBinding = new MemberGraphNodeBinding<string>(cameraSlotNode[nameof(CameraSlot.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, editor.UndoRedoService);
    }

    public void Dispose()
    {
        nameNodeBinding?.Dispose();
    }

    public SceneCameraSlot CameraSlot { get; }

    public string Title => CameraSlot?.GetType().Name ?? string.Empty;

    public string Name
    {
        get => nameNodeBinding.Value;
        set => nameNodeBinding.Value = value;
    }

    /// <inheritdoc/>
    public override IObjectNode GetRootNode() => cameraSlotNode;

    /// <inheritdoc />
    protected override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
        path.PushMember(nameof(GraphicsCompositorAsset.Cameras));
        path.PushTarget();
        path.PushIndex(new NodeIndex(Editor.Asset.Asset.Cameras.IndexOf(CameraSlot)));
        return path;
    }
}
