// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Xunit;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Scripts;
using Stride.Assets.Skyboxes;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.Editor.Preview;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Teste integrado para a Fatia 3 (slice-3) do Marco P31:
/// "Editor Preview Views, Window Services & Template Providers".
/// Valida rigorosamente os 18 contratos da Fatia 3.
/// </summary>
public class TestPreviewViewsAndWindowServicesIntegration
{
    private static readonly Assembly CoreAssetsEditorAssembly = typeof(Stride.Core.Assets.Editor.ViewModels.SessionViewModel).Assembly;
    private static readonly Assembly AssetsEditorAvaloniaAssembly = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.ModelPreviewView).Assembly;
    private static readonly Assembly CoreAssetsEditorAvaloniaAssembly = typeof(Stride.Core.Assets.Editor.Avalonia.Views.TypeMatchTemplateProvider).Assembly;
    private static readonly Assembly EditorAvaloniaAssembly = typeof(Stride.Editor.Avalonia.Preview.Views.StridePreviewView).Assembly;
    private static readonly Assembly EditorAssembly = typeof(Stride.Editor.Preview.Views.IPreviewView).Assembly;

    [Flags]
    private enum SampleFlagsEnum
    {
        None = 0,
        First = 1,
        Second = 2,
    }

    private enum SampleStandardEnum
    {
        OptionA,
        OptionB,
    }

    #region Mock NodeViewModel Helper
    private sealed class MockNodeViewModel : NodeViewModel
    {
        private readonly Type type;
        private readonly object? value;
        private readonly Dictionary<string, object> associatedData = new();
        private readonly NodeViewModel? parent;

        public MockNodeViewModel(Type type, object? value = null, NodeViewModel? parent = null, Stride.Core.Presentation.ViewModels.IViewModelServiceProvider? serviceProvider = null)
            : base(serviceProvider ?? new Stride.Core.Presentation.ViewModels.ViewModelServiceProvider())
        {
            this.type = type;
            this.value = value;
            this.parent = parent;
        }

        public override Type Type => type;
        public override object? NodeValue => value;
        public override NodeViewModel? Parent => parent;
        public override IDictionary<string, object> AssociatedData => associatedData;
    }

    private sealed class MockPluginService : IAssetsPluginService
    {
        public IReadOnlyCollection<AssetsPlugin> Plugins => [];
        public IReadOnlyList<Type> GetPrimitiveTypes() => [];
        public Type? GetAssetViewModelType(Type assetType) => null;
        public Type? GetEditorViewModelType(Type viewModelType) => null;
        public Type? GetEditorViewType(Type editorViewModelType) => null;
        public Type? GetPreviewViewModelType(Type previewType) => null;
        public Type? GetPreviewViewType(Type previewType) => null;
        public void EnsureInitialized(Stride.Core.Diagnostics.ILogger logger) { }
        public bool HasImagesForEnum(SessionViewModel? session, Type enumType) => enumType == typeof(SampleStandardEnum);
    }
    #endregion

    // =========================================================================
    // Contrato 01: IPreviewView
    // =========================================================================
    [Fact]
    public void Contract01_IPreviewView_InterfaceAndMethods_Valid()
    {
        var previewViewType = typeof(Stride.Editor.Preview.Views.IPreviewView);
        Assert.True(previewViewType.IsInterface, "IPreviewView deve ser uma interface.");

        var initMethod = previewViewType.GetMethod("InitializeView", [typeof(Stride.Editor.Preview.IPreviewBuilder), typeof(Stride.Editor.Preview.IAssetPreview)]);
        Assert.NotNull(initMethod);
        Assert.Equal(typeof(void), initMethod.ReturnType);

        var updateMethod = previewViewType.GetMethod("UpdateView", [typeof(Stride.Editor.Preview.IAssetPreview)]);
        Assert.NotNull(updateMethod);
        Assert.Equal(typeof(void), updateMethod.ReturnType);

        // Validar também a presença ou compatibilidade no namespace / assembly de Avalonia
        var avaloniaInterfaceType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Preview.Views.IPreviewView");
        Assert.NotNull(avaloniaInterfaceType);
        Assert.True(typeof(Stride.Editor.Preview.Views.IPreviewView).IsAssignableFrom(avaloniaInterfaceType));
    }

    // =========================================================================
    // Contrato 02: ModelPreviewView
    // =========================================================================
    [Fact]
    public void Contract02_ModelPreviewView_InheritsAndAttributes_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.ModelPreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "ModelPreviewView deve herdar de StridePreviewView.");

        var attributes = type.GetCustomAttributes<AssetPreviewViewAttribute>().ToList();
        Assert.Contains(attributes, a => a.AssetPreviewType == typeof(ModelPreview));
        Assert.Contains(attributes, a => a.AssetPreviewType == typeof(PrefabPreview));
        Assert.Contains(attributes, a => a.AssetPreviewType == typeof(PrefabModelPreview));
        Assert.Contains(attributes, a => a.AssetPreviewType == typeof(ProceduralModelPreview));
        Assert.Contains(attributes, a => a.AssetPreviewType == typeof(SpriteStudioSheetPreview));

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 03: SkyboxPreviewView
    // =========================================================================
    [Fact]
    public void Contract03_SkyboxPreviewView_InheritsAndAttributes_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.SkyboxPreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "SkyboxPreviewView deve herdar de StridePreviewView.");

        var attr = type.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(SkyboxPreview), attr.AssetPreviewType);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 04: TexturePreviewView
    // =========================================================================
    [Fact]
    public void Contract04_TexturePreviewView_InheritsAndAttributes_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.TexturePreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "TexturePreviewView deve herdar de StridePreviewView.");

        var attr = type.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(TexturePreview), attr.AssetPreviewType);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 05: ScenePreviewView
    // =========================================================================
    [Fact]
    public void Contract05_ScenePreviewView_InheritsAndAttributes_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.ScenePreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "ScenePreviewView deve herdar de StridePreviewView.");

        var attr = type.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(ScenePreview), attr.AssetPreviewType);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 06: MaterialPreviewView
    // =========================================================================
    [Fact]
    public void Contract06_MaterialPreviewView_InheritsAndAttributes_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.MaterialPreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "MaterialPreviewView deve herdar de StridePreviewView.");

        var attr = type.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(MaterialPreview), attr.AssetPreviewType);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 07: HeightmapPreviewView
    // =========================================================================
    [Fact]
    public void Contract07_HeightmapPreviewView_InheritsAndAttributes_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.HeightmapPreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "HeightmapPreviewView deve herdar de StridePreviewView.");

        var attr = type.GetCustomAttribute<AssetPreviewViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(HeightmapPreview), attr.AssetPreviewType);

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 08: EntityPreviewView
    // =========================================================================
    [Fact]
    public void Contract08_EntityPreviewView_InheritsAndInstantiation_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.Preview.EntityPreviewView);
        Assert.True(typeof(StridePreviewView).IsAssignableFrom(type), "EntityPreviewView deve herdar de StridePreviewView.");

        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
    }

    // =========================================================================
    // Contrato 09: UIPageEditorView
    // =========================================================================
    [Fact]
    public void Contract09_UIPageEditorView_AttributeAndInterface_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.UIPageEditorView);
        Assert.True(typeof(Control).IsAssignableFrom(type), "UIPageEditorView deve ser um Control Avalonia.");
        Assert.True(typeof(IAssetEditorView).IsAssignableFrom(type), "UIPageEditorView deve implementar IAssetEditorView.");

        var attr = type.GetCustomAttribute<AssetEditorViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(UIPageEditorViewModel), attr.EditorViewModelType);

        // Validar compatibilidade no namespace Stride.Assets.Editor.Avalonia.AssetEditors.UIPageEditor.Views
        var aliasType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.UIPageEditor.Views.UIPageEditorView");
        Assert.NotNull(aliasType);
        Assert.True(typeof(Control).IsAssignableFrom(aliasType));
    }

    // =========================================================================
    // Contrato 10: UILibraryEditorView
    // =========================================================================
    [Fact]
    public void Contract10_UILibraryEditorView_AttributeAndInterface_Valid()
    {
        var type = typeof(Stride.Assets.Editor.Avalonia.Views.UILibraryEditorView);
        Assert.True(typeof(Control).IsAssignableFrom(type), "UILibraryEditorView deve ser um Control Avalonia.");
        Assert.True(typeof(IAssetEditorView).IsAssignableFrom(type), "UILibraryEditorView deve implementar IAssetEditorView.");

        var attr = type.GetCustomAttribute<AssetEditorViewAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(UILibraryEditorViewModel), attr.EditorViewModelType);

        // Validar compatibilidade no namespace Stride.Assets.Editor.Avalonia.AssetEditors.UILibraryEditor.Views
        var aliasType = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.UILibraryEditor.Views.UILibraryEditorView");
        Assert.NotNull(aliasType);
        Assert.True(typeof(Control).IsAssignableFrom(aliasType));
    }

    // =========================================================================
    // Contrato 11: IEditorView
    // =========================================================================
    [Fact]
    public void Contract11_IEditorView_InterfaceContracts_Valid()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Services.IEditorView");
        Assert.True(type != null, "Contrato 11 Falhou: Stride.Core.Assets.Editor.Services.IEditorView não encontrado.");
        Assert.True(type.IsInterface, "IEditorView deve ser uma interface.");

        var dataContextProp = type.GetProperty("DataContext");
        Assert.NotNull(dataContextProp);
        Assert.True(dataContextProp.CanRead && dataContextProp.CanWrite);

        var editorInitProp = type.GetProperty("EditorInitialization");
        Assert.NotNull(editorInitProp);
        Assert.Equal(typeof(Task), editorInitProp.PropertyType);

        var initMethod = type.GetMethod("InitializeEditor", [typeof(IAssetEditorViewModel)]);
        Assert.NotNull(initMethod);
        Assert.Equal(typeof(Task<bool>), initMethod.ReturnType);
    }

    // =========================================================================
    // Contrato 12: IAssetCreationView
    // =========================================================================
    [Fact]
    public void Contract12_IAssetCreationView_InterfaceContracts_Valid()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Services.IAssetCreationView");
        Assert.True(type != null, "Contrato 12 Falhou: Stride.Core.Assets.Editor.Services.IAssetCreationView não encontrado.");
        Assert.True(type.IsInterface, "IAssetCreationView deve ser uma interface.");

        var createMethod = type.GetMethod("Create", [typeof(UFile), typeof(SessionViewModel), typeof(DirectoryBaseViewModel)]);
        Assert.NotNull(createMethod);
        Assert.Equal(typeof(Task<AssetItem>), createMethod.ReturnType);
    }

    // =========================================================================
    // Contrato 13: IItemTemplateDialog
    // =========================================================================
    [Fact]
    public void Contract13_IItemTemplateDialog_InterfaceContracts_Valid()
    {
        var type = CoreAssetsEditorAssembly.GetType("Stride.Core.Assets.Editor.Services.IItemTemplateDialog");
        Assert.True(type != null, "Contrato 13 Falhou: Stride.Core.Assets.Editor.Services.IItemTemplateDialog não encontrado.");
        Assert.True(type.IsInterface, "IItemTemplateDialog deve ser uma interface.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(type), "IItemTemplateDialog deve herdar de IModalDialog.");

        var selectedTemplateProp = type.GetProperty("SelectedTemplate");
        Assert.NotNull(selectedTemplateProp);
        Assert.Equal(typeof(ITemplateDescriptionViewModel), selectedTemplateProp.PropertyType);
    }

    // =========================================================================
    // Contrato 14: BindActualSizeBehavior
    // =========================================================================
    [Fact]
    public void Contract14_BindActualSizeBehavior_PropertiesAndBehavior_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Behaviors.BindActualSizeBehavior");
        Assert.True(type != null, "Contrato 14 Falhou: Stride.Assets.Editor.Avalonia.Behaviors.BindActualSizeBehavior não encontrado.");

        var behavior = (Behavior<Control>)Activator.CreateInstance(type)!;
        Assert.NotNull(behavior);

        var actualWidthProp = type.GetProperty("ActualWidth");
        Assert.NotNull(actualWidthProp);
        Assert.Equal(typeof(double), actualWidthProp.PropertyType);

        var actualHeightProp = type.GetProperty("ActualHeight");
        Assert.NotNull(actualHeightProp);
        Assert.Equal(typeof(double), actualHeightProp.PropertyType);

        // Testar anexo a um controle
        var control = new Border { Width = 250, Height = 120 };
        Interaction.GetBehaviors(control).Add(behavior);
        Assert.Equal(control, behavior.AssociatedObject);

        // Definir valores diretamente
        actualWidthProp.SetValue(behavior, 300.0);
        actualHeightProp.SetValue(behavior, 150.0);
        Assert.Equal(300.0, (double)actualWidthProp.GetValue(behavior)!);
        Assert.Equal(150.0, (double)actualHeightProp.GetValue(behavior)!);
    }

    // =========================================================================
    // Contrato 15: BindScriptTextEditorWorkspaceProjectIdBehavior
    // =========================================================================
    [Fact]
    public void Contract15_BindScriptTextEditorWorkspaceProjectIdBehavior_Valid()
    {
        var type = AssetsEditorAvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor.BindScriptTextEditorWorkspaceProjectIdBehavior");
        Assert.True(type != null, "Contrato 15 Falhou: Stride.Assets.Editor.Avalonia.AssetEditors.ScriptEditor.BindScriptTextEditorWorkspaceProjectIdBehavior não encontrado.");

        var behavior = (Behavior<Control>)Activator.CreateInstance(type)!;
        Assert.NotNull(behavior);

        var packageProp = type.GetProperty("Package");
        Assert.NotNull(packageProp);
        Assert.Equal(typeof(PackageViewModel), packageProp.PropertyType);

        var control = new Border();
        Interaction.GetBehaviors(control).Add(behavior);
        Assert.Equal(control, behavior.AssociatedObject);

        Interaction.GetBehaviors(control).Remove(behavior);
        Assert.Null(behavior.AssociatedObject);
    }

    // =========================================================================
    // Contrato 16: TypeMatchTemplateProvider
    // =========================================================================
    [Fact]
    public void Contract16_TypeMatchTemplateProvider_Matching_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.TemplateProviders.TypeMatchTemplateProvider")
                   ?? CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.TypeMatchTemplateProvider");
        Assert.True(type != null, "Contrato 16 Falhou: TypeMatchTemplateProvider não encontrado.");

        var provider = (Stride.Core.Assets.Editor.Avalonia.Views.TypeMatchTemplateProvider)Activator.CreateInstance(type)!;
        Assert.True(provider.AcceptNullable);

        provider.Type = typeof(int);
        Assert.Equal("Int32", provider.Name);

        var intNode = new MockNodeViewModel(typeof(int), 42);
        Assert.True(provider.MatchNode(intNode), "Deve combinar com int.");

        var nullableIntNode = new MockNodeViewModel(typeof(int?), 42);
        Assert.True(provider.MatchNode(nullableIntNode), "Deve combinar com int? quando AcceptNullable = true.");

        provider.AcceptNullable = false;
        Assert.False(provider.MatchNode(nullableIntNode), "Não deve combinar com int? quando AcceptNullable = false.");

        var stringNode = new MockNodeViewModel(typeof(string), "stride");
        Assert.False(provider.MatchNode(stringNode), "Não deve combinar com string.");

        provider.Type = null;
        Assert.True(provider.MatchNode(stringNode), "Deve combinar com qualquer node quando Type for null.");
    }

    // =========================================================================
    // Contrato 17: DictionaryNumberKeyTemplateProvider
    // =========================================================================
    [Fact]
    public void Contract17_DictionaryNumberKeyTemplateProvider_Matching_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.TemplateProviders.DictionaryNumberKeyTemplateProvider")
                   ?? CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.DictionaryNumberKeyTemplateProvider");
        Assert.True(type != null, "Contrato 17 Falhou: DictionaryNumberKeyTemplateProvider não encontrado.");

        var provider = (Stride.Core.Assets.Editor.Avalonia.Views.DictionaryNumberKeyTemplateProvider)Activator.CreateInstance(type)!;
        Assert.Equal("DictionaryNumberKey", provider.Name);

        // Criar um nó simulando dicionário
        var dictNode = new MockNodeViewModel(typeof(Dictionary<int, string>), new Dictionary<int, string>());
        dictNode.AssociatedData[DictionaryNodeUpdater.DictionaryNodeKeyType.Name] = typeof(int);

        Assert.True(provider.MatchNode(dictNode), "Deve combinar com dicionário com chave int.");

        var floatDictNode = new MockNodeViewModel(typeof(Dictionary<float, string>), new Dictionary<float, string>());
        floatDictNode.AssociatedData[DictionaryNodeUpdater.DictionaryNodeKeyType.Name] = typeof(float);
        Assert.True(provider.MatchNode(floatDictNode), "Deve combinar com dicionário com chave float.");

        var stringDictNode = new MockNodeViewModel(typeof(Dictionary<string, string>), new Dictionary<string, string>());
        stringDictNode.AssociatedData[DictionaryNodeUpdater.DictionaryNodeKeyType.Name] = typeof(string);
        Assert.False(provider.MatchNode(stringDictNode), "Não deve combinar com dicionário com chave string.");

        // Testar ApplyForItems com nó filho cujo pai é o dicionário
        provider.ApplyForItems = true;
        var childNode = new MockNodeViewModel(typeof(string), "value", parent: dictNode);
        Assert.True(provider.MatchNode(childNode), "Com ApplyForItems = true, deve avaliar o pai.");
    }

    // =========================================================================
    // Contrato 18: EnumTemplateProvider
    // =========================================================================
    [Fact]
    public void Contract18_EnumTemplateProvider_Matching_Valid()
    {
        var type = CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.TemplateProviders.EnumTemplateProvider")
                   ?? CoreAssetsEditorAvaloniaAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.EnumTemplateProvider");
        Assert.True(type != null, "Contrato 18 Falhou: EnumTemplateProvider não encontrado.");

        var provider = (Stride.Core.Assets.Editor.Avalonia.Views.EnumTemplateProvider)Activator.CreateInstance(type)!;
        Assert.Equal("Enum", provider.Name);

        var standardEnumNode = new MockNodeViewModel(typeof(SampleStandardEnum), SampleStandardEnum.OptionA);
        Assert.True(provider.MatchNode(standardEnumNode), "Deve combinar com standard enum.");

        var nullableEnumNode = new MockNodeViewModel(typeof(SampleStandardEnum?), null);
        Assert.True(provider.MatchNode(nullableEnumNode), "Deve combinar com nullable enum.");

        var flagsEnumNode = new MockNodeViewModel(typeof(SampleFlagsEnum), SampleFlagsEnum.First);
        Assert.True(provider.MatchNode(flagsEnumNode), "Deve combinar com flags enum por padrão.");

        provider.FlagEnum = true;
        Assert.Equal("FlagEnum", provider.Name);
        Assert.True(provider.MatchNode(flagsEnumNode), "Deve combinar com flags enum quando FlagEnum = true.");
        Assert.False(provider.MatchNode(standardEnumNode), "Não deve combinar com standard enum quando FlagEnum = true.");

        provider.FlagEnum = false;
        provider.ImageEnum = true;
        Assert.Equal("ImageEnum", provider.Name);

        var serviceProvider = new Stride.Core.Presentation.ViewModels.ViewModelServiceProvider();
        serviceProvider.RegisterService(new MockPluginService());
        var imageEnumNode = new MockNodeViewModel(typeof(SampleStandardEnum), SampleStandardEnum.OptionA, serviceProvider: serviceProvider);
        Assert.True(provider.MatchNode(imageEnumNode), "Deve combinar quando ImageEnum = true e o plugin service possui imagem para o enum.");

        var nonEnumNode = new MockNodeViewModel(typeof(int), 123);
        Assert.False(provider.MatchNode(nonEnumNode), "Não deve combinar com int.");
    }
}
