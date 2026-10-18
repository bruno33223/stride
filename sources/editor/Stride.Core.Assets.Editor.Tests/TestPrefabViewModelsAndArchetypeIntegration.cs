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
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using SessionObjectViewModel = Stride.Core.Assets.Presentation.ViewModels.SessionObjectViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Reflection;
using Stride.Core.Solutions;
using Stride.Engine;
using Stride.Assets.Presentation.SceneEditor;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestPrefabViewModelsAndArchetypeIntegration
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

            var depMgrCtor = typeof(Stride.Core.Assets.Analysis.AssetDependencyManager).GetConstructor(
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
            var prefabItem = new AssetItem(url, prefabAsset);

            var prefabVm = (PrefabViewModel)RuntimeHelpers.GetUninitializedObject(typeof(PrefabViewModel));
            typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, prefabItem);
            typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(prefabVm, [sp]);
            typeof(SessionObjectViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, sessionVm);

            var deps = new AssetDependenciesViewModel(prefabVm, false);
            typeof(AssetViewModel).GetField("<Dependencies>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, deps);

            typeof(DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(prefabVm, new NullDispatcherService());

            sessionVm.RegisterAsset(prefabVm);
            return prefabVm;
        }

        [Fact]
        public void TestEditorDesignPartViewModelInterfaceExists()
        {
            // Verify that IEditorDesignPartViewModel<TAssetPartDesign, TAssetPart> exists in Stride.Assets.Editor assembly
            var editorAssembly = typeof(PrefabEditorViewModel).Assembly;
            var targetType = editorAssembly.GetType("Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels.IEditorDesignPartViewModel`2");
            Assert.NotNull(targetType);
            Assert.True(targetType.IsInterface);

            var prop = targetType.GetProperty("PartDesign");
            Assert.NotNull(prop);
        }

        [Fact]
        public void TestPrefabEditorViewModelConstructorAndControllerParity()
        {
            var editorType = typeof(PrefabEditorViewModel);

            // Must have constructor receiving PrefabViewModel and controllerFactory
            var ctors = editorType.GetConstructors();
            var factoryCtor = ctors.FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 2 && p[0].ParameterType == typeof(PrefabViewModel) && typeof(MulticastDelegate).IsAssignableFrom(p[1].ParameterType);
            });
            Assert.NotNull(factoryCtor);

            // Must have Controller property
            var controllerProp = editorType.GetProperty("Controller");
            Assert.NotNull(controllerProp);
        }

        [Fact]
        public void TestPrefabRootViewModelContractAndParity()
        {
            var rootType = typeof(PrefabRootViewModel);

            // Must implement IAssetPropertyProviderViewModel
            Assert.True(typeof(IAssetPropertyProviderViewModel).IsAssignableFrom(rootType), "PrefabRootViewModel must implement IAssetPropertyProviderViewModel");

            // Must have RequestLoading method
            var requestLoading = rootType.GetMethod("RequestLoading");
            Assert.NotNull(requestLoading);

            // Must have RequestLocking method
            var requestLocking = rootType.GetMethod("RequestLocking");
            Assert.NotNull(requestLocking);

            // Must have SetEntityIsLoadedRecursively method
            var setLoadedRec = rootType.GetMethod("SetEntityIsLoadedRecursively", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(setLoadedRec);

            // Must have SetEntityIsLockedRecursively method
            var setLockedRec = rootType.GetMethod("SetEntityIsLockedRecursively", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(setLockedRec);

            // Must have NotifyGameSidePartAdded method
            var notifyGameSide = rootType.GetMethod("NotifyGameSidePartAdded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(notifyGameSide);

            // Must have constructor accepting (object?, PrefabViewModel) or (PrefabEditorViewModel, PrefabViewModel)
            var ctorWithEditor = rootType.GetConstructors().FirstOrDefault(c =>
            {
                var p = c.GetParameters();
                return p.Length == 2 && p[1].ParameterType == typeof(PrefabViewModel);
            });
            Assert.NotNull(ctorWithEditor);
        }

        [Fact]
        public async Task TestPrefabEditorViewModelLifecycleAndIntegration()
        {
            var (sessionVm, sp) = CreateTestSession();
            var prefabVm = CreatePrefabViewModel(sessionVm, sp, "Prefabs/MyPrefab");

            // Test instantiation of PrefabEditorViewModel
            var editorVm = new PrefabEditorViewModel(prefabVm);
            Assert.NotNull(editorVm.Asset);
            Assert.Same(prefabVm, editorVm.Asset);

            // Verify RootPart is PrefabRootViewModel
            var root = editorVm.HierarchyRoot as PrefabRootViewModel;
            Assert.NotNull(root);
            Assert.Same(prefabVm, root.Asset);

            // Verify InitializeEditor exists on PrefabEditorViewModel
            var initMethod = typeof(PrefabEditorViewModel).GetMethod("InitializeEditor");
            Assert.NotNull(initMethod);

            // Call InitializeEditor
            var task = (Task<bool>)initMethod.Invoke(editorVm, null)!;
            var result = await task;
            Assert.True(result);

            // Root must be expanded
            Assert.True(root.IsExpanded);

            // Root must be loaded
            var isLoadedProp = root.GetType().GetProperty("IsLoaded");
            Assert.NotNull(isLoadedProp);
            Assert.True((bool)isLoadedProp.GetValue(root)!);

            // Test direct methods on PrefabRootViewModel
            await root.RequestLocking(true);
            Assert.True(root.IsLocked);

            await root.RequestLocking(false);
            Assert.False(root.IsLocked);

            await root.RequestLoading(false);
            Assert.False(root.IsLoaded);

            await root.RequestLoading(true);
            Assert.True(root.IsLoaded);

            await root.NotifyGameSidePartAdded();

            // Test IAssetPropertyProviderViewModel
            var propProvider = (IAssetPropertyProviderViewModel)root;
            Assert.Same(prefabVm, propProvider.RelatedAsset);
            Assert.True(((IPropertyProviderViewModel)root).CanProvidePropertiesViewModel);
            Assert.NotNull(propProvider.GetAbsolutePathToRootNode());

            // Test LoadSettings
            var loadSettingsMethod = typeof(PrefabEditorViewModel).GetMethod("LoadSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadSettingsMethod);
            loadSettingsMethod.Invoke(editorVm, [new SceneSettingsData()]);
        }
    }
}
