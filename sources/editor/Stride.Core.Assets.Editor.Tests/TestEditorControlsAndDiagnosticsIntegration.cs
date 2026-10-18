// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Xunit;
using Stride.Core.Diagnostics;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

public class TestEditorControlsAndDiagnosticsIntegration
{
    private static string GetSourceRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "stride.sln")) || Directory.Exists(Path.Combine(current, "sources", "editor")))
            {
                return current;
            }
            var parent = Path.GetDirectoryName(current);
            if (parent == current) break;
            current = parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    }

    private static Assembly GetEditorAssembly()
    {
        return typeof(StatusViewModel).Assembly;
    }

    private static Assembly? GetAvaloniaEditorAssembly()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Stride.Core.Assets.Editor.Avalonia");
        if (loaded != null)
            return loaded;

        try
        {
            return Assembly.Load("Stride.Core.Assets.Editor.Avalonia");
        }
        catch
        {
            return null;
        }
    }

    [Fact]
    public void TestFilesExistAndLineParity()
    {
        var root = GetSourceRoot();
        var files = new (string relativePath, int minLines, int maxLines)[]
        {
            ("sources/editor/Stride.Core.Assets.Editor/ArgumentCheck.cs", 330, 450),
            ("sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/GridLogViewer.cs", 215, 400),
            ("sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/EditableContentListBox.cs", 170, 350),
            ("sources/editor/Stride.Core.Assets.Editor.Avalonia/Controls/DataGridEx.cs", 95, 200),
            ("sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ToolTipHelper.cs", 115, 200)
        };

        foreach (var (relativePath, minLines, maxLines) in files)
        {
            var fullPath = Path.Combine(root, relativePath);
            Assert.True(File.Exists(fullPath), $"Arquivo obrigatório não encontrado: {relativePath} no caminho {fullPath}");
            var lines = File.ReadAllLines(fullPath).Length;
            Assert.InRange(lines, minLines, maxLines);
        }
    }

    [Fact]
    public void TestArgumentCheckMethods()
    {
        var editorAssembly = GetEditorAssembly();
        var argCheckType = editorAssembly.GetType("Stride.Core.Assets.Editor.ArgumentCheck");
        Assert.NotNull(argCheckType);

        var notNullMethod = argCheckType.GetMethod("NotNull", BindingFlags.Public | BindingFlags.Static, new[] { typeof(object), typeof(string) });
        Assert.NotNull(notNullMethod);

        // NotNull success
        notNullMethod.Invoke(null, new object[] { "valid", "paramName" });

        // NotNull failure
        var ex = Assert.Throws<TargetInvocationException>(() => notNullMethod.Invoke(null, new object[] { null!, "nullParam" }));
        var argNullEx = Assert.IsType<ArgumentNullException>(ex.InnerException);
        Assert.Equal("nullParam", argNullEx.ParamName);

        // NotEmpty string
        var notEmptyStrMethod = argCheckType.GetMethod("NotEmpty", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string), typeof(string) });
        Assert.NotNull(notEmptyStrMethod);
        notEmptyStrMethod.Invoke(null, new object[] { "hello", "param" });

        var exEmptyStr = Assert.Throws<TargetInvocationException>(() => notEmptyStrMethod.Invoke(null, new object[] { "", "emptyParam" }));
        Assert.IsType<ArgumentException>(exEmptyStr.InnerException);

        // NotEmpty ICollection
        var notEmptyColMethod = argCheckType.GetMethod("NotEmpty", BindingFlags.Public | BindingFlags.Static, new[] { typeof(ICollection), typeof(string) });
        Assert.NotNull(notEmptyColMethod);
        notEmptyColMethod.Invoke(null, new object[] { new List<int> { 1, 2 }, "col" });

        var exEmptyCol = Assert.Throws<TargetInvocationException>(() => notEmptyColMethod.Invoke(null, new object[] { new List<int>(), "emptyCol" }));
        Assert.IsType<ArgumentOutOfRangeException>(exEmptyCol.InnerException);

        // NotWhiteSpace
        var notWhiteSpaceMethod = argCheckType.GetMethod("NotWhiteSpace", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string), typeof(string) });
        Assert.NotNull(notWhiteSpaceMethod);
        notWhiteSpaceMethod.Invoke(null, new object[] { "valid_text", "param" });

        var exWs = Assert.Throws<TargetInvocationException>(() => notWhiteSpaceMethod.Invoke(null, new object[] { "   ", "wsParam" }));
        Assert.IsType<ArgumentException>(exWs.InnerException);

        // NotEquals
        var notEqualsMethod = argCheckType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "NotEquals" && m.IsGenericMethodDefinition);
        Assert.NotNull(notEqualsMethod);
        var notEqualsGeneric = notEqualsMethod.MakeGenericMethod(typeof(int));
        notEqualsGeneric.Invoke(null, new object[] { 10, 20, "var" });

        var exEquals = Assert.Throws<TargetInvocationException>(() => notEqualsGeneric.Invoke(null, new object[] { 10, 10, "var" }));
        Assert.IsType<ArgumentException>(exEquals.InnerException);

        // Condition
        var conditionMethod = argCheckType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Condition" && m.IsGenericMethodDefinition);
        Assert.NotNull(conditionMethod);
        var condGeneric = conditionMethod.MakeGenericMethod(typeof(int));
        var predTrue = new Predicate<int>(x => x > 0);
        condGeneric.Invoke(null, new object[] { 5, predTrue, "x" });

        var exCond = Assert.Throws<TargetInvocationException>(() => condGeneric.Invoke(null, new object[] { -5, predTrue, "x" }));
        Assert.IsType<ArgumentException>(exCond.InnerException);
    }

    [Fact]
    public void TestArgumentDebugCheckMethods()
    {
        var editorAssembly = GetEditorAssembly();
        var debugCheckType = editorAssembly.GetType("Stride.Core.Assets.Editor.ArgumentDebugCheck");
        Assert.NotNull(debugCheckType);

        var notNullMethod = debugCheckType.GetMethod("NotNull", BindingFlags.Public | BindingFlags.Static, new[] { typeof(object), typeof(string) });
        Assert.NotNull(notNullMethod);
        notNullMethod.Invoke(null, new object[] { "ok", "param" });
    }

    [Fact]
    public void TestGridLogViewerControl()
    {
        var avaloniaAssembly = GetAvaloniaEditorAssembly();
        Assert.NotNull(avaloniaAssembly);

        var gridLogViewerType = avaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Controls.GridLogViewer");
        Assert.NotNull(gridLogViewerType);
        Assert.True(typeof(TemplatedControl).IsAssignableFrom(gridLogViewerType), "GridLogViewer deve herdar de TemplatedControl.");

        var instance = Activator.CreateInstance(gridLogViewerType);
        Assert.NotNull(instance);

        // Propriedades essenciais
        var isToolBarVisibleProp = gridLogViewerType.GetProperty("IsToolBarVisible");
        Assert.NotNull(isToolBarVisibleProp);
        Assert.True((bool)isToolBarVisibleProp.GetValue(instance)!);

        var showDebugProp = gridLogViewerType.GetProperty("ShowDebugMessages");
        Assert.NotNull(showDebugProp);
        Assert.True((bool)showDebugProp.GetValue(instance)!);

        var showInfoProp = gridLogViewerType.GetProperty("ShowInfoMessages");
        Assert.NotNull(showInfoProp);
        Assert.True((bool)showInfoProp.GetValue(instance)!);

        var logMessagesProp = gridLogViewerType.GetProperty("LogMessages");
        Assert.NotNull(logMessagesProp);

        var filteredLogMessagesProp = gridLogViewerType.GetProperty("FilteredLogMessages");
        Assert.NotNull(filteredLogMessagesProp);

        var searchTextProp = gridLogViewerType.GetProperty("SearchText");
        Assert.NotNull(searchTextProp);

        // Popula log messages
        var logList = new ObservableList<ILogMessage>
        {
            new LogMessage("TestModule", LogMessageType.Debug, "Debug message 1"),
            new LogMessage("TestModule", LogMessageType.Info, "Info message 1"),
            new LogMessage("TestModule", LogMessageType.Warning, "Warning message 1"),
            new LogMessage("TestModule", LogMessageType.Error, "Error message 1"),
            new LogMessage("TestModule", LogMessageType.Fatal, "Fatal message 1"),
            new LogMessage("TestModule", LogMessageType.Verbose, "Verbose message 1")
        };

        logMessagesProp.SetValue(instance, logList);

        var filtered = (ObservableList<ILogMessage>)filteredLogMessagesProp.GetValue(instance)!;
        Assert.NotNull(filtered);
        Assert.Equal(6, filtered.Count);

        // Desliga debug
        showDebugProp.SetValue(instance, false);
        filtered = (ObservableList<ILogMessage>)filteredLogMessagesProp.GetValue(instance)!;
        Assert.Equal(5, filtered.Count);
        Assert.DoesNotContain(filtered, m => m.Type == LogMessageType.Debug);

        // Teste de busca
        searchTextProp.SetValue(instance, "Warning");
        filtered = (ObservableList<ILogMessage>)filteredLogMessagesProp.GetValue(instance)!;
        Assert.Single(filtered);
        Assert.Equal(LogMessageType.Warning, filtered[0].Type);

        // Limpa busca
        searchTextProp.SetValue(instance, string.Empty);
        filtered = (ObservableList<ILogMessage>)filteredLogMessagesProp.GetValue(instance)!;
        Assert.Equal(5, filtered.Count);
    }

    [Fact]
    public void TestEditableContentListBoxControl()
    {
        var avaloniaAssembly = GetAvaloniaEditorAssembly();
        Assert.NotNull(avaloniaAssembly);

        var listBoxType = avaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Controls.EditableContentListBox");
        Assert.NotNull(listBoxType);
        Assert.True(typeof(ListBox).IsAssignableFrom(listBoxType), "EditableContentListBox deve herdar de ListBox.");

        var itemType = avaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Controls.EditableContentListBoxItem");
        Assert.NotNull(itemType);
        Assert.True(typeof(ListBoxItem).IsAssignableFrom(itemType), "EditableContentListBoxItem deve herdar de ListBoxItem.");

        var listBoxInstance = Activator.CreateInstance(listBoxType) as ListBox;
        Assert.NotNull(listBoxInstance);

        var canEditProp = listBoxType.GetProperty("CanEdit");
        Assert.NotNull(canEditProp);
        Assert.True((bool)canEditProp.GetValue(listBoxInstance)!);

        var beginEditCmdProp = listBoxType.GetProperty("BeginEditCommand", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(beginEditCmdProp);

        var commitEditCmdProp = listBoxType.GetProperty("CommitEditCommand", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(commitEditCmdProp);

        var cancelEditCmdProp = listBoxType.GetProperty("CancelEditCommand", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(cancelEditCmdProp);

        var itemInstance = Activator.CreateInstance(itemType);
        Assert.NotNull(itemInstance);

        var itemIsEditingProp = itemType.GetProperty("IsEditing");
        Assert.NotNull(itemIsEditingProp);
        Assert.False((bool)itemIsEditingProp.GetValue(itemInstance)!);

        var itemCanEditProp = itemType.GetProperty("CanEdit");
        Assert.NotNull(itemCanEditProp);
        Assert.True((bool)itemCanEditProp.GetValue(itemInstance)!);
    }

    [Fact]
    public void TestDataGridExControl()
    {
        var avaloniaAssembly = GetAvaloniaEditorAssembly();
        Assert.NotNull(avaloniaAssembly);

        var dataGridExType = avaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Controls.DataGridEx");
        Assert.NotNull(dataGridExType);

        Assert.Equal("DataGrid", dataGridExType.BaseType?.Name);

        var instance = Activator.CreateInstance(dataGridExType);
        Assert.NotNull(instance);
    }

    [Fact]
    public void TestToolTipHelperAttachedProperty()
    {
        var avaloniaAssembly = GetAvaloniaEditorAssembly();
        Assert.NotNull(avaloniaAssembly);

        var toolTipHelperType = avaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ToolTipHelper");
        Assert.NotNull(toolTipHelperType);

        var statusPropField = toolTipHelperType.GetField("StatusProperty", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(statusPropField);

        var getStatusMethod = toolTipHelperType.GetMethod("GetStatus", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(getStatusMethod);

        var setStatusMethod = toolTipHelperType.GetMethod("SetStatus", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(setStatusMethod);

        var border = new Border();
        var serviceProvider = new ViewModelServiceProvider();
        var statusVm = new StatusViewModel(serviceProvider);

        setStatusMethod.Invoke(null, new object[] { border, statusVm });
        var returnedStatus = getStatusMethod.Invoke(null, new object[] { border });
        Assert.Same(statusVm, returnedStatus);

        // Teste de Push / Pop com ToolTip text
        ToolTip.SetTip(border, "Test Tooltip Message");
        var pushStatusMethod = toolTipHelperType.GetMethod("PushToolTipStatus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(pushStatusMethod);
        pushStatusMethod.Invoke(null, new object[] { border });
        Assert.Equal("Test Tooltip Message", statusVm.CurrentStatus);

        var popStatusMethod = toolTipHelperType.GetMethod("PopToolTipStatus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(popStatusMethod);
        popStatusMethod.Invoke(null, new object[] { border });
        Assert.Null(statusVm.CurrentStatus);
    }
}
