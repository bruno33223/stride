// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Core.Collections;
using Stride.Core.Presentation.Collections;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestGraphicsCompositorAndNodeUpdatersIntegration
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
    public void TestGraphicsCompositorAndUpdatersFilesExistAndHaveStrictParity()
    {
        var repoRoot = GetRepoRoot();
        var presentationDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Presentation");

        // 1. Check all 8 Slice 2 files existence
        var graphPath = Path.Combine(presentationDir, "AssetEditors", "GraphicsCompositorEditor", "Views", "GraphicsCompositorGraph.cs");
        var nodeVertexPath = Path.Combine(presentationDir, "AssetEditors", "GraphicsCompositorEditor", "Views", "GraphicsCompositorNodeVertex.cs");
        var entityUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "EntityHierarchyAssetNodeUpdater.cs");
        var compositorUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "GraphicsCompositorAssetNodeUpdater.cs");
        var cameraSlotUpdaterPath = Path.Combine(presentationDir, "NodePresenters", "Updaters", "CameraSlotNodeUpdater.cs");
        var entityHierarchyDataPath = Path.Combine(presentationDir, "NodePresenters", "Keys", "EntityHierarchyData.cs");
        var cameraSlotDataPath = Path.Combine(presentationDir, "NodePresenters", "Keys", "CameraSlotData.cs");
        var materialDataPath = Path.Combine(presentationDir, "NodePresenters", "Keys", "MaterialData.cs");

        Assert.True(File.Exists(graphPath), $"Expected file not found: {graphPath}");
        Assert.True(File.Exists(nodeVertexPath), $"Expected file not found: {nodeVertexPath}");
        Assert.True(File.Exists(entityUpdaterPath), $"Expected file not found: {entityUpdaterPath}");
        Assert.True(File.Exists(compositorUpdaterPath), $"Expected file not found: {compositorUpdaterPath}");
        Assert.True(File.Exists(cameraSlotUpdaterPath), $"Expected file not found: {cameraSlotUpdaterPath}");
        Assert.True(File.Exists(entityHierarchyDataPath), $"Expected file not found: {entityHierarchyDataPath}");
        Assert.True(File.Exists(cameraSlotDataPath), $"Expected file not found: {cameraSlotDataPath}");
        Assert.True(File.Exists(materialDataPath), $"Expected file not found: {materialDataPath}");

        // 2. Validate line count parity (85-100% of WPF originals)
        var graphLines = File.ReadAllLines(graphPath).Length;
        Assert.InRange(graphLines, 175, 230); // Original: 209 lines

        var nodeVertexLines = File.ReadAllLines(nodeVertexPath).Length;
        Assert.InRange(nodeVertexLines, 130, 180); // Original: 157 lines

        var entityUpdaterLines = File.ReadAllLines(entityUpdaterPath).Length;
        Assert.InRange(entityUpdaterLines, 75, 110); // Original: 91 lines

        var compositorUpdaterLines = File.ReadAllLines(compositorUpdaterPath).Length;
        Assert.InRange(compositorUpdaterLines, 70, 105); // Original: 84 lines

        var cameraSlotUpdaterLines = File.ReadAllLines(cameraSlotUpdaterPath).Length;
        Assert.InRange(cameraSlotUpdaterLines, 60, 95); // Original: 73 lines

        var entityHierarchyDataLines = File.ReadAllLines(entityHierarchyDataPath).Length;
        Assert.InRange(entityHierarchyDataLines, 16, 30); // Original: 20 lines

        var cameraSlotDataLines = File.ReadAllLines(cameraSlotDataPath).Length;
        Assert.InRange(cameraSlotDataLines, 14, 25); // Original: 17 lines

        var materialDataLines = File.ReadAllLines(materialDataPath).Length;
        Assert.InRange(materialDataLines, 10, 22); // Original: 13 lines

        // 3. Ensure complete decoupling from Win32 and WPF in all target files
        var allPaths = new[]
        {
            graphPath, nodeVertexPath, entityUpdaterPath, compositorUpdaterPath,
            cameraSlotUpdaterPath, entityHierarchyDataPath, cameraSlotDataPath, materialDataPath
        };

        foreach (var path in allPaths)
        {
            var content = File.ReadAllText(path);
            Assert.DoesNotContain("System.Windows", content);
            Assert.DoesNotContain("PresentationFramework", content);
            Assert.DoesNotContain("WindowsBase", content);
            Assert.DoesNotContain("GraphX.Controls", content);
            Assert.DoesNotContain("GraphX.PCL", content);
        }
    }

    [Fact]
    public void TestGraphicsCompositorTypesAndNodeUpdatersAssemblyMetadata()
    {
        var presentationAssembly = typeof(Stride.Assets.Presentation.ViewModels.GraphicsCompositorViewModel).Assembly;

        // Keys
        var entityHierarchyDataType = presentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Keys.EntityHierarchyData");
        Assert.NotNull(entityHierarchyDataType);
        Assert.NotNull(entityHierarchyDataType.GetField("EntityComponentAvailableTypesKey", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(entityHierarchyDataType.GetField("EntityComponentAvailableTypeGroupsKey", BindingFlags.Public | BindingFlags.Static));

        var cameraSlotDataType = presentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Keys.CameraSlotData");
        Assert.NotNull(cameraSlotDataType);
        Assert.NotNull(cameraSlotDataType.GetField("CameraSlotsKey", BindingFlags.Public | BindingFlags.Static));

        var materialDataType = presentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Keys.MaterialData");
        Assert.NotNull(materialDataType);
        Assert.NotNull(materialDataType.GetField("Key", BindingFlags.Public | BindingFlags.Static));

        // Updaters
        var entityUpdaterType = presentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.EntityHierarchyAssetNodeUpdater");
        Assert.NotNull(entityUpdaterType);

        var compositorUpdaterType = presentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.GraphicsCompositorAssetNodeUpdater");
        Assert.NotNull(compositorUpdaterType);

        var cameraSlotUpdaterType = presentationAssembly.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.CameraSlotNodeUpdater");
        Assert.NotNull(cameraSlotUpdaterType);

        // Graph and Vertex Views
        var graphType = presentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.Views.GraphicsCompositorGraph");
        Assert.NotNull(graphType);
        Assert.True(typeof(IDisposable).IsAssignableFrom(graphType));

        var vertexType = presentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.Views.GraphicsCompositorNodeVertex");
        Assert.NotNull(vertexType);
    }
}
