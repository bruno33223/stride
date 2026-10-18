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
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Tests;

public class TestDotNetNewTemplatesAndDialogsIntegration
{
    private static readonly Assembly AssetsEditorAssembly = typeof(TemplateDotNetNewDescription).Assembly;
    private static readonly Assembly AvaloniaAssembly = typeof(Stride.Assets.Editor.Avalonia.Services.StrideDialogService).Assembly;

    [Fact]
    public void TestDotNetNewTemplateBridgeTypeAndRegistration()
    {
        var bridgeType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.DotNetNewTemplateBridge");
        Assert.True(bridgeType != null, "Stride.Assets.Editor.Templates.DotNetNewTemplateBridge must exist in Stride.Assets.Editor assembly.");

        var registryProp = bridgeType.GetProperty("Registry", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(registryProp != null, "DotNetNewTemplateBridge must have static property 'Registry'.");

        var registerMethod = bridgeType.GetMethod("RegisterProjectTemplates", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
        Assert.True(registerMethod != null, "DotNetNewTemplateBridge must have static method 'RegisterProjectTemplates()'.");
    }

    [Fact]
    public void TestDotNetNewTemplateRegistryTypeAndMethods()
    {
        var registryType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.DotNetNewTemplateRegistry");
        Assert.True(registryType != null, "Stride.Assets.Editor.Templates.DotNetNewTemplateRegistry must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(IDisposable).IsAssignableFrom(registryType), "DotNetNewTemplateRegistry must implement IDisposable.");

        var ctor = registryType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(string) },
            null);
        Assert.True(ctor != null, "DotNetNewTemplateRegistry must have constructor (string hostVersion, string profileDir).");

        var installMethod = registryType.GetMethod("InstallPackageAsync");
        Assert.True(installMethod != null, "DotNetNewTemplateRegistry must have 'InstallPackageAsync' method.");

        var getTemplatesMethod = registryType.GetMethod("GetTemplatesAsync");
        Assert.True(getTemplatesMethod != null, "DotNetNewTemplateRegistry must have 'GetTemplatesAsync' method.");

        var getLastChangeMethod = registryType.GetMethod("GetLastChangeTimeAsync");
        Assert.True(getLastChangeMethod != null, "DotNetNewTemplateRegistry must have 'GetLastChangeTimeAsync' method.");

        var instantiateMethod = registryType.GetMethod("InstantiateAsync");
        Assert.True(instantiateMethod != null, "DotNetNewTemplateRegistry must have 'InstantiateAsync' method.");
    }

    [Fact]
    public void TestStrideTemplateEngineEnvironmentIsolation()
    {
        var envType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.StrideTemplateEngineEnvironment");
        Assert.True(envType != null, "StrideTemplateEngineEnvironment must exist in Stride.Assets.Editor assembly.");

        var tempProfileDir = Path.Combine(Path.GetTempPath(), "StrideTestProfile_" + Guid.NewGuid().ToString("N"));
        var envInstance = Activator.CreateInstance(envType, tempProfileDir);
        Assert.NotNull(envInstance);

        var getEnvMethod = envType.GetMethod("GetEnvironmentVariable", new[] { typeof(string) });
        Assert.NotNull(getEnvMethod);

        var userProfile = (string?)getEnvMethod.Invoke(envInstance, new object[] { "USERPROFILE" });
        Assert.Equal(tempProfileDir, userProfile);

        var home = (string?)getEnvMethod.Invoke(envInstance, new object[] { "HOME" });
        Assert.Equal(tempProfileDir, home);
    }

    [Fact]
    public void TestDotNetNewTemplateGeneratorTypeAndSupport()
    {
        var generatorType = AssetsEditorAssembly.GetType("Stride.Assets.Editor.Templates.DotNetNewTemplateGenerator");
        Assert.True(generatorType != null, "Stride.Assets.Editor.Templates.DotNetNewTemplateGenerator must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(SessionTemplateGenerator).IsAssignableFrom(generatorType), "DotNetNewTemplateGenerator must inherit from SessionTemplateGenerator.");

        var generatorInstance = (SessionTemplateGenerator)Activator.CreateInstance(generatorType)!;
        Assert.NotNull(generatorInstance);

        // Test IsSupportingTemplate
        var dotNetDesc = new TemplateDotNetNewDescription();
        Assert.True(generatorInstance.IsSupportingTemplate(dotNetDesc), "DotNetNewTemplateGenerator must support TemplateDotNetNewDescription.");

        var otherDesc = new TemplateDescription();
        Assert.False(generatorInstance.IsSupportingTemplate(otherDesc), "DotNetNewTemplateGenerator must not support generic TemplateDescription.");

        Assert.Throws<ArgumentNullException>(() => generatorInstance.IsSupportingTemplate(null!));

        // Test SetParameters static method
        var setParamsMethod = generatorType.GetMethod("SetParameters", BindingFlags.Public | BindingFlags.Static);
        Assert.True(setParamsMethod != null, "DotNetNewTemplateGenerator must have static SetParameters method.");
    }

    [Fact]
    public void TestDotNetNewTemplateParametersWindowTypeAndModalDialog()
    {
        var windowType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.DotNetNewTemplateParametersWindow");
        Assert.True(windowType != null, "Stride.Assets.Editor.Avalonia.Views.DotNetNewTemplateParametersWindow must exist in Stride.Assets.Editor.Avalonia assembly.");

        var windowBaseType = typeof(global::Avalonia.Controls.Window);
        Assert.True(windowBaseType.IsAssignableFrom(windowType), "DotNetNewTemplateParametersWindow must inherit from Avalonia.Controls.Window.");

        var modalDialogInterface = typeof(IModalDialog);
        Assert.True(modalDialogInterface.IsAssignableFrom(windowType), "DotNetNewTemplateParametersWindow must implement IModalDialog.");

        var customDialogInterface = AssetsEditorAssembly.GetType("Stride.Assets.Editor.Services.IDotNetNewTemplateParametersDialog");
        Assert.True(customDialogInterface != null, "IDotNetNewTemplateParametersDialog must exist in Stride.Assets.Editor.Services.");
        Assert.True(customDialogInterface.IsAssignableFrom(windowType), "DotNetNewTemplateParametersWindow must implement IDotNetNewTemplateParametersDialog.");

        var paramsProp = windowType.GetProperty("Parameters");
        Assert.True(paramsProp != null, "DotNetNewTemplateParametersWindow must have 'Parameters' property.");

        var showModalMethod = windowType.GetMethod("ShowModal");
        Assert.True(showModalMethod != null, "DotNetNewTemplateParametersWindow must have 'ShowModal' method.");
    }

    [Theory]
    [InlineData("platforms", "Platforms")]
    [InlineData("graphicsProfile", "Graphics Profile")]
    [InlineData("HDR", "HDR")]
    [InlineData("orientation", "Orientation")]
    [InlineData("kebab-case-param", "Kebab Case Param")]
    [InlineData("snake_case_param", "Snake Case Param")]
    public void TestDotNetNewTemplateParametersWindowHumanize(string input, string expected)
    {
        var windowType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Views.DotNetNewTemplateParametersWindow");
        Assert.True(windowType != null, "DotNetNewTemplateParametersWindow must exist.");

        var humanizeMethod = windowType.GetMethod("Humanize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(humanizeMethod != null, "DotNetNewTemplateParametersWindow must have static 'Humanize' method.");

        var result = (string?)humanizeMethod.Invoke(null, new object[] { input });
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestStrideDialogServiceDotNetNewParametersDialogSupport()
    {
        var method = typeof(IStrideDialogService).GetMethod("CreateDotNetNewTemplateParametersDialog");
        Assert.True(method != null, "IStrideDialogService must declare CreateDotNetNewTemplateParametersDialog method.");

        var customDialogInterface = AssetsEditorAssembly.GetType("Stride.Assets.Editor.Services.IDotNetNewTemplateParametersDialog");
        Assert.NotNull(customDialogInterface);
        Assert.True(customDialogInterface.IsAssignableFrom(method.ReturnType), "CreateDotNetNewTemplateParametersDialog must return IDotNetNewTemplateParametersDialog.");

        var avaloniaDialogServiceType = AvaloniaAssembly.GetType("Stride.Assets.Editor.Avalonia.Services.StrideDialogService");
        Assert.NotNull(avaloniaDialogServiceType);
        Assert.True(typeof(IStrideDialogService).IsAssignableFrom(avaloniaDialogServiceType));
    }
}
