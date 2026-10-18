// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using DirectoryBaseViewModel = Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestCodeSolutionAndWorkspaceSynchronizationIntegration
{
    [Fact]
    public void TestCodeViewModelContractAndProperties()
    {
        var type = typeof(CodeViewModel);
        Assert.True(typeof(DispatcherViewModel).IsAssignableFrom(type), "CodeViewModel deve herdar de DispatcherViewModel");
        Assert.True(typeof(IDisposable).IsAssignableFrom(type), "CodeViewModel deve implementar IDisposable");

        // Verifica constantes de font size
        var minSizeField = type.GetField("MinimumEditorFontSize", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(minSizeField);
        Assert.Equal(8, (int)minSizeField.GetValue(null)!);

        var maxSizeField = type.GetField("MaximumEditorFontSize", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(maxSizeField);
        Assert.Equal(72, (int)maxSizeField.GetValue(null)!);

        // Verifica propriedade EditorFontSize
        var fontSizeProp = type.GetProperty("EditorFontSize", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(fontSizeProp);
        Assert.Equal(typeof(int), fontSizeProp.PropertyType);

        // Verifica propriedades desacopladas de cores
        var keywordColorProp = type.GetProperty("KeywordColor", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(keywordColorProp);
        Assert.Equal(typeof(Color), keywordColorProp.PropertyType);

        var typeColorProp = type.GetProperty("TypeColor", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(typeColorProp);
        Assert.Equal(typeof(Color), typeColorProp.PropertyType);

        // Verifica Logger e Tasks assíncronas
        var loggerProp = type.GetProperty("Logger", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(loggerProp);
        Assert.Equal(typeof(LoggerResult), loggerProp.PropertyType);

        var workspaceProp = type.GetProperty("Workspace", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(workspaceProp);
        Assert.Equal(typeof(Task<RoslynWorkspace>), workspaceProp.PropertyType);

        var watcherProp = type.GetProperty("ProjectWatcher", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(watcherProp);
        Assert.Equal(typeof(Task<ProjectWatcher>), watcherProp.PropertyType);
    }

    [Fact]
    public void TestCodeViewModelSyntaxHighlightingMethodsContract()
    {
        var type = typeof(CodeViewModel);

        // Verifica GetColorFromSymbolDisplayPartKind
        var methodSymbol = type.GetMethod("GetColorFromSymbolDisplayPartKind", BindingFlags.Public | BindingFlags.Instance, null, [typeof(SymbolDisplayPartKind)], null);
        Assert.NotNull(methodSymbol);
        Assert.Equal(typeof(Color?), methodSymbol.ReturnType);

        // Verifica GetColorFromTextTag
        var methodTag = type.GetMethod("GetColorFromTextTag", BindingFlags.Public | BindingFlags.Instance, null, [typeof(string)], null);
        Assert.NotNull(methodTag);
        Assert.Equal(typeof(Color?), methodTag.ReturnType);
    }

    [Fact]
    public void TestCodeViewModelWorkspaceSyncMethodsContract()
    {
        var type = typeof(CodeViewModel);

        // Verifica ReloadProject
        var reloadProjectMethod = type.GetMethod("ReloadProject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(reloadProjectMethod);
        Assert.Equal(typeof(Task), reloadProjectMethod.ReturnType);

        // Verifica DeleteRemovedProjectAssets
        var deleteMethod = type.GetMethod("DeleteRemovedProjectAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(deleteMethod);
        Assert.Equal(typeof(Task<bool>), deleteMethod.ReturnType);

        // Verifica AddNewProjectAssets
        var addMethod = type.GetMethod("AddNewProjectAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(addMethod);
        Assert.True(addMethod.ReturnType == typeof(Task) || addMethod.ReturnType == typeof(void));

        // Verifica GetAssets
        var getAssetsMethod = type.GetMethod("GetAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(getAssetsMethod);
        Assert.Equal(typeof(bool), getAssetsMethod.ReturnType);

        // Verifica UpdateDirtiness
        var updateDirtinessMethod = type.GetMethod("UpdateDirtiness", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(updateDirtinessMethod);
        Assert.Equal(typeof(void), updateDirtinessMethod.ReturnType);
    }

    [Fact]
    public void TestClassificationHighlightColorsDarkValues()
    {
        // Garante que as cores de destaque desacopladas correspondem aos padrões
        Assert.Equal(new Color(86, 156, 214), ClassificationHighlightColorsDark.KeywordColor);
        Assert.Equal(new Color(78, 201, 176), ClassificationHighlightColorsDark.TypeColor);
        Assert.Equal(new Color(87, 166, 74), ClassificationHighlightColorsDark.CommentColor);
        Assert.Equal(new Color(214, 157, 133), ClassificationHighlightColorsDark.StringColor);
    }

    [Fact]
    public void TestCodeViewModelColorHighlightingLogic()
    {
        var vm = (CodeViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CodeViewModel));
        typeof(CodeViewModel).GetField("keywordColor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(vm, ClassificationHighlightColorsDark.KeywordColor);
        typeof(CodeViewModel).GetField("typeColor", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(vm, ClassificationHighlightColorsDark.TypeColor);

        Assert.Equal(ClassificationHighlightColorsDark.KeywordColor, vm.KeywordColor);
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.TypeColor);

        // 1. Testa GetColorFromSymbolDisplayPartKind
        Assert.Equal(ClassificationHighlightColorsDark.KeywordColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.Keyword));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.ClassName));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.StructName));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.InterfaceName));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.EnumName));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.DelegateName));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.TypeParameterName));

        // Part kinds não coloridos devem retornar null
        Assert.Null(vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.Text));
        Assert.Null(vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.MethodName));
        Assert.Null(vm.GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind.PropertyName));

        // 2. Testa GetColorFromTextTag
        Assert.Equal(ClassificationHighlightColorsDark.KeywordColor, vm.GetColorFromTextTag(TextTags.Keyword));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromTextTag(TextTags.Class));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromTextTag(TextTags.Struct));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromTextTag(TextTags.Interface));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromTextTag(TextTags.Enum));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromTextTag(TextTags.Delegate));
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, vm.GetColorFromTextTag(TextTags.TypeParameter));

        // Text tags não coloridas devem retornar null
        Assert.Null(vm.GetColorFromTextTag(TextTags.Text));
        Assert.Null(vm.GetColorFromTextTag(TextTags.Method));
        Assert.Null(vm.GetColorFromTextTag("UnknownTag"));
    }

    [Fact]
    public void TestCodeViewModelFontSizeBounds()
    {
        var vm = (CodeViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CodeViewModel));
        typeof(CodeViewModel).GetField("editorFontSize", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(vm, 14);

        Assert.Equal(14, vm.EditorFontSize);

        // Valor abaixo do mínimo (8) não deve ser aceito
        vm.EditorFontSize = 5;
        Assert.Equal(14, vm.EditorFontSize);

        // Valor acima do máximo (72) não deve ser aceito
        vm.EditorFontSize = 100;
        Assert.Equal(14, vm.EditorFontSize);
    }

    [Fact]
    public void TestCodeViewModelCooperativeCancellationAndDispose()
    {
        var vm = (CodeViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CodeViewModel));
        var cts = new CancellationTokenSource();
        typeof(CodeViewModel).GetField("cancellationTokenSource", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(vm, cts);

        Assert.False(cts.IsCancellationRequested);

        // Ao descartar, deve cancelar cooperativamente o CancellationTokenSource
        vm.Dispose();

        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public void TestAssemblyChangedEventContract()
    {
        var assemblyChange = new AssemblyChangedEvent(null!, AssemblyChangeType.Source, "MyScript.cs", null!);
        Assert.Equal(AssemblyChangeType.Source, assemblyChange.ChangeType);
        Assert.Equal("MyScript.cs", assemblyChange.ChangedFile);
        Assert.Null(assemblyChange.Assembly);
        Assert.Null(assemblyChange.Project);

        var projectChange = new AssemblyChangedEvent(null!, AssemblyChangeType.Project, "MyProject.csproj", null!);
        Assert.Equal(AssemblyChangeType.Project, projectChange.ChangeType);
        Assert.Equal("MyProject.csproj", projectChange.ChangedFile);
    }
}
