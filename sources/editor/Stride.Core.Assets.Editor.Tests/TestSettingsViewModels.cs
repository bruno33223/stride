// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Assets;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;
using Stride.Core.Settings;
using EditorSettings = Stride.Core.Assets.Editor.Settings.EditorSettings;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSettingsViewModels
{
    [Fact]
    public void TestSettingsData_FullWpfParity()
    {
        var asm = typeof(EditorSettings).Assembly;
        var settingsDataType = asm.GetType("Stride.Core.Assets.Editor.Settings.ViewModels.SettingsData");
        Assert.True(settingsDataType != null, "Stride.Core.Assets.Editor.Settings.ViewModels.SettingsData must exist in Stride.Core.Assets.Editor.");

        var hasAcceptableValuesField = settingsDataType.GetField("HasAcceptableValues", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(hasAcceptableValuesField);
        Assert.Equal("HasAcceptableValues", hasAcceptableValuesField.GetValue(null));

        var acceptableValuesField = settingsDataType.GetField("AcceptableValues", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(acceptableValuesField);
        Assert.Equal("AcceptableValues", acceptableValuesField.GetValue(null));

        var hasAcceptableValuesKeyProp = settingsDataType.GetField("HasAcceptableValuesKey", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(hasAcceptableValuesKeyProp);

        var acceptableValuesKeyProp = settingsDataType.GetField("AcceptableValuesKey", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(acceptableValuesKeyProp);
    }

    [Fact]
    public void TestSettingsViewModel_CategoryHierarchyAndCommandsIntegration()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        var container = new SettingsContainer();
        var profile = container.CurrentProfile;

        // Ensure EditorSettings has commands registered
        EditorSettings.Initialize();

        var asm = typeof(EditorSettings).Assembly;
        var settingsVmType = asm.GetType("Stride.Core.Assets.Editor.Settings.ViewModels.SettingsViewModel");
        Assert.True(settingsVmType != null, "Stride.Core.Assets.Editor.Settings.ViewModels.SettingsViewModel must exist in Stride.Core.Assets.Editor.");

        var vm = Activator.CreateInstance(settingsVmType, sp, profile);
        Assert.NotNull(vm);

        var categoriesProp = settingsVmType.GetProperty("Categories");
        Assert.NotNull(categoriesProp);
        var categories = categoriesProp.GetValue(vm) as System.Collections.IEnumerable;
        Assert.NotNull(categories);

        // Check root categories include Interface
        object? interfaceCategory = null;
        foreach (var cat in categories)
        {
            var nameProp = cat.GetType().GetProperty("Name");
            if (nameProp?.GetValue(cat)?.ToString() == "Interface")
            {
                interfaceCategory = cat;
                break;
            }
        }
        Assert.NotNull(interfaceCategory);

        var pathProp = interfaceCategory.GetType().GetProperty("Path");
        Assert.Equal("Interface/", pathProp?.GetValue(interfaceCategory)?.ToString());

        // Verify SettingsCategoryViewModel is an IPropertyProviderViewModel
        var propProvider = interfaceCategory as IPropertyProviderViewModel;
        Assert.NotNull(propProvider);
        Assert.True(propProvider.CanProvidePropertiesViewModel);
        var rootNode = propProvider.GetRootNode();
        Assert.NotNull(rootNode);

        // In the original WPF implementation, ResetEditorLayout from EditorSettings.GetAllCommands()
        // was included in the SettingsList of the Interface category!
        var settingsListProp = interfaceCategory.GetType().GetProperty("SettingsList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(settingsListProp);
        var settingsList = settingsListProp.GetValue(interfaceCategory) as System.Collections.IDictionary;
        Assert.NotNull(settingsList);

        var commands = EditorSettings.GetAllCommands()?.ToList() ?? [];
        if (commands.Any(cmd => cmd.DisplayName.ToString().StartsWith("Interface/")))
        {
            bool containsCommand = false;
            foreach (System.Collections.DictionaryEntry entry in settingsList)
            {
                if (entry.Value is SettingsCommand || entry.Value?.GetType().Name == "SettingsCommand")
                {
                    containsCommand = true;
                    break;
                }
            }
            Assert.True(containsCommand, "SettingsCategoryViewModel must include SettingsCommand items from EditorSettings.GetAllCommands().");
        }
    }

    [Fact]
    public void TestGameSettingsViewModel_FullWpfParity()
    {
        var gameSettingsVmType = typeof(Stride.Assets.Presentation.ViewModels.GameSettingsViewModel);
        Assert.NotNull(gameSettingsVmType);

        // 1. AvailableFilters constant
        var availableFiltersField = gameSettingsVmType.GetField("AvailableFilters", BindingFlags.Public | BindingFlags.Static);
        Assert.True(availableFiltersField != null, "GameSettingsViewModel must define AvailableFilters constant.");
        Assert.Equal("AvailableFilters", availableFiltersField.GetValue(null));

        // 2. DefaultScene property
        var defaultSceneProp = gameSettingsVmType.GetProperty("DefaultScene", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(defaultSceneProp != null, "GameSettingsViewModel must define DefaultScene property.");
        Assert.True(defaultSceneProp.CanRead, "DefaultScene must have a getter.");
        Assert.True(defaultSceneProp.CanWrite, "DefaultScene must have a setter.");

        // 3. IsLocked property
        var isLockedProp = gameSettingsVmType.GetProperty("IsLocked", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(isLockedProp != null, "GameSettingsViewModel must define IsLocked property.");
        Assert.True(isLockedProp.CanRead, "IsLocked must have a getter.");

        // 4. OnSessionSaved method
        var onSessionSavedMethod = gameSettingsVmType.GetMethod("OnSessionSaved", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.True(onSessionSavedMethod != null, "GameSettingsViewModel must define OnSessionSaved method.");
    }

    [Fact]
    public void TestUIEditorSettings_And_ScriptEditorSettings_Exist()
    {
        var asm = typeof(Stride.Assets.Editor.StrideEditorPlugin).Assembly;

        // UIEditorSettings
        var uiSettingsType = asm.GetType("Stride.Assets.Presentation.AssetEditors.UIEditor.UIEditorSettings")
                          ?? asm.GetType("Stride.Assets.Editor.AssetEditors.UIEditor.UIEditorSettings");
        Assert.True(uiSettingsType != null, "UIEditorSettings must exist in Stride.Assets.Editor.");

        var askDeleteField = uiSettingsType.GetProperty("AskBeforeDeletingUIElements", BindingFlags.Public | BindingFlags.Static);
        Assert.True(askDeleteField != null, "UIEditorSettings.AskBeforeDeletingUIElements must exist.");

        var saveUiMethod = uiSettingsType.GetMethod("Save", BindingFlags.Public | BindingFlags.Static);
        Assert.True(saveUiMethod != null, "UIEditorSettings.Save() must exist.");

        // ScriptEditorSettings
        var scriptSettingsType = asm.GetType("Stride.Assets.Presentation.AssetEditors.ScriptEditor.ScriptEditorSettings")
                              ?? asm.GetType("Stride.Assets.Editor.AssetEditors.ScriptEditor.ScriptEditorSettings");
        Assert.True(scriptSettingsType != null, "ScriptEditorSettings must exist in Stride.Assets.Editor.");

        var fontSizeProp = scriptSettingsType.GetProperty("FontSize", BindingFlags.Public | BindingFlags.Static);
        Assert.True(fontSizeProp != null, "ScriptEditorSettings.FontSize must exist.");

        var saveScriptMethod = scriptSettingsType.GetMethod("Save", BindingFlags.Public | BindingFlags.Static);
        Assert.True(saveScriptMethod != null, "ScriptEditorSettings.Save() must exist.");
    }

    [Fact]
    public void TestGameSettingsProviderService_InAssetsEditor()
    {
        var asm = typeof(Stride.Assets.Editor.StrideEditorPlugin).Assembly;
        var providerType = asm.GetType("Stride.Assets.Editor.Build.GameSettingsProviderService")
                        ?? asm.GetType("Stride.Editor.Build.GameSettingsProviderService");
        Assert.True(providerType != null, "GameSettingsProviderService must exist in Stride.Assets.Editor or Stride.Editor.");

        var gameSettingsProp = providerType.GetProperty("CurrentGameSettings", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(gameSettingsProp);

        var getConfigMethod = providerType.GetMethod("GetConfiguration");
        Assert.NotNull(getConfigMethod);
    }

    [Fact]
    public void TestGameSettingsAssetNodeUpdater_InAssetsPresentation()
    {
        var presAsm = typeof(Stride.Assets.Presentation.ViewModels.GameSettingsViewModel).Assembly;
        var editorAsm = typeof(Stride.Assets.Editor.StrideEditorPlugin).Assembly;

        var updaterType = presAsm.GetType("Stride.Assets.Presentation.NodePresenters.Updaters.GameSettingsAssetNodeUpdater")
                       ?? editorAsm.GetType("Stride.Assets.Editor.Quantum.NodePresenters.Updaters.GameSettingsAssetNodeUpdater");
        Assert.True(updaterType != null, "GameSettingsAssetNodeUpdater must exist in Stride.Assets.Presentation or Stride.Assets.Editor.");
    }
}
