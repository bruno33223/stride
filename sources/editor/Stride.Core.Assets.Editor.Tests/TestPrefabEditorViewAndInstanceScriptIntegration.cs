// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Avalonia.Controls;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestPrefabEditorViewAndInstanceScriptIntegration
    {
        private static string FindRepoRoot()
        {
            var current = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                if (Directory.Exists(Path.Combine(current, "sources", "editor")))
                    return current;

                var parent = Directory.GetParent(current)?.FullName;
                if (parent == current || string.IsNullOrEmpty(parent))
                    break;

                current = parent;
            }

            throw new DirectoryNotFoundException("Não foi possível localizar o diretório raiz do repositório Stride.");
        }

        [Fact]
        public void TestPrefabEditorViewTypeAndAnnotations()
        {
            // O tipo PrefabEditorView deve residir em Stride.Assets.Editor.Avalonia.Views
            var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.EntityHierarchyEditorView).Assembly;
            var prefabViewType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.PrefabEditorView");

            Assert.NotNull(prefabViewType);

            // Deve herdar de EntityHierarchyEditorView e implementar IAssetEditorView
            Assert.Equal(typeof(Stride.Assets.Editor.Avalonia.Views.EntityHierarchyEditorView), prefabViewType.BaseType);
            Assert.True(typeof(IAssetEditorView).IsAssignableFrom(prefabViewType));
            Assert.True(typeof(UserControl).IsAssignableFrom(prefabViewType));

            // Deve possuir o atributo [AssetEditorView<PrefabEditorViewModel>]
            var expectedAttrType = typeof(AssetEditorViewAttribute<PrefabEditorViewModel>);
            var attr = prefabViewType.GetCustomAttribute(expectedAttrType, inherit: false);
            Assert.NotNull(attr);
        }

        [Fact]
        public void TestAssetCompositeHierarchyTreeViewHelperTypeAndCommands()
        {
            var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.EntityHierarchyEditorView).Assembly;
            var helperType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.AssetCompositeHierarchyTreeViewHelper");

            Assert.NotNull(helperType);

            // Verificar comandos estáticos CollapseAllItems e ExpandAllItems
            var collapseProp = helperType.GetProperty("CollapseAllItems", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(collapseProp);
            Assert.True(typeof(ICommand).IsAssignableFrom(collapseProp.PropertyType));
            var collapseCommand = collapseProp.GetValue(null) as ICommand;
            Assert.NotNull(collapseCommand);

            var expandProp = helperType.GetProperty("ExpandAllItems", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(expandProp);
            Assert.True(typeof(ICommand).IsAssignableFrom(expandProp.PropertyType));
            var expandCommand = expandProp.GetValue(null) as ICommand;
            Assert.NotNull(expandCommand);

            // Verificar métodos de suporte
            var collapseAllMethod = helperType.GetMethod("CollapseAll", BindingFlags.Public | BindingFlags.Static, [typeof(TreeView)]);
            Assert.NotNull(collapseAllMethod);

            var expandAllMethod = helperType.GetMethod("ExpandAll", BindingFlags.Public | BindingFlags.Static, [typeof(TreeView)]);
            Assert.NotNull(expandAllMethod);

            var collapseElemMethod = helperType.GetMethod("CollapseElementRecursively", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(TreeViewItem)]);
            Assert.NotNull(collapseElemMethod);

            var expandElemMethod = helperType.GetMethod("ExpandElementRecursively", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(TreeView), typeof(AssetCompositeItemViewModel)]);
            Assert.NotNull(expandElemMethod);
        }

        [Fact]
        public void TestAssetCompositeHierarchyTreeViewHelperCollapseLogic()
        {
            var assembly = typeof(Stride.Assets.Editor.Avalonia.Views.EntityHierarchyEditorView).Assembly;
            var helperType = assembly.GetType("Stride.Assets.Editor.Avalonia.Views.AssetCompositeHierarchyTreeViewHelper");
            Assert.NotNull(helperType);

            var collapseElemMethod = helperType.GetMethod("CollapseElementRecursively", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(TreeViewItem)]);
            Assert.NotNull(collapseElemMethod);

            var item = new TreeViewItem { IsExpanded = true };
            collapseElemMethod.Invoke(null, [item]);
            Assert.False(item.IsExpanded);
        }

        [Fact]
        public void TestPrefabInstanceScriptTemplateParityAndContent()
        {
            var repoRoot = FindRepoRoot();
            var scriptPath = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor", "Templates", "Assets", "Scripts", "Prefabs", "PrefabInstance.cs");
            var templatePath = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor", "Templates", "Assets", "Scripts", "Prefabs", "PrefabInstance.sdtpl");

            Assert.True(File.Exists(scriptPath), $"Script template não encontrado em: {scriptPath}");
            Assert.True(File.Exists(templatePath), $"Arquivo .sdtpl não encontrado em: {templatePath}");

            var lines = File.ReadAllLines(scriptPath);
            // Paridade estrita de 141 linhas
            Assert.Equal(141, lines.Length);

            var text = File.ReadAllText(scriptPath);

            // Verificações essenciais de conteúdo e tokens de template
            Assert.Contains("namespace ##Namespace##;", text);
            Assert.Contains("public class ##Scriptname## : AsyncScript", text);
            Assert.Contains("[DataMember(10)]", text);
            Assert.Contains("[Display(\"Source\")]", text);
            Assert.Contains("public Prefab SourcePrefab;", text);
            Assert.Contains("[DataMember(20)]", text);
            Assert.Contains("[Display(\"Following\")]", text);
            Assert.Contains("public bool Following { get; set; } = true;", text);
            Assert.Contains("[DataMember(30)]", text);
            Assert.Contains("[Display(\"Timeout\")]", text);
            Assert.Contains("public float InstanceTimeout = 3f;", text);
            Assert.Contains("[DataMember(40)]", text);
            Assert.Contains("[Display(\"Trigger\")]", text);
            Assert.Contains("public Keys Key = Keys.Space;", text);
            Assert.Contains("[DataMember(50)]", text);
            Assert.Contains("[Display(\"Interval\")]", text);
            Assert.Contains("public float TimeInterval { get; set; } = 0f;", text);
            Assert.Contains("public override async Task Execute()", text);
            Assert.Contains("protected bool IsTriggered()", text);
            Assert.Contains("protected void SpawnInstance()", text);
            Assert.Contains("var spawnedEntities = SourcePrefab.Instantiate();", text);
            Assert.Contains("Entity.AddChild(prefabEntity);", text);
            Assert.Contains("SceneSystem.SceneInstance.RootScene.Entities.Add(prefabEntity);", text);
            Assert.Contains("SceneSystem.SceneInstance.RootScene.Entities.Remove(clonedEntity);", text);
            Assert.Contains("Entity.RemoveChild(clonedEntity);", text);
            Assert.Contains("spawnedEntities.Clear();", text);

            // Validar também o .sdtpl
            var tplText = File.ReadAllText(templatePath);
            Assert.Contains("!TemplateAsset", tplText);
            Assert.Contains("Id: C51563DD-94F1-46D8-95AA-FA1CAADB1F63", tplText);
            Assert.Contains("AssetTypeName: ScriptSourceFileAsset", tplText);
            Assert.Contains("DefaultOutputName: PrefabInstance", tplText);
        }
    }
}
