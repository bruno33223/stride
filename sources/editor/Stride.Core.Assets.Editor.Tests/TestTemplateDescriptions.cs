// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestTemplateDescriptions
    {
        private static IViewModelServiceProvider CreateServiceProvider()
        {
            var sp = new ViewModelServiceProvider();
            sp.RegisterService(new NullDispatcherService());
            return sp;
        }

        [Fact]
        public void TestTemplateDescriptionGroupHierarchy()
        {
            var sp = CreateServiceProvider();
            var root = new TemplateDescriptionGroupViewModel(sp, "Root");
            var childA = new TemplateDescriptionGroupViewModel(root, "ChildA");
            var childB = new TemplateDescriptionGroupViewModel(root, "ChildB");
            var grandChild = new TemplateDescriptionGroupViewModel(childA, "GrandChild");

            Assert.True(root.IsRoot);
            Assert.False(childA.IsRoot);
            Assert.Equal(2, root.SubGroups.Count);
            Assert.Equal(1, childA.SubGroups.Count);
            Assert.Equal("ChildA/GrandChild/", grandChild.Path);
        }

        [Fact]
        public void TestNewPackageParameters()
        {
            var parameters = new NewPackageParameters
            {
                OutputName = "TestGame",
                OutputDirectory = "/tmp/StrideProjects"
            };

            Assert.Equal("TestGame", parameters.OutputName);
            Assert.Equal("/tmp/StrideProjects", parameters.OutputDirectory);
            Assert.Null(parameters.TemplateDescription);
        }

        [Fact]
        public void TestTemplateDescriptionViewModel()
        {
            var sp = CreateServiceProvider();
            var template = new TemplateDescription
            {
                Id = Guid.NewGuid(),
                Name = "Template Test",
                Description = "Short description",
                FullDescription = "Detailed full description",
                DefaultOutputName = "MyProject",
                Group = "Starters"
            };

            var vm = new TemplateDescriptionViewModel(sp, template);
            Assert.Equal("Template Test", vm.Name);
            Assert.Equal("Short description", vm.Description);
            Assert.Equal("Detailed full description", vm.FullDescription);
            Assert.Equal("MyProject", vm.DefaultOutputName);
            Assert.Equal("Starters", vm.Group);
            Assert.Same(template, vm.GetTemplate());
        }

        [Fact]
        public void TestDialogResultEnumValues()
        {
            Assert.Equal(0, (int)DialogResult.None);
            Assert.Equal(1, (int)DialogResult.Ok);
            Assert.Equal(2, (int)DialogResult.Cancel);
        }
    }
}
