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
using Stride.Assets;
using Stride.Assets.Editor.Avalonia.Services;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Core.Assets.Editor.Tests;

public class TestUpdatePlatformsAndSolutionViewModelsIntegration
{
    private static Assembly GetEditorAssembly()
    {
        return typeof(UpdatePlatformsTemplateGenerator).Assembly;
    }

    private static Assembly GetAvaloniaAssembly()
    {
        return typeof(UpdatePlatformsWindow).Assembly;
    }

    [Fact]
    public void TestSolutionPlatformViewModelTypeAndProperties()
    {
        var editorAsm = GetEditorAssembly();
        var vmType = editorAsm.GetType("Stride.Assets.Editor.Templates.SolutionPlatformViewModel");
        Assert.True(vmType != null, "Stride.Assets.Editor.Templates.SolutionPlatformViewModel must exist in Stride.Assets.Editor assembly.");

        Assert.True(typeof(ViewModelBase).IsAssignableFrom(vmType), "SolutionPlatformViewModel must inherit from ViewModelBase.");

        // Check constructor
        var ctor = vmType.GetConstructor(new[]
        {
            typeof(IViewModelServiceProvider),
            typeof(SolutionPlatform),
            typeof(bool),
            typeof(bool)
        });
        Assert.True(ctor != null, "SolutionPlatformViewModel must have (IViewModelServiceProvider, SolutionPlatform, bool, bool) constructor.");

        // Check properties
        Assert.NotNull(vmType.GetProperty("Name"));
        Assert.NotNull(vmType.GetProperty("Platform"));
        Assert.NotNull(vmType.GetProperty("CanBeUnselected"));
        Assert.NotNull(vmType.GetProperty("IsSelected"));
        Assert.NotNull(vmType.GetProperty("AvailableTemplates"));
        Assert.NotNull(vmType.GetProperty("HasTemplates"));
        Assert.NotNull(vmType.GetProperty("SelectedTemplate"));
        Assert.NotNull(vmType.GetProperty("IsAlreadyInstalled"));
        Assert.NotNull(vmType.GetProperty("IsAvailableOnMachine"));
        Assert.NotNull(vmType.GetProperty("MarkedToRemove"));
        Assert.NotNull(vmType.GetProperty("SolutionPlatform"));

        // Instantiate and test behavior
        var serviceProvider = new ViewModelServiceProvider(new object[] { new NullDispatcherService() });
        var windowsPlatform = AssetRegistry.SupportedPlatforms.FirstOrDefault(p => p.Type == PlatformType.Windows)
            ?? new SolutionPlatform { Type = PlatformType.Windows, Name = "Windows" };
        var androidPlatform = AssetRegistry.SupportedPlatforms.FirstOrDefault(p => p.Type == PlatformType.Android)
            ?? new SolutionPlatform { Type = PlatformType.Android, Name = "Android" };

        // Windows cannot be unselected
        var winVm = new SolutionPlatformViewModel(serviceProvider, windowsPlatform, true, true);
        Assert.False(winVm.CanBeUnselected, "Windows platform should not be unselectable.");
        Assert.True(winVm.IsSelected);
        Assert.False(winVm.MarkedToRemove);
        Assert.Equal(PlatformType.Windows, winVm.Platform);
        Assert.Equal(windowsPlatform.DisplayName ?? windowsPlatform.Name, winVm.Name);

        // Android can be unselected
        var androidVm = new SolutionPlatformViewModel(serviceProvider, androidPlatform, true, true);
        Assert.True(androidVm.CanBeUnselected, "Android platform should be unselectable.");
        Assert.True(androidVm.IsSelected);
        Assert.False(androidVm.MarkedToRemove);

        // When Android is already installed and unselected, MarkedToRemove becomes true
        androidVm.IsSelected = false;
        Assert.False((bool)androidVm.IsSelected);
        Assert.True((bool)androidVm.MarkedToRemove, "When installed platform is unselected, MarkedToRemove must be true.");

        // When platform was not installed, MarkedToRemove stays false even when unselected
        var notInstalledVm = new SolutionPlatformViewModel(serviceProvider, androidPlatform, false, false);
        Assert.False(notInstalledVm.IsSelected);
        Assert.False(notInstalledVm.MarkedToRemove, "When platform is not installed, MarkedToRemove must be false.");
    }

