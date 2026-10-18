// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Editor.AssetEditors.ScriptEditor.Converters;
using Stride.Assets.Editor.ValueConverters;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestScriptEditorViewModelsAndWorkspaceIntegration
{
    private static readonly Assembly AssetsEditorAssembly = typeof(SceneEditorViewModel).Assembly;

    [Fact]
    public async Task TestRoslynHostAndWorkspaceTypesAndLifecycle()
    {
        var host = new RoslynHost();
        Assert.NotNull(host);
        Assert.NotNull(host.HostServices);
        Assert.NotNull(host.Workspace);
        Assert.NotNull(host.ParseOptions);

        var ws = host.Workspace;
        Assert.True(ws.CanOpenDocuments);

        // 1. Cria e adiciona um projeto
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "IntegrationTestProject",
            "IntegrationTestAssembly",
            LanguageNames.CSharp,
            parseOptions: host.ParseOptions);

        ws.AddProject(projectInfo);
        Assert.Contains(ws.CurrentSolution.Projects, p => p.Id == projectId);

        // 2. Adiciona um documento
        var docPath = Path.Combine(Path.GetTempPath(), "TestFile.cs");
        var docId = ws.AddDocument(projectId, docPath);
        Assert.NotNull(docId);

        var doc = ws.GetDocument(docId);
        Assert.NotNull(doc);
        Assert.Equal(docPath, doc.FilePath);

        // 3. Atualiza o texto do documento
        var newSource = SourceText.From("public class MyTestScript { public int Value => 42; }");
        ws.UpdateDocument(docId, newSource);
        var updatedDoc = ws.GetDocument(docId);
        Assert.NotNull(updatedDoc);
        var docSourceText = await updatedDoc.GetTextAsync();
        var docText = docSourceText.ToString();
        Assert.Contains("MyTestScript", docText);

        // 4. Testa abertura e fechamento de documento
        var textContainer = new ScriptSourceTextContainer(new ScriptTextDocument(docText));
        var openedDocId = ws.OpenDocument(textContainer, docId, null);
        Assert.Equal(docId, openedDocId);
        Assert.True(ws.IsDocumentOpen(docId));

        ws.CloseDocument(docId);
        Assert.False(ws.IsDocumentOpen(docId));

        // 5. Remove documento e projeto
        ws.RemoveDocument(docId);
        Assert.DoesNotContain(ws.CurrentSolution.GetProject(projectId)!.Documents, d => d.Id == docId);

        ws.RemoveProject(projectId);
        Assert.DoesNotContain(ws.CurrentSolution.Projects, p => p.Id == projectId);
    }

    [Fact]
    public void TestMetadataUtilTypeAndMethods()
    {
        var path = MetadataUtil.GetAssemblyPath(typeof(object).Assembly);
        Assert.NotNull(path);
        Assert.EndsWith(".dll", path, StringComparison.OrdinalIgnoreCase);

        var types = MetadataUtil.LoadTypesByNamespaces(typeof(object).Assembly, "System");
        Assert.NotEmpty(types);
        Assert.Contains(types, t => t.FullName == "System.String" || t.FullName == "System.Object");

        var typeInfo = new MetadataUtil.TypeInfo("MyNamespace", "MyClass");
        Assert.Equal("MyNamespace.MyClass", typeInfo.FullName);
    }

    [Fact]
    public void TestScriptSourceFileAssetViewModelTypeAndTextTracking()
    {
        var vmType = typeof(ScriptSourceFileAssetViewModel);
        Assert.True(typeof(SourceCodeViewModel<ScriptSourceFileAsset>).IsAssignableFrom(vmType),
            "ScriptSourceFileAssetViewModel deve herdar de SourceCodeViewModel<ScriptSourceFileAsset>");

        // Testar ScriptTextDocument e ScriptUndoStack
        var textDoc = new ScriptTextDocument("initial script text");
        Assert.Equal("initial script text", textDoc.Text);
        Assert.True(textDoc.UndoStack.IsOriginalFile);

        var changeFired = false;
        textDoc.Changed += (s, e) => { changeFired = true; };
        textDoc.Text = "modified script text";

        Assert.True(changeFired);
        Assert.Equal("modified script text", textDoc.Text);
        Assert.False(textDoc.UndoStack.IsOriginalFile);

        textDoc.UndoStack.MarkAsOriginalFile();
        Assert.True(textDoc.UndoStack.IsOriginalFile);

        // Testar ScriptSourceTextContainer
        using var container = new ScriptSourceTextContainer(textDoc);
        Assert.Equal("modified script text", container.CurrentText.ToString());

        var containerChanged = false;
        container.TextChanged += (s, e) => { containerChanged = true; };
        container.UpdateText(SourceText.From("new text from source"));

        Assert.True(containerChanged);
        Assert.Equal("new text from source", container.CurrentText.ToString());
        Assert.Equal("new text from source", textDoc.Text);
    }

    [Fact]
    public void TestScriptEditorViewModelTypeAndDiagnostics()
    {
        var editorVmType = typeof(ScriptEditorViewModel);
        Assert.True(typeof(AssetEditorViewModel).IsAssignableFrom(editorVmType));

        var docOpenedEvent = editorVmType.GetEvent("DocumentOpened");
        Assert.NotNull(docOpenedEvent);

        var docClosedEvent = editorVmType.GetEvent("DocumentClosed");
        Assert.NotNull(docClosedEvent);

        var processDiagnosticsEvent = editorVmType.GetEvent("ProcessDiagnostics");
        Assert.NotNull(processDiagnosticsEvent);

        var codeProp = editorVmType.GetProperty("Code");
        Assert.NotNull(codeProp);

        var workspaceProp = editorVmType.GetProperty("Workspace");
        Assert.NotNull(workspaceProp);

        var roslynHostProp = editorVmType.GetProperty("RoslynHost");
        Assert.NotNull(roslynHostProp);
    }

    [Fact]
    public void TestScriptConvertersTypesAndBehaviors()
    {
        // 1. ClassificationHighlightColorsDark
        var darkColors = new ClassificationHighlightColorsDark();
        Assert.Equal(ClassificationHighlightColorsDark.DefaultColor, darkColors.Default);
        Assert.Equal(ClassificationHighlightColorsDark.TypeColor, darkColors.Type);
        Assert.Equal(ClassificationHighlightColorsDark.KeywordColor, darkColors.Keyword);
        Assert.Equal(ClassificationHighlightColorsDark.CommentColor, darkColors.Comment);
        Assert.Equal(ClassificationHighlightColorsDark.XmlCommentColor, darkColors.XmlComment);
        Assert.Equal(ClassificationHighlightColorsDark.PreprocessorKeywordColor, darkColors.PreprocessorKeyword);
        Assert.Equal(ClassificationHighlightColorsDark.StringColor, darkColors.String);
        Assert.Equal(ClassificationHighlightColorsDark.MethodColor, darkColors.Method);

        // 2. ScriptToEntityAndScript
        var scriptToEntityConverter = ScriptToEntityAndScript.Instance;
        var nullResult = scriptToEntityConverter.Convert(null);
        Assert.Equal(string.Empty, nullResult);

        // 3. CodeActionsConverter & CodeActionToGlyphConverter
        var actionsConverter = CodeActionsConverter.Instance;
        Assert.NotNull(actionsConverter);
        Assert.Null(actionsConverter.Convert(null));

        var glyphConverter = CodeActionToGlyphConverter.Instance;
        Assert.NotNull(glyphConverter);
        Assert.Null(glyphConverter.Convert(null));
    }
}
