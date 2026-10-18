// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.Assets.Editor;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
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
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Solutions;
using Stride.Engine;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestOpenDefaultScene
{
    private sealed class TestPluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => [];
        public void EnsureInitialized(ILogger logger) { }
        public Type? GetAssetViewModelType(Type assetType) => null;
        public Type? GetEditorViewModelType(Type viewModelType)
        {
            if (viewModelType == typeof(SceneViewModel))
                return typeof(SceneEditorViewModel);
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
        public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<UFile?>(null);
        public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<IReadOnlyList<UFile>>([]);
        public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null)
            => Task.FromResult<UDirectory?>(null);
        public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
            => Task.FromResult<UFile?>(null);
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

    private static (SessionViewModel sessionVm, ViewModelServiceProvider sp, PackageSession packageSession) CreateTestSession()
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
        return (sessionVm, sp, packageSession);
    }

    private static ProjectViewModel CreateProject(SessionViewModel sessionVm, PackageSession packageSession, bool isCurrent = true)
    {
        var package = new Package();
        var solutionProject = new SolutionProject(package, Guid.NewGuid(), "TestProject.csproj");
        typeof(PackageContainer).GetProperty("Session")!.GetSetMethod(true)!.Invoke(solutionProject, [packageSession]);
        var projectVm = new ProjectViewModel(sessionVm, solutionProject, false)
        {
            IsCurrentProject = isCurrent
        };
        return projectVm;
    }

    private static void AddAssetToDirectory(DirectoryBaseViewModel directory, AssetViewModel asset)
    {
        typeof(DirectoryBaseViewModel)
            .GetMethod("AddAsset", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(directory, [asset]);
    }

    private static SceneViewModel CreateSceneViewModel(SessionViewModel sessionVm, IViewModelServiceProvider sp, Package? package = null, string url = "MainScene")
    {
        var sceneAsset = new SceneAsset();
        var sceneItem = new AssetItem(url, sceneAsset);
        if (package != null)
        {
            typeof(AssetItem).GetProperty(nameof(AssetItem.Package))!.GetSetMethod(true)!.Invoke(sceneItem, [package]);
        }
        var sceneVm = (SceneViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SceneViewModel));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, sceneItem);
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(sceneVm, [sp]);
        typeof(SessionObjectViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, sessionVm);
        var deps = new AssetDependenciesViewModel(sceneVm, false);
        typeof(AssetViewModel).GetField("<Dependencies>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sceneVm, deps);
        sessionVm.RegisterAsset(sceneVm);
        return sceneVm;
    }

    private static AssetViewModel CreateGameSettingsViewModel(SessionViewModel sessionVm, IViewModelServiceProvider sp, SceneViewModel? defaultScene, Package? package = null)
    {
        var gameSettings = new GameSettingsAsset();
        if (defaultScene != null)
        {
            var sceneProxy = AttachedReferenceManager.CreateProxyObject<Scene>(defaultScene.Id, defaultScene.Url);
            gameSettings.DefaultScene = sceneProxy;
        }
        var gameSettingsItem = new AssetItem(GameSettingsAsset.GameSettingsLocation, gameSettings);
        if (package != null)
        {
            typeof(AssetItem).GetProperty(nameof(AssetItem.Package))!.GetSetMethod(true)!.Invoke(gameSettingsItem, [package]);
        }
        var gameSettingsVm = (AssetViewModel)RuntimeHelpers.GetUninitializedObject(typeof(AssetViewModel<GameSettingsAsset>));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(gameSettingsVm, gameSettingsItem);
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(gameSettingsVm, [sp]);
        typeof(SessionObjectViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(gameSettingsVm, sessionVm);
        var deps = new AssetDependenciesViewModel(gameSettingsVm, false);
        typeof(AssetViewModel).GetField("<Dependencies>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(gameSettingsVm, deps);
        sessionVm.RegisterAsset(gameSettingsVm);
        return gameSettingsVm;
    }

    [Fact]
    public void TestEditorCollectionResolvesSceneEditor()
    {
        var (sessionVm, sp, packageSession) = CreateTestSession();
        var sceneVm = CreateSceneViewModel(sessionVm, sp);

        Assert.Null(sessionVm.EditorCollection.ActiveEditor);

        sessionVm.EditorCollection.OpenAssetEditor(sceneVm);

        Assert.NotNull(sessionVm.EditorCollection.ActiveEditor);
        Assert.IsType<SceneEditorViewModel>(sessionVm.EditorCollection.ActiveEditor);
        Assert.Same(sceneVm, sessionVm.EditorCollection.ActiveEditor.Asset);
    }

    [Fact]
    public void TestOpenDefaultSceneWithGameSettings()
    {
        var (sessionVm, sp, packageSession) = CreateTestSession();
        var project = CreateProject(sessionVm, packageSession, isCurrent: true);

        var sceneVm = CreateSceneViewModel(sessionVm, sp, project.Package, "Scenes/DefaultScene");
        AddAssetToDirectory(project.AssetMountPoint, sceneVm);

        var gameSettingsVm = CreateGameSettingsViewModel(sessionVm, sp, sceneVm, project.Package);
        AddAssetToDirectory(project.AssetMountPoint, gameSettingsVm);

        Assert.Null(sessionVm.EditorCollection.ActiveEditor);

        var opened = DefaultSceneHelper.OpenDefaultScene(sessionVm);

        Assert.True(opened, "OpenDefaultScene should return true when a default scene is defined in GameSettings.");
        Assert.NotNull(sessionVm.EditorCollection.ActiveEditor);
        Assert.IsType<SceneEditorViewModel>(sessionVm.EditorCollection.ActiveEditor);
        Assert.Same(sceneVm, sessionVm.EditorCollection.ActiveEditor.Asset);
    }

    [Fact]
    public void TestOpenDefaultSceneFallbackWithoutGameSettings()
    {
        var (sessionVm, sp, packageSession) = CreateTestSession();
        var project = CreateProject(sessionVm, packageSession, isCurrent: true);

        var sceneVm = CreateSceneViewModel(sessionVm, sp, project.Package, "Scenes/Level1");
        AddAssetToDirectory(project.AssetMountPoint, sceneVm);

        Assert.Null(sessionVm.EditorCollection.ActiveEditor);

        var opened = DefaultSceneHelper.OpenDefaultScene(sessionVm);

        Assert.True(opened, "OpenDefaultScene should fallback to first SceneViewModel in AllAssets.");
        Assert.NotNull(sessionVm.EditorCollection.ActiveEditor);
        Assert.IsType<SceneEditorViewModel>(sessionVm.EditorCollection.ActiveEditor);
        Assert.Same(sceneVm, sessionVm.EditorCollection.ActiveEditor.Asset);
    }
}
