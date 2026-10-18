// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;

namespace Stride.Core.Assets.Editor.Tests;

public class TestPluginArchitectureAndLifecycleIntegration
{
    [Fact]
    public void TestAssetsPluginServiceContractParity()
    {
        var serviceType = typeof(IAssetsPluginService);

        // Verify required properties
        var pluginsProp = serviceType.GetProperty("Plugins");
        Assert.NotNull(pluginsProp);

        // Verify method signatures from original legacy IAssetsPluginService
        var registerSessionMethod = serviceType.GetMethod("RegisterSession", [typeof(SessionViewModel), typeof(ILogger)]);
        Assert.True(registerSessionMethod != null, "IAssetsPluginService must have RegisterSession(SessionViewModel, ILogger).");

        var ensureInitMethod = serviceType.GetMethod("EnsureInitialized", [typeof(ILogger)]);
        Assert.True(ensureInitMethod != null, "IAssetsPluginService must have EnsureInitialized(ILogger).");

        var hasImagesMethod = serviceType.GetMethod("HasImagesForEnum", [typeof(SessionViewModel), typeof(Type)]);
        Assert.True(hasImagesMethod != null, "IAssetsPluginService must have HasImagesForEnum(SessionViewModel?, Type).");

        var getImageMethod = serviceType.GetMethod("GetImageForEnum", [typeof(SessionViewModel), typeof(object)]);
        Assert.True(getImageMethod != null, "IAssetsPluginService must have GetImageForEnum(SessionViewModel?, object).");

        var getPrimitiveWithSession = serviceType.GetMethod("GetPrimitiveTypes", [typeof(SessionViewModel)]);
        Assert.True(getPrimitiveWithSession != null, "IAssetsPluginService must have GetPrimitiveTypes(SessionViewModel).");

        var getPrimitiveParameterless = serviceType.GetMethod("GetPrimitiveTypes", Type.EmptyTypes);
        Assert.True(getPrimitiveParameterless != null, "IAssetsPluginService must have parameterless GetPrimitiveTypes().");

        var hasEditorViewMethod = serviceType.GetMethod("HasEditorView", [typeof(SessionViewModel), typeof(Type)]);
        Assert.True(hasEditorViewMethod != null, "IAssetsPluginService must have HasEditorView(SessionViewModel, Type).");

        var getAssetVmMethod = serviceType.GetMethod("GetAssetViewModelType", [typeof(Type)]);
        Assert.True(getAssetVmMethod != null, "IAssetsPluginService must have GetAssetViewModelType(Type).");

        var getEditorVmMethod = serviceType.GetMethod("GetEditorViewModelType", [typeof(Type)]);
        Assert.True(getEditorVmMethod != null, "IAssetsPluginService must have GetEditorViewModelType(Type).");

        var getEditorViewTypeMethod = serviceType.GetMethod("GetEditorViewType", [typeof(Type)]);
        Assert.True(getEditorViewTypeMethod != null, "IAssetsPluginService must have GetEditorViewType(Type).");

        var getPreviewVmMethod = serviceType.GetMethod("GetPreviewViewModelType", [typeof(Type)]);
        Assert.True(getPreviewVmMethod != null, "IAssetsPluginService must have GetPreviewViewModelType(Type).");

        var getPreviewViewTypeMethod = serviceType.GetMethod("GetPreviewViewType", [typeof(Type)]);
        Assert.True(getPreviewViewTypeMethod != null, "IAssetsPluginService must have GetPreviewViewType(Type).");
    }

