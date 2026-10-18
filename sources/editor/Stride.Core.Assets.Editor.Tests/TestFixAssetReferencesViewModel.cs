// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.GameStudio.Avalonia.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestFixAssetReferencesViewModel
{
    private static IViewModelServiceProvider CreateServiceProvider()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        return sp;
    }

    [Fact]
    public void TestFixAssetReferenceItemViewModelDefaults()
    {
        var item = new FixAssetReferenceItemViewModel();
        Assert.Equal(string.Empty, item.SourceAssetName);
        Assert.Equal(string.Empty, item.OriginalPath);
        Assert.Equal(string.Empty, item.TargetReference);
        Assert.False(item.Ignore);
        Assert.Null(item.SourceAsset);
    }

    [Fact]
    public void TestFixAssetReferenceItemViewModelCustomValues()
    {
        var item = new FixAssetReferenceItemViewModel(null, "Textures/MissingTex", "Textures/NewTex", false)
        {
            SourceAssetName = "MyModelAsset"
        };

        Assert.Equal("MyModelAsset", item.SourceAssetName);
        Assert.Equal("Textures/MissingTex", item.OriginalPath);
        Assert.Equal("Textures/NewTex", item.TargetReference);
        Assert.False(item.Ignore);

        item.Ignore = true;
        Assert.True(item.Ignore);
    }

    [Fact]
    public void TestFixAssetReferencesViewModelOperations()
    {
        var sp = CreateServiceProvider();
        var vm = new FixAssetReferencesViewModel(sp);

        var item1 = new FixAssetReferenceItemViewModel(null, "Path/To/MissingA", "", false)
        {
            SourceAssetName = "Asset1"
        };
        var item2 = new FixAssetReferenceItemViewModel(null, "Path/To/MissingB", "", false)
        {
            SourceAssetName = "Asset2"
        };

        vm.AddItem(item1);
        vm.AddItem(item2);

        Assert.Equal(2, vm.Items.Count);

        // Test IgnoreAllCommand
        vm.IgnoreAllCommand.Execute(null);
        Assert.True(item1.Ignore);
        Assert.True(item2.Ignore);

        // Test ClearAllCommand
        vm.ClearAllCommand.Execute(null);
        Assert.False(item1.Ignore);
        Assert.False(item2.Ignore);
        Assert.Empty(item1.TargetReference);
        Assert.Empty(item2.TargetReference);

        // Test UseSingleReplace
        vm.UseSingleReplace = true;
        vm.SingleReplacement = "Path/To/ReplacementAsset";
        Assert.Equal("Path/To/ReplacementAsset", item1.TargetReference);
        Assert.Equal("Path/To/ReplacementAsset", item2.TargetReference);
    }

    [Fact]
    public void TestIEditorDialogServiceDefaults()
    {
        IEditorDialogService service = new MockEditorDialogService();
        // Verifies default interface methods do not throw
        service.ShowNotificationWindow("Test Title", "Test Message");
        service.CloseAllNotificationWindows();
    }

    private class MockEditorDialogService : IEditorDialogService
    {
        public bool HasMainWindow => false;

        public void Exit(int exitCode = 0) { }

        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons, MessageBoxImage image)
            => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));

        public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
            => Task.FromResult(new CheckedMessageBoxResult(MessageBoxResult.OK, isChecked));

        public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons, MessageBoxImage image)
            => Task.FromResult(MessageBoxResult.OK);

        public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
            => Task.FromResult(0);

        public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<UFile?>(null);

        public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
            => Task.FromResult<IReadOnlyList<UFile>>([]);

        public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null)
            => Task.FromResult<UDirectory?>(null);

        public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
            => Task.FromResult<UFile?>(null);

        public void ShowProgressWindow(WorkProgressViewModel workProgress) { }

        public INewProjectDialog CreateNewProjectDialog(SessionViewModel session) => null!;

        public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null) => null!;
    }
}
