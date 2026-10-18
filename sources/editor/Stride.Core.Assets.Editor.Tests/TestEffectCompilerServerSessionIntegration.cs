// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets.Editor;
using Stride.Assets.Effect;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Serialization;
using Stride.Engine.Network;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Core.Assets.Editor.Tests;

public class TestEffectCompilerServerSessionIntegration
{
    private sealed class MockEditorDialogService : IEditorDialogService
    {
        public readonly List<(string Title, string Message, ICommandBase? Command)> ShownNotifications = [];

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

        public void ShowNotificationWindow(string title, string message, ICommandBase? command = null, object? commandParameter = null)
        {
            ShownNotifications.Add((title, message, command));
        }
    }

    private sealed class TestMainViewModel : DispatcherViewModel, IMainViewModel
    {
        public TestMainViewModel(IViewModelServiceProvider serviceProvider) : base(serviceProvider)
        {
            Status = new StatusViewModel(serviceProvider);
        }
        public StatusViewModel Status { get; }
        public Task<bool> SaveSession() => Task.FromResult(true);
    }

    private sealed class TestPluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => [];
        public void EnsureInitialized(ILogger logger) { }
        public Type? GetAssetViewModelType(Type assetType)
        {
            if (assetType == typeof(EffectLogAsset))
                return typeof(EffectLogViewModel);
            return null;
        }
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
        upgradesField?.SetValue(session, Activator.CreateInstance(upgradesField.FieldType));

