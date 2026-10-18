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
using Stride.Assets.Editor.Services;
using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Tests;

public class TestScriptTemplatesAndGeneratorsIntegration
{
    private static string FindRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        while (baseDir != null && !Directory.Exists(Path.Combine(baseDir, "sources")))
        {
            baseDir = Path.GetDirectoryName(baseDir);
        }
        return baseDir ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    [Fact]
    public void TestTemplateDotNetNewDescriptionTypeAndProperties()
    {
        var editorAsm = typeof(ScriptTemplateGenerator).Assembly;
        var dotNetDescType = editorAsm.GetType("Stride.Assets.Editor.Templates.TemplateDotNetNewDescription");
        Assert.True(dotNetDescType != null, "Stride.Assets.Editor.Templates.TemplateDotNetNewDescription must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(TemplateDescription).IsAssignableFrom(dotNetDescType), "TemplateDotNetNewDescription must inherit from TemplateDescription.");

        var instance = Activator.CreateInstance(dotNetDescType);
        Assert.NotNull(instance);

        var identityProp = dotNetDescType.GetProperty("TemplateIdentity");
        Assert.NotNull(identityProp);
        identityProp.SetValue(instance, "Stride.Game.Template");
        Assert.Equal("Stride.Game.Template", identityProp.GetValue(instance));

        var shortNameProp = dotNetDescType.GetProperty("TemplateShortName");
        Assert.NotNull(shortNameProp);
        shortNameProp.SetValue(instance, "stride-game");
        Assert.Equal("stride-game", shortNameProp.GetValue(instance));
    }

    [Fact]
    public void TestScriptTemplateGeneratorSupport()
    {
        var generator = ScriptTemplateGenerator.Default;
        Assert.NotNull(generator);

        Assert.Throws<ArgumentNullException>(() => generator.IsSupportingTemplate(null!));

        var genericDesc = new TemplateDescription();
        Assert.False(generator.IsSupportingTemplate(genericDesc));

        var scriptTemplateDesc = new TemplateAssetDescription
        {
            AssetTypeName = "ScriptSourceFileAsset"
        };
        Assert.True(generator.IsSupportingTemplate(scriptTemplateDesc), "ScriptTemplateGenerator should support ScriptSourceFileAsset template.");
    }

    [Fact]
    public void TestScriptTemplateGeneratorOverrideParameters()
    {
        var customParams = ScriptTemplateGenerator.GetAssetOverrideParameters("MyCustomClass", true);
        Assert.True(customParams.Count > 0);

        var descList = new List<TemplateDescription>
        {
            new TemplateAssetDescription { AssetTypeName = "ScriptSourceFileAsset", Name = "Script1" },
            new TemplateAssetDescription { AssetTypeName = "OtherAsset", Name = "Other" },
            new TemplateDescription { Name = "Generic" }
        };

        var filtered = ScriptTemplateGenerator.GetScriptTemplateAssetDescriptions(descList).ToList();
        Assert.Single(filtered);
        Assert.Equal("Script1", filtered[0].Name);
    }

    [Fact]
    public void TestScriptNameWindowTypeAndModalDialog()
    {
        var avaloniaAsm = typeof(Stride.Assets.Editor.Avalonia.Services.StrideDialogService).Assembly;
        var scriptNameWindowType = avaloniaAsm.GetType("Stride.Assets.Editor.Avalonia.Views.ScriptNameWindow");
        Assert.True(scriptNameWindowType != null, "Stride.Assets.Editor.Avalonia.Views.ScriptNameWindow must exist in Stride.Assets.Editor.Avalonia assembly.");

        var modalDialogInterface = typeof(IModalDialog);
        Assert.True(modalDialogInterface.IsAssignableFrom(scriptNameWindowType), "ScriptNameWindow must implement IModalDialog.");

        var scriptNameDialogType = typeof(ScriptTemplateGenerator).Assembly.GetType("Stride.Assets.Editor.Services.IScriptNameDialog");
        Assert.True(scriptNameDialogType != null, "IScriptNameDialog must exist in Stride.Assets.Editor.Services.");
        Assert.True(scriptNameDialogType.IsAssignableFrom(scriptNameWindowType), "ScriptNameWindow must implement IScriptNameDialog.");
    }

    [Fact]
    public void TestScriptNameWindowValidationLogic()
    {
        var avaloniaAsm = typeof(Stride.Assets.Editor.Avalonia.Services.StrideDialogService).Assembly;
        var scriptNameWindowType = avaloniaAsm.GetType("Stride.Assets.Editor.Avalonia.Views.ScriptNameWindow");
        Assert.NotNull(scriptNameWindowType);

        Assert.True(typeof(IScriptNameDialog).IsAssignableFrom(scriptNameWindowType));
        Assert.True(typeof(IModalDialog).IsAssignableFrom(scriptNameWindowType));

        // Test constructor existence
        var ctor = scriptNameWindowType.GetConstructor(new[]
        {
            typeof(string),
            typeof(string),
            typeof(TemplateAssetDescription),
            typeof(bool),
            typeof(IEnumerable<TemplateAssetDescription>)
        });
        Assert.NotNull(ctor);

        // Test properties existence
        Assert.NotNull(scriptNameWindowType.GetProperty("ClassName"));
        Assert.NotNull(scriptNameWindowType.GetProperty("Namespace"));
        Assert.NotNull(scriptNameWindowType.GetProperty("ScriptTemplate"));
        Assert.NotNull(scriptNameWindowType.GetProperty("Result"));

        // Test methods existence
        Assert.NotNull(scriptNameWindowType.GetMethod("Validate"));
        Assert.NotNull(scriptNameWindowType.GetMethod("Cancel"));
        Assert.NotNull(scriptNameWindowType.GetMethod("RequestClose"));

        // Test C# identifier and namespace validation rules
        var validClass = Stride.Utilities.BuildValidClassName("My Test Script", null);
        Assert.Equal("My_Test_Script", validClass);

        var validNs = Stride.Utilities.BuildValidNamespaceName("My Test Namespace", null);
        Assert.Equal("My_Test_Namespace", validNs);

        Assert.Equal("ValidClassName", Stride.Utilities.BuildValidClassName("ValidClassName"));
        Assert.Equal("_123Invalid", Stride.Utilities.BuildValidClassName("123Invalid"));
        Assert.Equal("class_", Stride.Utilities.BuildValidClassName("class"));
        Assert.Equal("My.Valid.Namespace", Stride.Utilities.BuildValidNamespaceName("My.Valid.Namespace"));
        Assert.Equal("My_123InvalidNamespace", Stride.Utilities.BuildValidNamespaceName("My.123InvalidNamespace"));
    }

    private sealed class MockScriptNameDialog : IScriptNameDialog
    {
        public string ClassName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public TemplateAssetDescription? ScriptTemplate { get; set; }
        public object? DataContext { get; set; }
        public DialogResult Result { get; set; } = DialogResult.Ok;
        public Task<DialogResult> ShowModal() => Task.FromResult(Result);
        public void RequestClose(DialogResult result) => Result = result;
    }

    [Fact]
    public void TestScriptNameDialogServiceMethod()
    {
        var method = typeof(IStrideDialogService).GetMethod("CreateScriptNameDialog");
        Assert.True(method != null, "IStrideDialogService must declare CreateScriptNameDialog method.");

        var avaloniaDialogServiceType = typeof(Stride.Assets.Editor.Avalonia.Services.StrideDialogService);
        var implMethod = avaloniaDialogServiceType.GetMethod("CreateScriptNameDialog");
        Assert.True(implMethod != null, "StrideDialogService must implement CreateScriptNameDialog.");

        var prevFactory = Stride.Assets.Editor.Avalonia.Services.StrideDialogService.ScriptNameDialogFactory;
        try
        {
            Stride.Assets.Editor.Avalonia.Services.StrideDialogService.ScriptNameDialogFactory = (cls, ns, tpl, enableSelect, list) =>
                new MockScriptNameDialog { ClassName = cls, Namespace = ns, ScriptTemplate = tpl };

            var service = (IStrideDialogService)Activator.CreateInstance(avaloniaDialogServiceType)!;
            var dialog = service.CreateScriptNameDialog("TestClass", "TestNamespace", null, false, null);
            Assert.NotNull(dialog);
            Assert.Equal("TestClass", dialog.ClassName);
            Assert.Equal("TestNamespace", dialog.Namespace);
        }
        finally
        {
            Stride.Assets.Editor.Avalonia.Services.StrideDialogService.ScriptNameDialogFactory = prevFactory;
        }
    }

    [Fact]
    public void TestScriptTemplatesFilesAndMacroIntegrity()
    {
        var root = FindRepoRoot();
        var scriptsDir = Path.Combine(root, "sources", "editor", "Stride.Assets.Editor", "Templates", "Assets", "Scripts");
        Assert.True(Directory.Exists(scriptsDir), $"Script templates directory must exist at {scriptsDir}");

        var expectedScripts = new[]
        {
            "Camera/FreeCamera.cs",
            "Camera/BasicCameraController.cs",
            "Camera/FpsCamera.cs",
            "Camera/OrbitCamera.cs",
            "Camera/SideScrollingCamera.cs",
            "Physics/PlayerController.cs",
            "Physics/DebugShapesRender.cs",
            "Events/EventBroadcaster.cs",
            "Events/EventListener.cs",
            "Utility/SceneStreaming.cs",
            "Utility/GameProfiler.cs",
            "DefaultSyncScript.cs",
            "DefaultAsyncScript.cs",
            "DefaultStartupScript.cs"
        };

        foreach (var relativePath in expectedScripts)
        {
            var fullPath = Path.Combine(scriptsDir, relativePath);
            Assert.True(File.Exists(fullPath), $"Script template file '{relativePath}' must exist at '{fullPath}'.");

            var content = File.ReadAllText(fullPath);
            if (relativePath != "Camera/FreeCamera.cs" && relativePath != "Camera/OrbitCamera.cs")
            {
                Assert.Contains("##Namespace##", content);
                Assert.Contains("##Scriptname##", content);
            }
            else
            {
                Assert.Contains("namespace Stride.Scripts;", content);
            }
        }
    }

    [Fact]
    public async Task TestScriptTemplateGeneratorPrepareAssetCreationUnattended()
    {
        var generator = ScriptTemplateGenerator.Default;
        var parameters = new AssetTemplateGeneratorParameters(new UDirectory("/test"))
        {
            Unattended = true,
            Namespace = "Game.Scripts",
            Description = new TemplateAssetDescription
            {
                AssetTypeName = "ScriptSourceFileAsset",
                DefaultOutputName = "NewScript"
            }
        };

        var prepareMethod = typeof(ScriptTemplateGenerator).GetMethod("PrepareAssetCreation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(prepareMethod);

        var task = (Task<bool>)prepareMethod.Invoke(generator, new object[] { parameters })!;
        var result = await task;
        Assert.True(result);
    }
}
