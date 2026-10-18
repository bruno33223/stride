// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestStudioSelectionAndServicesIntegration
{
    public class SampleSelectableItem
    {
        public AbsoluteId Id { get; }
        public string Name { get; }

        public SampleSelectableItem(string name)
        {
            Id = new AbsoluteId(new AssetId(Guid.NewGuid()), Guid.NewGuid());
            Name = name;
        }
    }

    public class SampleDocumentedClass
    {
        public static readonly PropertyKey<string> TestPropertyKey = new("TestPropertyKey", typeof(SampleDocumentedClass));

        public string SomeProperty { get; set; } = string.Empty;
        public int SomeField = 42;
    }

    [Fact]
    public void TestSelectionServiceLifecycleAndNavigation()
    {
        var dispatcher = new NullDispatcherService();
        var selectionService = new SelectionService(dispatcher);

        var itemsScope1 = new List<SampleSelectableItem>
        {
            new("Item1"),
            new("Item2")
        };

        var itemsScope2 = new List<SampleSelectableItem>
        {
            new("Item3"),
            new("Item4")
        };

        var allItems = itemsScope1.Concat(itemsScope2).ToDictionary(x => x.Id, x => x);

        var col1 = new ObservableCollection<object>();
        var col2 = new ObservableCollection<object>();

        var scope1 = selectionService.RegisterSelectionScope(
            id => allItems.TryGetValue(id, out var it) ? it : null,
            obj => (obj as SampleSelectableItem)?.Id,
            col1);

        var scope2 = selectionService.RegisterSelectionScope(
            id => allItems.TryGetValue(id, out var it) ? it : null,
            obj => (obj as SampleSelectableItem)?.Id,
            col2);

        Assert.NotNull(scope1);
        Assert.NotNull(scope2);
        Assert.False(selectionService.CanGoBack);
        Assert.False(selectionService.CanGoForward);
        Assert.False(selectionService.PropertySelectionSuppressed);

        // Select item in Scope 1
        col1.Add(itemsScope1[0]);
        Assert.Contains(itemsScope1[0], col1);
        Assert.Empty(col2);
        Assert.True(selectionService.CurrentState.HasValidSelection());

        // Select item in Scope 2: scope 1 should be cleared automatically by SelectionService
        col2.Add(itemsScope2[0]);
        Assert.Empty(col1);
        Assert.Contains(itemsScope2[0], col2);
        Assert.True(selectionService.CanGoBack);

        // Navigate Backward (Undo selection)
        selectionService.NavigateBackward();
        Assert.Contains(itemsScope1[0], col1);
        Assert.Empty(col2);
        Assert.True(selectionService.CanGoForward);

        // Navigate Forward (Redo selection)
        selectionService.NextSelection();
        Assert.Empty(col1);
        Assert.Contains(itemsScope2[0], col2);

        // Unregister scopes
        selectionService.UnregisterSelectionScope(col1);
        selectionService.UnregisterSelectionScope(scope2);
    }

    [Fact]
    public void TestSelectionStateEqualityAndOperations()
    {
        var state1 = new SelectionState();
        var state2 = new SelectionState();

        Assert.True(state1.Equals(state2));
        Assert.False(state1.HasValidSelection());

        var col = new ObservableCollection<object>();
        var item = new SampleSelectableItem("StateTest");
        var scope = new SelectionScope(
            [col],
            id => id == item.Id ? item : null,
            obj => (obj as SampleSelectableItem)?.Id);

        col.Add(item);
        state1.SaveStates([scope]);
        Assert.True(state1.HasValidSelection());
        Assert.False(state1.Equals(state2));

        col.Clear();
        Assert.Empty(col);

        // Restore state
        state1.RestoreStates();
        Assert.Single(col);
        Assert.Equal(item, col[0]);

        // Test SelectionOperation
        var dispatcher = new NullDispatcherService();
        var service = new SelectionService(dispatcher);
        var op = new SelectionOperation(service, state1, state2);
        Assert.NotNull(op);
    }

    [Fact]
    public void TestUserDocumentationServiceXmlAndUsrdoc()
    {
        // Must allow parameterless constructor decoupled from WPF
        var docService = new UserDocumentationService();

        var tempDir = Path.Combine(Path.GetTempPath(), "StrideDocTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // 1. Test .usrdoc parsing
            var usrdocPath = Path.Combine(tempDir, "TestAssembly.usrdoc");
            File.WriteAllLines(usrdocPath,
            [
                "P:FakeDocAssembly.TestClass.Prop=Documentation for Prop from usrdoc",
                "F:FakeDocAssembly.TestClass.field=Documentation for field from usrdoc"
            ]);

            Assert.True(docService.CacheCustomDocumentation(usrdocPath));

            // 2. Test .xml parsing with <userdoc> tags
            var xmlPath = Path.Combine(tempDir, "TestAssembly.xml");
            File.WriteAllText(xmlPath,
                """
                <doc>
                    <members>
                        <member name="P:FakeDocAssembly.TestClass.XmlProp">
                            <userdoc>Documentation for XmlProp from xml</userdoc>
                        </member>
                        <member name="F:FakeDocAssembly.TestClass.XmlField">
                            <userdoc>Documentation for XmlField from xml</userdoc>
                        </member>
                    </members>
                </doc>
                """);

            Assert.True(docService.CacheXmlDocumentation(xmlPath));

            // 3. Clear cache and verify methods
            docService.ClearCachedAssemblyDocumentation(typeof(TestStudioSelectionAndServicesIntegration).Assembly);

            var typeDesc = TypeDescriptorFactory.Default.Find(typeof(SampleDocumentedClass));
            var memberDesc = typeDesc.Members.First(m => m.Name == nameof(SampleDocumentedClass.SomeProperty));
            var propKey = SampleDocumentedClass.TestPropertyKey;

            // Non-cached returns null
            Assert.Null(docService.GetPropertyKeyDocumentation(propKey));
            Assert.Null(docService.GetMemberDocumentation(memberDesc, typeof(SampleDocumentedClass)));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TestCodeEditorOpenerServiceCrossPlatformLinuxResolution()
    {
        // Verify Cross-Platform IDE command & arguments resolution
        var solutionPath = "/media/test/Project/MyGame.sln";

        // 1. Test VS Code resolution & arguments
        var vscodeInfo = new IDEInfo("VS Code", "code", IDEType.VSCode);
        var startInfoVsCode = CodeEditorOpenerService.BuildProcessStartInfo(solutionPath, vscodeInfo);

        Assert.NotNull(startInfoVsCode);
        Assert.Equal("\"/media/test/Project\"", startInfoVsCode.Arguments);
        Assert.True(startInfoVsCode.CreateNoWindow);
        Assert.False(startInfoVsCode.UseShellExecute);

        // 2. Test JetBrains Rider resolution & arguments
        var riderInfo = new IDEInfo("Rider", "rider", IDEType.Rider);
        var startInfoRider = CodeEditorOpenerService.BuildProcessStartInfo(solutionPath, riderInfo);

        Assert.NotNull(startInfoRider);
        Assert.Equal("\"/media/test/Project/MyGame.sln\"", startInfoRider.Arguments);
        Assert.False(startInfoRider.CreateNoWindow);
        Assert.False(startInfoRider.UseShellExecute);

        // 3. Test executable resolution from PATH helper
        var resolved = CodeEditorOpenerService.ResolveExecutablePath("sh");
        Assert.NotNull(resolved);
        Assert.True(File.Exists(resolved));
    }
}
