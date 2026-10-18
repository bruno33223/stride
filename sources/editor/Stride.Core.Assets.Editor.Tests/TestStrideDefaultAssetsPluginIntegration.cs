// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets;
using Stride.Assets.Presentation;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Packages;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Core.Assets.Editor.Tests;

public class TestStrideDefaultAssetsPluginIntegration
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

    private static (SessionViewModel session, ViewModelServiceProvider sp) CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new MockDialogService());

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        var nodeContainer = new AssetNodeContainer();
        var nodeContainerProp = typeof(SessionViewModel).GetProperty("AssetNodeContainer");
        if (nodeContainerProp?.GetSetMethod(true) != null)
            nodeContainerProp.GetSetMethod(true)!.Invoke(session, [nodeContainer]);
        else
            typeof(SessionViewModel).GetField("<AssetNodeContainer>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(session, nodeContainer);

        var activeProperties = new SessionObjectPropertiesViewModel(session);
        typeof(SessionViewModel).GetField("activeProperties", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, activeProperties);

        var suggestedField = typeof(SessionViewModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(f => f.Name.Contains("SuggestedPackages"));
        suggestedField?.SetValue(session, new List<PackageName>());

        return (session, sp);
    }

    [Fact]
    public void TestPluginRegistrationAndProfileSettings()
    {
        var pluginType = typeof(StrideDefaultAssetsPlugin);
        Assert.True(typeof(AssetsPlugin).IsAssignableFrom(pluginType), "StrideDefaultAssetsPlugin must inherit from AssetsPlugin.");

        var plugin = (StrideDefaultAssetsPlugin)Activator.CreateInstance(pluginType)!;
        Assert.NotNull(plugin);

        // Verify ProfileSettings exists and is populated with effect compilation settings
        var profileSettingsProp = pluginType.GetProperty("ProfileSettings", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(profileSettingsProp);

        var profileSettings = profileSettingsProp.GetValue(plugin) as IEnumerable<PackageSettingsEntry>;
        Assert.NotNull(profileSettings);
        Assert.Contains(profileSettings, entry => entry.SettingsKey == GameUserSettings.Effect.EffectCompilation);
        Assert.Contains(profileSettings, entry => entry.SettingsKey == GameUserSettings.Effect.RecordUsedEffects);
    }

    [Fact]
    public void TestComponentTypeComparerAndOrdering()
    {
        var pluginType = typeof(StrideDefaultAssetsPlugin);
        var plugin = (StrideDefaultAssetsPlugin)Activator.CreateInstance(pluginType)!;
        var logger = new LoggerResult();
        plugin.InitializePlugin(logger);

        // ComponentOrders must be populated after InitializePlugin
        var orders = StrideDefaultAssetsPlugin.ComponentOrders;
        Assert.NotNull(orders);
        Assert.NotEmpty(orders);

        // Verify GetHighestOrderComponent, GetLowestOrderComponent and GetComponentsByOrder
        var getHighestMethod = pluginType.GetMethod("GetHighestOrderComponent", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(getHighestMethod);

        var getLowestMethod = pluginType.GetMethod("GetLowestOrderComponent", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(getLowestMethod);

        var getByOrderMethod = pluginType.GetMethod("GetComponentsByOrder", BindingFlags.Static | BindingFlags.Public, null, [typeof(IEnumerable<Type>), typeof(bool)], null);
        Assert.NotNull(getByOrderMethod);

        // Test with sample component types from the registered orders
        var registeredTypes = orders.Select(x => x.type).Take(5).ToList();
        if (registeredTypes.Count >= 2)
        {
            var highest = getHighestMethod.Invoke(null, [registeredTypes]) as Type;
            Assert.NotNull(highest);
            Assert.Equal(registeredTypes.First(), highest);

            var lowest = getLowestMethod.Invoke(null, [registeredTypes]) as Type;
            Assert.NotNull(lowest);
            Assert.Equal(registeredTypes.Last(), lowest);

            var orderedAscending = (getByOrderMethod.Invoke(null, [registeredTypes, true]) as IEnumerable<Type>)?.ToList();
            Assert.NotNull(orderedAscending);
            Assert.Equal(registeredTypes.Last(), orderedAscending.First());
        }
    }

    [Fact]
    public void TestEntityFactoryCategoriesRegistration()
    {
        var pluginType = typeof(StrideDefaultAssetsPlugin);
        var plugin = (StrideDefaultAssetsPlugin)Activator.CreateInstance(pluginType)!;
        var logger = new LoggerResult();
        plugin.InitializePlugin(logger);

        var categoriesProp = pluginType.GetProperty("EntityFactoryCategories", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(categoriesProp);

        var categories = categoriesProp.GetValue(null) as System.Collections.IEnumerable;
        Assert.NotNull(categories);

        var categoryList = categories.Cast<object>().ToList();
        Assert.NotEmpty(categoryList);

        // Check category names
        var categoryNames = categoryList.Select(c => c.GetType().GetProperty("Name")?.GetValue(c)?.ToString()).ToList();
        Assert.Contains("Camera", categoryNames);
        Assert.Contains("Light", categoryNames);
        Assert.Contains("2D", categoryNames);
    }

    [Fact]
    public void TestGizmosAndAssetHighlightersRegistration()
    {
        var pluginType = typeof(StrideDefaultAssetsPlugin);
        var plugin = (StrideDefaultAssetsPlugin)Activator.CreateInstance(pluginType)!;
        var logger = new LoggerResult();
        plugin.InitializePlugin(logger);

        // GizmoTypeDictionary must be populated
        Assert.NotNull(StrideDefaultAssetsPlugin.GizmoTypeDictionary);
        Assert.NotEmpty(StrideDefaultAssetsPlugin.GizmoTypeDictionary);

        // AssetHighlighterTypesDictionary must exist and be accessible
        var highlighterProp = pluginType.GetProperty("AssetHighlighterTypesDictionary", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(highlighterProp);

        var highlighterDict = highlighterProp.GetValue(null) as IReadOnlyDictionary<Type, Type>;
        Assert.NotNull(highlighterDict);
    }

    [Fact]
    public void TestSessionInitializationCommandsAndUpdaters()
    {
        var (session, sp) = CreateTestSession();
        var pluginType = typeof(StrideDefaultAssetsPlugin);
        var plugin = (StrideDefaultAssetsPlugin)Activator.CreateInstance(pluginType)!;

        plugin.InitializeSession(session);

        // SuggestedPackages must be populated
        Assert.NotEmpty(session.SuggestedPackages);
        var packageNames = session.SuggestedPackages.Select(p => p.Id).ToList();
        Assert.Contains(typeof(EntityComponent).Assembly.GetName().Name, packageNames);
        Assert.Contains(typeof(Stride.UI.UIElement).Assembly.GetName().Name, packageNames);

        var graphVmService = session.ActiveProperties.ServiceProvider.Get<GraphViewModelService>()
            ?? (GraphViewModelService)typeof(PropertiesViewModel).GetField("ViewModelService", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(session.ActiveProperties)!;
        Assert.NotNull(graphVmService);
        var commands = graphVmService.AvailableCommands.Select(c => c.Name).ToList();
        Assert.Contains("SetEntityReference", commands);
        Assert.Contains("SetComponentReference", commands);
        Assert.Contains("FetchEntity", commands);
        Assert.Contains("PickupEntity", commands);
        Assert.Contains("PickupComponent", commands);

        // Quantum node presenter updaters must be registered
        var updaters = graphVmService.AvailableUpdaters.Select(u => u.GetType().Name).ToList();
        Assert.Contains("GameSettingsAssetNodeUpdater", updaters);
        Assert.Contains("AnimationAssetNodeUpdater", updaters);
        Assert.Contains("CameraSlotNodeUpdater", updaters);
        Assert.Contains("EntityHierarchyAssetNodeUpdater", updaters);
    }

    [Fact]
    public void TestPrimitiveTypesAndProcessorsRegistration()
    {
        var pluginType = typeof(StrideDefaultAssetsPlugin);
        var plugin = (StrideDefaultAssetsPlugin)Activator.CreateInstance(pluginType)!;

        // RegisterPrimitiveTypes method must exist and register primitive types
        var registerPrimMethod = pluginType.GetMethod("RegisterPrimitiveTypes", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(registerPrimMethod);

        var primitives = new HashSet<Type>();
        registerPrimMethod.Invoke(plugin, [primitives]);
        Assert.Contains(typeof(AssetReference), primitives);
        Assert.Contains(typeof(Stride.Core.Serialization.UrlReferenceBase), primitives);
    }
}
