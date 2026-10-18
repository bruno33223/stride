// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Point = Avalonia.Point;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Assets.Materials;
using Stride.Assets.Textures;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Presentation.ViewModels;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para a Fatia 2 do Marco P28: Viewport Behaviors, Scene Picking & Asset Highlighters.
/// </summary>
public class TestViewportBehaviorsAndPickingIntegration
{
    private static readonly Assembly PresentationAssembly = typeof(Stride.Assets.Presentation.StrideDefaultAssetsPlugin).Assembly;
    private static readonly Assembly AvaloniaAssembly = typeof(Stride.Assets.Editor.Avalonia.Behaviors.SuspendAnimationDuringSliderDragBehavior).Assembly;

    #region Mock Classes

    private sealed class MockResizingTarget : IResizingTarget
    {
        public bool StartedCalled { get; private set; }
        public ResizingDirection? StartedDirection { get; private set; }

        public int DeltaCallCount { get; private set; }
        public double LastDeltaX { get; private set; }
        public double LastDeltaY { get; private set; }

        public bool CompletedCalled { get; private set; }
        public double CompletedTotalX { get; private set; }
        public double CompletedTotalY { get; private set; }

        public void OnResizingStarted(ResizingDirection direction)
        {
            StartedCalled = true;
            StartedDirection = direction;
        }

        public void OnResizingDelta(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            DeltaCallCount++;
            LastDeltaX = horizontalChange;
            LastDeltaY = verticalChange;
        }

        public void OnResizingCompleted(ResizingDirection direction, double horizontalChange, double verticalChange)
        {
            CompletedCalled = true;
            CompletedTotalX = horizontalChange;
            CompletedTotalY = verticalChange;
        }
    }

    private sealed class MockAssetDependencyManager : IAssetDependencyManager
    {
        public AssetDependencies? DependenciesToReturn { get; set; }

        public AssetDependencies? ComputeDependencies(AssetId assetId, AssetDependencySearchOptions dependenciesOptions = AssetDependencySearchOptions.All, ContentLinkType linkTypes = ContentLinkType.Reference, HashSet<AssetId>? visited = null)
        {
            return DependenciesToReturn;
        }
    }

    #endregion

    #region 1. PickingSceneRenderer Tests

    [Fact]
    public void TestPickingSceneRendererTypeAndHierarchy()
    {
        var rendererType = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.PickingSceneRenderer");
        Assert.True(rendererType != null, "Stride.Assets.Presentation.SceneEditor.PickingSceneRenderer deve existir em Stride.Assets.Presentation.");

        Assert.True(typeof(SceneRendererBase).IsAssignableFrom(rendererType), "PickingSceneRenderer deve herdar de SceneRendererBase.");

        var stageProp = rendererType.GetProperty("PickingRenderStage");
        Assert.NotNull(stageProp);
        Assert.Equal(typeof(RenderStage), stageProp.PropertyType);

        var pickMethod = rendererType.GetMethod("Pick", Type.EmptyTypes);
        Assert.NotNull(pickMethod);
        Assert.Equal("EntityPickingResult", pickMethod.ReturnType.Name);
    }

    [Fact]
    public void TestPickingSceneRendererCacheAndPick()
    {
        var rendererType = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.PickingSceneRenderer");
        Assert.True(rendererType != null, "PickingSceneRenderer deve existir.");

        dynamic renderer = Activator.CreateInstance(rendererType!)!;
        Assert.NotNull((object)renderer);

        var entity = new Entity("TestEntity");
        var modelComp = new ModelComponent();
        entity.Components.Add(modelComp);

        renderer.CacheEntity(entity, false);
        renderer.CacheEntityComponent(modelComp);

        var scene = new Scene();
        scene.Entities.Add(entity);
        renderer.CacheScene(scene, false);

        renderer.UncacheEntityComponent(modelComp);
        renderer.UncacheEntity(entity, false);
        renderer.UncacheScene(scene, false);

        var pickResult = renderer.Pick();
        Assert.NotNull((object)pickResult);
    }

    [Fact]
    public void TestPickingObjectInfoPackingAndResult()
    {
        var pickingInfoType = PresentationAssembly.GetType("Stride.Assets.Presentation.SceneEditor.PickingObjectInfo");
        Assert.True(pickingInfoType != null, "PickingObjectInfo deve existir em Stride.Assets.Presentation.SceneEditor.");

        // PickingObjectInfo(int componentId, int meshIndex, int materialIndex)
        var ctor = pickingInfoType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
        Assert.NotNull(ctor);

        var infoObj = ctor.Invoke(new object[] { 42, 3, 7 });
        Assert.NotNull(infoObj);

        var getResultMethod = pickingInfoType.GetMethod("GetResult");
        Assert.NotNull(getResultMethod);

        var dict = new Dictionary<int, Entity>();
        var testEntity = new Entity("PickedEntity");
        dict[42] = testEntity;

        dynamic result = getResultMethod.Invoke(infoObj, new object[] { dict })!;
        Assert.Equal(42, (int)result.ComponentId);
        Assert.Equal(3, (int)result.MeshNodeIndex);
        Assert.Equal(7, (int)result.MaterialIndex);
        Assert.Same(testEntity, (Entity)result.Entity);
    }

