// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Themes;
using Stride.Core.Settings;
using PackageSettingsWrapper = Stride.Core.Assets.Editor.Components.Properties.PackageSettingsWrapper;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestSettingsCore
    {
        [Fact]
        public void TestEditorSettingsFullWpfParity()
        {
            // Verify Categories
            Assert.False(string.IsNullOrEmpty(EditorSettings.Environment));
            Assert.False(string.IsNullOrEmpty(EditorSettings.ExternalTools));
            Assert.False(string.IsNullOrEmpty(EditorSettings.Interface));
            Assert.False(string.IsNullOrEmpty(EditorSettings.Tools));

            // Verify Key Default Values and Properties
            Assert.NotNull(EditorSettings.DefaultTextEditor);
            Assert.Equal(new UFile(@"%windir%\system32\notepad.exe"), EditorSettings.DefaultTextEditor.DefaultValue);

            Assert.NotNull(EditorSettings.ShaderEditor);
            Assert.Equal(new UFile(@"%windir%\system32\notepad.exe"), EditorSettings.ShaderEditor.DefaultValue);

            Assert.NotNull(EditorSettings.DefaultIDE);
            Assert.False(string.IsNullOrEmpty(EditorSettings.DefaultIDE.DefaultValue));
            Assert.NotNull(EditorSettings.DefaultIDE.GetAcceptableValues);
            Assert.Contains(EditorSettings.DefaultIDE.DefaultValue, EditorSettings.DefaultIDE.GetAcceptableValues());

            Assert.True(EditorSettings.AskBeforeDeletingAssets.DefaultValue);
            Assert.True(EditorSettings.AskBeforeReloadingAssemblies.DefaultValue);
            Assert.True(EditorSettings.AutoReloadAssemblies.DefaultValue);
            Assert.True(EditorSettings.AskBeforeSavingNewScripts.DefaultValue);
            Assert.True(EditorSettings.EnableMetrics.DefaultValue);
            Assert.Equal(SupportedLanguage.MachineDefault, EditorSettings.Language.DefaultValue);

            Assert.False(EditorSettings.UseEffectCompilerServer.DefaultValue);
            Assert.False(EditorSettings.ReloadLastSession.DefaultValue);

            Assert.NotNull(EditorSettings.FallbackBuildCacheDirectory);
            Assert.Contains("BuildCache", EditorSettings.FallbackBuildCacheDirectory.FullPath);

            // Verify Commands
            Assert.NotNull(EditorSettings.ResetEditorLayout);
            Assert.Equal("Reset", EditorSettings.ResetEditorLayout.ActionName);
            var commands = EditorSettings.GetAllCommands().ToList();
            Assert.Contains(EditorSettings.ResetEditorLayout, commands);
        }

        [Fact]
        public void TestEditorSettingsNeedRestartTriggers()
        {
            EditorSettings.Initialize();
            var profile = EditorSettings.SettingsContainer.CurrentProfile;
            Assert.NotNull(profile);

            // Test that registered restart settings flip NeedRestart to true on changes validation
            EditorSettings.NeedRestart = false;
            EditorSettings.UseEffectCompilerServer.SetValue(false, profile);
            EditorSettings.UseEffectCompilerServer.SetValue(true, profile);
            profile.ValidateSettingsChanges();
            Assert.True(EditorSettings.NeedRestart);

            EditorSettings.NeedRestart = false;
            EditorSettings.Language.SetValue(SupportedLanguage.English, profile);
            EditorSettings.Language.SetValue(SupportedLanguage.French, profile);
            profile.ValidateSettingsChanges();
            Assert.True(EditorSettings.NeedRestart);

            EditorSettings.NeedRestart = false;
            EditorSettings.EnableMetrics.SetValue(true, profile);
            EditorSettings.EnableMetrics.SetValue(false, profile);
            profile.ValidateSettingsChanges();
            Assert.True(EditorSettings.NeedRestart);

            EditorSettings.NeedRestart = false;
            ThemesSettings.ThemeName.SetValue(ThemeType.ExpressionDark, profile);
            ThemesSettings.ThemeName.SetValue(ThemeType.Divided, profile);
            profile.ValidateSettingsChanges();
            Assert.True(EditorSettings.NeedRestart);
        }

        [Fact]
        public void TestInternalSettingsFullWpfParity()
        {
            Assert.NotNull(InternalSettings.SettingsContainer);
            Assert.NotNull(InternalSettings.MostRecentlyUsedSessions);
            Assert.NotNull(InternalSettings.LoadingStartupSession);
            Assert.NotNull(InternalSettings.FileDialogLastImportDirectory);
            Assert.NotNull(InternalSettings.FileDialogLastOpenSessionDirectory);
            Assert.NotNull(InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory);
            Assert.NotNull(InternalSettings.AssetViewSortRule);
            Assert.NotNull(InternalSettings.AssetViewDisplayMode);
            Assert.NotNull(InternalSettings.AssetViewTileThumbnailZoom);
            Assert.NotNull(InternalSettings.AssetViewGridThumbnailZoom);
            Assert.NotNull(InternalSettings.ViewFilters);

            Assert.Equal(SortRule.TypeOrderThenName, InternalSettings.AssetViewSortRule.DefaultValue);
            Assert.Equal(DisplayAssetMode.AssetAndFolderInSelectedFolder, InternalSettings.AssetViewDisplayMode.DefaultValue);
            Assert.Equal(96.0, InternalSettings.AssetViewTileThumbnailZoom.DefaultValue);
            Assert.Equal(16.0, InternalSettings.AssetViewGridThumbnailZoom.DefaultValue);

            // Test Profile Copy and Save methods
            var profileCopy = InternalSettings.LoadProfileCopy();
            Assert.NotNull(profileCopy);

            // Verify MRUDictionary usage
            var mru = InternalSettings.MostRecentlyUsedSessions.GetValue(profileCopy, true);
            Assert.NotNull(mru);

            // Verify WriteFile and Save do not crash
            InternalSettings.Save();
            InternalSettings.WriteFile();
            InternalSettings.SaveProfile();
        }

        [Fact]
        public void TestThemesSettingsPresenceAndInitialization()
        {
            Assert.Equal("Themes", ThemesSettings.Themes);
            Assert.NotNull(ThemesSettings.ThemeName);
            Assert.Equal(ThemeType.ExpressionDark, ThemesSettings.ThemeName.DefaultValue);

            ThemesSettings.Initialize();
            Assert.Equal(ThemesSettings.ThemeName.GetValue(), ThemeController.CurrentTheme);
        }

        [Fact]
        public void TestDebugTestSettingsPresence()
        {
            DebugTestSettings.Initialize();
            Assert.NotNull(DebugTestSettings.Container);
            Assert.True(DebugTestSettings.BoolValue.DefaultValue);
            Assert.Equal(10, DebugTestSettings.IntValue.DefaultValue);
            Assert.Equal(3.14, DebugTestSettings.DoubleValue.DefaultValue);
            Assert.Equal("Test string", DebugTestSettings.StringValue.DefaultValue);
            Assert.Equal(2, DebugTestSettings.IntList.DefaultValue.Count);
            Assert.Equal(2, DebugTestSettings.DoubleList.DefaultValue.Count);
            Assert.Equal(2, DebugTestSettings.StringList.DefaultValue.Count);
            Assert.Single(DebugTestSettings.StructList.DefaultValue);

            var structVal = DebugTestSettings.StuctValue.DefaultValue;
            Assert.True(structVal.Bool);
            Assert.Equal(6, structVal.Integer);
            Assert.Equal(3.14, structVal.Double);
            Assert.Equal("Struct!", structVal.String);
        }

        [Fact]
        public void TestSettingsCommandModel()
        {
            var cmd = new SettingsCommand(new UFile("Interface/TestReset"))
            {
                ActionName = "ResetAll",
                DisplayName = new UFile("Interface/Display"),
                Description = "Command Description"
            };

            Assert.Equal("Interface/TestReset", cmd.Name.FullPath);
            Assert.Equal("ResetAll", cmd.ActionName);
            Assert.Equal("Interface/Display", cmd.DisplayName.FullPath);
            Assert.Equal("Command Description", cmd.Description);
            Assert.Null(cmd.Command);
        }

        [Fact]
        public void TestPackageSettingsWrapperAndEntry()
        {
            EditorSettings.Initialize();
            var profile = EditorSettings.SettingsContainer.CurrentProfile;
            Assert.NotNull(profile);

            // Test SettingsKeyWrapper typed create
            var key = EditorSettings.AskBeforeDeletingAssets;
            var wrapper = PackageSettingsWrapper.SettingsKeyWrapper.Create(key, profile) as PackageSettingsWrapper.SettingsKeyWrapper<bool>;
            Assert.NotNull(wrapper);
            Assert.Same(key, wrapper.Key);
            Assert.True(wrapper.TypedValue);

            // Set new value
            wrapper.TypedValue = false;
            Assert.False(wrapper.TypedValue);
            Assert.False(key.GetValue(profile, true));

            // Test Package dirtying
            var package = new Core.Assets.Package();
            package.IsDirty = false;
            var wrapperWithPackage = PackageSettingsWrapper.SettingsKeyWrapper.Create(key, profile, package) as PackageSettingsWrapper.SettingsKeyWrapper<bool>;
            Assert.NotNull(wrapperWithPackage);
            wrapperWithPackage.TypedValue = true;
            Assert.True(package.IsDirty);

            // Test PackageSettingsWrapper collections
            var pkgSettings = new PackageSettingsWrapper();
            Assert.NotNull(pkgSettings.NonExecutableUserSettings);
            Assert.NotNull(pkgSettings.ExecutableUserSettings);

            pkgSettings.HasExecutables = false;
            Assert.Same(pkgSettings.NonExecutableUserSettings, pkgSettings.UserSettings);

            pkgSettings.HasExecutables = true;
            Assert.Same(pkgSettings.ExecutableUserSettings, pkgSettings.UserSettings);

            // Test PackageSettingsEntry struct
            var entry1 = new PackageSettingsEntry(key);
            Assert.Same(key, entry1.SettingsKey);
            Assert.Equal(TargetPackage.All, entry1.TargetPackage);

            var entry2 = new PackageSettingsEntry(key, TargetPackage.Executable);
            Assert.Equal(TargetPackage.Executable, entry2.TargetPackage);
        }
    }
}
