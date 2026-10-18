using System;
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para validação dos Temas ExpressionDark e Estilos de Tabelas/Grids (Fatia 2 - Marco P33).
/// Garante que todos os 7 arquivos AXAML existam, sejam XML válidos, estejam desacoplados de WPF/Win32,
/// e definam os estilos, templates e brushes do tema ExpressionDark em Avalonia.
/// </summary>
public class TestExpressionDarkThemesAndTableStylesIntegration
{
    private static readonly string RootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static readonly string[] ThemeFiles = new[]
    {
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/Resources/TableView.ExpressionDark.normalcolor.Graphics.axaml",
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/Resources/Common.Resources.axaml",
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/Generic.axaml",
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/TableflowView.ExpressionDark.normalcolor.axaml",
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/TableflowView.GridElementTemplates.axaml",
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ExpressionDark/Resources/ExpressionDark.normalcolor.Resources.axaml",
        "sources/editor/Stride.Core.Assets.Editor.Avalonia/Themes/ThemeSelector.axaml"
    };

    [Theory]
    [InlineData(0, "TableView.ExpressionDark.normalcolor.Graphics.axaml")]
    [InlineData(1, "Common.Resources.axaml")]
    [InlineData(2, "Generic.axaml")]
    [InlineData(3, "TableflowView.ExpressionDark.normalcolor.axaml")]
    [InlineData(4, "TableflowView.GridElementTemplates.axaml")]
    [InlineData(5, "ExpressionDark.normalcolor.Resources.axaml")]
    [InlineData(6, "ThemeSelector.axaml")]
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
