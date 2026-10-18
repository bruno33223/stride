// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Transactions;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para a Fatia 2 (slice-2) do Marco P32:
/// "Debug Tools UserControls & Editor Views".
/// Valida rigorosamente os contratos e implementação dos UserControls e Views Avalonia.
/// </summary>
public class TestDebugToolsAndEditorViewsIntegration
{
    private static readonly Assembly CoreAssetsEditorAvaloniaAssembly = typeof(Stride.Core.Assets.Editor.Avalonia.Views.TypeMatchTemplateProvider).Assembly;
    private static readonly Assembly AssetsEditorAvaloniaAssembly = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.ModelPreviewView).Assembly;

    #region Mock Services & Helpers
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

        public ITransaction CreateTransaction(TransactionFlags flags = TransactionFlags.None) => new MockTransaction();
        public void SetName(Stride.Core.Transactions.Operation operation, string name) { }
        public void SetName(ITransaction transaction, string name) { }
        public string? GetName(Stride.Core.Transactions.Operation operation) => null;
        public string? GetName(ITransaction transaction) => null;
        public string? GetName(IReadOnlyTransaction transaction) => null;
        public void PushOperation(Stride.Core.Transactions.Operation operation) { }
        public void Undo() { }
        public void Redo() { }
        public void NotifySave() { }
        public void Resize(int newCapacity) { }
        public IEnumerable<IReadOnlyTransaction> RetrieveAllTransactions() => Enumerable.Empty<IReadOnlyTransaction>();
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
    #endregion

    // =========================================================================
    // Contrato 01: DebugAssetNodesUserControl - Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract01_DebugAssetNodesUserControl_TypeAndInterfaces_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DebugTools.DebugAssetNodesUserControl");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "DebugAssetNodesUserControl deve herdar de UserControl.");
        Assert.True(typeof(IDebugPage).IsAssignableFrom(type), "DebugAssetNodesUserControl deve implementar IDebugPage.");
    }

    // =========================================================================
    // Contrato 02: DebugAssetNodesUserControl - Construtores e Propriedades
    // =========================================================================
    [Fact]
    public void Contract02_DebugAssetNodesUserControl_ConstructorsAndProperties_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DebugTools.DebugAssetNodesUserControl");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var sessionCtor = type.GetConstructor([typeof(SessionViewModel)]);
        Assert.NotNull(sessionCtor);

        var titleProp = type.GetProperty("Title");
        Assert.NotNull(titleProp);
        Assert.Equal(typeof(string), titleProp.PropertyType);

        var vmProp = type.GetProperty("ViewModel");
        Assert.NotNull(vmProp);
        Assert.True(typeof(DebugAssetNodeCollectionViewModel).IsAssignableFrom(vmProp.PropertyType));
    }

    // =========================================================================
    // Contrato 03: DebugUndoRedoUserControl - Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract03_DebugUndoRedoUserControl_TypeAndInterfaces_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DebugTools.DebugUndoRedoUserControl");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "DebugUndoRedoUserControl deve herdar de UserControl.");
        Assert.True(typeof(IDebugPage).IsAssignableFrom(type), "DebugUndoRedoUserControl deve implementar IDebugPage.");
        Assert.True(typeof(IDestroyable).IsAssignableFrom(type), "DebugUndoRedoUserControl deve implementar IDestroyable.");
    }

    // =========================================================================
    // Contrato 04: DebugUndoRedoUserControl - Construtores e Destruição
    // =========================================================================
    [Fact]
    public void Contract04_DebugUndoRedoUserControl_ConstructorsAndDestroy_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DebugTools.DebugUndoRedoUserControl");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var serviceCtor = type.GetConstructor([typeof(IViewModelServiceProvider), typeof(IUndoRedoService)]);
        Assert.NotNull(serviceCtor);

        var titleProp = type.GetProperty("Title");
        Assert.NotNull(titleProp);

        var destroyMethod = type.GetMethod("Destroy", Type.EmptyTypes);
        Assert.NotNull(destroyMethod);

        var serviceProvider = new ViewModelServiceProvider();
        serviceProvider.RegisterService(new MockDispatcherService());
        var undoRedo = new MockUndoRedoService();

        var instance = Activator.CreateInstance(type, serviceProvider, undoRedo);
        Assert.NotNull(instance);
        Assert.IsAssignableFrom<IDebugPage>(instance);
        Assert.IsAssignableFrom<IDestroyable>(instance);

        var control = (UserControl)instance;
        Assert.NotNull(control.DataContext);
        Assert.IsType<DebugUndoRedoViewModel>(control.DataContext);

        // Testar destruição limpa
        ((IDestroyable)instance).Destroy();
    }

    // =========================================================================
    // Contrato 05: DebugLogUserControl - Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract05_DebugLogUserControl_TypeAndInterfaces_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DebugTools.DebugLogUserControl");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "DebugLogUserControl deve herdar de UserControl.");
        Assert.True(typeof(IDebugPage).IsAssignableFrom(type), "DebugLogUserControl deve implementar IDebugPage.");
    }

    // =========================================================================
    // Contrato 06: DebugLogUserControl - Construtores e Propriedades
    // =========================================================================
    [Fact]
    public void Contract06_DebugLogUserControl_ConstructorsAndProperties_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DebugTools.DebugLogUserControl");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var loggerCtor = type.GetConstructor([typeof(LoggerViewModel)]);
        Assert.NotNull(loggerCtor);

        var titleProp = type.GetProperty("Title");
        Assert.NotNull(titleProp);

        var loggerProp = type.GetProperty("Logger");
        Assert.NotNull(loggerProp);
        Assert.Equal(typeof(LoggerViewModel), loggerProp.PropertyType);

        var serviceProvider = new ViewModelServiceProvider();
        serviceProvider.RegisterService(new MockDispatcherService());
        var loggerVm = new LoggerViewModel(serviceProvider);

        var instance = Activator.CreateInstance(type, loggerVm);
        Assert.NotNull(instance);
        Assert.Same(loggerVm, loggerProp.GetValue(instance));

        var control = (UserControl)instance;
        Assert.Same(loggerVm, control.DataContext);
    }

    // =========================================================================
    // Contrato 07: DebugEntityHierarchyEditorUserControl - Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract07_DebugEntityHierarchyEditorUserControl_TypeAndInterfaces_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.DebugEntityHierarchyEditorUserControl");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "DebugEntityHierarchyEditorUserControl deve herdar de UserControl.");
        Assert.True(typeof(IDebugPage).IsAssignableFrom(type), "DebugEntityHierarchyEditorUserControl deve implementar IDebugPage.");

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var editorCtor = type.GetConstructor([typeof(EntityHierarchyEditorViewModel)]);
        Assert.NotNull(editorCtor);

        var titleProp = type.GetProperty("Title");
        Assert.NotNull(titleProp);
    }

    // =========================================================================
    // Contrato 08: DebugEntityHierarchyEditorViewModel - Tipo Aninhado e Comandos
    // =========================================================================
    [Fact]
    public void Contract08_DebugEntityHierarchyEditorViewModel_NestedTypeAndCommands_Valid()
    {
        var parentType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.DebugEntityHierarchyEditorUserControl");
        Assert.NotNull(parentType);

        var vmType = parentType.GetNestedType("DebugEntityHierarchyEditorViewModel", BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(vmType);
        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(vmType), "DebugEntityHierarchyEditorViewModel deve herdar de DispatcherViewModel.");

        var loadingUrlsProp = vmType.GetProperty("LoadingTimeUrls");
        Assert.NotNull(loadingUrlsProp);
        Assert.True(typeof(ObservableList<string>).IsAssignableFrom(loadingUrlsProp.PropertyType));

        var loadedAssetsProp = vmType.GetProperty("LoadedAssets");
        Assert.NotNull(loadedAssetsProp);
        Assert.True(typeof(ObservableList<string>).IsAssignableFrom(loadedAssetsProp.PropertyType));

        var refreshCmdProp = vmType.GetProperty("RefreshCommand");
        Assert.NotNull(refreshCmdProp);
        Assert.True(typeof(ICommandBase).IsAssignableFrom(refreshCmdProp.PropertyType));
    }

    // =========================================================================
    // Contrato 09: ScriptEditorView - Atributos, Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract09_ScriptEditorView_TypeAttributesAndInterfaces_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor.ScriptEditorView");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "ScriptEditorView deve herdar de UserControl.");
        Assert.True(typeof(IEditorView).IsAssignableFrom(type), "ScriptEditorView deve implementar IEditorView.");
        Assert.True(typeof(IAssetEditorView).IsAssignableFrom(type), "ScriptEditorView deve implementar IAssetEditorView.");

        var attr = type.GetCustomAttribute<AssetEditorViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(ScriptEditorViewModel), attr.EditorViewModelType);
    }

    // =========================================================================
    // Contrato 10: ScriptEditorView - Ciclo de Inicialização
    // =========================================================================
    [Fact]
    public void Contract10_ScriptEditorView_InitializationLifecycle_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor.ScriptEditorView");
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        var editorView = (IEditorView)instance;
        Assert.NotNull(editorView.EditorInitialization);

        var initMethod = type.GetMethod("InitializeEditor", [typeof(IAssetEditorViewModel)]);
        Assert.NotNull(initMethod);
        Assert.Equal(typeof(Task<bool>), initMethod.ReturnType);
    }

    // =========================================================================
    // Contrato 11: UIEditorView - Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract11_UIEditorView_TypeAndInterfaces_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.UIEditor.Views.UIEditorView");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "UIEditorView deve herdar de UserControl.");
        Assert.True(typeof(IEditorView).IsAssignableFrom(type), "UIEditorView deve implementar IEditorView.");
        Assert.True(typeof(IAssetEditorView).IsAssignableFrom(type), "UIEditorView deve implementar IAssetEditorView.");

        var initProp = type.GetProperty("EditorInitialization");
        Assert.NotNull(initProp);
        Assert.Equal(typeof(Task), initProp.PropertyType);

        var initMethod = type.GetMethod("InitializeEditor", [typeof(IAssetEditorViewModel)]);
        Assert.NotNull(initMethod);
        Assert.Equal(typeof(Task<bool>), initMethod.ReturnType);
    }

    // =========================================================================
    // Contrato 12: UIEditorView - Derivação dos Editores de UI
    // =========================================================================
    [Fact]
    public void Contract12_UIEditorView_DerivationAndSubclasses_Valid()
    {
        var baseType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.UIEditor.Views.UIEditorView");
        Assert.NotNull(baseType);

        var uiPageType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.UIPageEditorView");
        Assert.NotNull(uiPageType);

        var uiLibType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.UILibraryEditorView");
        Assert.NotNull(uiLibType);

        Assert.True(baseType.IsAssignableFrom(uiPageType), "UIPageEditorView deve herdar de UIEditorView.");
        Assert.True(baseType.IsAssignableFrom(uiLibType), "UILibraryEditorView deve herdar de UIEditorView.");
    }

    // =========================================================================
    // Contrato 13: GraphicsCompositorEditorView - Atributos, Tipo e Interfaces
    // =========================================================================
    [Fact]
    public void Contract13_GraphicsCompositorEditorView_TypeAttributesAndInterfaces_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.GraphicsCompositorEditor.Views.GraphicsCompositorEditorView");
        Assert.NotNull(type);
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "GraphicsCompositorEditorView deve herdar de UserControl.");
        Assert.True(typeof(IEditorView).IsAssignableFrom(type), "GraphicsCompositorEditorView deve implementar IEditorView.");
        Assert.True(typeof(IAssetEditorView).IsAssignableFrom(type), "GraphicsCompositorEditorView deve implementar IAssetEditorView.");

        var attr = type.GetCustomAttribute<AssetEditorViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(GraphicsCompositorEditorViewModel), attr.EditorViewModelType);
    }

    // =========================================================================
    // Contrato 14: GraphicsCompositorEditorView - Inicialização e Subclasses
    // =========================================================================
    [Fact]
    public void Contract14_GraphicsCompositorEditorView_InitializationLifecycle_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.GraphicsCompositorEditor.Views.GraphicsCompositorEditorView");
        Assert.NotNull(type);

        var existingViewType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.GraphicsCompositorEditorView");
        Assert.NotNull(existingViewType);

        Assert.True(type.IsAssignableFrom(existingViewType) || existingViewType.IsAssignableFrom(type) || type == existingViewType,
            "As implementações de GraphicsCompositorEditorView devem ser compatíveis.");

        var initProp = type.GetProperty("EditorInitialization");
        Assert.NotNull(initProp);

        var initMethod = type.GetMethod("InitializeEditor", [typeof(IAssetEditorViewModel)]);
        Assert.NotNull(initMethod);
    }

    // =========================================================================
    // Contrato 15: StrideEditorViewPlugin - Registro de ScriptEditorView
    // =========================================================================
    [Fact]
    public void Contract15_StrideEditorViewPlugin_RegistersScriptEditorView_Valid()
    {
        var pluginType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.StrideEditorViewPlugin");
        Assert.NotNull(pluginType);

        var plugin = Activator.CreateInstance(pluginType);
        Assert.NotNull(plugin);

        var registerMethod = pluginType.GetMethod("RegisterAssetEditorViewTypes", [typeof(IDictionary<Type, Type>)]);
        Assert.NotNull(registerMethod);

        var dict = new Dictionary<Type, Type>();
        registerMethod.Invoke(plugin, [dict]);

        Assert.True(dict.ContainsKey(typeof(ScriptEditorViewModel)), "StrideEditorViewPlugin deve registrar ScriptEditorViewModel.");
        var registeredView = dict[typeof(ScriptEditorViewModel)];
        var expectedView = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor.ScriptEditorView");
        Assert.Equal(expectedView, registeredView);
    }

    // =========================================================================
    // Contrato 16: Arquivos AXAML Criados e Conteúdo Não Vazio
    // =========================================================================
    [Fact]
    public void Contract16_AxamlFiles_ExistAndValidMarkup_Valid()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "sources", "editor")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        var baseDir = Path.Combine(dir.FullName, "sources", "editor");
        var files = new[]
        {
            Path.Combine(baseDir, "Stride.Core.Assets.Editor.Avalonia/Views/DebugTools/DebugAssetNodesUserControl.axaml"),
            Path.Combine(baseDir, "Stride.Core.Assets.Editor.Avalonia/Views/DebugTools/DebugUndoRedoUserControl.axaml"),
            Path.Combine(baseDir, "Stride.Core.Assets.Editor.Avalonia/Views/DebugTools/DebugLogUserControl.axaml"),
            Path.Combine(baseDir, "Stride.Assets.Editor.Avalonia/Views/DebugEntityHierarchyEditorUserControl.axaml"),
            Path.Combine(baseDir, "Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/ScriptEditorView.axaml"),
        };

        foreach (var file in files)
        {
            Assert.True(File.Exists(file), $"Arquivo AXAML obrigatório não encontrado: {file}");
            var content = File.ReadAllText(file);
            Assert.False(string.IsNullOrWhiteSpace(content), $"Arquivo AXAML vazio: {file}");
            Assert.Contains("http://schemas.microsoft.com/winfx/2006/xaml", content);
        }
    }
}
