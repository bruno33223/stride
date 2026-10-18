// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using RoslynAccessibility = Microsoft.CodeAnalysis.Accessibility;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<VisualScriptViewModel>]
public partial class VisualScriptEditorViewModel : AssetEditorViewModel<VisualScriptViewModel>
{
    private static readonly object NewMethodSymbol = new();

    protected readonly GraphViewModelService ViewModelService;
    private readonly MemberGraphNodeBinding<string> baseTypeNodeBinding;
    private readonly IMemberNode propertiesNode;
    private readonly IObjectNode methodsNode;

    private readonly Lazy<BlockTemplateDescriptionCollectionViewModel> blockTemplateDescriptionCollection;

    private GraphViewModel? properties;

    private IEntityComponentSourceCodeResolver? sourceResolver;
    private SemanticModel? semanticModel;

    private bool symbolSearchOpen;
    private string? symbolSearchText;
    private ISymbol? symbolSearchValidatedItem;

    private VisualScriptMethodViewModel? selectedMethod;
    private VisualScriptMethodEditorViewModel? visibleMethod;
    private Task? lastSwitchFunctionEditor;

    public VisualScriptEditorViewModel(VisualScriptViewModel visualScript)
        : this(null, visualScript)
    {
    }

    public VisualScriptEditorViewModel(IVisualScriptViewModelService? visualScriptViewModelService, VisualScriptViewModel visualScript)
        : base(visualScript)
    {
        // Create the service needed to manage observable view models
        ViewModelService = new GraphViewModelService(Session.AssetNodeContainer);

        // Update the service provider of this view model
        ServiceProvider = new ViewModelServiceProvider(ServiceProvider, ViewModelService.Yield());

        VisualScriptViewModelService = visualScriptViewModelService ?? ServiceProvider.TryGet<IVisualScriptViewModelService>();

        blockTemplateDescriptionCollection = new Lazy<BlockTemplateDescriptionCollectionViewModel>(() => new BlockTemplateDescriptionCollectionViewModel(this));

        AddNewPropertyCommand = new AnonymousCommand(ServiceProvider, AddNewProperty);
        RemoveSelectedPropertiesCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedProperties);

        AddNewMethodCommand = new AnonymousCommand<object?>(ServiceProvider, AddNewMethod);
        RemoveSelectedMethodCommand = new AnonymousCommand(ServiceProvider, RemoveSelectedFunction);

        var rootNode = Session.AssetNodeContainer.GetNode(visualScript.Asset);
        baseTypeNodeBinding = new MemberGraphNodeBinding<string>(rootNode[nameof(VisualScriptAsset.BaseType)], nameof(BaseType), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        propertiesNode = rootNode[nameof(VisualScriptAsset.Properties)];
        methodsNode = rootNode[nameof(VisualScriptAsset.Methods)].Target;

        methodsNode.ItemChanged += MethodsContentChanged;
        foreach (var method in visualScript.Asset.Methods)
            Methods.Add(new VisualScriptMethodViewModel(this, method));
    }

    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    public IVisualScriptViewModelService? VisualScriptViewModelService { get; set; }

    public ObservableList<VisualScriptMethodViewModel> Methods { get; } = new();

    public SemanticModel? SemanticModel
    {
        get => semanticModel;
        set => SetValue(ref semanticModel, value);
    }

    public ObservableList<object> OverridableMethods { get; } = new() { NewMethodSymbol };

    #region Symbol search properties
    public bool SymbolSearchOpen
    {
        get => symbolSearchOpen;
        set => SetValue(ref symbolSearchOpen, value);
    }

    public string? SymbolSearchText
    {
        get => symbolSearchText;
        set => SetValue(ref symbolSearchText, value);
    }

    public ISymbol? SymbolSearchValidatedItem
    {
        get => symbolSearchValidatedItem;
        set => SetValue(ref symbolSearchValidatedItem, value);
    }

    public ObservableCollection<ISymbol> SymbolSearchValues { get; } = new();
    #endregion

    #region Commands
    public ICommandBase AddNewPropertyCommand { get; }

    public ICommandBase RemoveSelectedPropertiesCommand { get; }

    public ICommandBase AddNewMethodCommand { get; }

    public ICommandBase RemoveSelectedMethodCommand { get; }
    #endregion

