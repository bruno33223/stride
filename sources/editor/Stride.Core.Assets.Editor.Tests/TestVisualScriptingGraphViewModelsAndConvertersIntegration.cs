// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestVisualScriptingGraphViewModelsAndConvertersIntegration
{
    private static string GetRepoRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(current) && !File.Exists(Path.Combine(current, "stride-env.sh")))
        {
            var parent = Directory.GetParent(current);
            if (parent == null)
                break;
            current = parent.FullName;
        }

        if (string.IsNullOrEmpty(current) || !File.Exists(Path.Combine(current, "stride-env.sh")))
        {
            current = "/media/bruno/SteamGames/apps/stride-source/stride";
        }

        return current;
    }

    [Fact]
    public void TestVisualScriptViewModelAndMethodViewModelExistAndHaveParity()
    {
        var repoRoot = GetRepoRoot();
        var editorDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor");
        var avaloniaDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor.Avalonia");

        // 1. Check file existence
        var vsVmPath = Path.Combine(editorDir, "ViewModels", "VisualScriptViewModel.cs");
        var vsMethodVmPath = Path.Combine(editorDir, "ViewModels", "VisualScriptMethodViewModel.cs");
        var diagnosticsPath = Path.Combine(editorDir, "AssetEditors", "VisualScriptEditor", "VisualScriptEditorViewModel.Diagnostics.cs");
        var funcParamVmPath = Path.Combine(editorDir, "ViewModels", "VisualScriptFunctionParameterViewModel.cs");
        var blockNodeVertexPath = Path.Combine(editorDir, "AssetEditors", "VisualScriptEditor", "ViewModels", "BlockNodeVertex.cs");
        var linkNodeEdgePath = Path.Combine(editorDir, "AssetEditors", "VisualScriptEditor", "ViewModels", "LinkNodeEdge.cs");
        var vsSlotVmPath = Path.Combine(editorDir, "AssetEditors", "VisualScriptEditor", "ViewModels", "VisualScriptSlotViewModel.cs");

        var methodToDisplayNamePath = Path.Combine(avaloniaDir, "Views", "MethodToDisplayName.cs");
        var availVarConverterPath = Path.Combine(avaloniaDir, "Views", "AvailableVariableReferenceValueConverter.cs");
        var slotBrushPath = Path.Combine(avaloniaDir, "Views", "VisualScriptSlotBrush.cs");
        var vsTemplatesPath = Path.Combine(avaloniaDir, "Views", "VisualScriptingTemplates.axaml");
        var graphTemplatesPath = Path.Combine(avaloniaDir, "Views", "GraphTemplates.axaml");

        Assert.True(File.Exists(vsVmPath), $"Expected file not found: {vsVmPath}");
        Assert.True(File.Exists(vsMethodVmPath), $"Expected file not found: {vsMethodVmPath}");
        Assert.True(File.Exists(diagnosticsPath), $"Expected file not found: {diagnosticsPath}");
        Assert.True(File.Exists(funcParamVmPath), $"Expected file not found: {funcParamVmPath}");
        Assert.True(File.Exists(methodToDisplayNamePath), $"Expected file not found: {methodToDisplayNamePath}");
        Assert.True(File.Exists(availVarConverterPath), $"Expected file not found: {availVarConverterPath}");
        Assert.True(File.Exists(slotBrushPath), $"Expected file not found: {slotBrushPath}");
        Assert.True(File.Exists(vsTemplatesPath), $"Expected file not found: {vsTemplatesPath}");
        Assert.True(File.Exists(graphTemplatesPath), $"Expected file not found: {graphTemplatesPath}");

        // 2. Validate line count parity (85-100% of WPF original)
        var vsVmLines = File.ReadAllLines(vsVmPath).Length;
        Assert.InRange(vsVmLines, 88, 120); // Original: 103 lines

        var vsMethodVmLines = File.ReadAllLines(vsMethodVmPath).Length;
        Assert.InRange(vsMethodVmLines, 270, 350); // Original: 318 lines

        var diagnosticsLines = File.ReadAllLines(diagnosticsPath).Length;
        Assert.InRange(diagnosticsLines, 190, 260); // Original: 226 lines

        var methodToDisplayNameLines = File.ReadAllLines(methodToDisplayNamePath).Length;
        Assert.InRange(methodToDisplayNameLines, 74, 110); // Original: 87 lines

        var availVarLines = File.ReadAllLines(availVarConverterPath).Length;
        Assert.InRange(availVarLines, 36, 60); // Original: 43 lines

        var slotBrushLines = File.ReadAllLines(slotBrushPath).Length;
        Assert.InRange(slotBrushLines, 34, 55); // Original: 40 lines

        var vsTemplatesLines = File.ReadAllLines(vsTemplatesPath).Length;
        Assert.InRange(vsTemplatesLines, 25, 45); // Original: 30 lines

        var graphTemplatesLines = File.ReadAllLines(graphTemplatesPath).Length;
        Assert.InRange(graphTemplatesLines, 18, 35); // Original: 22 lines
    }

    [Fact]
    public void TestVisualScriptingTypesAndArchitecture()
    {
        var editorAssembly = typeof(Stride.Assets.Editor.ViewModels.VisualScriptEditorViewModel).Assembly;
        var avaloniaAssembly = typeof(Stride.Assets.Editor.Avalonia.Views.VisualScriptEditorView).Assembly;

        // VisualScriptViewModel type
        var vsVmType = editorAssembly.GetType("Stride.Assets.Editor.ViewModels.VisualScriptViewModel");
        Assert.NotNull(vsVmType);
        Assert.True(typeof(AssetViewModel<VisualScriptAsset>).IsAssignableFrom(vsVmType));

        var assetVmAttr = vsVmType.GetCustomAttribute<AssetViewModelAttribute>();
        Assert.NotNull(assetVmAttr);
        Assert.Equal(typeof(VisualScriptAsset), assetVmAttr.AssetType);

        // VisualScriptViewModel methods and properties
        Assert.NotNull(vsVmType.GetProperty("Methods"));
        Assert.NotNull(vsVmType.GetMethod("AddMethod"));
        Assert.NotNull(vsVmType.GetMethod("RemoveMethod"));
        Assert.NotNull(vsVmType.GetMethod("AddProperty"));
        Assert.NotNull(vsVmType.GetMethod("RemoveProperty"));

        // VisualScriptMethodViewModel type
        var vsMethodVmType = editorAssembly.GetType("Stride.Assets.Editor.ViewModels.VisualScriptMethodViewModel");
        Assert.NotNull(vsMethodVmType);
        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(vsMethodVmType));
        Assert.NotNull(vsMethodVmType.GetProperty("Method"));
        Assert.NotNull(vsMethodVmType.GetProperty("Name"));
        Assert.NotNull(vsMethodVmType.GetProperty("ReturnType"));
        Assert.NotNull(vsMethodVmType.GetProperty("Accessibility"));
        Assert.NotNull(vsMethodVmType.GetProperty("VirtualModifier"));
        Assert.NotNull(vsMethodVmType.GetProperty("IsStatic"));
        Assert.NotNull(vsMethodVmType.GetMethod("AddBlock"));
        Assert.NotNull(vsMethodVmType.GetMethod("RemoveBlock"));
        Assert.NotNull(vsMethodVmType.GetMethod("AddParameter"));
        Assert.NotNull(vsMethodVmType.GetMethod("RemoveParameter"));
        Assert.NotNull(vsMethodVmType.GetMethod("AddLink"));
        Assert.NotNull(vsMethodVmType.GetMethod("RemoveLink"));
        Assert.NotNull(vsMethodVmType.GetMethod("RegenerateSlots", Type.EmptyTypes));

        // VisualScriptFunctionParameterViewModel type
        var funcParamVmType = editorAssembly.GetType("Stride.Assets.Editor.ViewModels.VisualScriptFunctionParameterViewModel");
        Assert.NotNull(funcParamVmType);
        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(funcParamVmType));

        // BlockNodeVertex
        var blockNodeVertexType = editorAssembly.GetType("Stride.Assets.Editor.ViewModels.BlockNodeVertex");
        Assert.NotNull(blockNodeVertexType);
        Assert.Equal(16, (int)blockNodeVertexType.GetField("SnapGridSize")!.GetValue(null)!);

        // LinkNodeEdge
        var linkNodeEdgeType = editorAssembly.GetType("Stride.Assets.Editor.ViewModels.LinkNodeEdge");
        Assert.NotNull(linkNodeEdgeType);
        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(linkNodeEdgeType));

        // VisualScriptSlotViewModel
        var slotVmType = editorAssembly.GetType("Stride.Assets.Editor.ViewModels.VisualScriptSlotViewModel");
        Assert.NotNull(slotVmType);
        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(slotVmType));

        // Converters in Avalonia assembly
        var methodToDisplayNameType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.MethodToDisplayName");
        Assert.NotNull(methodToDisplayNameType);

        var availVarConverterType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.AvailableVariableReferenceValueConverter");
        Assert.NotNull(availVarConverterType);

        var slotBrushConnectorFillType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrushConnectorFill");
        Assert.NotNull(slotBrushConnectorFillType);

        var slotBrushMouseOverType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrushMouseOverConnectorFill");
        Assert.NotNull(slotBrushMouseOverType);

        var slotBrushStrokeType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.VisualScriptSlotBrushConnectorStroke");
        Assert.NotNull(slotBrushStrokeType);

        // AXAML Templates
        var vsTemplatesType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.VisualScriptingTemplates");
        Assert.NotNull(vsTemplatesType);

        var graphTemplatesType = avaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.GraphTemplates");
        Assert.NotNull(graphTemplatesType);
    }

    [Fact]
    public void TestBlockNodeVertexSnapAndSlotLogic()
    {
        // Test SnapGridSize rounding
        var snapped = Stride.Assets.Editor.ViewModels.BlockNodeVertex.RoundSnapPosition(new Int2(17, 31));
        Assert.Equal(new Int2(16, 32), snapped);

        var snapped2 = Stride.Assets.Editor.ViewModels.BlockNodeVertex.RoundSnapPosition(new Int2(24, 25));
        Assert.Equal(new Int2(32, 32), snapped2);

        var snapped3 = Stride.Assets.Editor.ViewModels.BlockNodeVertex.RoundSnapPosition(new Int2(0, 8));
        Assert.Equal(new Int2(0, 16), snapped3);
    }
}