    [Fact]
    public void TestUpdatePlatformsTemplateGeneratorTypeAndTemplateSupport()
    {
        var generatorType = typeof(UpdatePlatformsTemplateGenerator);
        var expectedGuid = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");

        Assert.Equal(expectedGuid, UpdatePlatformsTemplateGenerator.TemplateId);
        Assert.NotNull(UpdatePlatformsTemplateGenerator.Default);

        var matchingDesc = new TemplateDescription { Id = expectedGuid };
        var nonMatchingDesc = new TemplateDescription { Id = Guid.NewGuid() };

        Assert.True(UpdatePlatformsTemplateGenerator.Default.IsSupportingTemplate(matchingDesc));
        Assert.False(UpdatePlatformsTemplateGenerator.Default.IsSupportingTemplate(nonMatchingDesc));
        Assert.Throws<ArgumentNullException>(() => UpdatePlatformsTemplateGenerator.Default.IsSupportingTemplate(null!));
    }

    [Fact]
    public void TestUpdatePlatformsWindowTypeAndModalDialogContract()
    {
        var dialogInterface = typeof(IUpdatePlatformsDialog);
        Assert.True(typeof(IModalDialog).IsAssignableFrom(dialogInterface), "IUpdatePlatformsDialog must implement IModalDialog.");

        var windowType = typeof(UpdatePlatformsWindow);
        Assert.True(typeof(global::Avalonia.Controls.Window).IsAssignableFrom(windowType), "UpdatePlatformsWindow must inherit from Avalonia.Controls.Window.");
        Assert.True(dialogInterface.IsAssignableFrom(windowType), "UpdatePlatformsWindow must implement IUpdatePlatformsDialog.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(windowType), "UpdatePlatformsWindow must implement IModalDialog.");

        // Check constructors
        Assert.NotNull(windowType.GetConstructor(Type.EmptyTypes));
        Assert.NotNull(windowType.GetConstructor(new[] { typeof(ICollection<PlatformType>), typeof(bool) }));

        // Check properties
        Assert.NotNull(windowType.GetProperty("AvailablePlatforms"));
        Assert.NotNull(windowType.GetProperty("ForcePlatformRegenerationVisible"));
        Assert.NotNull(windowType.GetProperty("ForcePlatformRegeneration"));
        Assert.NotNull(windowType.GetProperty("SelectedPlatforms"));
        Assert.NotNull(windowType.GetProperty("Result"));

        // Check methods
        Assert.NotNull(windowType.GetMethod("ValidateAsync"));
        Assert.NotNull(windowType.GetMethod("Cancel"));
        Assert.NotNull(windowType.GetMethod("RequestClose"));
        Assert.NotNull(windowType.GetMethod("ShowModal"));
    }

    [Fact]
    public void TestStrideDialogServiceUpdatePlatformsDialogMethod()
    {
        var service = new StrideDialogService();
        var installed = new[] { PlatformType.Windows };

        var prevFactory = StrideDialogService.UpdatePlatformsDialogFactory;
        try
        {
            bool factoryCalled = false;
            StrideDialogService.UpdatePlatformsDialogFactory = (plat, force) =>
            {
                factoryCalled = true;
                return new MockUpdatePlatformsDialog();
            };

            var dialog = service.CreateUpdatePlatformsDialog(installed, true);
            Assert.NotNull(dialog);
            Assert.True(factoryCalled);
        }
        finally
        {
            StrideDialogService.UpdatePlatformsDialogFactory = prevFactory;
        }
    }

    [Fact]
    public void TestStrideTemplatesRegistration()
    {
        StrideTemplates.Register();

        // Check if UpdatePlatformsTemplateGenerator is registered
        var expectedGuid = new Guid("446B52D3-A6A8-4274-A357-736ADEA87321");
        var matchingDesc = new TemplateDescription { Id = expectedGuid };
        var generator = TemplateManager.FindTemplateGenerator<PackageTemplateGeneratorParameters>(matchingDesc);
        Assert.NotNull(generator);
        Assert.IsType<UpdatePlatformsTemplateGenerator>(generator);
    }

    [Fact]
    public async Task TestUpdatePlatformsTemplateGeneratorPrepareForRunUnattended()
    {
        var generator = UpdatePlatformsTemplateGenerator.Default;
        var package = new Package();
        var parameters = new PackageTemplateGeneratorParameters
        {
            Package = package,
            OutputDirectory = new UDirectory("/test"),
            Unattended = true,
            Name = "TestGame"
        };

        // Throws exception because game settings asset is missing / platforms not set
        await Assert.ThrowsAnyAsync<Exception>(() => generator.PrepareForRun(parameters));
    }

    [Fact]
    public async Task TestUpdatePlatformsTemplateGeneratorInteractiveDialogFlow()
    {
        var generator = UpdatePlatformsTemplateGenerator.Default;

        var package = new Package
        {
            FullPath = new UFile("/test/Game/Game.sdpkg")
        };
        var project = new SolutionProject(package, Guid.NewGuid(), "/test/Game/Game.csproj")
        {
            Platform = PlatformType.Shared
        };

        var gameSettings = new GameSettingsAsset();
        package.Assets.Add(new AssetItem(GameSettingsAsset.GameSettingsLocation, gameSettings));

        var parameters = new PackageTemplateGeneratorParameters
        {
            Package = package,
            OutputDirectory = new UDirectory("/test"),
            Unattended = false,
            Name = "TestGame",
            Description = new TemplateDescription { Scope = TemplateScope.Package, Id = UpdatePlatformsTemplateGenerator.TemplateId },
            Logger = new LoggerResult()
        };

        var prevDelegate = UpdatePlatformsTemplateGenerator.CreateUpdatePlatformsDialogDelegate;
        try
        {
            // 1. Simulate dialog returning Cancel -> PrepareForRun returns false
            UpdatePlatformsTemplateGenerator.CreateUpdatePlatformsDialogDelegate = (installed, forceVisible) =>
            {
                return new MockUpdatePlatformsDialog { Result = DialogResult.Cancel };
            };

            var resCancel = await generator.PrepareForRun(parameters);
            Assert.False(resCancel);

            // 2. Simulate dialog returning Ok -> PrepareForRun returns true
            var winPlatform = AssetRegistry.SupportedPlatforms.First(p => p.Type == PlatformType.Windows);
            UpdatePlatformsTemplateGenerator.CreateUpdatePlatformsDialogDelegate = (installed, forceVisible) =>
            {
                return new MockUpdatePlatformsDialog
                {
                    Result = DialogResult.Ok,
                    ForcePlatformRegeneration = true,
                    SelectedPlatforms = new[] { new SelectedSolutionPlatform(winPlatform, new SolutionPlatformTemplate("Windows/Template", "Windows Template")) }
                };
            };

            var resOk = await generator.PrepareForRun(parameters);
            Assert.True(resOk);
        }
        finally
        {
            UpdatePlatformsTemplateGenerator.CreateUpdatePlatformsDialogDelegate = prevDelegate;
        }
    }

    private sealed class MockUpdatePlatformsDialog : IUpdatePlatformsDialog
    {
        public IReadOnlyList<SolutionPlatformViewModel> AvailablePlatforms { get; set; } = new List<SolutionPlatformViewModel>();
        public bool ForcePlatformRegenerationVisible { get; set; }
        public bool ForcePlatformRegeneration { get; set; }
        public IEnumerable<SelectedSolutionPlatform> SelectedPlatforms { get; set; } = Enumerable.Empty<SelectedSolutionPlatform>();
        public object? DataContext { get; set; }
        public DialogResult Result { get; set; } = DialogResult.Ok;
        public Task<DialogResult> ShowModal() => Task.FromResult(Result);
        public void RequestClose(DialogResult result) => Result = result;
    }
}
