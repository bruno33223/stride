// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Stride.Assets;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Assets.Presentation.Thumbnails;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using SpriteSheetViewModel = Stride.Assets.Presentation.ViewModels.SpriteSheetViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Editor.Annotations;
using Stride.Editor.Preview.ViewModels;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.SpriteStudio.Offline;
using Stride.SpriteStudio.Runtime;
using Xunit;
using Color = Stride.Core.Mathematics.Color;
using Rectangle = Stride.Core.Mathematics.Rectangle;
using Point = Stride.Core.Mathematics.Point;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSpriteStudioThumbnailsAndCacheIntegration
{
    static TestSpriteStudioThumbnailsAndCacheIntegration()
    {
        AssemblyRegistry.Register(typeof(SpriteStudioModelAssetThumbnailCompiler).Assembly, AssemblyCommonCategories.Assets);
        AssemblyRegistry.Register(typeof(ThumbnailFromSpriteBatchCommand<SpriteSheet>).Assembly, AssemblyCommonCategories.Assets);
    }

    private static (SessionViewModel session, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        return (session, sp);
    }

    private static ThumbnailCompilerContext CreateTestContext()
    {
        var context = new ThumbnailCompilerContext();
        var gameSettings = new GameSettingsAsset();
        context.SetGameSettingsAsset(gameSettings);
        var dummyGenerator = (ThumbnailGenerator)RuntimeHelpers.GetUninitializedObject(typeof(ThumbnailGenerator));
        context.Properties.Set(ThumbnailGenerator.Key, dummyGenerator);
        return context;
    }

    private static void AttachDummySession(Package package)
    {
        var container = new StandalonePackage(package);
        var session = (PackageSession)RuntimeHelpers.GetUninitializedObject(typeof(PackageSession));
        typeof(PackageContainer).GetMethod("SetSessionInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.Invoke(container, new object?[] { session });
    }

    private class ConcreteSpriteBatchCommand : ThumbnailFromSpriteBatchCommand<SpriteSheet>
    {
        public bool RenderSpritesCalled;

        public ConcreteSpriteBatchCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
            : base(context, assetItem, assetFinder, url, parameters)
        {
        }

        public Scene PublicCreateScene(GraphicsCompositor graphicsCompositor) => CreateScene(graphicsCompositor);
        public GraphicsCompositor PublicCreateSharedGraphicsCompositor(GraphicsDevice device) => CreateSharedGraphicsCompositor(device);
        public string PublicGraphicsCompositorKey => GraphicsCompositorKey;

        protected override void RenderSprites(RenderDrawContext context)
        {
            RenderSpritesCalled = true;
        }
    }

    private sealed class FakeAssetNodePresenter : IAssetNodePresenter
    {
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; } = null!;
        public Type Type { get; set; } = typeof(object);
        public bool IsVisible { get; set; } = true;
        public AssetViewModel? Asset { get; set; }

        public List<IAssetNodePresenter> ChildrenList { get; } = new();
        IReadOnlyList<INodePresenter> INodePresenter.Children => ChildrenList;

        public IAssetNodePresenter this[string childName]
        {
            get
            {
                var match = ChildrenList.FirstOrDefault(c => c.Name == childName);
                if (match == null)
                {
                    match = new FakeAssetNodePresenter { Name = childName, Asset = Asset };
                    ChildrenList.Add(match);
                }
                return match;
            }
        }

        INodePresenter INodePresenter.this[string childName] => this[childName];

        public string DisplayName { get => Name; set => Name = value; }
        public INodePresenter Root => this;
        public INodePresenter? Parent => null;
        public List<INodePresenterCommand> Commands { get; } = new();
        public PropertyContainerClass AttachedProperties { get; } = new();
        public bool IsEnumerable => false;
        public bool IsReadOnly { get; set; }
        public NodeIndex Index => NodeIndex.Empty;
        public ITypeDescriptor? Descriptor => null;
        public int? Order { get; set; }
        public string CombineKey { get; set; } = string.Empty;
        Stride.Core.Presentation.Quantum.IPropertyProviderViewModel? INodePresenter.PropertyProvider => null;
        INodePresenterFactory INodePresenter.Factory => null!;
        public AssetNodePresenterFactory Factory => null!;

        public bool HasBase => false;
        public bool IsInherited => false;
        public bool IsOverridden => false;

        public event EventHandler<ValueChangingEventArgs>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
        public event EventHandler<EventArgs>? OverrideChanging;
        public event EventHandler<EventArgs>? OverrideChanged;

        public void UpdateValue(object? newValue) => Value = newValue!;
        public void AddItem(object value) { }
        public void AddItem(object value, NodeIndex index) { }
        public void RemoveItem(object value, NodeIndex index) { }
        public NodeAccessor GetNodeAccessor() => default;
        public void AddDependency(INodePresenter node, bool refreshOnNestedNodeChanges) { }
        public void ChangeParent(INodePresenter newParent) { }
        public void Rename(string newName, bool overwriteCombineKey = true) { Name = newName; }
        public INodePresenter? TryGetChild(string childName) => ChildrenList.FirstOrDefault(c => c.Name == childName);
        public bool IsObjectReference(object? value) => false;
        public void ResetOverride() { }
        public void Dispose() { }
    }

    [Fact]
    public void TestThumbnailFromSpriteBatchCommand_ArchitectureAndCompositor()
    {
        var context = CreateTestContext();
        var spriteSheetAsset = new SpriteSheetAsset();
        var package = new Package();
        var assetItem = new AssetItem("Sprites/TestSheet", spriteSheetAsset);
        package.Assets.Add(assetItem);
        AttachDummySession(package);

        var parameters = new ThumbnailCommandParameters(spriteSheetAsset, "__THUMBNAIL__TestSheet", new Int2(128, 128));
        var command = new ConcreteSpriteBatchCommand(context, assetItem, package, "__THUMBNAIL__TestSheet", parameters);

        // RenderingMode should always be initialized to LDR
        Assert.Equal(RenderingMode.LDR, command.Parameters.RenderingMode);
        Assert.Equal("ThumbnailSpriteBatchGraphicsCompositorKey", command.PublicGraphicsCompositorKey);

        // CreateScene should attach ThumbnailFromSpriteBatchCommand.Key
        var scene = command.PublicCreateScene(null!);
        Assert.NotNull(scene);
        Assert.True(scene.Tags.TryGetValue(ThumbnailFromSpriteBatchCommand.Key, out var cmdInTag));
        Assert.Same(command, cmdInTag);

        // CreateSharedGraphicsCompositor should create a compositor with ClearRenderer and DelegateSceneRenderer
        var compositor = command.PublicCreateSharedGraphicsCompositor(null!);
        Assert.NotNull(compositor);
        var rendererCollection = Assert.IsType<SceneRendererCollection>(compositor.Game);
        Assert.Equal(2, rendererCollection.Children.Count);
        Assert.IsType<ClearRenderer>(rendererCollection.Children[0]);
        Assert.IsType<DelegateSceneRenderer>(rendererCollection.Children[1]);
    }

    [Fact]
    public void TestSpriteStudioCompilers_ResolutionAndPreparation()
    {
        var registry = BuildDependencyManager.AssetCompilerRegistry;

        // Model compiler resolution
        var modelCompiler = registry.GetCompiler(typeof(SpriteStudioModelAsset), typeof(ThumbnailCompilationContext));
        Assert.NotNull(modelCompiler);
        Assert.Equal(nameof(SpriteStudioModelAssetThumbnailCompiler), modelCompiler.GetType().Name);
        var thumbModelCompiler = Assert.IsAssignableFrom<IThumbnailCompiler>(modelCompiler);
        Assert.False(thumbModelCompiler.IsStatic);

        // Animation compiler resolution
        var animCompiler = registry.GetCompiler(typeof(SpriteStudioAnimationAsset), typeof(ThumbnailCompilationContext));
        Assert.NotNull(animCompiler);
        Assert.Equal(nameof(SpriteStudioAnimationThumbnailCompiler), animCompiler.GetType().Name);
        var thumbAnimCompiler = Assert.IsAssignableFrom<IThumbnailCompiler>(animCompiler);
        Assert.True(thumbAnimCompiler.IsStatic);

        // Model compilation prepare step
        var context = CreateTestContext();
        var package = new Package();
        var modelAsset = new SpriteStudioModelAsset();
        var modelItem = new AssetItem("SpriteStudio/TestModel", modelAsset);
        package.Assets.Add(modelItem);
        var animAsset = new SpriteStudioAnimationAsset();
        var animItem = new AssetItem("SpriteStudio/TestAnim", animAsset);
        package.Assets.Add(animItem);
        AttachDummySession(package);

        var modelResult = modelCompiler.Prepare(context, modelItem);
        Assert.False(modelResult.HasErrors, string.Join("; ", modelResult.Messages.Select(m => m.ToString())));
        Assert.NotEmpty(modelResult.BuildSteps.Steps);
        var modelStep = Assert.IsType<ThumbnailBuildStep>(modelResult.BuildSteps.Steps.First());
        Assert.NotNull(modelStep.Command);
        Assert.IsType<SpriteStudioModelAssetThumbnailCompiler.SpriteStudioSheetThumbnailCommand>(modelStep.Command);

        // Animation compilation prepare step
        var animResult = animCompiler.Prepare(context, animItem);
        Assert.False(animResult.HasErrors, string.Join("; ", animResult.Messages.Select(m => m.ToString())));
        Assert.NotEmpty(animResult.BuildSteps.Steps);
        var animStep = Assert.IsType<ThumbnailBuildStep>(animResult.BuildSteps.Steps.First());
        Assert.NotNull(animStep.Command);
        Assert.IsType<StaticThumbnailCommand<SpriteStudioAnimationAsset>>(animStep.Command);

        // Verify static thumbnail resource availability
        Assert.NotNull(StaticThumbnails.AnimationThumbnail);
        Assert.NotEmpty(StaticThumbnails.AnimationThumbnail);
    }

    [Fact]
    public void TestSpriteEditorImageCache_NullAndSafeHandling()
    {
        using var cache = new SpriteEditorImageCache();

        // Null sources
        Assert.Null(cache.GetPixelSize(null));
        Assert.Null(cache.PickPixelColor(null, Vector2.Zero));
        Assert.Null(cache.FindSpriteRegion(null, Vector2.Zero, true, Color.White));

        // Non-existent files
        var nonExistent = new UFile(Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.png"));
        Assert.Null(cache.GetPixelSize(nonExistent));
        Assert.Null(cache.PickPixelColor(nonExistent, Vector2.Zero));
        Assert.Null(cache.FindSpriteRegion(nonExistent, Vector2.Zero, true, Color.White));

        // Multiple dispose safety
        cache.Dispose();
        cache.Dispose();
    }

    [Fact]
    public void TestSpriteEditorImageCache_LiveTextureAndEviction()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_sprite_cache_{Guid.NewGuid():N}.png");
        try
        {
            // Create a 16x16 test image: transparent background, 4x4 red square at [4..7, 4..7]
            using (var img = new Image<Rgba32>(16, 16))
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        img[x, y] = new Rgba32(0, 0, 0, 0);
                    }
                }

                for (int y = 4; y < 8; y++)
                {
                    for (int x = 4; x < 8; x++)
                    {
                        img[x, y] = new Rgba32(255, 0, 0, 255);
                    }
                }

                img.SaveAsPng(tempFile);
            }

            using var cache = new SpriteEditorImageCache();
            var uFile = new UFile(tempFile);

            // GetPixelSize
            var size = cache.GetPixelSize(uFile);
            Assert.NotNull(size);
            Assert.Equal(16, size.Value.Width);
            Assert.Equal(16, size.Value.Height);

            // PickPixelColor inside square
            var redColor = cache.PickPixelColor(uFile, new Vector2(5, 5));
            Assert.NotNull(redColor);
            Assert.Equal(255, redColor.Value.R);
            Assert.Equal(0, redColor.Value.G);
            Assert.Equal(0, redColor.Value.B);
            Assert.Equal(255, redColor.Value.A);

            // PickPixelColor on transparent background
            var transparentColor = cache.PickPixelColor(uFile, new Vector2(0, 0));
            Assert.NotNull(transparentColor);
            Assert.Equal(0, transparentColor.Value.A);

            // FindSpriteRegion with transparency
            var region = cache.FindSpriteRegion(uFile, new Vector2(5, 5), true, Color.Transparent);
            Assert.NotNull(region);
            Assert.Equal(4, region.Value.X);
            Assert.Equal(4, region.Value.Y);
            Assert.Equal(4, region.Value.Width);
            Assert.Equal(4, region.Value.Height);

            // Cache invalidation test: update write timestamp
            File.SetLastWriteTime(tempFile, DateTime.Now.AddSeconds(10));
            var reloadedSize = cache.GetPixelSize(uFile);
            Assert.NotNull(reloadedSize);
            Assert.Equal(16, reloadedSize.Value.Width);

            // LRU Cache limit eviction verification: load > 30 entries
            var extraFiles = new List<string>();
            try
            {
                for (int i = 0; i < 35; i++)
                {
                    var dummyFile = Path.Combine(Path.GetTempPath(), $"dummy_cache_{i}_{Guid.NewGuid():N}.png");
                    extraFiles.Add(dummyFile);
                    File.Copy(tempFile, dummyFile, true);
                    cache.GetPixelSize(new UFile(dummyFile));
                }
            }
            finally
            {
                foreach (var f in extraFiles)
                {
                    try { if (File.Exists(f)) File.Delete(f); } catch { }
                }
            }
        }
        finally
        {
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
        }
    }

    [Fact]
    public void TestSpriteSheetAssetNodeUpdater_VisibilityRules()
    {
        var updater = new SpriteSheetAssetNodeUpdater();
        Assert.NotNull(updater);

        // Helper to invoke private/protected UpdateNode
        var updateNodeMethod = typeof(SpriteSheetAssetNodeUpdater)
            .GetMethod("UpdateNode", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(IAssetNodePresenter)], null);
        Assert.NotNull(updateNodeMethod);

        // Create a mock SpriteSheetViewModel
        var spriteSheetAsset = new SpriteSheetAsset { Type = SpriteSheetType.Sprite2D };
        var assetItem = new AssetItem("Sprites/Sheet", spriteSheetAsset);
        var vm = (SpriteSheetViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SpriteSheetViewModel));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(vm, assetItem);

        // 1. Sprites collection node visibility: should hide node and all children
        var spritesNode = new FakeAssetNodePresenter
        {
            Name = nameof(SpriteSheetAsset.Sprites),
            Value = new List<SpriteInfo>(),
            Asset = vm,
            IsVisible = true
        };
        var child1 = new FakeAssetNodePresenter { Name = "Child1", IsVisible = true };
        var child2 = new FakeAssetNodePresenter { Name = "Child2", IsVisible = true };
        spritesNode.ChildrenList.Add(child1);
        spritesNode.ChildrenList.Add(child2);

        updateNodeMethod.Invoke(updater, [spritesNode]);
        Assert.False(spritesNode.IsVisible);
        Assert.False(child1.IsVisible);
        Assert.False(child2.IsVisible);

        // 2. Sprite2D mode: Borders hidden, Center remains visible
        spriteSheetAsset.Type = SpriteSheetType.Sprite2D;
        var sprite2DNode = new FakeAssetNodePresenter
        {
            Name = "SpriteEntry",
            Type = typeof(SpriteInfo),
            Asset = vm
        };
        var bordersChild = sprite2DNode[nameof(SpriteInfo.Borders)];
        bordersChild.IsVisible = true;
        var centerChild = sprite2DNode[nameof(SpriteInfo.Center)];
        centerChild.IsVisible = true;

        updateNodeMethod.Invoke(updater, [sprite2DNode]);
        Assert.False(bordersChild.IsVisible);
        Assert.True(centerChild.IsVisible);

        // 3. UI mode: Center and CenterFromMiddle hidden, Borders remains visible
        spriteSheetAsset.Type = SpriteSheetType.UI;
        var uiNode = new FakeAssetNodePresenter
        {
            Name = "SpriteEntry",
            Type = typeof(SpriteInfo),
            Asset = vm
        };
        var uiBordersChild = uiNode[nameof(SpriteInfo.Borders)];
        uiBordersChild.IsVisible = true;
        var uiCenterChild = uiNode[nameof(SpriteInfo.Center)];
        uiCenterChild.IsVisible = true;
        var uiCenterFromMiddleChild = uiNode[nameof(SpriteInfo.CenterFromMiddle)];
        uiCenterFromMiddleChild.IsVisible = true;

        updateNodeMethod.Invoke(updater, [uiNode]);
        Assert.True(uiBordersChild.IsVisible);
        Assert.False(uiCenterChild.IsVisible);
        Assert.False(uiCenterFromMiddleChild.IsVisible);
    }

    [Fact]
    public void TestPreviewViewModels_ContractsCommandsAndAttributes()
    {
        var (session, _) = CreateTestSession();

        // 1. SpriteSheetPreviewViewModel
        var sheetVm = new SpriteSheetPreviewViewModel(session);
        Assert.NotNull(sheetVm.PreviewPreviousFrameCommand);
        Assert.NotNull(sheetVm.PreviewNextFrameCommand);
        Assert.True(sheetVm.PreviewPreviousFrameCommand.IsEnabled);
        Assert.True(sheetVm.PreviewNextFrameCommand.IsEnabled);

        var sheetAttr = typeof(SpriteSheetPreviewViewModel).GetCustomAttribute<AssetPreviewViewModelAttribute>();
        Assert.NotNull(sheetAttr);
        Assert.Equal(typeof(SpriteSheetPreview), sheetAttr.AssetPreviewType);

        // Attach preview to test frame cycling and mode
        var sheetPreview = (SpriteSheetPreview)RuntimeHelpers.GetUninitializedObject(typeof(SpriteSheetPreview));
        var dummySprites = new SpriteSheet();
        dummySprites.Sprites.Add(new Sprite { Name = "Sprite0" });
        dummySprites.Sprites.Add(new Sprite { Name = "Sprite1" });
        dummySprites.Sprites.Add(new Sprite { Name = "Sprite2" });
        typeof(SpriteSheetPreview).GetField("SpriteSheet", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.SetValue(sheetPreview, dummySprites);
        typeof(SpriteSheetPreview).GetField("spriteTextures", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.SetValue(sheetPreview, new List<Texture>());

        sheetVm.AttachPreview(sheetPreview);
        Assert.Equal(3, sheetVm.PreviewFrameCount);
        Assert.Equal(1, sheetVm.PreviewCurrentFrame);

        // Next frame -> 2
        sheetVm.PreviewNextFrameCommand.Execute(null);
        Assert.Equal(2, sheetVm.PreviewCurrentFrame);

        // Next frame -> 3
        sheetVm.PreviewNextFrameCommand.Execute(null);
        Assert.Equal(3, sheetVm.PreviewCurrentFrame);

        // Next frame wraps around -> 1
        sheetVm.PreviewNextFrameCommand.Execute(null);
        Assert.Equal(1, sheetVm.PreviewCurrentFrame);

        // Previous frame wraps around -> 3
        sheetVm.PreviewPreviousFrameCommand.Execute(null);
        Assert.Equal(3, sheetVm.PreviewCurrentFrame);

        // Mode change
        sheetVm.DisplayMode = SpriteSheetDisplayMode.SpriteTextures;
        Assert.Equal(SpriteSheetDisplayMode.SpriteTextures, sheetPreview.Mode);

        // 2. SpriteStudioSheetPreviewViewModel
        var studioVm = new SpriteStudioSheetPreviewViewModel(session);
        Assert.NotNull(studioVm.ResetModelCommand);
        Assert.True(studioVm.ResetModelCommand.IsEnabled);

        var studioAttr = typeof(SpriteStudioSheetPreviewViewModel).GetCustomAttribute<AssetPreviewViewModelAttribute>();
        Assert.NotNull(studioAttr);
        Assert.Equal(typeof(SpriteStudioSheetPreview), studioAttr.AssetPreviewType);

        // ResetModel safe without attached preview
        studioVm.ResetModelCommand.Execute(null);

        // 3. SpriteFontPreviewViewModel
        var fontVm = new SpriteFontPreviewViewModel(session);
        Assert.NotNull(fontVm.ClearTextCommand);
        Assert.True(fontVm.ClearTextCommand.IsEnabled);

        var fontAttr = typeof(SpriteFontPreviewViewModel).GetCustomAttribute<AssetPreviewViewModelAttribute>();
        Assert.NotNull(fontAttr);
        Assert.Equal(typeof(SpriteFontPreview), fontAttr.AssetPreviewType);

        fontVm.PreviewString = "Hello Stride 3D!";
        Assert.Equal("Hello Stride 3D!", fontVm.PreviewString);

        fontVm.ClearTextCommand.Execute(null);
        Assert.Equal(string.Empty, fontVm.PreviewString);
    }

    [Fact]
    public void TestSpriteEditorImageCache_DisposedAndLRUEvictionOrder()
    {
        var tempFiles = new List<string>();
        try
        {
            var baseFile = Path.Combine(Path.GetTempPath(), $"lru_base_{Guid.NewGuid():N}.png");
            tempFiles.Add(baseFile);
            using (var img = new Image<Rgba32>(16, 16))
            {
                img[0, 0] = new Rgba32(255, 0, 0, 255);
                img.SaveAsPng(baseFile);
            }

            var cache = new SpriteEditorImageCache();
            var uBase = new UFile(baseFile);

            var size = cache.GetPixelSize(uBase);
            Assert.NotNull(size);

            // Test Disposed safety: after dispose, calls return null and do not re-read
            cache.Dispose();
            Assert.Null(cache.GetPixelSize(uBase));
            Assert.Null(cache.PickPixelColor(uBase, Vector2.Zero));
            Assert.Null(cache.FindSpriteRegion(uBase, Vector2.Zero, true, Color.White));

            // Test LRU eviction order
            using var lruCache = new SpriteEditorImageCache();
            var testFiles = new List<string>();
            for (int i = 0; i < 31; i++)
            {
                var f = Path.Combine(Path.GetTempPath(), $"lru_test_{i}_{Guid.NewGuid():N}.png");
                testFiles.Add(f);
                tempFiles.Add(f);
                File.Copy(baseFile, f, true);
            }

            // Load items 0 to 29 (30 items total)
            for (int i = 0; i < 30; i++)
            {
                var s = lruCache.GetPixelSize(new UFile(testFiles[i]));
                Assert.NotNull(s);
            }

            // Access item 0 again: in true LRU, item 0 is promoted to MRU, so item 1 becomes the oldest (LRU)
            var reaccessed = lruCache.GetPixelSize(new UFile(testFiles[0]));
            Assert.NotNull(reaccessed);

            // Load item 30 (31st item, forces eviction of 1 item)
            var item30 = lruCache.GetPixelSize(new UFile(testFiles[30]));
            Assert.NotNull(item30);

            // Inspect internal cache list via reflection to verify exact LRU ordering and eviction
            var cacheField = typeof(SpriteEditorImageCache).GetField("cache", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(cacheField);
            var internalList = (System.Collections.IList)cacheField.GetValue(lruCache)!;
            Assert.Equal(30, internalList.Count);

            var fileProp = internalList[0]!.GetType().GetProperty("File");
            Assert.NotNull(fileProp);
            var cachedFiles = internalList.Cast<object>().Select(e => (UFile)fileProp.GetValue(e)!).ToList();

            // Item 0 was reaccessed, so it was promoted to MRU and must still be in cache (at index 28, before item 30 at index 29)
            // Item 1 was the least recently used, so it was evicted from cache
            Assert.Contains(new UFile(testFiles[0]), cachedFiles);
            Assert.DoesNotContain(new UFile(testFiles[1]), cachedFiles);
            Assert.Equal(new UFile(testFiles[0]), cachedFiles[^2]);
            Assert.Equal(new UFile(testFiles[30]), cachedFiles[^1]);
        }
        finally
        {
            foreach (var f in tempFiles)
            {
                try { if (File.Exists(f)) File.Delete(f); } catch { }
            }
        }
    }

    [Fact]
    public void TestSpriteSheetPreviewViewModel_NullPreviewSafety()
    {
        var (session, _) = CreateTestSession();
        var sheetVm = new SpriteSheetPreviewViewModel(session);

        // Preview is not attached: accessing properties should not throw NullReferenceException
        Assert.Equal(0, sheetVm.PreviewFrameCount);
        Assert.Equal(1, sheetVm.PreviewCurrentFrame);

        // Setting properties should be safe
        sheetVm.PreviewCurrentFrame = 2;
        sheetVm.DisplayMode = SpriteSheetDisplayMode.Sprites;
        Assert.Equal(SpriteSheetDisplayMode.Sprites, sheetVm.DisplayMode);

        // Executing commands should be safe
        sheetVm.PreviewNextFrameCommand.Execute(null);
        sheetVm.PreviewPreviousFrameCommand.Execute(null);
    }

    [Fact]
    public void TestSpriteSheetAssetNodeUpdater_SymmetricalVisibilityToggle()
    {
        var updater = new SpriteSheetAssetNodeUpdater();
        var updateNodeMethod = typeof(SpriteSheetAssetNodeUpdater)
            .GetMethod("UpdateNode", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(IAssetNodePresenter)], null);
        Assert.NotNull(updateNodeMethod);

        var spriteSheetAsset = new SpriteSheetAsset { Type = SpriteSheetType.UI };
        var assetItem = new AssetItem("Sprites/ToggleSheet", spriteSheetAsset);
        var vm = (SpriteSheetViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SpriteSheetViewModel));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(vm, assetItem);

        var node = new FakeAssetNodePresenter
        {
            Name = "SpriteEntry",
            Type = typeof(SpriteInfo),
            Asset = vm
        };
        var bordersChild = node[nameof(SpriteInfo.Borders)];
        bordersChild.IsVisible = true;
        var centerChild = node[nameof(SpriteInfo.Center)];
        centerChild.IsVisible = true;
        var centerFromMiddleChild = node[nameof(SpriteInfo.CenterFromMiddle)];
        centerFromMiddleChild.IsVisible = true;

        // Step 1: In UI mode, Borders is true, Center is false, CenterFromMiddle is false
        updateNodeMethod.Invoke(updater, [node]);
        Assert.True(bordersChild.IsVisible);
        Assert.False(centerChild.IsVisible);
        Assert.False(centerFromMiddleChild.IsVisible);

        // Step 2: Toggle to Sprite2D mode. Symmetrical restoration must restore Center and CenterFromMiddle to true, and Borders to false
        spriteSheetAsset.Type = SpriteSheetType.Sprite2D;
        updateNodeMethod.Invoke(updater, [node]);
        Assert.False(bordersChild.IsVisible);
        Assert.True(centerChild.IsVisible);
        Assert.True(centerFromMiddleChild.IsVisible);

        // Step 3: Toggle back to UI mode. Borders must be restored to true, and Center / CenterFromMiddle to false
        spriteSheetAsset.Type = SpriteSheetType.UI;
        updateNodeMethod.Invoke(updater, [node]);
        Assert.True(bordersChild.IsVisible);
        Assert.False(centerChild.IsVisible);
        Assert.False(centerFromMiddleChild.IsVisible);
    }
}