    #endregion

    #region 2. ThumbLikeBehavior Tests

    [Fact]
    public void TestThumbLikeBehaviorTypeAndProperties()
    {
        var behaviorType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ThumbLikeBehavior");
        Assert.True(behaviorType != null, "Stride.Assets.Editor.Avalonia.Behaviors.ThumbLikeBehavior deve existir.");

        Assert.True(typeof(Behavior<Control>).IsAssignableFrom(behaviorType), "ThumbLikeBehavior deve herdar de Behavior<Control>.");

        Assert.NotNull(behaviorType.GetProperty("Direction"));
        Assert.NotNull(behaviorType.GetProperty("Target"));
        Assert.NotNull(behaviorType.GetProperty("Reference"));
        Assert.NotNull(behaviorType.GetProperty("IsDragging"));
    }

    [Fact]
    public void TestThumbLikeBehaviorDraggingLifecycle()
    {
        var behaviorType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ThumbLikeBehavior");
        Assert.True(behaviorType != null, "ThumbLikeBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(behaviorType!)!;
        Assert.NotNull((object)behavior);

        var mockTarget = new MockResizingTarget();
        behavior.Target = mockTarget;
        behavior.Direction = ResizingDirection.Right;

        var control = new Border { Width = 100, Height = 100 };
        ((Behavior<Control>)behavior).Attach(control);

        var startMethod = behaviorType.GetMethod("SimulateDragStart", BindingFlags.Public | BindingFlags.Instance)
            ?? behaviorType.GetMethod("StartResizing", BindingFlags.Public | BindingFlags.Instance);

        var deltaMethod = behaviorType.GetMethod("SimulateDragDelta", BindingFlags.Public | BindingFlags.Instance)
            ?? behaviorType.GetMethod("DeltaResizing", BindingFlags.Public | BindingFlags.Instance);

        var completeMethod = behaviorType.GetMethod("SimulateDragComplete", BindingFlags.Public | BindingFlags.Instance)
            ?? behaviorType.GetMethod("CompleteResizing", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(startMethod != null, "ThumbLikeBehavior deve expor método para simular/iniciar arraste.");
        Assert.True(deltaMethod != null, "ThumbLikeBehavior deve expor método para simular delta de arraste.");
        Assert.True(completeMethod != null, "ThumbLikeBehavior deve expor método para simular conclusão de arraste.");

        startMethod.Invoke(behavior, new object[] { new Point(10, 10) });
        Assert.True(mockTarget.StartedCalled, "OnResizingStarted deveria ser chamado.");
        Assert.Equal(ResizingDirection.Right, mockTarget.StartedDirection);

        deltaMethod.Invoke(behavior, new object[] { new Point(25, 10) });
        Assert.True(mockTarget.DeltaCallCount > 0, "OnResizingDelta deveria ser chamado.");
        Assert.Equal(15.0, mockTarget.LastDeltaX);

        completeMethod.Invoke(behavior, new object[] { new Point(30, 10) });
        Assert.True(mockTarget.CompletedCalled, "OnResizingCompleted deveria ser chamado.");
        Assert.Equal(20.0, mockTarget.CompletedTotalX);

        ((Behavior<Control>)behavior).Detach();
    }

    #endregion

    #region 3. ViewportPaneBehavior Tests

    [Fact]
    public void TestViewportPaneBehaviorTypeAndProperties()
    {
        var behaviorType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ViewportPaneBehavior");
        Assert.True(behaviorType != null, "Stride.Assets.Editor.Avalonia.Behaviors.ViewportPaneBehavior deve existir.");

        Assert.True(typeof(Behavior<Control>).IsAssignableFrom(behaviorType), "ViewportPaneBehavior deve herdar de Behavior<Control>.");

        Assert.NotNull(behaviorType.GetProperty("Modifiers"));
        Assert.NotNull(behaviorType.GetProperty("Viewport"));
        Assert.NotNull(behaviorType.GetProperty("IsPanning"));
    }

    [Fact]
    public void TestViewportPaneBehaviorPanningLifecycle()
    {
        var behaviorType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.ViewportPaneBehavior");
        Assert.True(behaviorType != null, "ViewportPaneBehavior deve existir.");

        dynamic behavior = Activator.CreateInstance(behaviorType!)!;
        Assert.NotNull((object)behavior);

        var serviceProvider = new ViewModelServiceProvider();
        var viewport = new ViewportViewModel(serviceProvider)
        {
            ViewportWidth = 800,
            ViewportHeight = 600,
            HorizontalOffset = 100,
            VerticalOffset = 50,
        };

        behavior.Viewport = viewport;

        var control = new Panel { Width = 800, Height = 600 };
        ((Behavior<Control>)behavior).Attach(control);

        var startPan = behaviorType.GetMethod("SimulatePanStart", BindingFlags.Public | BindingFlags.Instance)
            ?? behaviorType.GetMethod("StartPanning", BindingFlags.Public | BindingFlags.Instance);

        var movePan = behaviorType.GetMethod("SimulatePanMove", BindingFlags.Public | BindingFlags.Instance)
            ?? behaviorType.GetMethod("MovePanning", BindingFlags.Public | BindingFlags.Instance);

        var endPan = behaviorType.GetMethod("SimulatePanEnd", BindingFlags.Public | BindingFlags.Instance)
            ?? behaviorType.GetMethod("EndPanning", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(startPan != null, "ViewportPaneBehavior deve expor método para simular início de pan.");
        Assert.True(movePan != null, "ViewportPaneBehavior deve expor método para simular movimento de pan.");
        Assert.True(endPan != null, "ViewportPaneBehavior deve expor método para simular término de pan.");

        startPan.Invoke(behavior, new object[] { new Point(50, 50) });
        Assert.True((bool)behavior.IsPanning, "IsPanning deve ser true após iniciar pan.");

        movePan.Invoke(behavior, new object[] { new Point(70, 80) });
        Assert.Equal(120.0, viewport.HorizontalOffset); // 100 + 20
        Assert.Equal(80.0, viewport.VerticalOffset);    // 50 + 30

        endPan.Invoke(behavior, null);
        Assert.False((bool)behavior.IsPanning, "IsPanning deve ser false após finalizar pan.");

        ((Behavior<Control>)behavior).Detach();
    }

    #endregion

    #region 4. AssetHighlighters Tests

    [Fact]
    public void TestTextureAssetHighlighterTypeAndAttributes()
    {
        var highlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.TextureAssetHighlighter");
        Assert.True(highlighterType != null, "Stride.Assets.Presentation.AssetEditors.AssetHighlighters.TextureAssetHighlighter deve existir.");

        var baseHighlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.AssetHighlighter");
        Assert.True(baseHighlighterType != null);
        Assert.True(baseHighlighterType.IsAssignableFrom(highlighterType), "TextureAssetHighlighter deve herdar de AssetHighlighter.");

        var attrType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.AssetHighlighterAttribute");
        Assert.True(attrType != null);

        var attr = highlighterType.GetCustomAttributes(attrType, inherit: false).FirstOrDefault();
        Assert.NotNull(attr);

        var targetAssetTypeProp = attrType.GetProperty("AssetType");
        Assert.NotNull(targetAssetTypeProp);
        Assert.Equal(typeof(TextureAsset), targetAssetTypeProp.GetValue(attr));
    }

    [Fact]
    public void TestMaterialAssetHighlighterTypeAndAttributes()
    {
        var highlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.MaterialAssetHighlighter");
        Assert.True(highlighterType != null, "Stride.Assets.Presentation.AssetEditors.AssetHighlighters.MaterialAssetHighlighter deve existir.");

        var baseHighlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.AssetHighlighter");
        Assert.True(baseHighlighterType != null);
        Assert.True(baseHighlighterType.IsAssignableFrom(highlighterType), "MaterialAssetHighlighter deve herdar de AssetHighlighter.");

        var attrType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.AssetHighlighterAttribute");
        Assert.True(attrType != null);

        var attr = highlighterType.GetCustomAttributes(attrType, inherit: false).FirstOrDefault();
        Assert.NotNull(attr);

        var targetAssetTypeProp = attrType.GetProperty("AssetType");
        Assert.NotNull(targetAssetTypeProp);
        Assert.Equal(typeof(MaterialAsset), targetAssetTypeProp.GetValue(attr));
    }

    [Fact]
    public void TestAssetHighlightersInstantiationAndClear()
    {
        var texHighlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.TextureAssetHighlighter");
        var matHighlighterType = PresentationAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetHighlighters.MaterialAssetHighlighter");

        Assert.True(texHighlighterType != null, "TextureAssetHighlighter deve existir.");
        Assert.True(matHighlighterType != null, "MaterialAssetHighlighter deve existir.");

        var depManager = new MockAssetDependencyManager();

        dynamic texHighlighter = Activator.CreateInstance(texHighlighterType!, new object[] { depManager })!;
        dynamic matHighlighter = Activator.CreateInstance(matHighlighterType!, new object[] { depManager })!;

        Assert.NotNull((object)texHighlighter);
        Assert.NotNull((object)matHighlighter);

        // Clear deve executar sem lançar exceções
        texHighlighter.Clear();
        matHighlighter.Clear();
    }

    #endregion
}