    public BlockTemplateDescriptionCollectionViewModel BlockTemplateDescriptionCollection => blockTemplateDescriptionCollection.Value;

    public string BaseType
    {
        get => baseTypeNodeBinding.Value;
        set => baseTypeNodeBinding.Value = value;
    }

    public NodeViewModel? Properties => properties?.RootNode;

    public ObservableList<object> SelectedProperties { get; } = new();

    public VisualScriptMethodViewModel? SelectedMethod
    {
        get => selectedMethod;
        set
        {
            var previousFunction = selectedMethod;
            if (SetValue(ref selectedMethod, value))
            {
                lastSwitchFunctionEditor = SwitchFunctionEditor(previousFunction, selectedMethod);
            }
        }
    }

    public VisualScriptMethodEditorViewModel? VisibleMethod
    {
        get => visibleMethod;
        private set => SetValue(ref visibleMethod, value);
    }

    /// <inheritdoc/>
    public override async Task<bool> Initialize()
    {
        sourceResolver = ServiceProvider.TryGet<IEntityComponentSourceCodeResolver>();
        if (sourceResolver != null)
        {
            if (sourceResolver.LatestCompilation == null)
            {
                var compilationReady = new TaskCompletionSource<bool>();
                EventHandler compilationChange = (sender, args) => compilationReady.TrySetResult(true);

                sourceResolver.LatestCompilationChanged += compilationChange;
                await Task.WhenAny(compilationReady.Task, Task.Delay(500));
                sourceResolver.LatestCompilationChanged -= compilationChange;
            }

            sourceResolver.LatestCompilationChanged += CompilationUpdated;
        }

        properties = GraphViewModel.Create(ServiceProvider, new[] { new SinglePropertyProvider(propertiesNode.Target) });

        await RegenerateSlots();

        Session.AssetPropertiesChanged += Session_AssetPropertiesChanged;

        SelectedMethod = Methods.FirstOrDefault();

        TriggerBackgroundCompilation().Forget();

        return true;
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        if (selectedMethod != null)
        {
            SwitchFunctionEditor(selectedMethod, null).Wait();
        }

        methodsNode.ItemChanged -= MethodsContentChanged;
        Session.AssetPropertiesChanged -= Session_AssetPropertiesChanged;

        if (sourceResolver != null)
        {
            sourceResolver.LatestCompilationChanged -= CompilationUpdated;
        }

        properties?.Destroy();
        baseTypeNodeBinding.Dispose();

        base.Destroy();
    }

    private async void CompilationUpdated(object? sender, EventArgs e)
    {
        await RegenerateSlots();
    }

