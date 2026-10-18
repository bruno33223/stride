// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Tests;

public class TestSessionArchitectureAndQuantumIntegration
{
    private static readonly Assembly EditorAssembly = typeof(SessionViewModel).Assembly;

    [Fact]
    public void TestISessionObjectViewModelContractParity()
    {
        var interfaceType = EditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModels.ISessionObjectViewModel");
        Assert.True(interfaceType != null, "ISessionObjectViewModel must exist in Stride.Core.Assets.Editor.ViewModels.");
        Assert.True(interfaceType.IsInterface, "ISessionObjectViewModel must be an interface.");

        // Must inherit from or be compatible with Presentation ISessionObjectViewModel
        Assert.True(typeof(Stride.Core.Assets.Presentation.ViewModels.ISessionObjectViewModel).IsAssignableFrom(interfaceType),
            "ISessionObjectViewModel in Editor must implement Presentation ISessionObjectViewModel.");

        // Check properties
        var isEditableProp = interfaceType.GetProperty("IsEditable");
        Assert.NotNull(isEditableProp);
        Assert.Equal(typeof(bool), isEditableProp.PropertyType);

        var nameProp = interfaceType.GetProperty("Name");
        Assert.NotNull(nameProp);
        Assert.Equal(typeof(string), nameProp.PropertyType);
        Assert.True(nameProp.CanRead && nameProp.CanWrite);

        var sessionProp = interfaceType.GetProperty("Session");
        Assert.NotNull(sessionProp);
        Assert.Equal(typeof(SessionViewModel), sessionProp.PropertyType);

        var thumbnailDataProp = interfaceType.GetProperty("ThumbnailData");
        Assert.NotNull(thumbnailDataProp);
        Assert.Equal(typeof(Stride.Core.Assets.Editor.Services.ThumbnailData), thumbnailDataProp.PropertyType);

        var typeDisplayNameProp = interfaceType.GetProperty("TypeDisplayName");
        Assert.NotNull(typeDisplayNameProp);
        Assert.Equal(typeof(string), typeDisplayNameProp.PropertyType);
    }