    [Fact]
    public void TestAssetsPluginTypeAndRegistrationServices()
    {
        var editorAssembly = typeof(SessionViewModel).Assembly;
        var pluginType = editorAssembly.GetType("Stride.Core.Assets.Editor.Services.AssetsPlugin");
        Assert.True(pluginType != null, "AssetsPlugin must exist in Stride.Core.Assets.Editor.Services.");

        // Verify RegisteredPlugins property
        var regPluginsProp = pluginType.GetProperty("RegisteredPlugins", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(regPluginsProp);

        // Verify RegisterPlugin method returns the instance
        var regMethod = pluginType.GetMethod("RegisterPlugin", BindingFlags.Static | BindingFlags.Public, [typeof(Type)]);
        Assert.NotNull(regMethod);
        Assert.Equal(pluginType, regMethod.ReturnType);

        // Verify RegisterPrimitiveTypes method exists on AssetsPlugin
        var regPrimMethod = pluginType.GetMethod("RegisterPrimitiveTypes", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(regPrimMethod);

        // Verify SessionLoaded and SessionDisposed methods exist
        var sessionLoadedMethod = pluginType.GetMethod("SessionLoaded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(sessionLoadedMethod);

        var sessionDisposedMethod = pluginType.GetMethod("SessionDisposed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(sessionDisposedMethod);
    }

    [Fact]
    public void TestAssetsEditorPluginArchitecture()
    {
        var editorAssembly = typeof(SessionViewModel).Assembly;
        var pluginType = editorAssembly.GetType("Stride.Core.Assets.Editor.Services.AssetsPlugin");
        Assert.NotNull(pluginType);

        var editorPluginType = editorAssembly.GetType("Stride.Core.Assets.Editor.Services.AssetsEditorPlugin");
        Assert.True(editorPluginType != null, "AssetsEditorPlugin must exist in Stride.Core.Assets.Editor.Services.");
        Assert.True(pluginType.IsAssignableFrom(editorPluginType), "AssetsEditorPlugin must inherit from AssetsPlugin.");

        // Verify TypeImagesDictionary
        var typeImagesProp = editorPluginType.GetProperty("TypeImagesDictionary", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(typeImagesProp);

        // Verify ProfileSettings
        var profileSettingsField = editorPluginType.GetField("ProfileSettings", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(profileSettingsField);

        // Verify processor registration methods
        var regCopyMethod = editorPluginType.GetMethod("RegisterCopyProcessors", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(regCopyMethod);

        var regPasteMethod = editorPluginType.GetMethod("RegisterPasteProcessors", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(regPasteMethod);

        var regPostPasteMethod = editorPluginType.GetMethod("RegisterPostPasteProcessors", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(regPostPasteMethod);

        var regEnumImagesMethod = editorPluginType.GetMethod("RegisterEnumImages", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(regEnumImagesMethod);
    }

    [Fact]
    public void TestCoreAssetsEditorPluginRegistrationAndProcessors()
    {
        var editorAssembly = typeof(SessionViewModel).Assembly;
        var corePluginType = editorAssembly.GetType("Stride.Core.Assets.Editor.CoreAssetsEditorPlugin");
        Assert.True(corePluginType != null, "CoreAssetsEditorPlugin must exist in Stride.Core.Assets.Editor.");

        var editorPluginType = editorAssembly.GetType("Stride.Core.Assets.Editor.Services.AssetsEditorPlugin");
        Assert.NotNull(editorPluginType);
        Assert.True(editorPluginType.IsAssignableFrom(corePluginType), "CoreAssetsEditorPlugin must inherit from AssetsEditorPlugin.");

        // Instantiate CoreAssetsEditorPlugin
        var plugin = Activator.CreateInstance(corePluginType);
        Assert.NotNull(plugin);

        // InitializePlugin with LoggerResult
        var initMethod = corePluginType.GetMethod("InitializePlugin", [typeof(ILogger)]);
        Assert.NotNull(initMethod);
        initMethod.Invoke(plugin, [new LoggerResult()]);

        // Verify RegisterPasteProcessors adds AssetPropertyPasteProcessor and AssetItemPasteProcessor
        var pasteProcessors = new List<IPasteProcessor>();
        var regPasteMethod = corePluginType.GetMethod("RegisterPasteProcessors", [typeof(ICollection<IPasteProcessor>), typeof(SessionViewModel)]);
        Assert.NotNull(regPasteMethod);

        var session = (SessionViewModel)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        regPasteMethod.Invoke(plugin, [pasteProcessors, session]);

        Assert.Equal(2, pasteProcessors.Count);
        Assert.Contains(pasteProcessors, p => p.GetType().Name == "AssetPropertyPasteProcessor");
        Assert.Contains(pasteProcessors, p => p.GetType().Name == "AssetItemPasteProcessor");
    }

    [Fact]
    public void TestStrideAssetsPluginArchitectureAndPrimitives()
    {
        var strideAssetsAssembly = Assembly.Load("Stride.Assets.Editor");
        Assert.NotNull(strideAssetsAssembly);

        var strideAssetsPluginType = strideAssetsAssembly.GetType("Stride.Assets.Editor.StrideAssetsPlugin");
        Assert.True(strideAssetsPluginType != null, "StrideAssetsPlugin must exist in Stride.Assets.Editor.");

        var editorAssembly = typeof(SessionViewModel).Assembly;
        var editorPluginType = editorAssembly.GetType("Stride.Core.Assets.Editor.Services.AssetsEditorPlugin");
        Assert.NotNull(editorPluginType);
        Assert.True(editorPluginType.IsAssignableFrom(strideAssetsPluginType), "StrideAssetsPlugin must inherit from AssetsEditorPlugin.");

        // Abstract method Initialize(ILogger)
        var initMethod = strideAssetsPluginType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(initMethod);
        Assert.True(initMethod.IsAbstract, "Initialize(ILogger) must be abstract.");

        // Sealed InitializePlugin(ILogger)
        var initPluginMethod = strideAssetsPluginType.GetMethod("InitializePlugin", [typeof(ILogger)]);
        Assert.NotNull(initPluginMethod);

        // RegisterPrimitiveTypes
        var regPrimMethod = strideAssetsPluginType.GetMethod("RegisterPrimitiveTypes", [typeof(ICollection<Type>)]);
        Assert.NotNull(regPrimMethod);

        // RegisterAssetPreviewViewModelTypes
        var regPreviewVmMethod = strideAssetsPluginType.GetMethod("RegisterAssetPreviewViewModelTypes", [typeof(IDictionary<Type, Type>)]);
        Assert.NotNull(regPreviewVmMethod);
    }
}
