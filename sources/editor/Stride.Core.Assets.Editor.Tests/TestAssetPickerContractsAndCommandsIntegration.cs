// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Engine;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetPickerContractsAndCommandsIntegration
{
    private class MockPackagePickerDialog : IPackagePickerDialog
    {
        public object? DataContext { get; set; }
        public bool AllowMultiSelection { get; set; }
        public Func<PickablePackageViewModel, bool>? Filter { get; set; }
        public List<PickablePackageViewModel> SelectedPackagesList { get; } = new();
        public IReadOnlyCollection<PickablePackageViewModel> SelectedPackages => SelectedPackagesList;

        public Task<DialogResult> ShowModal()
        {
            return Task.FromResult(DialogResult.Ok);
        }

        public void RequestClose(DialogResult result)
        {
        }
    }

    private class MockEditorDialogService : DialogService, IEditorDialogService
    {
        public MockEditorDialogService() : base(null!) { }

        public void ShowProgressWindow(WorkProgressViewModel workProgress) { }
        public INewProjectDialog CreateNewProjectDialog(SessionViewModel session) => throw new NotImplementedException();
        public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null) => throw new NotImplementedException();
    }

    private class CustomEditorDialogServiceWithPicker : MockEditorDialogService, IEditorDialogService
    {
        public IPackagePickerDialog MockPicker { get; } = new MockPackagePickerDialog();

        public IPackagePickerDialog? CreatePackagePickerDialog(SessionViewModel session)
        {
            return MockPicker;
        }
    }

    private class TestAddReferenceViewModelImpl : AddReferenceViewModel
    {
        public NodeViewModel? GetTargetNode() => TargetNode;

        public override bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            message = "OK";
            return children.Count > 0;
        }

        public override void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
        }
    }

    [Fact]
    public void TestPackagePickerDialogContract()
    {
        var dialog = new MockPackagePickerDialog();
        Assert.IsAssignableFrom<IModalDialog>(dialog);
        Assert.False(dialog.AllowMultiSelection);
        dialog.AllowMultiSelection = true;
        Assert.True(dialog.AllowMultiSelection);

        dialog.Filter = pkg => pkg.Name == "TestPackage";
        Assert.NotNull(dialog.Filter);
        Assert.Empty(dialog.SelectedPackages);

        dialog.DataContext = new object();
        Assert.NotNull(dialog.DataContext);
    }

    [Fact]
    public void TestEditorDialogServiceCreatePackagePickerDialog()
    {
        IEditorDialogService service = new MockEditorDialogService();
        var picker = service.CreatePackagePickerDialog(null!);
        Assert.Null(picker);

        var customServiceImpl = new CustomEditorDialogServiceWithPicker();
        IEditorDialogService customService = customServiceImpl;
        var customPicker = customService.CreatePackagePickerDialog(null!);
        Assert.NotNull(customPicker);
        Assert.Same(customServiceImpl.MockPicker, customPicker);
    }

    [Fact]
    public void TestContentReferenceHelperConstantsAndCreateReference()
    {
        Assert.Equal("(Broken reference)", ContentReferenceHelper.BrokenReference);
        Assert.Equal("(No asset selected)", ContentReferenceHelper.EmptyReference);

        var nullRef = ContentReferenceHelper.CreateReference<AssetReference>(null!);
        Assert.Null(nullRef);

        var nullRefObj = ContentReferenceHelper.CreateReference(null!, typeof(AssetReference));
        Assert.Null(nullRefObj);

        var target = ContentReferenceHelper.GetReferenceTarget(null!, new object());
        Assert.Null(target);
    }

    [Fact]
    public void TestReferenceDataKey()
    {
        Assert.Equal("AddReferenceViewModel", ReferenceData.AddReferenceViewModel);
        Assert.NotNull(ReferenceData.Key);
        Assert.Equal("AddReferenceViewModel", ReferenceData.Key.Name);
    }

    [Fact]
    public void TestAddReferenceViewModelBase()
    {
        var vm = new TestAddReferenceViewModelImpl();
        Assert.IsAssignableFrom<Stride.Core.Assets.Editor.ViewModels.IAddReferenceViewModel>(vm);
        Assert.IsAssignableFrom<Stride.Core.Assets.Presentation.ViewModels.IAddReferenceViewModel>(vm);

        var canAdd = vm.CanAddChildren(Array.Empty<object>(), AddChildModifiers.None, out var message);
        Assert.False(canAdd);

        canAdd = vm.CanAddChildren(new object[] { "child" }, AddChildModifiers.None, out message);
        Assert.True(canAdd);
        Assert.Equal("OK", message);
    }

    [Fact]
    public void TestContentReferenceViewModelCanAddChildrenWithInvalidSelection()
    {
        var vm = new ContentReferenceViewModel();
        Assert.IsAssignableFrom<AddReferenceViewModel>(vm);

        var canAdd = vm.CanAddChildren(Array.Empty<object>(), AddChildModifiers.None, out var message);
        Assert.False(canAdd);
        Assert.Equal("The selection is not an asset", message);

        canAdd = vm.CanAddChildren(new object[] { "not an asset" }, AddChildModifiers.None, out message);
        Assert.False(canAdd);
        Assert.Equal("The selection is not an asset", message);

    }

    [Fact]
    public void TestEntityReferenceViewModelContracts()
    {
        var vm = new EntityReferenceViewModel();
        Assert.IsAssignableFrom<AddReferenceViewModel>(vm);

        var canAdd = vm.CanAddChildren(Array.Empty<object>(), AddChildModifiers.None, out var message);
        Assert.False(canAdd);
        Assert.Equal("The selection is not an entity", message);

        canAdd = vm.CanAddChildren(new object[] { "not an entity" }, AddChildModifiers.None, out message);
        Assert.False(canAdd);
        Assert.Equal("The selection is not an entity", message);

        var entity1 = (EntityViewModel)RuntimeHelpers.GetUninitializedObject(typeof(EntityViewModel));
        var entity2 = (EntityViewModel)RuntimeHelpers.GetUninitializedObject(typeof(EntityViewModel));
        canAdd = vm.CanAddChildren(new object[] { entity1, entity2 }, AddChildModifiers.None, out message);
        Assert.False(canAdd);
        Assert.Equal("Multiple entities selected", message);
    }

    [Fact]
    public void TestComponentReferenceViewModelContracts()
    {
        var vm = new ComponentReferenceViewModel();
        Assert.IsAssignableFrom<AddReferenceViewModel>(vm);

        var canAdd = vm.CanAddChildren(Array.Empty<object>(), AddChildModifiers.None, out var message);
        Assert.False(canAdd);
        Assert.Equal("The selection is not an entity", message);

        canAdd = vm.CanAddChildren(new object[] { "not an entity" }, AddChildModifiers.None, out message);
        Assert.False(canAdd);
        Assert.Equal("The selection is not an entity", message);

    }

    [Fact]
    public void TestSetEntityReferenceCommand()
    {
        var cmd = new SetEntityReferenceCommand();
        Assert.Equal("SetEntityReference", SetEntityReferenceCommand.CommandName);
        Assert.Equal(SetEntityReferenceCommand.CommandName, cmd.Name);
    }

    [Fact]
    public void TestSetComponentReferenceCommand()
    {
        var cmd = new SetComponentReferenceCommand();
        Assert.Equal("SetComponentReference", SetComponentReferenceCommand.CommandName);
        Assert.Equal(SetComponentReferenceCommand.CommandName, cmd.Name);

        var param = new SetComponentReferenceCommand.Parameter
        {
            Entity = null!,
            Index = 42
        };
        Assert.Equal(42, param.Index);
    }
}
