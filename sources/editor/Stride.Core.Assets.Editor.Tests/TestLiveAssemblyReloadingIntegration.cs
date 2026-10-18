// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Editor;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestLiveAssemblyReloadingIntegration
{
    private static string FindRepoRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        while (baseDir != null && !Directory.Exists(Path.Combine(baseDir, "sources")))
        {
            baseDir = Path.GetDirectoryName(baseDir);
        }
        return baseDir ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    [Fact]
    public void TestAssemblyReloadingSourceFilesAndLineParity()
    {
        var repoRoot = FindRepoRoot();
        var reloadingDir = Path.Combine(repoRoot, "sources", "editor", "Stride.Assets.Editor", "AssemblyReloading");

        var reloadOpFile = Path.Combine(reloadingDir, "ReloadAssembliesOperation.cs");
        var reloaderFile = Path.Combine(reloadingDir, "GameStudioAssemblyReloader.cs");

        // 1. Files must exist
        Assert.True(File.Exists(reloadOpFile), $"Expected file missing: {reloadOpFile}");
        Assert.True(File.Exists(reloaderFile), $"Expected file missing: {reloaderFile}");

        // 2. Parity check: 85-100%+ line parity
        // Original ReloadAssembliesOperation.cs: 131 lines
        var reloadOpLines = File.ReadAllLines(reloadOpFile).Length;
        Assert.InRange(reloadOpLines, 110, 160);

        // Original GameStudioAssemblyReloader.cs: 490 lines
        var reloaderLines = File.ReadAllLines(reloaderFile).Length;
        Assert.InRange(reloaderLines, 416, 560);
    }

    [Fact]
    public void TestReloadAssembliesOperationTypeAndArchitecture()
    {
        var editorAsm = typeof(StrideEditorPlugin).Assembly;
        var opType = editorAsm.GetType("Stride.Assets.Editor.AssemblyReloading.ReloadAssembliesOperation");

        Assert.NotNull(opType);
        Assert.True(typeof(DirtyingOperation).IsAssignableFrom(opType), "ReloadAssembliesOperation must inherit from DirtyingOperation.");

        // Check constructor (AssemblyContainer, Dictionary<PackageLoadedAssembly, string>, IEnumerable<IDirtiable>)
        var ctor = opType.GetConstructor(new[]
        {
            typeof(AssemblyContainer),
            typeof(Dictionary<PackageLoadedAssembly, string>),
            typeof(IEnumerable<IDirtiable>)
        });
        Assert.NotNull(ctor);

        // Check Execute method
        var executeMethod = opType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ILogger) }, null);
        Assert.NotNull(executeMethod);

        // Check protected methods: Undo, Redo, FreezeContent
        var undoMethod = opType.GetMethod("Undo", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        Assert.NotNull(undoMethod);

        var redoMethod = opType.GetMethod("Redo", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        Assert.NotNull(redoMethod);

        var freezeMethod = opType.GetMethod("FreezeContent", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        Assert.NotNull(freezeMethod);
    }

    [Fact]
    public void TestGameStudioAssemblyReloaderTypeAndArchitecture()
    {
        var editorAsm = typeof(StrideEditorPlugin).Assembly;
        var reloaderType = editorAsm.GetType("Stride.Assets.Editor.AssemblyReloading.GameStudioAssemblyReloader");

        Assert.NotNull(reloaderType);
        Assert.True(reloaderType.IsAbstract && reloaderType.IsSealed, "GameStudioAssemblyReloader must be a static class.");

        // Check Reload method
        var reloadMethod = reloaderType.GetMethod("Reload", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(reloadMethod);
        var parameters = reloadMethod.GetParameters();
        Assert.Equal(5, parameters.Length);
        Assert.Equal(typeof(SessionViewModel), parameters[0].ParameterType);
        Assert.Equal(typeof(ILogger), parameters[1].ParameterType);
        Assert.Equal(typeof(Action), parameters[2].ParameterType);
        Assert.Equal(typeof(Action), parameters[3].ParameterType);
        Assert.Equal(typeof(Dictionary<PackageLoadedAssembly, string>), parameters[4].ParameterType);

        // Check nested types
        var nestedTypes = reloaderType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
        var visitorBaseType = nestedTypes.FirstOrDefault(t => t.Name == "ReloaderVisitorBase");
        Assert.NotNull(visitorBaseType);

        var unloadingVisitorType = nestedTypes.FirstOrDefault(t => t.Name == "UnloadingVisitor");
        Assert.NotNull(unloadingVisitorType);
        Assert.True(visitorBaseType.IsAssignableFrom(unloadingVisitorType));

        var itemToReloadType = nestedTypes.FirstOrDefault(t => t.Name == "ItemToReload");
        Assert.NotNull(itemToReloadType);
    }

    [Fact]
    public void TestSessionViewModelAndAssetViewModelIntegrationContracts()
    {
        // 1. SessionViewModel.CreateAssetFixupContext
        var sessionType = typeof(SessionViewModel);
        var createFixupMethod = sessionType.GetMethod("CreateAssetFixupContext", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(createFixupMethod);
        Assert.True(typeof(IDisposable).IsAssignableFrom(createFixupMethod.ReturnType));

        // 2. SessionViewModel.IsInFixupAssetContext
        var isInFixupProp = sessionType.GetProperty("IsInFixupAssetContext", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(isInFixupProp);
        Assert.Equal(typeof(bool), isInFixupProp.PropertyType);

        // 3. ISessionViewModel.IsInFixupAssetContext
        var iSessionType = typeof(ISessionViewModel);
        var iIsInFixupProp = iSessionType.GetProperty("IsInFixupAssetContext", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(iIsInFixupProp);
        Assert.Equal(typeof(bool), iIsInFixupProp.PropertyType);

        // 4. SessionViewModel.AssemblyContainer
        var assemblyContainerProp = sessionType.GetProperty("AssemblyContainer", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(assemblyContainerProp);
        Assert.Equal(typeof(AssemblyContainer), assemblyContainerProp.PropertyType);

        // 5. AssetViewModel.UpdateAsset
        var assetVmType = typeof(AssetViewModel);
        var updateAssetMethod = assetVmType.GetMethod("UpdateAsset", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Asset), typeof(ILogger) }, null);
        Assert.NotNull(updateAssetMethod);
    }

    [Fact]
    public void TestSessionViewModelFixupContextLifecycle()
    {
        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        Assert.False(session.IsInFixupAssetContext);

        using (session.CreateAssetFixupContext())
        {
            Assert.True(session.IsInFixupAssetContext);
        }

        Assert.False(session.IsInFixupAssetContext);
    }

    [Fact]
    public void TestReloadAssembliesOperationExecutionAndUndoRedo()
    {
        var container = new AssemblyContainer();
        var currentAsm = typeof(TestLiveAssemblyReloadingIntegration).Assembly;
        var packageLoadedAsm = new PackageLoadedAssembly(null, currentAsm.Location)
        {
            Assembly = currentAsm
        };

        var dict = new Dictionary<PackageLoadedAssembly, string>
        {
            [packageLoadedAsm] = currentAsm.Location
        };

        var op = new Stride.Assets.Editor.AssemblyReloading.ReloadAssembliesOperation(container, dict, []);
        Assert.NotNull(op);

        var logger = new LoggerResult();
        op.Execute(logger);

        // Verify that packageLoadedAsm has updated path and assembly loaded
        Assert.Equal(currentAsm.Location, packageLoadedAsm.Path);
        Assert.NotNull(packageLoadedAsm.Assembly);

        // Test Undo through reflection
        var undoMethod = typeof(Stride.Assets.Editor.AssemblyReloading.ReloadAssembliesOperation)
            .GetMethod("Undo", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(undoMethod);
        undoMethod.Invoke(op, null);

        // Test Redo through reflection
        var redoMethod = typeof(Stride.Assets.Editor.AssemblyReloading.ReloadAssembliesOperation)
            .GetMethod("Redo", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(redoMethod);
        redoMethod.Invoke(op, null);

        // Test FreezeContent
        var freezeMethod = typeof(Stride.Assets.Editor.AssemblyReloading.ReloadAssembliesOperation)
            .GetMethod("FreezeContent", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(freezeMethod);
        freezeMethod.Invoke(op, null);
    }
}
