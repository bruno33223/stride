// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestTemplateDescriptionsValidation
{
    private class ConcreteProjectTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
    {
        private readonly TemplateDescriptionGroupViewModel rootGroup;

        public ConcreteProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            rootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "Root");
        }

        public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => [rootGroup];

        protected override string UpdateNameFromSelectedTemplate()
        {
            return GenerateUniqueNameAtLocation();
        }

        public string PublicGenerateUniqueNameAtLocation(List<string>? conflictingNames = null)
        {
            return GenerateUniqueNameAtLocation(conflictingNames);
        }

        public static TemplateDescriptionGroupViewModel? PublicProcessGroup(TemplateDescriptionGroupViewModel rootGroup, string? groupPath)
        {
            return ProcessGroup(rootGroup, groupPath);
        }
    }

    private static IViewModelServiceProvider CreateServiceProvider()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        return sp;
    }

    [Fact]
    public void TestValidatePropertiesInvalidLocation()
    {
        var sp = CreateServiceProvider();
        var vm = new ConcreteProjectTemplateCollectionViewModel(sp);
        var template = new TemplateDescription { Name = "TestTemplate", DefaultOutputName = "MyProject" };
        vm.SelectedTemplate = new TemplateDescriptionViewModel(sp, template);
        vm.Name = "ValidName";
        vm.Location = "";

        bool isValid = vm.ValidateProperties(out var error);

        Assert.False(isValid);
        Assert.Equal("Invalid output directory.", error);
    }

    [Fact]
    public void TestValidatePropertiesInvalidName()
    {
        var sp = CreateServiceProvider();
        var vm = new ConcreteProjectTemplateCollectionViewModel(sp);
        var template = new TemplateDescription { Name = "TestTemplate", DefaultOutputName = "MyProject" };
        vm.SelectedTemplate = new TemplateDescriptionViewModel(sp, template);
        vm.Location = Path.GetTempPath();

        vm.Name = "Invalid/Name";
        Assert.False(vm.ValidateProperties(out var errorSlash));
        Assert.Equal("Invalid name.", errorSlash);

        vm.Name = "Invalid\\Name";
        Assert.False(vm.ValidateProperties(out var errorBackslash));
        Assert.Equal("Invalid name.", errorBackslash);
    }

    [Fact]
    public void TestValidatePropertiesFolderAlreadyExists()
    {
        var sp = CreateServiceProvider();
        var vm = new ConcreteProjectTemplateCollectionViewModel(sp);
        var template = new TemplateDescription { Name = "TestTemplate", DefaultOutputName = "MyProject" };
        vm.SelectedTemplate = new TemplateDescriptionViewModel(sp, template);

        var tempDir = Path.Combine(Path.GetTempPath(), "StrideTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var existingSubDir = Path.Combine(tempDir, "ExistingProject");
            Directory.CreateDirectory(existingSubDir);

            vm.Location = tempDir;
            vm.Name = "ExistingProject";

            bool isValid = vm.ValidateProperties(out var error);
            Assert.False(isValid);
            Assert.Equal("Cannot use the selected name because a folder with the same name already exists in the same location.", error);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TestValidatePropertiesSuccess()
    {
        var sp = CreateServiceProvider();
        var vm = new ConcreteProjectTemplateCollectionViewModel(sp);
        var template = new TemplateDescription { Name = "TestTemplate", DefaultOutputName = "MyProject" };
        vm.SelectedTemplate = new TemplateDescriptionViewModel(sp, template);

        var tempDir = Path.Combine(Path.GetTempPath(), "StrideTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            vm.Location = tempDir;
            vm.Name = "NonExistentProject";

            bool isValid = vm.ValidateProperties(out var error);
            Assert.True(isValid);
            Assert.Empty(error);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TestGenerateUniqueNameAtLocation()
    {
        var sp = CreateServiceProvider();
        var vm = new ConcreteProjectTemplateCollectionViewModel(sp);
        var template = new TemplateDescription { Name = "GameTemplate", DefaultOutputName = "MyGame" };
        vm.SelectedTemplate = new TemplateDescriptionViewModel(sp, template);

        var conflicting = new List<string> { "MyGame", "MyGame2" };
        var uniqueName = vm.PublicGenerateUniqueNameAtLocation(conflicting);

        Assert.Equal("MyGame3", uniqueName);
    }

    [Fact]
    public void TestProcessGroupHierarchy()
    {
        var sp = CreateServiceProvider();
        var root = new TemplateDescriptionGroupViewModel(sp, "Root");

        var leaf = ConcreteProjectTemplateCollectionViewModel.PublicProcessGroup(root, "Category/SubCategory");
        Assert.NotNull(leaf);
        Assert.Equal("SubCategory", leaf.Name);
        Assert.NotNull(leaf.Parent);
        Assert.Equal("Category", leaf.Parent.Name);
        Assert.Same(root, leaf.Parent.Parent);

        Assert.Null(ConcreteProjectTemplateCollectionViewModel.PublicProcessGroup(root, "   "));
        Assert.Null(ConcreteProjectTemplateCollectionViewModel.PublicProcessGroup(root, null));
    }
}