        var constraintField = typeof(PackageSession).GetField("constraintProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        constraintField?.SetValue(session, Activator.CreateInstance(constraintField.FieldType));

        var solution = new Stride.Core.Solutions.Solution { FullPath = "TestSolution.sln" };
        typeof(PackageSession).GetField("VSSolution", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, solution);

        var projectsField = typeof(PackageSession).GetField("<Projects>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        projectsField?.SetValue(session, new ProjectCollection());

        var assemblyContainerField = typeof(PackageSession).GetField("<AssemblyContainer>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        assemblyContainerField?.SetValue(session, new Stride.Core.Reflection.AssemblyContainer());

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

    private static (SessionViewModel sessionVm, ViewModelServiceProvider sp, PackageSession packageSession, MockEditorDialogService dialogService) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new UndoRedoService(20));
        sp.RegisterService(new TestPluginService());
        sp.RegisterService(new TestDebugService());
        var dialogService = new MockEditorDialogService();
        sp.RegisterService(dialogService);

        var packageSession = CreateMockPackageSession();
        var mainVm = new TestMainViewModel(sp);
        var logger = new LoggerResult();

        var ctor = typeof(SessionViewModel).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IViewModelServiceProvider), typeof(PackageSession), typeof(IMainViewModel), typeof(ILogger)],
            null);

        var sessionVm = (SessionViewModel)ctor!.Invoke([sp, packageSession, mainVm, logger]);
        return (sessionVm, sp, packageSession, dialogService);
    }

    private static ProjectViewModel CreateProject(SessionViewModel sessionVm, PackageSession packageSession, bool isCurrent = true)
    {
        var package = new Package
        {
            FullPath = new UFile("/path/to/TestProject/TestPackage.sdpkg"),
            Meta = { Name = "TestPackage" }
        };
        var solutionProject = new SolutionProject(package, Guid.NewGuid(), "TestProject.csproj");
        typeof(PackageContainer).GetProperty("Session")!.GetSetMethod(true)!.Invoke(solutionProject, [packageSession]);
        var projectVm = new ProjectViewModel(sessionVm, solutionProject, false)
        {
            IsCurrentProject = isCurrent
        };
        if (isCurrent)
        {
            typeof(SessionViewModel).GetProperty("CurrentProject", BindingFlags.Instance | BindingFlags.Public)!
                .GetSetMethod(true)!.Invoke(sessionVm, [projectVm]);
        }
        return projectVm;
    }

    [Fact]
    public void TestEffectCompilerServerSessionTypeAndArchitectureDecoupledFromWpf()
    {
        var effectCompilerSessionType = typeof(EffectCompilerServerSession);
        Assert.NotNull(effectCompilerSessionType);
        Assert.True(typeof(IDisposable).IsAssignableFrom(effectCompilerSessionType), "EffectCompilerServerSession must implement IDisposable.");

        // Verify that the assembly does not reference any WPF binaries (PresentationFramework, PresentationCore, WindowsBase)
        var referencedAssemblies = effectCompilerSessionType.Assembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("PresentationFramework", referencedAssemblies);
        Assert.DoesNotContain("PresentationCore", referencedAssemblies);
        Assert.DoesNotContain("WindowsBase", referencedAssemblies);
    }

    [Fact]
    public void TestSessionViewModelEffectCompilerProperties()
    {
        var (sessionVm, _, _, _) = CreateTestSession();

        Assert.Equal(0, sessionVm.ImportEffectLogPendingCount);
        bool countChanged = false;
        sessionVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(sessionVm.ImportEffectLogPendingCount))
                countChanged = true;
        };

        sessionVm.ImportEffectLogPendingCount = 3;
        Assert.Equal(3, sessionVm.ImportEffectLogPendingCount);
        Assert.True(countChanged, "PropertyChanged must be raised when ImportEffectLogPendingCount changes.");

        Assert.Null(sessionVm.ImportEffectLogCommand);
    }

    [Fact]
    public void TestEffectLogViewModelTextProperty()
    {
        var (sessionVm, _, packageSession, _) = CreateTestSession();
        var projectVm = CreateProject(sessionVm, packageSession, isCurrent: true);
        var effectLogAsset = new EffectLogAsset();
        var assetItem = new AssetItem("EffectCompileLog", effectLogAsset);

        var ctorParams = new AssetViewModel.ConstructorParameters(assetItem, projectVm.AssetMountPoint, false);
        var effectLogVm = new EffectLogViewModel(ctorParams);
        Assert.NotNull(effectLogVm);

        effectLogVm.Text = "CustomLogEntry\n";
        Assert.Equal("CustomLogEntry\n", effectLogVm.Text);
        Assert.Equal("CustomLogEntry\n", effectLogAsset.Text);
    }

    [Fact]
    public void TestEffectCompilerServerSessionLifecycleAndCommandState()
    {
        var (sessionVm, _, packageSession, _) = CreateTestSession();

        var compilerSession = new EffectCompilerServerSession(sessionVm);
        Assert.NotNull(sessionVm.ImportEffectLogCommand);

        // Before CurrentProject is set, CanExecute is false
        Assert.False(sessionVm.ImportEffectLogCommand.CanExecute(null));

        // When project is created and set as current
        var projectVm = CreateProject(sessionVm, packageSession, isCurrent: true);
        Assert.True(sessionVm.ImportEffectLogCommand.CanExecute(null));

        // Dynamic package tracking via LocalPackages
        sessionVm.LocalPackages.Add(projectVm);

        // Disposal untracks packages and clears command
        compilerSession.Dispose();
        Assert.Null(sessionVm.ImportEffectLogCommand);
    }

    [Fact]
    public void TestEffectCompilerServerSessionHandleRequestedPacketAndNotification()
    {
        var (sessionVm, _, packageSession, dialogService) = CreateTestSession();
        var projectVm = CreateProject(sessionVm, packageSession, isCurrent: true);
        sessionVm.LocalPackages.Add(projectVm);

        var compilerSession = new EffectCompilerServerSession(sessionVm);

        // Create serialized EffectCompileRequest
        var compileRequest = new EffectCompileRequest("MyEffectShader", new CompilerParameters());
        var memStream = new MemoryStream();
        var binaryWriter = new BinarySerializationWriter(memStream);
        binaryWriter.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
        binaryWriter.SerializeExtended(ref compileRequest!, ArchiveMode.Serialize, null);

        var packet = new RemoteEffectCompilerEffectRequested
        {
            Request = memStream.ToArray()
        };

        // Invoke private HandleEffectCompilerRequestedPacket via reflection
        var handleMethod = typeof(EffectCompilerServerSession).GetMethod(
            "HandleEffectCompilerRequestedPacket",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(handleMethod);

        handleMethod.Invoke(compilerSession, [packet, projectVm]);

        Assert.Equal(1, sessionVm.ImportEffectLogPendingCount);
        Assert.Single(dialogService.ShownNotifications);
        Assert.Equal("New effects to import", dialogService.ShownNotifications[0].Title);

        // Submitting duplicate request should not increment pending count
        handleMethod.Invoke(compilerSession, [packet, projectVm]);
        Assert.Equal(1, sessionVm.ImportEffectLogPendingCount);

        compilerSession.Dispose();
    }

    [Fact]
    public void TestImportEffectLogCommandExecutionAndUndoRedo()
    {
        var (sessionVm, _, packageSession, _) = CreateTestSession();
        var projectVm = CreateProject(sessionVm, packageSession, isCurrent: true);
        sessionVm.LocalPackages.Add(projectVm);

        var compilerSession = new EffectCompilerServerSession(sessionVm);

        var compileRequest = new EffectCompileRequest("TestUndoRedoEffect", new CompilerParameters());
        var memStream = new MemoryStream();
        var binaryWriter = new BinarySerializationWriter(memStream);
        binaryWriter.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
        binaryWriter.SerializeExtended(ref compileRequest!, ArchiveMode.Serialize, null);

        var packet = new RemoteEffectCompilerEffectRequested
        {
            Request = memStream.ToArray()
        };

        var handleMethod = typeof(EffectCompilerServerSession).GetMethod(
            "HandleEffectCompilerRequestedPacket",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        handleMethod!.Invoke(compilerSession, [packet, projectVm]);

        Assert.Equal(1, sessionVm.ImportEffectLogPendingCount);
        Assert.True(sessionVm.ImportEffectLogCommand!.CanExecute(null));

        // Execute import
        sessionVm.ImportEffectLogCommand.Execute(null);

        // Count should reset to 0
        Assert.Equal(0, sessionVm.ImportEffectLogPendingCount);

        // Verify EffectLogAsset was created
        var effectLogAsset = projectVm.Assets.FirstOrDefault(a => a.Name == EffectLogAsset.DefaultFile) as EffectLogViewModel;
        Assert.NotNull(effectLogAsset);
        Assert.False(string.IsNullOrEmpty(effectLogAsset.Text));

        // Test Undo
        var undoRedo = sessionVm.UndoRedoService;
        Assert.NotNull(undoRedo);
        Assert.True(undoRedo.CanUndo);
        undoRedo.Undo();

        // After undo, pending count should be restored
        Assert.Equal(1, sessionVm.ImportEffectLogPendingCount);

        // Test Redo
        Assert.True(undoRedo.CanRedo);
        undoRedo.Redo();
        Assert.Equal(0, sessionVm.ImportEffectLogPendingCount);

        compilerSession.Dispose();
    }
}
