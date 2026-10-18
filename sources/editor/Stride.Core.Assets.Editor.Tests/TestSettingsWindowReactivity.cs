// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Settings.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.IO;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Quantum;
using Stride.Core.Settings;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSettingsWindowReactivity
{
    private class MockDialogService : IDialogService
    {
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

        public bool HasMainWindow => true;

        public void Exit(int exitCode = 0) { }
    }

    private static Assembly? LoadAvaloniaAssembly()
    {
        var avaloniaAssemblyPath = Path.Combine(AppContext.BaseDirectory, "Stride.GameStudio.Avalonia.dll");
        if (!File.Exists(avaloniaAssemblyPath))
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !Directory.Exists(Path.Combine(dir, "sources")))
                dir = Path.GetDirectoryName(dir);
            if (dir != null)
                avaloniaAssemblyPath = Path.Combine(dir, "sources", "editor", "Stride.GameStudio.Avalonia", "bin", "Debug", "net10.0", "Stride.GameStudio.Avalonia.dll");
        }

        if (File.Exists(avaloniaAssemblyPath))
        {
            return Assembly.LoadFrom(avaloniaAssemblyPath);
        }

        return null;
    }

    [Fact]
    public void TestSettingsCategoryToExpandedAtInitialization_TypeExistsAndConverts()
    {
        var asm = LoadAvaloniaAssembly();
        Assert.True(asm != null, "Stride.GameStudio.Avalonia assembly must be present.");

        var converterType = asm.GetType("Stride.GameStudio.Avalonia.Converters.SettingsCategoryToExpandedAtInitialization");
        Assert.True(converterType != null, "Stride.GameStudio.Avalonia.Converters.SettingsCategoryToExpandedAtInitialization must exist.");

        var converterInstance = Activator.CreateInstance(converterType);
        Assert.NotNull(converterInstance);

        var convertMethod = converterType.GetMethod("Convert", [typeof(object), typeof(Type), typeof(object), typeof(CultureInfo)]);
        Assert.NotNull(convertMethod);

        // Root category (Parent == null) -> True
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        var container = new SettingsContainer();
        var profile = container.CurrentProfile;
        var nodeContainer = new AssetNodeContainer();

        var rootCat = new SettingsCategoryViewModel(sp, profile, "General", null, nodeContainer);
        var resultRoot = convertMethod.Invoke(converterInstance, [rootCat, typeof(bool), null, CultureInfo.InvariantCulture]);
        Assert.True(resultRoot is bool b && b, "Root category with Parent == null must convert to true (expanded).");

        // Subcategory (Parent != null) -> False
        var subCat = new SettingsCategoryViewModel(sp, profile, "SubGeneral", rootCat, nodeContainer);
        var resultSub = convertMethod.Invoke(converterInstance, [subCat, typeof(bool), null, CultureInfo.InvariantCulture]);
        Assert.True(resultSub is bool bSub && !bSub, "Subcategory with Parent != null must convert to false (collapsed).");

        // Null / other value -> False
        var resultNull = convertMethod.Invoke(converterInstance, [null, typeof(bool), null, CultureInfo.InvariantCulture]);
        Assert.True(resultNull is bool bNull && !bNull, "Null value must convert to false.");
    }

    [Fact]
    public void TestMainViewModel_CanonicalSettingsAndDebugCommands_Exist()
    {
        var asm = LoadAvaloniaAssembly();
        Assert.True(asm != null, "Stride.GameStudio.Avalonia assembly must be present.");

        var mainVmType = asm.GetType("Stride.GameStudio.Avalonia.ViewModels.MainViewModel");
        Assert.True(mainVmType != null, "MainViewModel must exist.");

        var openSettingsProp = mainVmType.GetProperty("OpenSettingsCommand");
        Assert.True(openSettingsProp != null, "MainViewModel must expose OpenSettingsCommand.");

        var openDebugProp = mainVmType.GetProperty("OpenDebugWindowCommand");
        Assert.True(openDebugProp != null, "MainViewModel must expose OpenDebugWindowCommand.");
    }

    [Fact]
    public void TestSettingsViewModel_ReactivityAndCommands()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new MockDialogService());

        var container = new SettingsContainer();
        var profile = container.CurrentProfile;

        var vm = new SettingsViewModel(sp, profile);

        Assert.NotNull(vm.Categories);
        Assert.NotNull(vm.ValidateChangesCommand);
        Assert.NotNull(vm.DiscardChangesCommand);

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        var nodeContainer = new AssetNodeContainer();
        var category = new SettingsCategoryViewModel(sp, profile, "Editor", null, nodeContainer);

        vm.SelectedCategory = category;

        Assert.Equal(category, vm.SelectedCategory);
        Assert.Contains(nameof(vm.SelectedCategory), changedProperties);
    }

    [Fact]
    public void TestWindowConstructors_AcceptServiceProvider()
    {
        var asm = LoadAvaloniaAssembly();
        Assert.True(asm != null, "Stride.GameStudio.Avalonia assembly must be present.");

        var settingsWinType = asm.GetType("Stride.GameStudio.Avalonia.Views.SettingsWindow");
        Assert.True(settingsWinType != null, "SettingsWindow must exist.");
        var settingsCtor = settingsWinType.GetConstructor([typeof(IViewModelServiceProvider)]);
        Assert.True(settingsCtor != null, "SettingsWindow must have a constructor taking IViewModelServiceProvider.");

        var debugWinType = asm.GetType("Stride.GameStudio.Avalonia.Views.DebugWindow");
        Assert.True(debugWinType != null, "DebugWindow must exist.");
        var debugCtor = debugWinType.GetConstructor([typeof(IViewModelServiceProvider)]);
        Assert.True(debugCtor != null, "DebugWindow must have a constructor taking IViewModelServiceProvider.");
    }
}
