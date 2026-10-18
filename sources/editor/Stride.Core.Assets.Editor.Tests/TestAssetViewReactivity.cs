// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Xunit;

using DirectoryBaseViewModel = Stride.Core.Assets.Editor.ViewModel.DirectoryBaseViewModel;
using DirectoryViewModel = Stride.Core.Assets.Editor.ViewModel.DirectoryViewModel;
using MountPointViewModel = Stride.Core.Assets.Editor.ViewModel.MountPointViewModel;
using AssetMountPointViewModel = Stride.Core.Assets.Editor.ViewModel.AssetMountPointViewModel;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetViewReactivity
{
    private class TestSession : ViewModelBase, ISessionViewModel
    {
        public TestSession() : base(CreateServiceProvider()) { }

        private static IViewModelServiceProvider CreateServiceProvider()
        {
            var sp = new ViewModelServiceProvider();
            var dispatcher = new NullDispatcherService();
            sp.RegisterService(dispatcher);
            sp.RegisterService(new UndoRedoService(100));
            return sp;
        }

        public IDispatcherService Dispatcher { get; } = new NullDispatcherService();
        public IEnumerable<AssetViewModel> AllAssets => Enumerable.Empty<AssetViewModel>();
        public IEnumerable<PackageViewModel> AllPackages => Enumerable.Empty<PackageViewModel>();
        public IObservableCollection<PackageViewModel> LocalPackages { get; } = new ObservableList<PackageViewModel>();
        public IObservableCollection<PackageViewModel> StorePackages { get; } = new ObservableList<PackageViewModel>();
        public IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories { get; } = new Dictionary<string, PackageCategoryViewModel>();
        public AssetNodeContainer AssetNodeContainer => throw new NotImplementedException();
        public ProjectViewModel? CurrentProject => null;
        public IAssetDependencyManager DependencyManager => throw new NotImplementedException();
        public AssetPropertyGraphContainer GraphContainer => throw new NotImplementedException();
        public IAssetSourceTrackerViewModel SourceTracker => throw new NotImplementedException();

        public event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;
        public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;

        public AssetViewModel? GetAssetById(AssetId id) => null;
        public Type GetAssetViewModelType(AssetItem assetItem) => typeof(AssetViewModel);
        public void RegisterAsset(AssetViewModel asset) { }
        public void UnregisterAsset(AssetViewModel asset) { }
        public void CheckConsistency() { }
    }

    private class TestPackageViewModel : PackageViewModel
    {
        public TestPackageViewModel(ISessionViewModel session)
            : base(session, new StandalonePackage(new Package()), false)
        {
        }

        public override bool IsEditable => true;
    }

    private class TestMountPoint : MountPointViewModel
    {
        public TestMountPoint(PackageViewModel package) : base(package) { }
        public override string Name { get => "TestRoot"; set { } }
        public override bool IsEditable => true;
        public override bool CanDelete(out string error) { error = ""; return true; }
        public override bool AcceptAssetType(Type assetType) => true;
    }

    private class MockAssetPickerDialog : IAssetPickerDialog
    {
        public string Message { get; set; } = string.Empty;
        public Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel? InitialLocation { get; set; }
        public AssetViewModel? InitialAsset { get; set; }
        public bool AllowMultiSelection { get; set; }
        public List<Type> AcceptedTypes { get; } = new List<Type>();
        public Func<AssetViewModel, bool>? Filter { get; set; }
        public IReadOnlyCollection<AssetViewModel> SelectedAssets { get; set; } = Array.Empty<AssetViewModel>();
        public object? DataContext { get; set; }
        public void RequestClose(DialogResult result) { }
        public Task<DialogResult> ShowModal() => Task.FromResult(DialogResult.Ok);
    }

    [Fact]
    public void TestDirectoryHierarchyAndPath()
    {
        var session = new TestSession();
        var package = new TestPackageViewModel(session);
        var root = new TestMountPoint(package);

        Assert.Equal(string.Empty, root.Path);
        Assert.Equal(0, root.Level);

        var subDir = new DirectoryViewModel("FolderA", root, false);
        Assert.Single(root.SubDirectories);
        Assert.Equal(root, subDir.Parent);
        Assert.Equal("FolderA/", subDir.Path);
        Assert.Equal(1, subDir.Level);

        var childDir = new DirectoryViewModel("FolderB", subDir, false);
        Assert.Single(subDir.SubDirectories);
        Assert.Equal("FolderA/FolderB/", childDir.Path);
        Assert.Equal(2, childDir.Level);
        Assert.Equal(root, childDir.Root);
    }

    [Fact]
    public void TestDirectoryNameValidation()
    {
        var session = new TestSession();
        var package = new TestPackageViewModel(session);
        var root = new TestMountPoint(package);

        var subDir = new DirectoryViewModel("ValidFolder", root, false);
        Assert.NotNull(subDir);

        // Nomes duplicados devem lançar ArgumentException
        Assert.Throws<ArgumentException>(() => new DirectoryViewModel("ValidFolder", root, false));

        // Nomes com caracteres inválidos de dispositivo/sistema
        Assert.False(DirectoryViewModel.IsPathAcceptedByFilesystem(@"\\.\pipe\test", out _));
    }

    [Fact]
    public void TestDirectoryCreationAndRemoval()
    {
        var session = new TestSession();
        var package = new TestPackageViewModel(session);
        var root = new TestMountPoint(package);

        var created = root.CreateSubDirectory(false);
        Assert.NotNull(created);
        Assert.Contains(created, root.SubDirectories);

        Assert.True(created.CanDelete(out var error));
        Assert.Empty(error);

        created.Delete();
        Assert.DoesNotContain(created, root.SubDirectories);
    }

    [Fact]
    public void TestDirectoryGetOrCreateDirectory()
    {
        var session = new TestSession();
        var package = new TestPackageViewModel(session);
        var root = new TestMountPoint(package);

        var dir = root.GetOrCreateDirectory("Level1/Level2", false);
        Assert.NotNull(dir);
        Assert.Equal("Level1/Level2/", dir.Path);

        var fetched = root.GetDirectory("Level1/Level2");
        Assert.Same(dir, fetched);

        var notFound = root.GetDirectory("NonExistent");
        Assert.Null(notFound);
    }

    [Fact]
    public void TestAssetPickerDialogInterfaceContract()
    {
        var dialog = new MockAssetPickerDialog
        {
            Message = "Select Model",
            AllowMultiSelection = true
        };

        dialog.AcceptedTypes.Add(typeof(string));

        Assert.Equal("Select Model", dialog.Message);
        Assert.True(dialog.AllowMultiSelection);
        Assert.Single(dialog.AcceptedTypes);
        Assert.Empty(dialog.SelectedAssets);
    }

    [Fact]
    public void TestAssetMountPointContract()
    {
        var session = new TestSession();
        var package = new TestPackageViewModel(session);
        var assetMountPoint = new AssetMountPointViewModel(package);

        Assert.Equal("Assets", assetMountPoint.Name);
        Assert.False(assetMountPoint.IsEditable);
        Assert.False(assetMountPoint.CanDelete(out var error));
        Assert.Equal("Unable to delete the asset root folder.", error);
        Assert.Throws<InvalidOperationException>(() => assetMountPoint.Name = "Renamed");
        Assert.Throws<NotSupportedException>(() => assetMountPoint.Delete());
    }
}
