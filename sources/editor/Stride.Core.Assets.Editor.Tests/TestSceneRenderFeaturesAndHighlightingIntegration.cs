// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para o Marco P29 Fatia 1: Scene & Graphics Render Features & Highlighting Infrastructure.
/// Valida presença, hierarquia e comportamento em Stride.Assets.Presentation:
/// - WireframeRenderFeature
/// - HighlightRenderFeature
/// - PickingRenderFeature
/// - ModelAssetHighlighter
/// - IPickedEntity
/// - IPickedDebugger
/// </summary>
public class TestSceneRenderFeaturesAndHighlightingIntegration
{
    private static readonly Assembly PresentationAssembly = typeof(Stride.Assets.Presentation.StrideDefaultAssetsPlugin).Assembly;

    #region Mocks

    private sealed class MockAssetDependencyManager : IAssetDependencyManager
    {
        public AssetDependencies? ComputeDependencies(AssetId assetId, AssetDependencySearchOptions dependenciesOptions = AssetDependencySearchOptions.All, ContentLinkType linkTypes = ContentLinkType.Reference, HashSet<AssetId>? visited = null)
        {
            return null;
        }
    }

    private sealed class MockEntitySelectionService : IEditorGameEntitySelectionService
    {
        public bool DisplaySelectionMask { get; set; } = true;

        public int SelectedIdCount => 0;

        public int SelectedRootIdCount => 0;

        public event EventHandler<EntitySelectionEventArgs>? SelectionUpdated;

        public void TriggerSelectionUpdated()
        {
            SelectionUpdated?.Invoke(this, new EntitySelectionEventArgs(Array.Empty<Entity>(), Array.Empty<Entity>()));
        }

        public IReadOnlyCollection<AbsoluteId> GetSelectedIds() => Array.Empty<AbsoluteId>();

        public IReadOnlyCollection<AbsoluteId> GetSelectedRootIds() => Array.Empty<AbsoluteId>();

        public EntityPickingResult Pick() => default;

        public Task<bool> DuplicateSelection() => Task.FromResult(true);

        public bool IsInitialized => true;

        public bool IsActive { get; set; } = true;

        public IEnumerable<Type> Dependencies => Enumerable.Empty<Type>();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task<bool> InitializeService(EditorServiceGame game) => Task.FromResult(true);

        public void RegisterScene(Scene scene) { }

        public void UpdateGraphicsCompositor(EditorServiceGame game) { }
    }

    #endregion

    #region 1. WireframeRenderFeature Tests

    [Fact]
    public void TestWireframeRenderFeatureTypeAndHierarchy()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.WireframeRenderFeature");
        Assert.True(type != null, "WireframeRenderFeature deve existir em Stride.Assets.Presentation.SceneEditor.");
        Assert.True(typeof(SubRenderFeature).IsAssignableFrom(type), "WireframeRenderFeature deve herdar de SubRenderFeature.");