    [Fact]
    public void TestSessionObjectViewModelContractAndHierarchy()
    {
        var classType = EditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModels.SessionObjectViewModel");
        Assert.True(classType != null, "SessionObjectViewModel must exist in Stride.Core.Assets.Editor.ViewModels.");
        Assert.True(classType.IsAbstract, "SessionObjectViewModel must be abstract.");

        Assert.True(typeof(DirtiableEditableViewModel).IsAssignableFrom(classType),
            "SessionObjectViewModel must inherit from DirtiableEditableViewModel.");
        Assert.True(typeof(IAddChildViewModel).IsAssignableFrom(classType),
            "SessionObjectViewModel must implement IAddChildViewModel.");
        Assert.True(typeof(IIsEditableViewModel).IsAssignableFrom(classType),
            "SessionObjectViewModel must implement IIsEditableViewModel.");

        var iSessionObjType = EditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModels.ISessionObjectViewModel");
        Assert.NotNull(iSessionObjType);
        Assert.True(iSessionObjType.IsAssignableFrom(classType),
            "SessionObjectViewModel must implement ISessionObjectViewModel.");

        // Verify constructor accepts SessionViewModel
        var ctor = classType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, [typeof(SessionViewModel)], null);
        Assert.True(ctor != null, "SessionObjectViewModel must have a constructor accepting SessionViewModel.");
    }

    [Fact]
    public void TestSessionNodeContainerConstructorAndVisibility()
    {
        var containerType = typeof(SessionNodeContainer);
        Assert.True(containerType.IsPublic, "SessionNodeContainer must be public.");

        // Must have constructor accepting SessionViewModel
        var sessionCtor = containerType.GetConstructor([typeof(SessionViewModel)]);
        Assert.True(sessionCtor != null, "SessionNodeContainer must have a public constructor accepting SessionViewModel.");

        // Must have constructor accepting IViewModelServiceProvider
        var serviceProviderCtor = containerType.GetConstructor([typeof(IViewModelServiceProvider)]);
        Assert.True(serviceProviderCtor != null, "SessionNodeContainer must have a constructor accepting IViewModelServiceProvider.");
    }

    [Fact]
    public void TestNewSessionParametersContract()
    {
        var paramType = EditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModels.NewSessionParameters");
        Assert.True(paramType != null, "NewSessionParameters must exist in Stride.Core.Assets.Editor.ViewModels.");

        var instance = Activator.CreateInstance(paramType);
        Assert.NotNull(instance);

        // Check fields/properties
        var templateDesc = paramType.GetField("TemplateDescription") ?? (MemberInfo?)paramType.GetProperty("TemplateDescription");
        Assert.NotNull(templateDesc);

        var outputName = paramType.GetField("OutputName") ?? (MemberInfo?)paramType.GetProperty("OutputName");
        Assert.NotNull(outputName);

        var outputDir = paramType.GetField("OutputDirectory") ?? (MemberInfo?)paramType.GetProperty("OutputDirectory");
        Assert.NotNull(outputDir);

        var solutionName = paramType.GetField("SolutionName") ?? (MemberInfo?)paramType.GetProperty("SolutionName");
        Assert.NotNull(solutionName);

        var solutionLoc = paramType.GetField("SolutionLocation") ?? (MemberInfo?)paramType.GetProperty("SolutionLocation");
        Assert.NotNull(solutionLoc);
    }

    [Fact]
    public void TestSessionStateChangedEventArgsContract()
    {
        var argsType = EditorAssembly.GetType("Stride.Core.Assets.Editor.ViewModels.SessionStateChangedEventArgs");
        Assert.True(argsType != null, "SessionStateChangedEventArgs must exist in Stride.Core.Assets.Editor.ViewModels.");
        Assert.True(typeof(EventArgs).IsAssignableFrom(argsType), "SessionStateChangedEventArgs must inherit from EventArgs.");

        var instance = Activator.CreateInstance(argsType);
        Assert.NotNull(instance);
    }

    [Fact]
    public void TestSessionDataKeysContract()
    {
        Assert.Equal("Session", SessionData.Session);
        Assert.Equal("DynamicThumbnail", SessionData.DynamicThumbnail);

        Assert.NotNull(SessionData.SessionKey);
        Assert.NotNull(SessionData.DynamicThumbnailKey);

        Assert.Equal(typeof(SessionViewModel), SessionData.SessionKey.PropertyType);
        Assert.Equal(typeof(bool), SessionData.DynamicThumbnailKey.PropertyType);
    }

    [Fact]
    public void TestSessionInitialDirectoryProviderResolution()
    {
        // Provider returns currentPath when session or session path is unavailable
        var providerWithNullSession = new SessionInitialDirectoryProvider(null!);
        var initialDir = new UDirectory("/test/path");
        Assert.Equal(initialDir, providerWithNullSession.GetInitialDirectory(initialDir));
    }

    [Fact]
    public void TestCreateNewSessionMethodContract()
    {
        var sessionType = typeof(SessionViewModel);
        var createNewSessionMethod = sessionType.GetMethod("CreateNewSession", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(createNewSessionMethod);
    }

    [Fact]
    public void TestGameStudioNewCommandAndProjectSelectionContracts()
    {
        var asmPath = typeof(SessionViewModel).Assembly.Location;
        var dir = Path.GetDirectoryName(asmPath)!;
        var gameStudioAsmPath = Path.Combine(dir, "Stride.GameStudio.Avalonia.dll");
        if (!File.Exists(gameStudioAsmPath))
        {
            gameStudioAsmPath = "/media/bruno/SteamGames/apps/stride-source/stride/sources/editor/Stride.GameStudio.Avalonia/bin/Debug/net10.0/Stride.GameStudio.Avalonia.dll";
        }
        Assert.True(File.Exists(gameStudioAsmPath), $"Stride.GameStudio.Avalonia.dll must exist at {gameStudioAsmPath}");
        var asm = Assembly.LoadFrom(gameStudioAsmPath);

        var mainVmType = asm.GetType("Stride.GameStudio.Avalonia.ViewModels.MainViewModel");
        Assert.NotNull(mainVmType);

        var newCommandProp = mainVmType.GetProperty("NewCommand");
        Assert.NotNull(newCommandProp);

        var dialogServiceType = asm.GetType("Stride.GameStudio.Avalonia.Services.EditorDialogService");
        Assert.NotNull(dialogServiceType);

        var showMethod = dialogServiceType.GetMethod("ShowProjectSelectionWindowAsync");
        Assert.NotNull(showMethod);
    }
}
