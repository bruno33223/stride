// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.CodeEditorSupport;
using Stride.Core.Extensions;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Services;

public static class VisualStudioService
{
    private static readonly string[] IdeProcessNames = ["devenv", "code", "rider", "rider64"];

    public static Process? FindVisualStudioInstance(UFile? solutionPath)
    {
        if (solutionPath == null)
            return null;

        var solutionFileName = solutionPath.GetFileNameWithoutExtension();
        var solutionFullPath = solutionPath.ToOSPath();
        var solutionDir = solutionPath.GetFullDirectory().ToOSPath();

        foreach (var procName in IdeProcessNames)
        {
            try
            {
                var processes = Process.GetProcessesByName(procName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (OperatingSystem.IsLinux())
                        {
                            var cmdLinePath = $"/proc/{process.Id}/cmdline";
                            if (File.Exists(cmdLinePath))
                            {
                                var cmdLine = File.ReadAllText(cmdLinePath);
                                if (cmdLine.Contains(solutionFullPath, StringComparison.OrdinalIgnoreCase) ||
                                    cmdLine.Contains(solutionDir, StringComparison.OrdinalIgnoreCase))
                                {
                                    return process;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(process.MainWindowTitle) &&
                            process.MainWindowTitle.StartsWith(solutionFileName, StringComparison.OrdinalIgnoreCase))
                        {
                            return process;
                        }
                    }
                    catch
                    {
                        // Ignore individual process access errors
                    }
                }
            }
            catch
            {
                // Ignore process enumeration failures
            }
        }

        return null;
    }

    public static async Task<Process?> GetInstance(SessionViewModel session, bool makeActive)
    {
        if (session?.SolutionPath == null || !await CodeEditorOpenerService.CheckCanOpenSolution(session, IDEInfo.DefaultIDE))
            return null;

        try
        {
            var process = FindVisualStudioInstance(session.SolutionPath);

            if (process != null && makeActive)
            {
                // Active window activation can be supported on platforms with windowing services
            }

            return process;
        }
        catch (Exception e)
        {
            e.Ignore();
            return null;
        }
    }
}
