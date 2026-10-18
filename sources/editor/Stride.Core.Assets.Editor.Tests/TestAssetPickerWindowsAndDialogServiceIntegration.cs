// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetPickerWindowsAndDialogServiceIntegration
{
    private static Assembly? LoadAssemblySafely(string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            var baseDir = AppContext.BaseDirectory;
            while (baseDir != null && !Directory.Exists(Path.Combine(baseDir, "sources")))
            {
                baseDir = Path.GetDirectoryName(baseDir);
            }
            if (baseDir != null)
            {
                var candidatePaths = Directory.GetFiles(baseDir, assemblyName + ".dll", SearchOption.AllDirectories)
                    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                                !p.Contains(Path.DirectorySeparatorChar + "ref" + Path.DirectorySeparatorChar))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .ToArray();
                if (candidatePaths.Length > 0)
                {
                    return Assembly.LoadFrom(candidatePaths[0]);
                }
            }
            return null;
        }
    }

    [Fact]
    public void TestAssetPickerWindowTypeAndInterfaces()
    {
        var avaloniaAsm = LoadAssemblySafely("Stride.Core.Assets.Editor.Avalonia");
        Assert.NotNull(avaloniaAsm);

        var assetPickerType = avaloniaAsm.GetType("Stride.Core.Assets.Editor.Avalonia.Views.AssetPickerWindow");
        Assert.True(assetPickerType != null, "Stride.Core.Assets.Editor.Avalonia.Views.AssetPickerWindow must exist.");
        Assert.True(typeof(IAssetPickerDialog).IsAssignableFrom(assetPickerType), "AssetPickerWindow must implement IAssetPickerDialog.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(assetPickerType), "AssetPickerWindow must implement IModalDialog.");
    }

    [Fact]
    public void TestPackagePickerWindowTypeAndInterfaces()
    {
        var avaloniaAsm = LoadAssemblySafely("Stride.Core.Assets.Editor.Avalonia");
        Assert.NotNull(avaloniaAsm);

        var packagePickerType = avaloniaAsm.GetType("Stride.Core.Assets.Editor.Avalonia.Views.PackagePickerWindow");
        Assert.True(packagePickerType != null, "Stride.Core.Assets.Editor.Avalonia.Views.PackagePickerWindow must exist.");

        var packagePickerDialogType = Type.GetType("Stride.Core.Assets.Editor.Services.IPackagePickerDialog, Stride.Core.Assets.Editor")
            ?? typeof(IEditorDialogService).Assembly.GetType("Stride.Core.Assets.Editor.Services.IPackagePickerDialog");
        Assert.True(packagePickerDialogType != null, "IPackagePickerDialog interface must exist in Stride.Core.Assets.Editor.Services.");

        Assert.True(packagePickerDialogType.IsAssignableFrom(packagePickerType), "PackagePickerWindow must implement IPackagePickerDialog.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(packagePickerType), "PackagePickerWindow must implement IModalDialog.");
    }

    [Fact]
    public void TestPackagePickerWindowShowModalHasThreadSafeguard()
    {
        var avaloniaAsm = LoadAssemblySafely("Stride.Core.Assets.Editor.Avalonia");
        Assert.NotNull(avaloniaAsm);

        var packagePickerType = avaloniaAsm.GetType("Stride.Core.Assets.Editor.Avalonia.Views.PackagePickerWindow");
        Assert.NotNull(packagePickerType);

        var showModalMethod = packagePickerType.GetMethod("ShowModal", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(showModalMethod);

        var stateMachineAttr = showModalMethod.GetCustomAttribute<AsyncStateMachineAttribute>();
        Assert.NotNull(stateMachineAttr);

        var moveNextMethod = stateMachineAttr.StateMachineType.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moveNextMethod);

        var body = moveNextMethod.GetMethodBody();
        Assert.NotNull(body);

        var il = body.GetILAsByteArray();
        Assert.NotNull(il);

        var module = stateMachineAttr.StateMachineType.Module;
        var calledMethods = new List<string>();

        for (int i = 0; i < il.Length; i++)
        {
            byte op = il[i];
            if (op == 0x28 || op == 0x6F) // call or callvirt
            {
                if (i + 4 < il.Length)
                {
                    int token = BitConverter.ToInt32(il, i + 1);
                    try
                    {
                        var method = module.ResolveMethod(token);
                        if (method != null)
                        {
                            calledMethods.Add(method.DeclaringType?.FullName + "." + method.Name);
                        }
                    }
                    catch { }
                    i += 4;
                }
            }
        }

        Assert.Contains(calledMethods, m => m.Contains("CheckAccess") || m.Contains("UIThread"));
    }

    [Fact]
    public void TestDebuggerPickerDialogAndPickedDebuggerInterfaces()
    {
        var assetsEditorAsm = LoadAssemblySafely("Stride.Assets.Editor");
        Assert.NotNull(assetsEditorAsm);

        var debuggerPickerDialogType = assetsEditorAsm.GetType("Stride.Assets.Editor.Services.IDebuggerPickerDialog");
        Assert.True(debuggerPickerDialogType != null, "IDebuggerPickerDialog must exist in Stride.Assets.Editor.Services.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(debuggerPickerDialogType), "IDebuggerPickerDialog must inherit from IModalDialog.");

        var prop = debuggerPickerDialogType.GetProperty("SelectedDebugger");
        Assert.True(prop != null, "IDebuggerPickerDialog must declare SelectedDebugger property.");
        Assert.True(prop.PropertyType.Name == "IPickedDebugger", "SelectedDebugger must return IPickedDebugger.");
    }

    [Fact]
    public void TestDebuggerPickerWindowTypeAndInterfaces()
    {
        var avaloniaAsm = LoadAssemblySafely("Stride.Assets.Editor.Avalonia");
        Assert.NotNull(avaloniaAsm);

        var debuggerPickerType = avaloniaAsm.GetType("Stride.Assets.Editor.Avalonia.Views.DebuggerPickerWindow");
        Assert.True(debuggerPickerType != null, "DebuggerPickerWindow must exist in Stride.Assets.Editor.Avalonia.Views.");

        var assetsEditorAsm = LoadAssemblySafely("Stride.Assets.Editor");
        Assert.NotNull(assetsEditorAsm);
        var debuggerPickerDialogType = assetsEditorAsm.GetType("Stride.Assets.Editor.Services.IDebuggerPickerDialog");
        Assert.NotNull(debuggerPickerDialogType);

        Assert.True(debuggerPickerDialogType.IsAssignableFrom(debuggerPickerType), "DebuggerPickerWindow must implement IDebuggerPickerDialog.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(debuggerPickerType), "DebuggerPickerWindow must implement IModalDialog.");
    }

    [Fact]
    public void TestEditorDialogServicePickerMethods()
    {
        var studioAsm = LoadAssemblySafely("Stride.GameStudio.Avalonia");
        Assert.NotNull(studioAsm);

        var serviceType = studioAsm.GetType("Stride.GameStudio.Avalonia.Services.EditorDialogService");
        Assert.True(serviceType != null, "EditorDialogService must exist in Stride.GameStudio.Avalonia.Services.");

        var createAssetPickerMethod = serviceType.GetMethod("CreateAssetPickerDialog");
        Assert.True(createAssetPickerMethod != null, "EditorDialogService must implement CreateAssetPickerDialog.");
        Assert.True(typeof(IAssetPickerDialog).IsAssignableFrom(createAssetPickerMethod.ReturnType), "CreateAssetPickerDialog must return IAssetPickerDialog.");

        var createPackagePickerMethod = serviceType.GetMethod("CreatePackagePickerDialog");
        Assert.True(createPackagePickerMethod != null, "EditorDialogService must implement CreatePackagePickerDialog.");
        Assert.True(typeof(IModalDialog).IsAssignableFrom(createPackagePickerMethod.ReturnType), "CreatePackagePickerDialog must return an IModalDialog/IPackagePickerDialog.");
    }
}
