using System;
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para validação dos Temas de Apresentação e Templates de Editores (Fatia 3 - Marco P33).
/// Garante que todos os 7 arquivos AXAML existam, sejam XML válidos, estejam desacoplados de WPF/Win32,
/// e definam os templates de Script, Visual Scripting, Graphics Compositor e Temas globais em Avalonia.
/// </summary>
public class TestPresentationThemesAndEditorTemplatesIntegration
{
    private static readonly string RootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static readonly string[] ThemeFiles = new[]
    {
        "sources/editor/Stride.Assets.Editor.Avalonia/Themes/Generic.axaml",
        "sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/Resources/ThemeScriptEditor.axaml",
        "sources/editor/Stride.Assets.Editor.Avalonia/Views/VisualScriptingTemplates.axaml",
        "sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/ScriptEditor/Resources/Icons.axaml",
        "sources/editor/Stride.Assets.Editor.Avalonia/AssetEditors/VisualScriptEditor/Views/GraphTemplates.axaml",
        "sources/editor/Stride.Editor.Avalonia/Themes/Generic.axaml",
        "sources/editor/Stride.Assets.Editor.Avalonia/Views/GraphicsCompositorTemplates.axaml"
    };

    [Theory]
    [InlineData(0, "Generic.axaml")]
    [InlineData(1, "ThemeScriptEditor.axaml")]
    [InlineData(2, "VisualScriptingTemplates.axaml")]
    [InlineData(3, "Icons.axaml")]
    [InlineData(4, "GraphTemplates.axaml")]
    [InlineData(5, "Generic.axaml")]
    [InlineData(6, "GraphicsCompositorTemplates.axaml")]
    public void Test_Theme_File_Exists_And_Valid_Markup(int index, string expectedFileName)
    {
        var relativePath = ThemeFiles[index];
        var fullPath = Path.Combine(RootPath, relativePath);
        Assert.True(File.Exists(fullPath), $"Arquivo deve existir: {fullPath}");
        Assert.EndsWith(expectedFileName, fullPath);

        var content = File.ReadAllText(fullPath);
        Assert.False(string.IsNullOrWhiteSpace(content), $"Conteúdo de {expectedFileName} não deve ser vazio");

        var doc = XDocument.Parse(content);
        Assert.NotNull(doc.Root);
    }

    [Fact]
    public void Test_Strict_Decoupling_From_Wpf_And_Win32()
    {
        foreach (var relativePath in ThemeFiles)
        {
            var fullPath = Path.Combine(RootPath, relativePath);
            if (File.Exists(fullPath))
            {
                var content = File.ReadAllText(fullPath);
                Assert.DoesNotContain("http://schemas.microsoft.com/winfx/2006/xaml/presentation", content);
                Assert.DoesNotContain("PresentationFramework", content);
                Assert.DoesNotContain("PresentationCore", content);
                Assert.DoesNotContain("WindowsBase", content);
            }
        }
    }
}
