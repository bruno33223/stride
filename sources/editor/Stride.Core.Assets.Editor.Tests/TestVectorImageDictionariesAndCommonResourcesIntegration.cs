using System;
using System.IO;
using System.Xml.Linq;
using Avalonia.Markup.Xaml;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para validação dos Dicionários de Imagens Vetoriais e Recursos Comuns (Fatia 1 - Marco P33).
/// Garante que os arquivos AXAML existam, sejam XML válidos, estejam desacoplados de WPF/Win32,
/// e contenham as definições e recursos gráficos necessários para a renderização multiplataforma.
/// </summary>
public class TestVectorImageDictionariesAndCommonResourcesIntegration
{
    private static readonly string RootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact]
    public void Test_01_Core_ImageDictionary_File_Exists_And_Valid_Markup()
    {
        var coreImagePath = Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ImageDictionary.axaml");
        Assert.True(File.Exists(coreImagePath), $"Arquivo deve existir: {coreImagePath}");

        var content = File.ReadAllText(coreImagePath);
        Assert.False(string.IsNullOrWhiteSpace(content), "Conteúdo do ImageDictionary.axaml não deve ser vazio");

        // Parse XML para validação sintática estrita
        var doc = XDocument.Parse(content);
        Assert.NotNull(doc.Root);
        Assert.Contains("ResourceDictionary", doc.Root.Name.LocalName);
    }

    [Fact]
    public void Test_02_Presentation_ImageDictionary_File_Exists_And_Valid_Markup()
    {
        var presImagePath = Path.Combine(RootPath, "sources/editor/Stride.Assets.Editor.Avalonia/Views/ImageDictionary.axaml");
        Assert.True(File.Exists(presImagePath), $"Arquivo deve existir: {presImagePath}");

        var content = File.ReadAllText(presImagePath);
        Assert.False(string.IsNullOrWhiteSpace(content), "Conteúdo do ImageDictionary.axaml não deve ser vazio");

        var doc = XDocument.Parse(content);
        Assert.NotNull(doc.Root);
        Assert.Contains("ResourceDictionary", doc.Root.Name.LocalName);
    }

    [Fact]
    public void Test_03_Core_CommonResources_File_Exists_And_Valid_Markup()
    {
        var commonPath = Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/CommonResources.axaml");
        Assert.True(File.Exists(commonPath), $"Arquivo deve existir: {commonPath}");

        var content = File.ReadAllText(commonPath);
        Assert.False(string.IsNullOrWhiteSpace(content), "Conteúdo do CommonResources.axaml não deve ser vazio");

        var doc = XDocument.Parse(content);
        Assert.NotNull(doc.Root);
        Assert.Contains("ResourceDictionary", doc.Root.Name.LocalName);
    }

    [Fact]
    public void Test_04_Strict_Decoupling_From_Wpf_And_Win32()
    {
        var files = new[]
        {
            Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ImageDictionary.axaml"),
            Path.Combine(RootPath, "sources/editor/Stride.Assets.Editor.Avalonia/Views/ImageDictionary.axaml"),
            Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/CommonResources.axaml")
        };

        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("http://schemas.microsoft.com/winfx/2006/xaml/presentation", content);
                Assert.DoesNotContain("PresentationFramework", content);
                Assert.DoesNotContain("PresentationCore", content);
                Assert.DoesNotContain("WindowsBase", content);
            }
        }
    }

    [Fact]
    public void Test_05_Core_ImageDictionary_Full_Key_Parity()
    {
        var wpfPath = Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Wpf/View/ImageDictionary.xaml");
        var avaPath = Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/ImageDictionary.axaml");

        Assert.True(File.Exists(wpfPath), $"Arquivo legado deve existir: {wpfPath}");
        Assert.True(File.Exists(avaPath), $"Arquivo Avalonia deve existir: {avaPath}");

        var wpfKeys = ExtractKeys(wpfPath);
        var avaKeys = ExtractKeys(avaPath);

        var missingInAvalonia = new System.Collections.Generic.HashSet<string>(wpfKeys);
        missingInAvalonia.ExceptWith(avaKeys);

        Assert.True(missingInAvalonia.Count == 0, $"Chaves ausentes no Core Avalonia: {string.Join(", ", missingInAvalonia)}");
        Assert.Equal(90, wpfKeys.Count);
        Assert.Equal(90, avaKeys.Count);
    }

    [Fact]
    public void Test_06_Presentation_ImageDictionary_Full_Key_Parity()
    {
        var wpfPath = Path.Combine(RootPath, "sources/editor/Stride.Assets.Presentation.Wpf/View/ImageDictionary.xaml");
        var avaPath = Path.Combine(RootPath, "sources/editor/Stride.Assets.Editor.Avalonia/Views/ImageDictionary.axaml");

        Assert.True(File.Exists(wpfPath), $"Arquivo legado deve existir: {wpfPath}");
        Assert.True(File.Exists(avaPath), $"Arquivo Avalonia deve existir: {avaPath}");

        var wpfKeys = ExtractKeys(wpfPath);
        var avaKeys = ExtractKeys(avaPath);

        var missingInAvalonia = new System.Collections.Generic.HashSet<string>(wpfKeys);
        missingInAvalonia.ExceptWith(avaKeys);

        Assert.True(missingInAvalonia.Count == 0, $"Chaves ausentes no Presentation Avalonia: {string.Join(", ", missingInAvalonia)}");
        Assert.Equal(108, wpfKeys.Count);
        Assert.Equal(108, avaKeys.Count);
    }

    [Fact]
    public void Test_07_CommonResources_Full_Key_Parity()
    {
        var wpfPath = Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Wpf/View/CommonResources.xaml");
        var avaPath = Path.Combine(RootPath, "sources/editor/Stride.Core.Assets.Editor.Avalonia/Views/CommonResources.axaml");

        Assert.True(File.Exists(wpfPath), $"Arquivo legado deve existir: {wpfPath}");
        Assert.True(File.Exists(avaPath), $"Arquivo Avalonia deve existir: {avaPath}");

        var wpfKeys = ExtractKeys(wpfPath);
        var avaKeys = ExtractKeys(avaPath);

        var missingInAvalonia = new System.Collections.Generic.HashSet<string>(wpfKeys);
        missingInAvalonia.ExceptWith(avaKeys);

        Assert.True(missingInAvalonia.Count == 0, $"Chaves ausentes no CommonResources Avalonia: {string.Join(", ", missingInAvalonia)}");
        Assert.Equal(9, wpfKeys.Count);
        Assert.True(avaKeys.Count >= 9, $"Avalonia deve conter no mínimo 9 chaves, encontrou: {avaKeys.Count}");
    }

    private static System.Collections.Generic.HashSet<string> ExtractKeys(string path)
    {
        var keys = new System.Collections.Generic.HashSet<string>();
        var content = File.ReadAllText(path);
        var doc = XDocument.Parse(content);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        foreach (var elem in doc.Descendants())
        {
            var keyAttr = elem.Attribute(x + "Key");
            if (keyAttr != null && !string.IsNullOrWhiteSpace(keyAttr.Value))
            {
                keys.Add(keyAttr.Value);
            }
        }
        return keys;
    }
}
