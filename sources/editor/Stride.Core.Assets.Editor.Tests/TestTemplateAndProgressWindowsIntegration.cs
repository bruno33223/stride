// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Xunit;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Testes de integração rigorosos para a Fatia 1 (slice-1) do Marco P32:
/// "Template & Progress Windows & Dialogs".
/// Valida contratos, herança, propriedades, styled properties, métodos e desacoplamento total de WPF.
/// </summary>
public class TestTemplateAndProgressWindowsIntegration
{
    private static readonly Assembly AvaloniaViewsAssembly = typeof(Stride.Core.Assets.Editor.Avalonia.Views.TypeMatchTemplateProvider).Assembly;

    // =========================================================================
    // Contrato 1: ProjectSelectionWindow - Tipo, Interfaces e Estrutura
    // =========================================================================
    [Fact]
    public void Contract01_ProjectSelectionWindow_TypeAndInterfaces_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ProjectSelectionWindow");
        Assert.True(type != null, "ProjectSelectionWindow deve existir em Stride.Core.Assets.Editor.Avalonia.Views");
        Assert.True(typeof(Window).IsAssignableFrom(type), "ProjectSelectionWindow deve herdar de Avalonia.Controls.Window");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(type), "ProjectSelectionWindow deve implementar IModalDialog");

        var newSessionParamsProp = type.GetProperty("NewSessionParameters");
        Assert.NotNull(newSessionParamsProp);
        Assert.Equal(typeof(NewSessionParameters), newSessionParamsProp.PropertyType);

        var existingSessionPathProp = type.GetProperty("ExistingSessionPath");
        Assert.NotNull(existingSessionPathProp);
        Assert.Equal(typeof(UFile), existingSessionPathProp.PropertyType);

        var templatesProp = type.GetProperty("Templates");
        Assert.NotNull(templatesProp);
        Assert.Equal(typeof(NewOrOpenSessionTemplateCollectionViewModel), templatesProp.PropertyType);

        var resultProp = type.GetProperty("Result");
        Assert.NotNull(resultProp);
        Assert.Equal(typeof(DialogResult), resultProp.PropertyType);

        var showModalMethod = type.GetMethod("ShowModal", Type.EmptyTypes);
        Assert.NotNull(showModalMethod);
        Assert.Equal(typeof(Task<DialogResult>), showModalMethod.ReturnType);

        var requestCloseMethod = type.GetMethod("RequestClose", [typeof(DialogResult)]);
        Assert.NotNull(requestCloseMethod);

        var validatePropertiesMethod = type.GetMethod("ValidateProperties", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(validatePropertiesMethod);
        Assert.Equal(typeof(bool), validatePropertiesMethod.ReturnType);
    }

    // =========================================================================
    // Contrato 2: ProjectSelectionWindow - Construtor e Bytecode
    // =========================================================================
    [Fact]
    public void Contract02_ProjectSelectionWindow_ConstructorsAndBytecode_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ProjectSelectionWindow");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var showModalMethod = type.GetMethod("ShowModal", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(showModalMethod);
        var stateMachineAttr = showModalMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        Assert.NotNull(stateMachineAttr);
    }

    // =========================================================================
    // Contrato 3: WorkProgressWindow - Tipo, Interfaces e Estrutura
    // =========================================================================
    [Fact]
    public void Contract03_WorkProgressWindow_TypeAndInterfaces_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.WorkProgressWindow");
        Assert.True(type != null, "WorkProgressWindow deve existir em Stride.Core.Assets.Editor.Avalonia.Views");
        Assert.True(typeof(Window).IsAssignableFrom(type), "WorkProgressWindow deve herdar de Avalonia.Controls.Window");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(type), "WorkProgressWindow deve implementar IModalDialog");

        var workProgressProp = type.GetProperty("WorkProgress");
        Assert.NotNull(workProgressProp);
        Assert.Equal(typeof(WorkProgressViewModel), workProgressProp.PropertyType);

        var resultProp = type.GetProperty("Result");
        Assert.NotNull(resultProp);
        Assert.Equal(typeof(DialogResult), resultProp.PropertyType);

        var currentHandleField = type.GetField("CurrentHandle", BindingFlags.Public | BindingFlags.Static);
        var currentHandleProp = type.GetProperty("CurrentHandle", BindingFlags.Public | BindingFlags.Static);
        Assert.True(currentHandleField != null || currentHandleProp != null, "WorkProgressWindow deve possuir membro estático CurrentHandle");

        var showModalMethod = type.GetMethod("ShowModal", Type.EmptyTypes);
        Assert.NotNull(showModalMethod);
        Assert.Equal(typeof(Task<DialogResult>), showModalMethod.ReturnType);

        var requestCloseMethod = type.GetMethod("RequestClose", [typeof(DialogResult)]);
        Assert.NotNull(requestCloseMethod);
    }

    // =========================================================================
    // Contrato 4: WorkProgressWindow - Construtores e Bytecode
    // =========================================================================
    [Fact]
    public void Contract04_WorkProgressWindow_ConstructorsAndBytecode_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.WorkProgressWindow");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var paramCtor = type.GetConstructor([typeof(WorkProgressViewModel)]);
        Assert.NotNull(paramCtor);

        var showModalMethod = type.GetMethod("ShowModal", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(showModalMethod);
        var stateMachineAttr = showModalMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        Assert.NotNull(stateMachineAttr);
    }

    // =========================================================================
    // Contrato 5: ItemTemplatesWindow - Tipo, Interfaces e Estrutura
    // =========================================================================
    [Fact]
    public void Contract05_ItemTemplatesWindow_TypeAndInterfaces_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ItemTemplatesWindow");
        Assert.True(type != null, "ItemTemplatesWindow deve existir em Stride.Core.Assets.Editor.Avalonia.Views");
        Assert.True(typeof(Window).IsAssignableFrom(type), "ItemTemplatesWindow deve herdar de Avalonia.Controls.Window");
        Assert.True(typeof(IItemTemplateDialog).IsAssignableFrom(type), "ItemTemplatesWindow deve implementar IItemTemplateDialog");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(type), "ItemTemplatesWindow deve implementar IModalDialog");

        var viewModelProp = type.GetProperty("ViewModel");
        Assert.NotNull(viewModelProp);
        Assert.Equal(typeof(AssetTemplatesViewModel), viewModelProp.PropertyType);

        var selectedTemplateProp = type.GetProperty("SelectedTemplate");
        Assert.NotNull(selectedTemplateProp);
        Assert.Equal(typeof(ITemplateDescriptionViewModel), selectedTemplateProp.PropertyType);

        var resultProp = type.GetProperty("Result");
        Assert.NotNull(resultProp);
        Assert.Equal(typeof(DialogResult), resultProp.PropertyType);

        var showModalMethod = type.GetMethod("ShowModal", Type.EmptyTypes);
        Assert.NotNull(showModalMethod);
        Assert.Equal(typeof(Task<DialogResult>), showModalMethod.ReturnType);

        var requestCloseMethod = type.GetMethod("RequestClose", [typeof(DialogResult)]);
        Assert.NotNull(requestCloseMethod);
    }

    // =========================================================================
    // Contrato 6: ItemTemplatesWindow - Construtores e Bytecode
    // =========================================================================
    [Fact]
    public void Contract06_ItemTemplatesWindow_ConstructorsAndBytecode_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ItemTemplatesWindow");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var paramCtor = type.GetConstructor([typeof(AssetTemplatesViewModel)]);
        Assert.NotNull(paramCtor);

        var showModalMethod = type.GetMethod("ShowModal", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(showModalMethod);
        var stateMachineAttr = showModalMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        Assert.NotNull(stateMachineAttr);
    }

    // =========================================================================
    // Contrato 7: AddItemWindow - Tipo, Interfaces e Estrutura
    // =========================================================================
    [Fact]
    public void Contract07_AddItemWindow_TypeAndInterfaces_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.AddItemWindow");
        Assert.True(type != null, "AddItemWindow deve existir em Stride.Core.Assets.Editor.Avalonia.Views");
        Assert.True(typeof(Window).IsAssignableFrom(type), "AddItemWindow deve herdar de Avalonia.Controls.Window");
        Assert.True(typeof(IItemTemplateDialog).IsAssignableFrom(type), "AddItemWindow deve implementar IItemTemplateDialog");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(type), "AddItemWindow deve implementar IModalDialog");

        var addItemCommandProp = type.GetProperty("AddItemCommand");
        Assert.NotNull(addItemCommandProp);
        Assert.True(typeof(ICommand).IsAssignableFrom(addItemCommandProp.PropertyType));

        var selectedTemplateProp = type.GetProperty("SelectedTemplate");
        Assert.NotNull(selectedTemplateProp);
        Assert.Equal(typeof(ITemplateDescriptionViewModel), selectedTemplateProp.PropertyType);

        var resultProp = type.GetProperty("Result");
        Assert.NotNull(resultProp);
        Assert.Equal(typeof(DialogResult), resultProp.PropertyType);

        var showModalMethod = type.GetMethod("ShowModal", Type.EmptyTypes);
        Assert.NotNull(showModalMethod);
        Assert.Equal(typeof(Task<DialogResult>), showModalMethod.ReturnType);

        var requestCloseMethod = type.GetMethod("RequestClose", [typeof(DialogResult)]);
        Assert.NotNull(requestCloseMethod);
    }

    // =========================================================================
    // Contrato 8: AddItemWindow - Construtores e Bytecode
    // =========================================================================
    [Fact]
    public void Contract08_AddItemWindow_ConstructorsAndBytecode_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.AddItemWindow");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var paramCtor = type.GetConstructor([typeof(IViewModelServiceProvider), typeof(TemplateDescriptionCollectionViewModel)]);
        Assert.NotNull(paramCtor);

        var showModalMethod = type.GetMethod("ShowModal", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(showModalMethod);
        var stateMachineAttr = showModalMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        Assert.NotNull(stateMachineAttr);
    }

    // =========================================================================
    // Contrato 9: AddItemUserControl - Tipo, StyledProperties e Constantes
    // =========================================================================
    [Fact]
    public void Contract09_AddItemUserControl_TypeAndStyledProperties_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.AddItemUserControl");
        Assert.True(type != null, "AddItemUserControl deve existir em Stride.Core.Assets.Editor.Avalonia.Views");
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "AddItemUserControl deve herdar de Avalonia.Controls.UserControl");

        var templateListWidthConst = type.GetField("TemplateListWidth", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(templateListWidthConst);
        Assert.Equal(500.0, (double)templateListWidthConst.GetValue(null)!);

        var templateCollectionField = type.GetField("TemplateCollectionProperty", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(templateCollectionField);
        Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(templateCollectionField.FieldType));

        var addItemCommandField = type.GetField("AddItemCommandProperty", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(addItemCommandField);
        Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(addItemCommandField.FieldType));

        var selectFilesField = type.GetField("SelectFilesToCreateItemCommandProperty", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(selectFilesField);
        Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(selectFilesField.FieldType));
    }

    // =========================================================================
    // Contrato 10: AddItemUserControl - Construtores e Membros
    // =========================================================================
    [Fact]
    public void Contract10_AddItemUserControl_ConstructorsAndMembers_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.AddItemUserControl");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var templateCollectionProp = type.GetProperty("TemplateCollection");
        Assert.NotNull(templateCollectionProp);
        Assert.Equal(typeof(AddItemTemplateCollectionViewModel), templateCollectionProp.PropertyType);

        var addItemCommandProp = type.GetProperty("AddItemCommand");
        Assert.NotNull(addItemCommandProp);
        Assert.True(typeof(ICommand).IsAssignableFrom(addItemCommandProp.PropertyType));

        var selectFilesProp = type.GetProperty("SelectFilesToCreateItemCommand");
        Assert.NotNull(selectFilesProp);
        Assert.True(typeof(ICommand).IsAssignableFrom(selectFilesProp.PropertyType));
    }

    // =========================================================================
    // Contrato 11: ObjectBrowserUserControl - Tipo e StyledProperties
    // =========================================================================
    [Fact]
    public void Contract11_ObjectBrowserUserControl_TypeAndStyledProperties_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ObjectBrowserUserControl");
        Assert.True(type != null, "ObjectBrowserUserControl deve existir em Stride.Core.Assets.Editor.Avalonia.Views");
        Assert.True(typeof(UserControl).IsAssignableFrom(type), "ObjectBrowserUserControl deve herdar de Avalonia.Controls.UserControl");

        string[] requiredProperties =
        [
            "HierarchyItemsSource",
            "SelectedHierarchyItem",
            "HierarchyItemTemplate",
            "HierarchyItemContainerStyle",
            "ObjectItemsSource",
            "SelectedObjectItem",
            "ObjectItemTemplate",
            "ObjectItemTemplateSelector",
            "ObjectItemContainerStyle",
            "ObjectDescriptionTemplate",
            "ObjectDescriptionTemplateSelector"
        ];

        foreach (var propName in requiredProperties)
        {
            var styledPropField = type.GetField(propName + "Property", BindingFlags.Public | BindingFlags.Static);
            Assert.True(styledPropField != null, $"ObjectBrowserUserControl deve conter a propriedade Avalonia {propName}Property");
            Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(styledPropField.FieldType), $"{propName}Property deve ser um AvaloniaProperty");

            var clrProp = type.GetProperty(propName);
            Assert.True(clrProp != null, $"ObjectBrowserUserControl deve conter a propriedade CLR {propName}");
        }
    }

    // =========================================================================
    // Contrato 12: ObjectBrowserUserControl - Construtor e Membros
    // =========================================================================
    [Fact]
    public void Contract12_ObjectBrowserUserControl_ConstructorsAndMembers_Valid()
    {
        var type = AvaloniaViewsAssembly.GetType("Stride.Core.Assets.Editor.Avalonia.Views.ObjectBrowserUserControl");
        Assert.NotNull(type);

        var defaultCtor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(defaultCtor);

        var hierarchyItemsProp = type.GetProperty("HierarchyItemsSource");
        Assert.NotNull(hierarchyItemsProp);
        Assert.Equal(typeof(IEnumerable), hierarchyItemsProp.PropertyType);

        var selectedHierarchyProp = type.GetProperty("SelectedHierarchyItem");
        Assert.NotNull(selectedHierarchyProp);
        Assert.Equal(typeof(object), selectedHierarchyProp.PropertyType);

        var objectItemsProp = type.GetProperty("ObjectItemsSource");
        Assert.NotNull(objectItemsProp);
        Assert.Equal(typeof(IEnumerable), objectItemsProp.PropertyType);

        var selectedObjectProp = type.GetProperty("SelectedObjectItem");
        Assert.NotNull(selectedObjectProp);
        Assert.Equal(typeof(object), selectedObjectProp.PropertyType);
    }

    // =========================================================================
    // Contrato 13: Desacoplamento Estrito de Win32/WPF
    // =========================================================================
    [Fact]
    public void Contract13_StrictDecouplingFromWpfAndWin32()
    {
        string[] targetTypeNames =
        [
            "Stride.Core.Assets.Editor.Avalonia.Views.ProjectSelectionWindow",
            "Stride.Core.Assets.Editor.Avalonia.Views.WorkProgressWindow",
            "Stride.Core.Assets.Editor.Avalonia.Views.ItemTemplatesWindow",
            "Stride.Core.Assets.Editor.Avalonia.Views.AddItemWindow",
            "Stride.Core.Assets.Editor.Avalonia.Views.AddItemUserControl",
            "Stride.Core.Assets.Editor.Avalonia.Views.ObjectBrowserUserControl"
        ];

        foreach (var typeName in targetTypeNames)
        {
            var type = AvaloniaViewsAssembly.GetType(typeName);
            Assert.True(type != null, $"Tipo {typeName} deve existir para validação de desacoplamento.");

            // Verifica que o tipo não herda de nenhum tipo de PresentationFramework/PresentationCore/WindowsBase
            var baseType = type.BaseType;
            while (baseType != null)
            {
                var asmName = baseType.Assembly.GetName().Name ?? string.Empty;
                Assert.False(asmName.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase), $"{typeName} herda de tipo WPF: {baseType.FullName}");
                Assert.False(asmName.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase), $"{typeName} herda de tipo WPF: {baseType.FullName}");
                Assert.False(asmName.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase), $"{typeName} herda de tipo WPF: {baseType.FullName}");
                baseType = baseType.BaseType;
            }

            // Verifica interfaces implementadas
            foreach (var iface in type.GetInterfaces())
            {
                var asmName = iface.Assembly.GetName().Name ?? string.Empty;
                Assert.False(asmName.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase), $"{typeName} implementa interface WPF: {iface.FullName}");
                Assert.False(asmName.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase), $"{typeName} implementa interface WPF: {iface.FullName}");
            }
        }
    }
}
