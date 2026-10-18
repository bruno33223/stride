// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets.Editor.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

public class TestModelAndProjectLibraryTemplatesIntegration
{
    private static readonly Assembly EditorAssembly = typeof(ScriptTemplateGenerator).Assembly;
    private static readonly Assembly AvaloniaAssembly = typeof(Stride.Assets.Editor.Avalonia.Services.StrideDialogService).Assembly;

    [Fact]
    public void TestModelFromFileTemplateGeneratorTypeAndSettings()
    {
        var modelGeneratorType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.ModelFromFileTemplateGenerator");
        Assert.True(modelGeneratorType != null, "Stride.Assets.Editor.Templates.ModelFromFileTemplateGenerator must exist in Stride.Assets.Editor.");

        Assert.True(typeof(AssetFromFileTemplateGenerator).IsAssignableFrom(modelGeneratorType),
            "ModelFromFileTemplateGenerator must inherit from AssetFromFileTemplateGenerator.");

        var defaultProp = modelGeneratorType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultProp);
        var defaultInstance = defaultProp.GetValue(null);
        Assert.NotNull(defaultInstance);

        var idField = modelGeneratorType.GetField("Id", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var id = (Guid)idField.GetValue(null)!;
        Assert.Equal(new Guid("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D"), id);

        var isSupportingMethod = modelGeneratorType.GetMethod("IsSupportingTemplate");
        Assert.NotNull(isSupportingMethod);
        var supportedDesc = new TemplateDescription { Id = id };
        var unsupportedDesc = new TemplateDescription { Id = Guid.NewGuid() };
        Assert.True((bool)isSupportingMethod.Invoke(defaultInstance, new object[] { supportedDesc })!);
        Assert.False((bool)isSupportingMethod.Invoke(defaultInstance, new object[] { unsupportedDesc })!);

        // Verify ModelFromFileTemplateSettings
        var settingsType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.ModelFromFileTemplateSettings");
        Assert.True(settingsType != null, "ModelFromFileTemplateSettings must exist in Stride.Assets.Editor.Templates.");

        Assert.NotNull(settingsType.GetField("ImportMaterials", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(settingsType.GetField("DeduplicateMaterials", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(settingsType.GetField("ImportTextures", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(settingsType.GetField("ImportAnimations", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(settingsType.GetField("ImportSkeleton", BindingFlags.Public | BindingFlags.Static));
        Assert.NotNull(settingsType.GetField("DefaultSkeleton", BindingFlags.Public | BindingFlags.Static));
    }

    [Fact]
    public void TestProjectLibraryTemplateGeneratorTypeAndProperties()
    {
        var libGeneratorType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.ProjectLibraryTemplateGenerator");
        Assert.True(libGeneratorType != null, "Stride.Assets.Editor.Templates.ProjectLibraryTemplateGenerator must exist in Stride.Assets.Editor.");

        var sessionGenType = typeof(SessionViewModel).Assembly.GetType("Stride.Core.Assets.Editor.Components.TemplateDescriptions.SessionTemplateGenerator");
        Assert.True(sessionGenType != null, "SessionTemplateGenerator must exist in Stride.Core.Assets.Editor.Components.TemplateDescriptions.");
        Assert.True(sessionGenType.IsAssignableFrom(libGeneratorType),
            "ProjectLibraryTemplateGenerator must inherit from SessionTemplateGenerator.");

        var defaultProp = libGeneratorType.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(defaultProp);
        var defaultInstance = defaultProp.GetValue(null);
        Assert.NotNull(defaultInstance);

        var idField = libGeneratorType.GetField("TemplateId", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(idField);
        var id = (Guid)idField.GetValue(null)!;
        Assert.Equal(new Guid("e12246ff-41a1-49d4-90e4-e72a4eb4a3e9"), id);

        var isSupportingMethod = libGeneratorType.GetMethod("IsSupportingTemplate");
        Assert.NotNull(isSupportingMethod);
        var supportedDesc = new TemplateDescription { Id = id };
        var unsupportedDesc = new TemplateDescription { Id = Guid.NewGuid() };
        Assert.True((bool)isSupportingMethod.Invoke(defaultInstance, new object[] { supportedDesc })!);
        Assert.False((bool)isSupportingMethod.Invoke(defaultInstance, new object[] { unsupportedDesc })!);
    }

    [Fact]
    public void TestModelAssetTemplateWindowTypeAndModalDialog()
    {
        var modelWindowType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.ModelAssetTemplateWindow");
        Assert.True(modelWindowType != null, "Stride.Assets.Editor.Avalonia.Views.ModelAssetTemplateWindow must exist in Stride.Assets.Editor.Avalonia.");

        Assert.True(typeof(IModalDialog).IsAssignableFrom(modelWindowType),
            "ModelAssetTemplateWindow must implement IModalDialog.");

        var dialogInterface = EditorAssembly.GetType("Stride.Assets.Editor.Services.IModelAssetTemplateDialog");
        Assert.True(dialogInterface != null, "IModelAssetTemplateDialog must exist in Stride.Assets.Editor.Services.");
        Assert.True(dialogInterface.IsAssignableFrom(modelWindowType),
            "ModelAssetTemplateWindow must implement IModelAssetTemplateDialog.");

        Assert.NotNull(modelWindowType.GetProperty("Parameters"));
        Assert.NotNull(modelWindowType.GetProperty("Result"));
        Assert.NotNull(modelWindowType.GetMethod("ShowModal"));
    }

    [Fact]
    public void TestProjectLibraryWindowTypeAndModalDialog()
    {
        var libWindowType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.ProjectLibraryWindow");
        Assert.True(libWindowType != null, "Stride.Assets.Editor.Avalonia.Views.ProjectLibraryWindow must exist in Stride.Assets.Editor.Avalonia.");

        Assert.True(typeof(IModalDialog).IsAssignableFrom(libWindowType),
            "ProjectLibraryWindow must implement IModalDialog.");

        var dialogInterface = EditorAssembly.GetType("Stride.Assets.Editor.Services.IProjectLibraryDialog");
        Assert.True(dialogInterface != null, "IProjectLibraryDialog must exist in Stride.Assets.Editor.Services.");
        Assert.True(dialogInterface.IsAssignableFrom(libWindowType),
            "ProjectLibraryWindow must implement IProjectLibraryDialog.");

        Assert.NotNull(libWindowType.GetProperty("LibraryName"));
        Assert.NotNull(libWindowType.GetProperty("Namespace"));
        Assert.NotNull(libWindowType.GetProperty("HasError"));
        Assert.NotNull(libWindowType.GetProperty("LibNameInputValidator"));
        Assert.NotNull(libWindowType.GetProperty("Result"));
        Assert.NotNull(libWindowType.GetMethod("ShowModal"));
    }

    [Fact]
    public void TestStrideTemplatesRegistrationIncludesModelAndProjectLibrary()
    {
        StrideTemplates.Register();

        var modelGenType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.ModelFromFileTemplateGenerator");
        Assert.NotNull(modelGenType);
        var libGenType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.ProjectLibraryTemplateGenerator");
        Assert.NotNull(libGenType);

        var modelDefault = modelGenType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var libDefault = libGenType.GetField("Default", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

        Assert.NotNull(modelDefault);
        Assert.NotNull(libDefault);

        var modelDesc = new TemplateDescription { Id = new Guid("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D") };
        var generatorForModel = TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(modelDesc);
        Assert.Same(modelDefault, generatorForModel);

        var libDesc = new TemplateDescription { Id = new Guid("e12246ff-41a1-49d4-90e4-e72a4eb4a3e9") };
        var generatorForLib = TemplateManager.FindTemplateGenerator<SessionTemplateGeneratorParameters>(libDesc);
        Assert.Same(libDefault, generatorForLib);
    }

    [Fact]
    public void TestStrideDialogServiceMethodsForTemplates()
    {
        var serviceInterface = EditorAssembly.GetType("Stride.Assets.Editor.Services.IStrideDialogService");
        Assert.NotNull(serviceInterface);

        var createModelMethod = serviceInterface.GetMethod("CreateModelAssetTemplateDialog");
        Assert.True(createModelMethod != null, "IStrideDialogService must declare CreateModelAssetTemplateDialog.");

        var createLibMethod = serviceInterface.GetMethod("CreateProjectLibraryDialog");
        Assert.True(createLibMethod != null, "IStrideDialogService must declare CreateProjectLibraryDialog.");

        var implType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Services.StrideDialogService");
        Assert.NotNull(implType);

        Assert.NotNull(implType.GetMethod("CreateModelAssetTemplateDialog"));
        Assert.NotNull(implType.GetMethod("CreateProjectLibraryDialog"));
    }

    [Fact]
    public async Task TestModelFromFileTemplateGeneratorPrepareAssetCreationUnattended()
    {
        var modelGenType = EditorAssembly.GetType("Stride.Assets.Editor.Templates.ModelFromFileTemplateGenerator");
        Assert.NotNull(modelGenType);

        var defaultProp = modelGenType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
        var generator = defaultProp?.GetValue(null);
        Assert.NotNull(generator);

        var parameters = new AssetTemplateGeneratorParameters(new UDirectory("/test"))
        {
            Unattended = true,
            Description = new TemplateDescription { Id = new Guid("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D") }
        };

        var prepareMethod = modelGenType.GetMethod("PrepareAssetCreation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(prepareMethod);

        var task = (Task<bool>)prepareMethod.Invoke(generator, new object[] { parameters })!;
        var result = await task;
        Assert.True(result);
    }
}