    private async Task RegenerateSlots()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        foreach (var function in Methods)
        {
            await function.RegenerateSlots();
        }
        UndoRedoService.SetName(transaction, "Regenerated slots (code update)");
    }

    private void AddNewProperty()
    {
        var count = Properties?.Children.Count ?? Asset.Asset.Properties.Count;
        var property = new Property("bool", $"Member{count}");
        propertiesNode.Target.Add(property);
    }

    private void RemoveSelectedProperties()
    {
        using var transaction = UndoRedoService.CreateTransaction();
        var propertiesToDelete = SelectedProperties
            .Select(x => x is NodeViewModel nvm ? nvm.NodeValue as Property : x as Property)
            .Where(x => x != null)
            .ToList();

        foreach (var property in propertiesToDelete)
        {
            var index = Asset.Asset.Properties.IndexOf(property!);
            if (index >= 0)
                propertiesNode.Target.Remove(property!, new NodeIndex(index));
        }

        UndoRedoService.SetName(transaction, "Delete property(ies)");
    }

    private void AddNewMethod(object? methodSymbolObject)
    {
        Method method;
        if (methodSymbolObject is not IMethodSymbol methodSymbol)
        {
            method = new Method($"Method{Methods.Count}");
        }
        else
        {
            method = new Method(methodSymbol.Name);

            foreach (var parameterSymbol in methodSymbol.Parameters)
            {
                if (parameterSymbol.IsThis)
                    continue;

                var parameter = new Parameter
                {
                    Name = parameterSymbol.Name,
                    Type = parameterSymbol.Type.ToDisplayString(),
                };

                if (string.IsNullOrEmpty(parameter.Name))
                    parameter.Name = $"param{parameterSymbol.Ordinal + 1}";

                switch (parameterSymbol.RefKind)
                {
                    case RefKind.None:
                        break;
                    case RefKind.Out:
                        parameter.RefKind = ParameterRefKind.Out;
                        break;
                    case RefKind.Ref:
                        parameter.RefKind = ParameterRefKind.Ref;
                        break;
                }

                method.Parameters.Add(parameter);
            }

            switch (methodSymbol.DeclaredAccessibility)
            {
                case RoslynAccessibility.Protected:
                    method.Accessibility = Scripts.Accessibility.Protected;
                    break;
                case RoslynAccessibility.Internal:
                    method.Accessibility = Scripts.Accessibility.Internal;
                    break;
                case RoslynAccessibility.ProtectedOrInternal:
                    method.Accessibility = Scripts.Accessibility.ProtectedOrInternal;
                    break;
                case RoslynAccessibility.Public:
                    method.Accessibility = Scripts.Accessibility.Public;
                    break;
                default:
                    method.Accessibility = Scripts.Accessibility.Protected;
                    break;
            }

            method.VirtualModifier = methodSymbol.ContainingType.TypeKind == TypeKind.Interface
                ? VirtualModifier.Virtual
                : VirtualModifier.Override;

            method.ReturnType = methodSymbol.ReturnType.ToDisplayString();
        }

        methodsNode.Add(method);
    }

    private void RemoveSelectedFunction()
    {
        if (SelectedMethod == null)
            return;

        using var transaction = UndoRedoService.CreateTransaction();
        UndoRedoService.SetName(transaction, $"Delete function {SelectedMethod.Method.Name}");

        var index = Asset.Asset.Methods.IndexOf(SelectedMethod.Method);
        if (index >= 0)
            methodsNode.Remove(SelectedMethod.Method, new NodeIndex(index));
    }

    private void MethodsContentChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.None:
            case ContentChangeType.CollectionAdd:
            {
                var function = (Method)e.NewValue!;
                var vm = new VisualScriptMethodViewModel(this, function);
                Methods.Insert(e.Index.Int, vm);
                SelectedMethod ??= vm;
                break;
            }
            case ContentChangeType.CollectionRemove:
            {
                var vm = Methods[e.Index.Int];
                if (SelectedMethod == vm)
                    SelectedMethod = Methods.FirstOrDefault(m => m != vm);
                vm?.Destroy();
                Methods.RemoveAt(e.Index.Int);
                break;
            }
        }
    }

    private async Task SwitchFunctionEditor(VisualScriptMethodViewModel? previousMethod, VisualScriptMethodViewModel? newMethod)
    {
        if (lastSwitchFunctionEditor != null)
            await lastSwitchFunctionEditor;

        if (previousMethod != null)
        {
            VisibleMethod?.Destroy();
            VisibleMethod = null;
        }

        if (newMethod != null)
        {
            var functionEditor = new VisualScriptMethodEditorViewModel(this, newMethod);
            await functionEditor.Initialize();
            VisibleMethod = functionEditor;
        }
    }

    #region Symbol Search
    public async Task<ISymbol?> AskUserForSymbol()
    {
        ISymbol? result = null;

        using (new SymbolSearchHelper(this))
        {
            var userClosed = new TaskCompletionSource<bool>();
            PropertyChangedEventHandler propertyChanged = (sender, e) =>
            {
                if (e.PropertyName == nameof(SymbolSearchOpen) && SymbolSearchOpen == false)
                    userClosed.TrySetResult(true);
            };

            PropertyChanged += propertyChanged;
            await userClosed.Task;
            PropertyChanged -= propertyChanged;

            if (SymbolSearchValidatedItem != null)
            {
                result = symbolSearchValidatedItem;
                symbolSearchValidatedItem = null;
            }
        }

        return result;
    }

    private sealed class SymbolSearchHelper : IDisposable
    {
        private const int MaxResults = 100;
        private readonly VisualScriptEditorViewModel viewModel;
        private readonly IEntityComponentSourceCodeResolver? sourceResolver;
        private CancellationTokenSource? symbolSearchCancellationToken;

        public SymbolSearchHelper(VisualScriptEditorViewModel viewModel)
        {
            this.viewModel = viewModel;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.SymbolSearchValues.Clear();
            viewModel.SymbolSearchOpen = true;

            sourceResolver = viewModel.ServiceProvider.TryGet<IEntityComponentSourceCodeResolver>();

            symbolSearchCancellationToken = new CancellationTokenSource();
            if (sourceResolver?.LatestCompilation != null)
            {
                Task.Run(() => GenerateSymbolSearchValues(sourceResolver.LatestCompilation, symbolSearchCancellationToken.Token)).Forget();
                sourceResolver.LatestCompilationChanged += RestartSearch;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SymbolSearchText))
            {
                RestartSearch(this, EventArgs.Empty);
            }
        }

        private void RestartSearch(object? sender, EventArgs e)
        {
            symbolSearchCancellationToken?.Cancel();
            symbolSearchCancellationToken = new CancellationTokenSource();
            viewModel.Dispatcher.Invoke(() => viewModel.SymbolSearchValues.Clear());
            if (sourceResolver?.LatestCompilation != null)
            {
                Task.Run(() => GenerateSymbolSearchValues(sourceResolver.LatestCompilation, symbolSearchCancellationToken.Token));
            }
        }

        public void Dispose()
        {
            if (sourceResolver != null)
            {
                sourceResolver.LatestCompilationChanged -= RestartSearch;
            }
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            symbolSearchCancellationToken?.Cancel();
            symbolSearchCancellationToken = null;

            viewModel.SymbolSearchText = null;
            viewModel.SymbolSearchValues.Clear();
            viewModel.SymbolSearchOpen = false;
        }

        private void GenerateSymbolSearchValues(Compilation latestCompilation, CancellationToken cancellationToken)
        {
            int count = 0;

            foreach (var assembly in GetAssemblies(latestCompilation, cancellationToken)
                .OrderBy(assembly =>
                {
                    if (SymbolEqualityComparer.Default.Equals(assembly, latestCompilation.Assembly))
                        return 0;

                    if (assembly.Name.Contains("Stride"))
                        return 1;

                    if (assembly.Name == "mscorlib" || assembly.Name.StartsWith("System", StringComparison.Ordinal))
                        return 3;

                    return 2;
                }))
            {
                var methods = new List<IMethodSymbol>();

                foreach (var type in GetAllTypes(assembly, cancellationToken)
                    .Where(type => type.DeclaredAccessibility != RoslynAccessibility.Private || SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, latestCompilation.Assembly)))
                {
                    foreach (var method in type.GetMembers().OfType<IMethodSymbol>()
                        .Where(member => member.DeclaredAccessibility != RoslynAccessibility.Private || SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, latestCompilation.Assembly)))
                    {
                        if (method.MethodKind != MethodKind.Ordinary
                            && method.MethodKind != MethodKind.UserDefinedOperator
                            && method.MethodKind != MethodKind.BuiltinOperator
                            && method.MethodKind != MethodKind.Conversion)
                            continue;

                        var methodString = method.ToDisplayString();
                        if (!string.IsNullOrEmpty(viewModel.SymbolSearchText) && !viewModel.SymbolSearchText.MatchCamelCase(methodString))
                            continue;

                        if (count++ > MaxResults)
                            break;

                        methods.Add(method);
                    }

                    if (count > MaxResults)
                        break;
                }

                if (methods.Count > 0)
                {
                    viewModel.Dispatcher.Invoke(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        foreach (var method in methods)
                            viewModel.SymbolSearchValues.Add(method);
                    });
                }
            }
        }

        private static IEnumerable<IAssemblySymbol> GetAssemblies(Compilation compilation, CancellationToken cancellationToken)
        {
            var stack = new Stack<IAssemblySymbol>();
            var processedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default)
            {
                compilation.Assembly
            };
            stack.Push(compilation.Assembly);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = stack.Pop();
                yield return current;

                foreach (var module in current.Modules)
                {
                    foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
                    {
                        if (processedAssemblies.Add(referencedAssembly))
                        {
                            stack.Push(referencedAssembly);
                        }
                    }
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(IAssemblySymbol assemblySymbol, CancellationToken cancellationToken)
        {
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(assemblySymbol.GlobalNamespace);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();
                if (current is INamespaceSymbol currentNs)
                {
                    foreach (var member in currentNs.GetMembers())
                        stack.Push(member);
                }
                else if (current is INamedTypeSymbol namedType)
                {
                    foreach (var member in namedType.GetTypeMembers())
                        stack.Push(member);
                    yield return namedType;
                }
            }
        }
    }
    #endregion
}
