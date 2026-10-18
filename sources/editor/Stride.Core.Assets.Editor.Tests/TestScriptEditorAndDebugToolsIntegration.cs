// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Presentation.AssetEditors.ScriptEditor;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using DirectoryBaseViewModel = Stride.Core.Assets.Editor.ViewModel.DirectoryBaseViewModel;
using MountPointViewModel = Stride.Core.Assets.Editor.ViewModel.MountPointViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Transactions;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestScriptEditorAndDebugToolsIntegration
{
    private sealed class MockDispatcherService : IDispatcherService
    {
        public bool CheckAccess() => true;
        public void EnsureAccess(bool inDispatcherThread = true) { }
        public void Invoke(Action callback) => callback();
        public TResult Invoke<TResult>(Func<TResult> callback) => callback();
        public Task InvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }
        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(callback());
        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> callback, CancellationToken token = default) => callback();
        public Task InvokeTask(Func<Task> callback, CancellationToken token = default) => callback();
        public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }
    }

    private sealed class MockUndoRedoService : IUndoRedoService
    {
        public event EventHandler<TransactionEventArgs>? Done;
        public event EventHandler<TransactionEventArgs>? Undone;
        public event EventHandler<TransactionEventArgs>? Redone;
        public event EventHandler<TransactionsDiscardedEventArgs>? TransactionDiscarded;
        public event EventHandler<EventArgs>? Cleared;

        public int Capacity => 10;
        public bool CanUndo => false;
        public bool CanRedo => false;
        public bool TransactionInProgress => false;
        public bool UndoRedoInProgress => false;
        public Task TransactionCompletion => Task.CompletedTask;
        public Task UndoRedoCompletion => Task.CompletedTask;

        public ITransaction CreateTransaction(TransactionFlags flags = TransactionFlags.None)
            => new MockTransaction();

        public void SetName(Operation operation, string name) { }
        public void SetName(ITransaction transaction, string name) { }
        public string? GetName(Operation operation) => null;
        public string? GetName(ITransaction transaction) => null;
        public string? GetName(IReadOnlyTransaction transaction) => null;
        public void PushOperation(Operation operation) { }
        public void Undo() { }
        public void Redo() { }
        public void NotifySave() { }
        public void Resize(int newCapacity) { }

        public IEnumerable<IReadOnlyTransaction> RetrieveAllTransactions()
            => Enumerable.Empty<IReadOnlyTransaction>();

        public void NotifyDone(TransactionEventArgs args) => Done?.Invoke(this, args);
        public void NotifyDiscarded(TransactionsDiscardedEventArgs args) => TransactionDiscarded?.Invoke(this, args);
        public void NotifyCleared() => Cleared?.Invoke(this, EventArgs.Empty);
    }

    private sealed class MockTransaction : ITransaction
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsEmpty => true;
        public void Continue() { }
        public void Complete() { }
        public void AddReference() { }
        public void Dispose() { }
    }

    private sealed class MockDebugPage : IDebugPage, IDestroyable
    {
        public string Title { get; set; } = "Test Page";
        public bool IsDestroyed { get; private set; }
        public void Destroy() => IsDestroyed = true;
    }

    [Fact]
    public void TestAssetViewModelConstructionParametersValidationAndProperties()
    {
        var services = new ViewModelServiceProvider();
        services.RegisterService(new MockDispatcherService());
        services.RegisterService(new MockUndoRedoService());

        var session = new MockSessionViewModel(services);
        var rawAsset = new RawAsset();

        // Testar falhas de validação de argumentos nulos
        Assert.Throws<ArgumentNullException>(() =>
            new AssetViewModelConstructionParameters(services, null!, new Package(), new AssetItem("test", rawAsset), new SessionNodeContainer(), false));

        var dummyDirectory = new MockDirectoryViewModel(session, "Folder");
        Assert.Throws<ArgumentNullException>(() =>
            new AssetViewModelConstructionParameters(services, dummyDirectory, null!, new AssetItem("test", rawAsset), new SessionNodeContainer(), false));

        Assert.Throws<ArgumentNullException>(() =>
            new AssetViewModelConstructionParameters(services, dummyDirectory, new Package(), null!, new SessionNodeContainer(), false));

        Assert.Throws<ArgumentNullException>(() =>
            new AssetViewModelConstructionParameters(services, dummyDirectory, new Package(), new AssetItem("test", rawAsset), null!, false));

        // Testar construção com parâmetros válidos
        var package = new Package();
        var assetItem = new AssetItem("testAsset", rawAsset);
        var container = new SessionNodeContainer();

        var parameters = new AssetViewModelConstructionParameters(services, dummyDirectory, package, assetItem, container, true);

        Assert.Same(services, parameters.ServiceProvider);
        Assert.Same(dummyDirectory, parameters.Directory);
        Assert.Same(package, parameters.Package);
        Assert.Same(assetItem, parameters.AssetItem);
        Assert.Same(container, parameters.Container);
        Assert.True(parameters.CanUndoRedoCreation);
    }

    [Fact]
    public void TestReorderCollectionItemViewModelImplementation()
    {
        var reorderVm = new ReorderCollectionItemViewModel();
        Assert.IsAssignableFrom<IReorderItemViewModel>(reorderVm);

        var canInsert = reorderVm.CanInsertChildren(new List<object> { new object() }, InsertPosition.After, AddChildModifiers.None, out var message);
        Assert.False(canInsert);
        Assert.Equal(string.Empty, message);

        // Operações no-op não devem lançar exceção
        reorderVm.InsertChildren(new List<object> { new object() }, InsertPosition.Before, AddChildModifiers.None);
        reorderVm.SetTargetNode(null!);
    }

    [Fact]
    public void TestDebugUndoRedoViewModelLifecycleAndCommands()
    {
        var services = new ViewModelServiceProvider();
        services.RegisterService(new MockDispatcherService());

        var mockUndoRedo = new MockUndoRedoService();
        var vm = new DebugUndoRedoViewModel(services, mockUndoRedo);

        Assert.NotNull(vm.Transactions);
        Assert.NotNull(vm.DiscardedTransactions);
        Assert.NotNull(vm.ClearDiscardedItemsCommand);
        Assert.NotNull(vm.Title);

        // Testar ClearDiscardedItemsCommand
        vm.DiscardedTransactions.Add(null!);
        Assert.Single(vm.DiscardedTransactions);
        vm.ClearDiscardedItemsCommand.Execute();
        Assert.Empty(vm.DiscardedTransactions);

        // Testar stack cleared
        mockUndoRedo.NotifyCleared();
        Assert.Empty(vm.Transactions);

        // Testar Destroy
        vm.Destroy();
    }

    [Fact]
    public void TestEditorDebugToolsRegistrationAndPages()
    {
        var page = new MockDebugPage { Title = "Custom Debug Tool" };
        EditorDebugTools.RegisterDebugPage(page);

        var services = new ViewModelServiceProvider();
        services.RegisterService(new MockDispatcherService());
        var debugWindow = new DebugWindowViewModel(services);

        EditorDebugTools.RegisterDebugWindow(debugWindow);
        Assert.Contains(debugWindow.Pages, p => p.Title == "Custom Debug Tool");

        EditorDebugTools.UnregisterDebugPage(page);
        Assert.True(page.IsDestroyed);
        Assert.DoesNotContain(debugWindow.Pages, p => p.Title == "Custom Debug Tool");

        EditorDebugTools.UnregisterDebugWindow(debugWindow);
        Assert.Empty(debugWindow.Pages);
    }

    [Fact]
    public void TestSimpleCodeTextEditorAndScriptTextEditorInfrastructure()
    {
        var host = new RoslynHost();
        var workspace = host.Workspace;

        // 1. Validar SimpleCodeTextEditor
        var simpleEditor = new SimpleCodeTextEditor();
        Assert.NotNull(simpleEditor);

        // 2. Validar ScriptTextEditor
        var scriptEditor = new ScriptTextEditor();
        Assert.NotNull(scriptEditor);

        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "ScriptEditorIntegrationTestProject",
            "ScriptEditorIntegrationTestAssembly",
            LanguageNames.CSharp,
            parseOptions: host.ParseOptions);
        workspace.AddProject(projectInfo);

        scriptEditor.Text = "public class ScriptSample { }";
        scriptEditor.ProjectId = projectId;
        scriptEditor.Workspace = workspace;

        Assert.Same(workspace, scriptEditor.Workspace);
        Assert.Equal(projectId, scriptEditor.ProjectId);
        Assert.Equal("public class ScriptSample { }", scriptEditor.Text);

        workspace.RemoveProject(projectId);
    }

    private sealed class MockSessionViewModel : ISessionViewModel
    {
        public MockSessionViewModel(IViewModelServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IViewModelServiceProvider ServiceProvider { get; }
        public IEnumerable<AssetViewModel> AllAssets => Enumerable.Empty<AssetViewModel>();
        public IEnumerable<PackageViewModel> AllPackages => Enumerable.Empty<PackageViewModel>();
        public Stride.Core.Assets.Quantum.AssetNodeContainer AssetNodeContainer => null!;
        public ProjectViewModel? CurrentProject => null;
        public Stride.Core.Assets.Analysis.IAssetDependencyManager DependencyManager => null!;
        public IDispatcherService Dispatcher => ServiceProvider.Get<IDispatcherService>();
        public Stride.Core.Assets.Quantum.AssetPropertyGraphContainer GraphContainer => null!;
        public IObservableCollection<PackageViewModel> LocalPackages => new ObservableList<PackageViewModel>();
        public IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories => new Dictionary<string, PackageCategoryViewModel>();
        public IAssetSourceTrackerViewModel SourceTracker => null!;
        public IObservableCollection<PackageViewModel> StorePackages => new ObservableList<PackageViewModel>();

        public event EventHandler<Stride.Core.Assets.Presentation.ViewModels.AssetChangedEventArgs>? AssetPropertiesChanged;
        public event EventHandler<Stride.Core.Assets.Presentation.ViewModels.SessionStateChangedEventArgs>? SessionStateChanged;

        public AssetViewModel? GetAssetById(AssetId id) => null;
        public Type GetAssetViewModelType(AssetItem assetItem) => typeof(AssetViewModel);
        public void RegisterAsset(AssetViewModel asset) { }
        public void UnregisterAsset(AssetViewModel asset) { }
    }

    private sealed class MockDirectoryViewModel : DirectoryBaseViewModel
    {
        private string name;
        private readonly PackageViewModel package;

        public MockDirectoryViewModel(ISessionViewModel session, string name) : base(session)
        {
            this.name = name;
            package = new MockPackageViewModel(session);
        }

        public override PackageViewModel Package => package;
        public override string Path => Name;
        public override string Name { get => name; set => name = value; }
        public override DirectoryBaseViewModel? Parent { get; set; }
        public override Stride.Core.Assets.Editor.ViewModel.MountPointViewModel Root => null!;
        public override bool IsEditable => true;
        public override string TypeDisplayName => "Folder";
        public override bool CanDelete(out string message) { message = ""; return true; }
        public override void Delete() { }
        protected override void UpdateIsDeletedStatus() { }
    }

    private sealed class MockPackageViewModel : PackageViewModel
    {
        public MockPackageViewModel(ISessionViewModel session) : base(session, new StandalonePackage(new Package()), false) { }
    }
}
