// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets.Entities;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.Game;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using SessionObjectViewModel = Stride.Core.Assets.Presentation.ViewModels.SessionObjectViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Solutions;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestPrefabEditorControllerAndGameIntegration
    {
        private sealed class TestPluginService : IAssetsPluginService
        {
            public IReadOnlyCollection<AssetsPlugin> Plugins => [];
            public void EnsureInitialized(ILogger logger) { }
            public Type? GetAssetViewModelType(Type assetType) => null;
            public Type? GetEditorViewModelType(Type viewModelType)
            {
                if (viewModelType == typeof(PrefabViewModel))
                    return typeof(PrefabEditorViewModel);
                return null;
            }
            public Type? GetEditorViewType(Type editorViewModelType) => null;
            public Type? GetPreviewViewModelType(Type previewType) => null;
            public Type? GetPreviewViewType(Type previewType) => null;
            public IReadOnlyList<Type> GetPrimitiveTypes() => [];
        }

        private sealed class TestDebugService : IEditorDebugService
        {
            public IDebugPage? CreateLogDebugPage(Logger logger, string title, bool register = true) => null;
            public IDebugPage? CreateUndoRedoDebugPage(IUndoRedoService actionService, string title, bool register = true) => null;
            public IDebugPage? CreateAssetNodesDebugPage(SessionViewModel session, string title, bool register = true) => null;
            public void RegisterDebugPage(IDebugPage? page) { }
            public void UnregisterDebugPage(IDebugPage? page) { }
        }

        private sealed class TestMainViewModel : IMainViewModel
        {
            public StatusViewModel Status { get; }
            public TestMainViewModel(IViewModelServiceProvider sp) { Status = new StatusViewModel(sp); }
        }

        private sealed class MockEditorDialogService : IEditorDialogService
        {
            public bool HasMainWindow => false;
            public void Exit(int exitCode = 0) { }
            public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons, MessageBoxImage image)
                => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
            public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
                => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));
            public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons, MessageBoxImage image)
                => Task.FromResult(MessageBoxResult.OK);
            public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
                => Task.FromResult(0);
            public Task<Stride.Core.IO.UFile?> OpenFilePickerAsync(Stride.Core.IO.UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
                => Task.FromResult<Stride.Core.IO.UFile?>(null);
            public Task<IReadOnlyList<Stride.Core.IO.UFile>> OpenMultipleFilesPickerAsync(Stride.Core.IO.UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
                => Task.FromResult<IReadOnlyList<Stride.Core.IO.UFile>>([]);
            public Task<Stride.Core.IO.UDirectory?> OpenFolderPickerAsync(Stride.Core.IO.UDirectory? initialPath = null)
                => Task.FromResult<Stride.Core.IO.UDirectory?>(null);
            public Task<Stride.Core.IO.UFile?> SaveFilePickerAsync(Stride.Core.IO.UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
                => Task.FromResult<Stride.Core.IO.UFile?>(null);
            public void ShowProgressWindow(WorkProgressViewModel workProgress) { }
            public INewProjectDialog CreateNewProjectDialog(SessionViewModel session) => null!;
            public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null) => null!;
        }

        private static PackageSession CreateMockPackageSession()
        {
            var session = (PackageSession)RuntimeHelpers.GetUninitializedObject(typeof(PackageSession));

            typeof(PackageSession).GetField("dependenciesLock", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, new object());

            typeof(PackageSession).GetField("packages", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, new PackageCollection());

            typeof(PackageSession).GetField("packagesCopy", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, new Dictionary<Package, Package>());

            var upgradesField = typeof(PackageSession).GetField("pendingPackageUpgradesPerPackage", BindingFlags.Instance | BindingFlags.NonPublic);
            if (upgradesField != null)
            {
                upgradesField.SetValue(session, Activator.CreateInstance(upgradesField.FieldType));
            }

            var constraintField = typeof(PackageSession).GetField("constraintProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            if (constraintField != null)
            {
                constraintField.SetValue(session, Activator.CreateInstance(constraintField.FieldType));
            }

            var solution = new Solution { FullPath = "TestSolution.sln" };
            typeof(PackageSession).GetField("VSSolution", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, solution);

            var projectsField = typeof(PackageSession).GetField("<Projects>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            projectsField?.SetValue(session, new ProjectCollection());

            var assemblyContainerField = typeof(PackageSession).GetField("<AssemblyContainer>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            assemblyContainerField?.SetValue(session, new AssemblyContainer());

            var depMgrCtor = typeof(AssetDependencyManager).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                [typeof(PackageSession)],
                null);
            var depMgr = depMgrCtor!.Invoke([session]);
            typeof(PackageSession).GetField("dependencies", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, depMgr);

            return session;
        }

        private static (SessionViewModel sessionVm, ViewModelServiceProvider sp) CreateTestSession()
        {
            var sp = new ViewModelServiceProvider();
            sp.RegisterService(new NullDispatcherService());
            sp.RegisterService(new UndoRedoService(10));
            sp.RegisterService(new TestPluginService());
            sp.RegisterService(new TestDebugService());
            sp.RegisterService(new MockEditorDialogService());

            var packageSession = CreateMockPackageSession();
            var mainVm = new TestMainViewModel(sp);
            var logger = new LoggerResult();

            var ctor = typeof(SessionViewModel).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(IViewModelServiceProvider), typeof(PackageSession), typeof(IMainViewModel), typeof(ILogger)],
                null);

            var sessionVm = (SessionViewModel)ctor!.Invoke([sp, packageSession, mainVm, logger]);
            return (sessionVm, sp);
        }

        private static PrefabViewModel CreatePrefabViewModel(SessionViewModel sessionVm, IViewModelServiceProvider sp, string url = "Prefabs/TestPrefab")
        {
            var prefabAsset = new PrefabAsset();
            var assetItem = new AssetItem(url, prefabAsset);

            var prefabVm = (PrefabViewModel)RuntimeHelpers.GetUninitializedObject(typeof(PrefabViewModel));
            typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, assetItem);
            typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(prefabVm, [sp]);
            typeof(SessionObjectViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, sessionVm);

            var deps = new AssetDependenciesViewModel(prefabVm, false);
            typeof(AssetViewModel).GetField("<Dependencies>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, deps);

            typeof(DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, new NullDispatcherService());

            sessionVm.RegisterAsset(prefabVm);
            return prefabVm;
        }

        private static PrefabEditorGame CreateTestPrefabEditorGame(bool initializeContentScene = true)
        {
            var game = (PrefabEditorGame)RuntimeHelpers.GetUninitializedObject(typeof(PrefabEditorGame));
            if (initializeContentScene)
            {
                var contentScene = new Scene();
                typeof(EntityHierarchyEditorGame).GetProperty("ContentScene")!.SetValue(game, contentScene);
            }
            return game;
        }

        [Fact]
        public void TestPrefabEditorGameTypesAndHierarchy()
        {
            Assert.True(typeof(PrefabEditorGame).IsSealed);
            Assert.True(typeof(EntityHierarchyEditorGame).IsAssignableFrom(typeof(PrefabEditorGame)));
        }

        [Fact]
        public void TestPrefabEditorGameEntityOperations()
        {
            var game = CreateTestPrefabEditorGame(initializeContentScene: true);
            Assert.NotNull(game.ContentScene);

            var entity1 = new Entity("Entity1");
            var entity2 = new Entity("Entity2");
            var entity3 = new Entity("Entity3");

            // Load single entity
            game.LoadEntity(entity1);
            Assert.Single(game.ContentScene.Entities);
            Assert.Same(entity1, game.ContentScene.Entities[0]);

            // FindSubEntity (sceneId is ignored on PrefabEditorGame, returns from ContentScene)
            var found = game.FindSubEntity(Guid.Empty, entity1.Id);
            Assert.Same(entity1, found);
            Assert.Null(game.FindSubEntity(Guid.Empty, entity2.Id));

            // Load multiple entities
            game.LoadEntities([entity2, entity3]);
            Assert.Equal(3, game.ContentScene.Entities.Count);
            Assert.Same(entity2, game.FindSubEntity(Guid.NewGuid(), entity2.Id));
            Assert.Same(entity3, game.FindSubEntity(Guid.NewGuid(), entity3.Id));

            // Unload single entity
            game.UnloadEntity(entity1);
            Assert.Equal(2, game.ContentScene.Entities.Count);
            Assert.Null(game.FindSubEntity(Guid.Empty, entity1.Id));

            // Unload multiple entities
            game.UnloadEntities([entity2, entity3]);
            Assert.Empty(game.ContentScene.Entities);
        }

        [Fact]
        public void TestPrefabEditorGameValidations()
        {
            var gameWithScene = CreateTestPrefabEditorGame(initializeContentScene: true);

            // Null checks
            Assert.Throws<ArgumentNullException>(() => gameWithScene.LoadEntity(null!));
            Assert.Throws<ArgumentNullException>(() => gameWithScene.LoadEntities(null!));
            Assert.Throws<ArgumentNullException>(() => gameWithScene.UnloadEntity(null!));
            Assert.Throws<ArgumentNullException>(() => gameWithScene.UnloadEntities(null!));

            // EnsureContentScene check when ContentScene is null
            var gameWithoutScene = CreateTestPrefabEditorGame(initializeContentScene: false);
            Assert.Null(gameWithoutScene.ContentScene);

            var entity = new Entity("TestEntity");
            Assert.Throws<InvalidOperationException>(() => gameWithoutScene.LoadEntity(entity));
            Assert.Throws<InvalidOperationException>(() => gameWithoutScene.LoadEntities([entity]));
            Assert.Throws<InvalidOperationException>(() => gameWithoutScene.UnloadEntity(entity));
            Assert.Throws<InvalidOperationException>(() => gameWithoutScene.UnloadEntities([entity]));
            Assert.Throws<InvalidOperationException>(() => gameWithoutScene.FindSubEntity(Guid.Empty, entity.Id));
        }

        [Fact]
        public void TestPrefabEditorLightServiceParity()
        {
            var service = new PrefabEditorLightService();
            Assert.IsAssignableFrom<EditorGameServiceBase>(service);

            var scene = new Scene();
            service.RegisterScene(scene);

            // Should have 3 light entities: 1 ambient light + 2 directional lights
            Assert.Equal(3, scene.Entities.Count);

            var ambientLight = scene.Entities.FirstOrDefault(e => e.Name == "Prefab Editor Ambient Light");
            Assert.NotNull(ambientLight);
            var ambientComp = ambientLight.Get<LightComponent>();
            Assert.NotNull(ambientComp);
            Assert.IsType<LightAmbient>(ambientComp.Type);
            Assert.Equal(0.3f, ambientComp.Intensity);
            Assert.True(ambientLight.Tags.Get(GizmoBase.NoGizmoKey));

            var dirLight1 = scene.Entities.FirstOrDefault(e => e.Name == "Prefab Editor Ambient Light1");
            Assert.NotNull(dirLight1);
            var dirComp1 = dirLight1.Get<LightComponent>();
            Assert.NotNull(dirComp1);
            Assert.IsType<LightDirectional>(dirComp1.Type);
            Assert.Equal(2.5f, dirComp1.Intensity);
            Assert.True(dirLight1.Tags.Get(GizmoBase.NoGizmoKey));
            var expectedRot1 = Quaternion.RotationYawPitchRoll(MathUtil.Pi * 1.125f, MathUtil.Pi * -0.125f, 0.0f);
            Assert.Equal(expectedRot1, dirLight1.Transform.Rotation);

            var dirLight2 = scene.Entities.FirstOrDefault(e => e.Name == "Prefab Editor Ambient Light2");
            Assert.NotNull(dirLight2);
            var dirComp2 = dirLight2.Get<LightComponent>();
            Assert.NotNull(dirComp2);
            Assert.IsType<LightDirectional>(dirComp2.Type);
            Assert.Equal(2.5f, dirComp2.Intensity);
            Assert.True(dirLight2.Tags.Get(GizmoBase.NoGizmoKey));
            var expectedRot2 = Quaternion.RotationYawPitchRoll(MathUtil.Pi * 0.125f, MathUtil.Pi * -0.25f, 0.0f);
            Assert.Equal(expectedRot2, dirLight2.Transform.Rotation);
        }

        [Fact]
        public void TestPrefabEditorControllerParityAndAbsoluteId()
        {
            Assert.True(typeof(PrefabEditorController).IsSealed);
            Assert.True(typeof(EntityHierarchyEditorController).IsAssignableFrom(typeof(PrefabEditorController)));

            var (sessionVm, sp) = CreateTestSession();
            var prefabVm = CreatePrefabViewModel(sessionVm, sp, "Prefabs/PlayerPrefab");
            var editorVm = new PrefabEditorViewModel(prefabVm);

            var controller = new PrefabEditorController(prefabVm, editorVm);
            Assert.NotNull(controller);

            // Absolute ID computation
            var testEntity = new Entity("Player") { Id = Guid.NewGuid() };
            var absoluteId = controller.GetAbsoluteId(testEntity);
            Assert.Equal(prefabVm.Id, absoluteId.AssetId);
            Assert.Equal(testEntity.Id, absoluteId.ObjectId);

            // Root Part
            var root = editorVm.HierarchyRoot as PrefabRootViewModel;
            Assert.NotNull(root);
            Assert.Equal(prefabVm.Id, root.Asset.Id);
        }
    }
}
