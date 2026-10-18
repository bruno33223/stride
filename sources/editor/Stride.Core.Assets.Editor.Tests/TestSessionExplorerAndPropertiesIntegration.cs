// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets.Navigation;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSessionExplorerAndPropertiesIntegration
{
    private static Assembly GetCoreAssetsEditorAssembly()
    {
        return typeof(PropertiesViewModel).Assembly;
    }

    private static Assembly GetAssetsPresentationAssembly()
    {
        return typeof(NavigationMeshViewModel).Assembly;
    }

    private sealed class MockSessionExplorerTreeView : ISessionExplorerTreeView
    {
        public List<object> ExpandedItems { get; } = [];
        public List<object> CollapsedItems { get; } = [];
        public List<object> BroughtIntoViewItems { get; } = [];
        public List<object> RootItems { get; } = [];

        public IEnumerable Items => RootItems;

        public void ExpandItem(object item) => ExpandedItems.Add(item);
        public void CollapseItem(object item) => CollapsedItems.Add(item);
        public void BringIntoView(object item) => BroughtIntoViewItems.Add(item);
    }

    [Fact]
    public void TestSessionExplorerHelperTypeAndCommands()
    {
        var asm = GetCoreAssetsEditorAssembly();
        var helperType = asm.GetType("Stride.Core.Assets.Editor.View.SessionExplorerHelper");
        Assert.True(helperType != null, "SessionExplorerHelper must exist in Stride.Core.Assets.Editor.View namespace.");

        var expandAssetFoldersCmd = SessionExplorerHelper.ExpandAssetFolders;
        Assert.NotNull(expandAssetFoldersCmd);
        Assert.Equal("ExpandAssetFolders", expandAssetFoldersCmd.Name);

        var expandAllFoldersCmd = SessionExplorerHelper.ExpandAllFolders;
        Assert.NotNull(expandAllFoldersCmd);
        Assert.Equal("ExpandAllFolders", expandAllFoldersCmd.Name);

        var collapseAllFoldersCmd = SessionExplorerHelper.CollapseAllFolders;
        Assert.NotNull(collapseAllFoldersCmd);
        Assert.Equal("CollapseAllFolders", collapseAllFoldersCmd.Name);

        // CanExecute on commands should return true even if parameter is null or a tree target
        Assert.True(expandAssetFoldersCmd.CanExecute(null));
        Assert.True(expandAllFoldersCmd.CanExecute(null));
        Assert.True(collapseAllFoldersCmd.CanExecute(null));

        // Commands execution should be safe and non-throwing with null
        expandAssetFoldersCmd.Execute(null);
        expandAllFoldersCmd.Execute(null);
        collapseAllFoldersCmd.Execute(null);

        // Test execution with ISessionExplorerTreeView
        var mockTree = new MockSessionExplorerTreeView();
        mockTree.RootItems.Add("RootFolder");

        collapseAllFoldersCmd.Execute(mockTree);
        Assert.Contains("RootFolder", mockTree.CollapsedItems);
    }

    [Fact]
    public void TestSessionObjectPropertiesViewModelTypeAndArchitecture()
    {
        var asm = GetCoreAssetsEditorAssembly();
        var vmType = asm.GetType("Stride.Core.Assets.Editor.Components.Properties.SessionObjectPropertiesViewModel");
        Assert.True(vmType != null, "SessionObjectPropertiesViewModel must exist in Stride.Core.Assets.Editor.Components.Properties.");

        Assert.True(typeof(PropertiesViewModel).IsAssignableFrom(vmType), "SessionObjectPropertiesViewModel must inherit from PropertiesViewModel.");

        // Check constructor
        var ctor = vmType.GetConstructor(new[] { typeof(SessionViewModel) });
        Assert.True(ctor != null, "SessionObjectPropertiesViewModel must have a constructor taking SessionViewModel.");

        // Check properties
        Assert.NotNull(vmType.GetProperty("Session"));
        Assert.NotNull(vmType.GetProperty("TypeDescription"));
        Assert.NotNull(vmType.GetProperty("Name"));
        Assert.NotNull(vmType.GetProperty("ShowOverridesOnly"));

        // Check methods
        Assert.NotNull(vmType.GetMethod("GetRelatedAssets"));

        // Instantiate uninitialized SessionObjectPropertiesViewModel to test UpdateTypeAndName behavior
        var instance = (SessionObjectPropertiesViewModel)RuntimeHelpers.GetUninitializedObject(vmType);

        // 1. Empty selection
        instance.UpdateTypeAndName(Array.Empty<string>(), x => x, x => x, "items");
        Assert.Equal("No selection", instance.TypeDescription);
        Assert.Equal(string.Empty, instance.Name);

        // 2. Single item selection
        instance.UpdateTypeAndName(new[] { "SpriteSheet" }, x => "Sprite", x => "PlayerSprite", "sprites");
        Assert.Equal("Sprite", instance.TypeDescription);
        Assert.Equal("PlayerSprite", instance.Name);

        // 3. Multi-selection of same type
        instance.UpdateTypeAndName(new[] { "Item1", "Item2" }, x => "Model", x => x, "models");
        Assert.Equal("Model", instance.TypeDescription);
        Assert.Equal("2 models selected", instance.Name);

        // 4. Multi-selection of different types
        instance.UpdateTypeAndName(new[] { "Item1", "Item2" }, x => x == "Item1" ? "Model" : "Texture", x => x, "assets");
        Assert.Equal("Multi-selection", instance.TypeDescription);
        Assert.Equal("2 assets selected", instance.Name);
    }

    [Fact]
    public void TestNewOrOpenSessionTemplateCollectionViewModelTypeAndProperties()
    {
        var asm = GetCoreAssetsEditorAssembly();
        var vmType = asm.GetType("Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.NewOrOpenSessionTemplateCollectionViewModel");
        Assert.True(vmType != null, "NewOrOpenSessionTemplateCollectionViewModel must exist in Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.");

        Assert.True(typeof(ProjectTemplateCollectionViewModel).IsAssignableFrom(vmType), "NewOrOpenSessionTemplateCollectionViewModel must inherit from ProjectTemplateCollectionViewModel.");

        // Check constructor
        var ctor = vmType.GetConstructor(new[] { typeof(IViewModelServiceProvider), typeof(IModalDialog) });
        Assert.True(ctor != null, "NewOrOpenSessionTemplateCollectionViewModel must have a (IViewModelServiceProvider, IModalDialog) constructor.");

        // Check properties
        Assert.NotNull(vmType.GetProperty("RootGroups"));
        Assert.NotNull(vmType.GetProperty("SolutionName"));
        Assert.NotNull(vmType.GetProperty("SolutionLocation"));
        Assert.NotNull(vmType.GetProperty("AutoReloadSession"));
        Assert.NotNull(vmType.GetProperty("ArePropertiesValid"));
        Assert.NotNull(vmType.GetProperty("BrowseForExistingProjectCommand"));

        // Instantiate uninitialized NewOrOpenSessionTemplateCollectionViewModel to test property manipulation & validation
        var instance = (NewOrOpenSessionTemplateCollectionViewModel)RuntimeHelpers.GetUninitializedObject(vmType);

        instance.SolutionName = "MyGame";
        Assert.Equal("MyGame", instance.SolutionName);

        instance.SolutionLocation = new UDirectory("/tmp/MyGameDir");
        Assert.Equal(new UDirectory("/tmp/MyGameDir"), instance.SolutionLocation);

        instance.ArePropertiesValid = true;
        Assert.True(instance.ArePropertiesValid);
    }

    [Fact]
    public void TestExistingProjectViewModelExploreAndRemove()
    {
        var sp = new ViewModelServiceProvider();
        var wasRemoved = false;
        var path = new UFile("/path/to/SampleSolution.sln");

        var vm = new ExistingProjectViewModel(sp, path, item =>
        {
            wasRemoved = true;
        });

        Assert.Equal("SampleSolution", vm.Name);
        Assert.Equal(path.ToOSPath(), vm.Description);
        Assert.Null(vm.Icon);
        Assert.Empty(vm.Screenshots);
        Assert.Null(vm.GetTemplate());

        // Test RemoveCommand
        Assert.NotNull(vm.RemoveCommand);
        vm.RemoveCommand.Execute();
        Assert.True(wasRemoved);

        // Test ExploreCommand (should not throw)
        Assert.NotNull(vm.ExploreCommand);
        vm.ExploreCommand.Execute();
    }

    [Fact]
    public void TestVisualStudioServiceCrossPlatform()
    {
        var asm = GetCoreAssetsEditorAssembly();
        var serviceType = asm.GetType("Stride.Core.Assets.Editor.Services.VisualStudioService");
        Assert.True(serviceType != null, "VisualStudioService must exist in Stride.Core.Assets.Editor.Services namespace.");

        var findMethod = serviceType.GetMethod("FindVisualStudioInstance", BindingFlags.Public | BindingFlags.Static, new[] { typeof(UFile) });
        Assert.True(findMethod != null, "FindVisualStudioInstance(UFile) must exist.");

        var getInstanceMethod = serviceType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static, new[] { typeof(SessionViewModel), typeof(bool) });
        Assert.True(getInstanceMethod != null, "GetInstance(SessionViewModel, bool) must exist.");

        // Running FindVisualStudioInstance on Linux must safely return null without throwing PlatformNotSupportedException or Win32 exceptions
        var dummyPath = new UFile("/path/to/solution.sln");
        var result = VisualStudioService.FindVisualStudioInstance(dummyPath);
        Assert.Null(result);

        var nullResult = VisualStudioService.FindVisualStudioInstance(null);
        Assert.Null(nullResult);
    }

    [Fact]
    public void TestNavigationViewModelTypeAndAttribute()
    {
        var asm = GetAssetsPresentationAssembly();
        var navType = asm.GetType("Stride.Assets.Presentation.ViewModels.NavigationViewModel")
            ?? asm.GetType("Stride.Assets.Presentation.ViewModel.NavigationViewModel");

        Assert.True(navType != null, "NavigationViewModel must exist in Stride.Assets.Presentation (ViewModels or ViewModel namespace).");

        Assert.True(typeof(AssetViewModel<NavigationMeshAsset>).IsAssignableFrom(navType), "NavigationViewModel must inherit from AssetViewModel<NavigationMeshAsset>.");

        // Verify [AssetViewModel<NavigationMeshAsset>] attribute
        var attr = navType.GetCustomAttributes(inherit: false)
            .FirstOrDefault(a => a.GetType().IsGenericType && a.GetType().GetGenericTypeDefinition() == typeof(AssetViewModelAttribute<>) && a.GetType().GenericTypeArguments[0] == typeof(NavigationMeshAsset));

        Assert.True(attr != null, "NavigationViewModel must be decorated with [AssetViewModel<NavigationMeshAsset>].");
    }
}