        var registerMethod = type.GetMethod("RegisterSelectionService", new[] { typeof(IEditorGameEntitySelectionService) });
        Assert.NotNull(registerMethod);
    }

    [Fact]
    public void TestWireframeRenderFeatureLifecycleAndRegistration()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.WireframeRenderFeature");
        Assert.True(type != null, "WireframeRenderFeature deve existir em Stride.Assets.Presentation.SceneEditor.");

        dynamic wireframe = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)wireframe);

        var mockSelection = new MockEntitySelectionService();
        wireframe.RegisterSelectionService(mockSelection);

        // Dispara evento de seleção atualizada sem lançar exceções
        mockSelection.TriggerSelectionUpdated();

        // Invoca Destroy
        var destroyMethod = type.GetMethod("Destroy", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(destroyMethod);
        destroyMethod.Invoke(wireframe, null);

        // Disparo pós-destroy não deve falhar
        mockSelection.TriggerSelectionUpdated();
    }

    #endregion

    #region 2. HighlightRenderFeature Tests

    [Fact]
    public void TestHighlightRenderFeatureTypeAndStaticCollections()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.HighlightRenderFeature");
        Assert.True(type != null, "HighlightRenderFeature deve existir em Stride.Assets.Presentation.SceneEditor.");
        Assert.True(typeof(SubRenderFeature).IsAssignableFrom(type), "HighlightRenderFeature deve herdar de SubRenderFeature.");

        var matColorsField = type.GetField("MaterialHighlightColors", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(matColorsField);
        var meshColorsField = type.GetField("MeshHighlightColors", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(meshColorsField);
        var modelColorsField = type.GetField("ModelHighlightColors", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(modelColorsField);
        var matModelField = type.GetField("MaterialsHighlightedForModel", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(matModelField);

        var meshColors = (Dictionary<Mesh, Color4>)meshColorsField.GetValue(null)!;
        Assert.NotNull(meshColors);
        var mesh = new Mesh();
        var color = new Color4(1f, 0.5f, 0.25f, 1f);
        meshColors[mesh] = color;
        Assert.True(meshColors.ContainsKey(mesh));
        meshColors.Remove(mesh);
    }

    [Fact]
    public void TestHighlightRenderFeatureInstanceMethods()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.HighlightRenderFeature");
        Assert.True(type != null, "HighlightRenderFeature deve existir.");

        dynamic highlight = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)highlight);

        var initMethod = type.GetMethod("InitializeCore", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(initMethod);
        var extractMethod = type.GetMethod("Extract", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(extractMethod);
        var prepareMethod = type.GetMethod("Prepare", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prepareMethod);
        var processPipelineMethod = type.GetMethod("ProcessPipelineState", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(processPipelineMethod);
    }

    #endregion

    #region 3. PickingRenderFeature Tests

    [Fact]
    public void TestPickingRenderFeatureTypeAndHierarchy()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.PickingRenderFeature");
        Assert.True(type != null, "PickingRenderFeature deve existir em Stride.Assets.Presentation.SceneEditor.");
        Assert.True(typeof(SubRenderFeature).IsAssignableFrom(type), "PickingRenderFeature deve herdar de SubRenderFeature.");

        dynamic picking = Activator.CreateInstance(type!)!;
        Assert.NotNull((object)picking);

        var initMethod = type.GetMethod("InitializeCore", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(initMethod);
        var extractMethod = type.GetMethod("Extract", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(extractMethod);
        var prepareMethod = type.GetMethod("Prepare", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prepareMethod);
    }

    #endregion

    #region 4. ModelAssetHighlighter Tests

    [Fact]
    public void TestModelAssetHighlighterTypeAndAttributes()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.ModelAssetHighlighter");
        Assert.True(type != null, "ModelAssetHighlighter deve existir em Stride.Assets.Presentation.AssetEditors.AssetHighlighters.");

        var baseHighlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.AssetHighlighter");
        Assert.True(baseHighlighterType != null);
        Assert.True(baseHighlighterType.IsAssignableFrom(type), "ModelAssetHighlighter deve herdar de AssetHighlighter.");

        var attrType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.AssetHighlighterAttribute");
        Assert.NotNull(attrType);

        var attrs = type.GetCustomAttributes(attrType, inherit: false).Cast<object>().ToList();
        Assert.Equal(2, attrs.Count);

        var assetTypeProp = attrType.GetProperty("AssetType");
        Assert.NotNull(assetTypeProp);
        var targetTypes = attrs.Select(a => (Type)assetTypeProp.GetValue(a)!).ToList();
        Assert.Contains(typeof(ModelAsset), targetTypes);
        Assert.Contains(typeof(ProceduralModelAsset), targetTypes);
    }

    [Fact]
    public void TestModelAssetHighlighterInstantiationAndClear()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.ModelAssetHighlighter");
        Assert.True(type != null, "ModelAssetHighlighter deve existir em Stride.Assets.Presentation.");

        var depManager = new MockAssetDependencyManager();
        dynamic highlighter = Activator.CreateInstance(type!, new object[] { depManager })!;
        Assert.NotNull((object)highlighter);

        // Deve executar Clear sem exceções
        highlighter.Clear();
    }

    #endregion

    #region 5. IPickedEntity Tests

    [Fact]
    public void TestIPickedEntityContract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.Services.IPickedEntity");
        Assert.True(type != null, "IPickedEntity deve existir em Stride.Assets.Presentation.SceneEditor.Services.");
        Assert.True(type.IsInterface, "IPickedEntity deve ser uma interface.");

        var entityProp = type.GetProperty("Entity");
        Assert.NotNull(entityProp);
        Assert.Equal("EntityViewModel", entityProp.PropertyType.Name);

        var compIndexProp = type.GetProperty("ComponentIndex");
        Assert.NotNull(compIndexProp);
        Assert.Equal(typeof(int), compIndexProp.PropertyType);
    }

    #endregion

    #region 6. IPickedDebugger Tests

    [Fact]
    public void TestIPickedDebuggerContract()
    {
        var type = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.Services.IPickedDebugger");
        Assert.True(type != null, "IPickedDebugger deve existir em Stride.Assets.Presentation.SceneEditor.Services.");
        Assert.True(type.IsInterface, "IPickedDebugger deve ser uma interface.");

        var nameProp = type.GetProperty("Name");
        Assert.NotNull(nameProp);
        Assert.Equal(typeof(string), nameProp.PropertyType);

        var launchMethod = type.GetMethod("Launch", new[] { typeof(SessionViewModel) });
        Assert.NotNull(launchMethod);
        Assert.Equal(typeof(Task<Process>), launchMethod.ReturnType);
    }

    #endregion
}
