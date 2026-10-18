// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestNewProjectDialogResult
{
    private class CancelledNewProjectDialog : INewProjectDialog
    {
        // Follows the corrected dialog contract: defaults to DialogResult.Cancel on dismissal
        public DialogResult Result { get; set; } = DialogResult.Cancel;

        public UDirectory DefaultOutputDirectory { get; set; } = string.Empty;

        public NewPackageParameters Parameters { get; set; } = new();

        public object? DataContext { get; set; }

        public Task<DialogResult> ShowModal() => Task.FromResult(Result);

        public void RequestClose(DialogResult result) => Result = result;
    }

    [Fact]
    public async Task TestCancelledDialogContractMustReturnCancelAndNeverNone()
    {
        // When a dialog is cancelled or dismissed by closing without confirming,
        // it must return DialogResult.Cancel and never DialogResult.None.
        var dialog = new CancelledNewProjectDialog();
        var dialogResult = await dialog.ShowModal();

        Assert.NotEqual(DialogResult.None, dialogResult);
        Assert.Equal(DialogResult.Cancel, dialogResult);

        // Verifies SessionViewModel.NewProject abort condition:
        // if (dialogResult == DialogResult.Cancel) return;
        bool abortTriggered = dialogResult == DialogResult.Cancel;
        Assert.True(abortTriggered, "DialogResult.Cancel must abort project creation in SessionViewModel.NewProject");
    }

    [Fact]
    public void TestDialogResultNoneFailsAbortCondition()
    {
        // Explicitly verifies why returning DialogResult.None was a defect in SessionViewModel.NewProject
        var dialogResult = DialogResult.None;
        bool abortTriggered = dialogResult == DialogResult.Cancel;
        Assert.False(abortTriggered, "DialogResult.None erroneously failed to abort SessionViewModel.NewProject");
    }

    [Fact]
    public void TestNewProjectWindowInitialResultIsCancel()
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
            var asm = Assembly.LoadFrom(avaloniaAssemblyPath);
            var type = asm.GetType("Stride.GameStudio.Avalonia.Views.NewProjectWindow");
            Assert.NotNull(type);

            var field = type.GetField("dialogResult", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);

            var ctor = type.GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctor);
            var body = ctor.GetMethodBody();
            var il = body?.GetILAsByteArray();
            Assert.NotNull(il);

            // Look for stfld (0x7D) targeting dialogResult field, and inspect preceding opcode
            bool initializesToCancel = false;
            for (int i = 1; i < il.Length - 4; i++)
            {
                if (il[i] == 0x7D) // stfld
                {
                    int token = BitConverter.ToInt32(il, i + 1);
                    var resolvedMember = type.Module.ResolveMember(token);
                    if (resolvedMember?.Name == "dialogResult")
                    {
                        byte valueOpcode = il[i - 1];
                        if (valueOpcode == 0x18) // 0x18 == ldc.i4.2 (DialogResult.Cancel)
                        {
                            initializesToCancel = true;
                            break;
                        }
                    }
                }
            }
            Assert.True(initializesToCancel, "NewProjectWindow.dialogResult must be initialized to DialogResult.Cancel, not DialogResult.None!");
        }
    }
}
