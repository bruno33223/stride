// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Rendering;

namespace Stride.Assets.Editor.ViewModels;

public sealed class ModelMaterialSlotViewModel
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MaterialReferenceName { get; set; } = "(None)";
    public ModelMaterial? SourceMaterial { get; set; }
}

public sealed class ModelNodeItemViewModel
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ParentIndex { get; set; }
    public int Depth { get; set; }
    public string TransformSummary { get; set; } = string.Empty;
    public string Flags { get; set; } = string.Empty;
}

[AssetEditorViewModel<ModelViewModel>]
public sealed class ModelEditorViewModel : AssetEditorViewModel<ModelViewModel>
{
    private ModelMaterialSlotViewModel? selectedMaterial;
    private ModelNodeItemViewModel? selectedNode;
    private bool isWireframe;
    private bool showGrid = true;
    private bool showNormals;

    public ModelEditorViewModel(ModelViewModel asset)
        : base(asset)
    {
        MaterialSlots = new ObservableList<ModelMaterialSlotViewModel>();
        if (asset.Asset.Materials != null)
        {
            for (int i = 0; i < asset.Asset.Materials.Count; i++)
            {
                var mat = asset.Asset.Materials[i];
                MaterialSlots.Add(new ModelMaterialSlotViewModel
                {
                    Index = i,
                    Name = string.IsNullOrEmpty(mat.Name) ? $"Slot_{i}" : mat.Name,
                    MaterialReferenceName = mat.MaterialInstance?.Material != null ? mat.MaterialInstance.Material.ToString()! : "(None)",
                    SourceMaterial = mat
                });
            }
        }

        Nodes = new ObservableList<ModelNodeItemViewModel>();
        if (asset.Asset.Skeleton?.Nodes != null)
        {
            for (int i = 0; i < asset.Asset.Skeleton.Nodes.Length; i++)
            {
                var node = asset.Asset.Skeleton.Nodes[i];
                int depth = 0;
                int p = node.ParentIndex;
                while (p >= 0 && p < asset.Asset.Skeleton.Nodes.Length && depth < 20)
                {
                    depth++;
                    p = asset.Asset.Skeleton.Nodes[p].ParentIndex;
                }

                Nodes.Add(new ModelNodeItemViewModel
                {
                    Index = i,
                    Name = string.IsNullOrEmpty(node.Name) ? $"Node_{i}" : node.Name,
                    ParentIndex = node.ParentIndex,
                    Depth = depth,
                    TransformSummary = $"Pos: {node.Transform.Position:F2}, Rot: {node.Transform.Rotation:F2}, Scl: {node.Transform.Scale:F2}",
                    Flags = node.Flags.ToString()
                });
            }
        }

        ResetCameraCommand = new AnonymousCommand(ServiceProvider, () => { });
        ToggleWireframeCommand = new AnonymousCommand(ServiceProvider, () => IsWireframe = !IsWireframe);
        ToggleGridCommand = new AnonymousCommand(ServiceProvider, () => ShowGrid = !ShowGrid);
        ToggleNormalsCommand = new AnonymousCommand(ServiceProvider, () => ShowNormals = !ShowNormals);
    }

    public ObservableList<ModelMaterialSlotViewModel> MaterialSlots { get; }
    public ObservableList<ModelNodeItemViewModel> Nodes { get; }

    public ModelMaterialSlotViewModel? SelectedMaterial
    {
        get => selectedMaterial;
        set => SetValue(ref selectedMaterial, value);
    }

    public ModelNodeItemViewModel? SelectedNode
    {
        get => selectedNode;
        set => SetValue(ref selectedNode, value);
    }

    public bool IsWireframe
    {
        get => isWireframe;
        set => SetValue(ref isWireframe, value);
    }

    public bool ShowGrid
    {
        get => showGrid;
        set => SetValue(ref showGrid, value);
    }

    public bool ShowNormals
    {
        get => showNormals;
        set => SetValue(ref showNormals, value);
    }

    public ICommandBase ResetCameraCommand { get; }
    public ICommandBase ToggleWireframeCommand { get; }
    public ICommandBase ToggleGridCommand { get; }
    public ICommandBase ToggleNormalsCommand { get; }

    public int MaterialCount => MaterialSlots.Count;
    public int NodeCount => Nodes.Count;
    public string SourceFile => Asset.Asset.Source?.FullPath ?? "(Embedded)";
    public float ScaleImport => Asset.Asset.ScaleImport;
    public bool MergeMeshes => Asset.Asset.MergeMeshes;
}
