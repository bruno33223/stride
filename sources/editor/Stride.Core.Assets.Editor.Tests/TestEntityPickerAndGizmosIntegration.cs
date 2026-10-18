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
using Stride.Assets;
using Stride.Assets.Entities;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.Services;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Avalonia.Services;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using SessionObjectViewModel = Stride.Core.Assets.Presentation.ViewModels.SessionObjectViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Solutions;
using Stride.Engine;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestEntityPickerAndGizmosIntegration
    {
        private sealed class TestPluginService : IAssetsPluginService
        {
            public IReadOnlyCollection<AssetsPlugin> Plugins => [];
            public void EnsureInitialized(ILogger logger) { }
            public Type? GetAssetViewModelType(Type assetType) => null;
            public Type? GetEditorViewModelType(Type viewModelType) => null;
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

        private static SceneViewModel CreateSceneViewModel(SessionViewModel sessionVm, IViewModelServiceProvider sp, string url = "MainScene")
        {
            var sceneAsset = new SceneAsset();
            var sceneItem = new AssetItem(url, sceneAsset);

            var sceneVm = (SceneViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SceneViewModel));
            typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, sceneItem);
            typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(sceneVm, [sp]);
            typeof(SessionObjectViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, sessionVm);

            var childrenList = new ObservableSet<SceneViewModel>();
            typeof(SceneViewModel).GetField("<Children>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, childrenList);

            var deps = new AssetDependenciesViewModel(sceneVm, false);
            typeof(AssetViewModel).GetField("<Dependencies>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, deps);

            typeof(DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, new NullDispatcherService());

            sessionVm.RegisterAsset(sceneVm);
            return sceneVm;
        }

        [Fact]
        public void TestGizmosParity()
        {
            // Verify TransformationGizmo hierarchy and concrete types
            Assert.True(typeof(TransformationGizmo).IsAbstract);
            Assert.True(typeof(TranslationGizmo).IsSubclassOf(typeof(TransformationGizmo)));
            Assert.True(typeof(RotationGizmo).IsSubclassOf(typeof(TransformationGizmo)));
            Assert.True(typeof(ScaleGizmo).IsSubclassOf(typeof(TransformationGizmo)));
        }

        [Fact]
        public void TestEntityPickerTypeAndInterface()
        {
            var pickerType = typeof(EntityPickerWindow);
            Assert.NotNull(pickerType);
            Assert.True(typeof(IEntityPickerDialog).IsAssignableFrom(pickerType));
            Assert.True(typeof(IModalDialog).IsAssignableFrom(pickerType));
        }

        [Fact]
        public void TestStrideDialogServiceContract()
        {
            var serviceType = typeof(StrideDialogService);
            Assert.NotNull(serviceType);
            Assert.True(typeof(IStrideDialogService).IsAssignableFrom(serviceType));

            var service = new StrideDialogService();
            Assert.Throws<ArgumentNullException>(() => service.CreateEntityPickerDialog(null!));
            Assert.Throws<ArgumentNullException>(() => service.CreateEntityComponentPickerDialog(null!, typeof(CameraComponent)));
            Assert.Throws<ArgumentException>(() => service.CreateEntityComponentPickerDialog((EntityHierarchyEditorViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SceneEditorViewModel)), typeof(string)));
        }

        [Fact]
        public void TestEntityPickerWrapperFiltering()
        {
            var (sessionVm, sp) = CreateTestSession();
            var sceneVm = CreateSceneViewModel(sessionVm, sp);

            // Create an entity with a CameraComponent and a TransformComponent
            var entityAsset = new Entity("CameraEntity");
            var cameraComp = new CameraComponent();
            entityAsset.Components.Add(cameraComp);

            var design = new EntityDesign(entityAsset);
            var entityVm = new EntityViewModel(sceneVm, design);

            // Test wrapper with matching ComponentType
            var wrapperCamera = new EntityPickerWindow.EntityHierarchyItemViewModelWrapper(entityVm, x => true, typeof(CameraComponent));
            Assert.True(wrapperCamera.MatchesCriteria);
            Assert.NotNull(wrapperCamera.Components);
            Assert.Single(wrapperCamera.Components);
            Assert.Same(cameraComp, wrapperCamera.Components[0].Item2);
            Assert.Same(cameraComp, wrapperCamera.SelectedComponent?.Item2);
            Assert.Equal(1, ((IPickedEntity)wrapperCamera).ComponentIndex);

            // Test wrapper with non-matching ComponentType (e.g. LightComponent)
            var wrapperLight = new EntityPickerWindow.EntityHierarchyItemViewModelWrapper(entityVm, x => true, typeof(LightComponent));
            Assert.False(wrapperLight.MatchesCriteria);
            Assert.NotNull(wrapperLight.Components);
            Assert.Empty(wrapperLight.Components);
            Assert.Null(wrapperLight.SelectedComponent);
            Assert.Equal(-1, ((IPickedEntity)wrapperLight).ComponentIndex);

            // Test wrapper without ComponentType filter (matches any entity)
            var wrapperAny = new EntityPickerWindow.EntityHierarchyItemViewModelWrapper(entityVm, x => true, null);
            Assert.True(wrapperAny.MatchesCriteria);
            Assert.Null(wrapperAny.Components);
        }
    }
}
