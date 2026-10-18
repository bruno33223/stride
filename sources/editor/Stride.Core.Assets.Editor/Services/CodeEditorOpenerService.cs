// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.CodeEditorSupport;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Services;

public static class CodeEditorOpenerService
{
    public static async Task<bool> StartOrToggle(SessionViewModel session, IDEInfo ideInfo)
    {
        if (!await CheckCanOpenSolution(session, ideInfo) && ideInfo?.IDEType != IDEType.VSCode)
            return false;

        Process? process = await StartInstance(session, ideInfo);
        return process != null;
    }

    public static async Task<Process?> StartInstance(SessionViewModel session, IDEInfo? ideInfo)
    {
        if (!await CheckCanOpenSolution(session, ideInfo))
            return null;

        var startInfo = BuildProcessStartInfo(session.SolutionPath!, ideInfo);

        try
        {
            return Process.Start(startInfo);
        }
        catch
        {
            var dialogService = session.ServiceProvider.TryGet<IDialogService>();
            if (dialogService != null)
            {
                await dialogService.MessageBoxAsync(string.Format(Tr._p("Message", "An error occurred while starting {0}."), ideInfo?.IDEType), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return null;
        }
    }

    public static ProcessStartInfo BuildProcessStartInfo(string solutionPath, IDEInfo? ideInfo)
    {
        return BuildProcessStartInfo(new UFile(solutionPath), ideInfo);
    }

    public static ProcessStartInfo BuildProcessStartInfo(UFile solutionPath, IDEInfo? ideInfo)
    {
        var startInfo = new ProcessStartInfo();
        if (ideInfo == null)
        {
            var defaultIDEName = EditorSettings.DefaultIDE.GetValue();

            if (!EditorSettings.DefaultIDE.GetAcceptableValues().Contains(defaultIDEName))
                defaultIDEName = EditorSettings.DefaultIDE.DefaultValue;

            IEnumerable<IDEInfo> ides = IDEInfoVersions.AvailableIDEs();
            ideInfo = ides.FirstOrDefault(x => x.DisplayName == defaultIDEName) ?? IDEInfo.DefaultIDE;
        }

        string ideArguments = ideInfo.IDEType == IDEType.VSCode ? solutionPath.GetFullDirectory() : solutionPath.FullPath;

        var resolvedPath = ResolveExecutablePath(ideInfo.ProgramPath);

        if (resolvedPath == null)
        {
            if (ideInfo.IDEType == IDEType.VSCode)
            {
                resolvedPath = ResolveExecutablePath("code") ?? ResolveExecutablePath("codium");
            }
            else if (ideInfo.IDEType == IDEType.Rider)
            {
                resolvedPath = ResolveExecutablePath("rider");
            }
        }

        if (resolvedPath != null)
        {
            startInfo.FileName = resolvedPath;
            startInfo.Arguments = $"\"{ideArguments}\"";
            startInfo.CreateNoWindow = ideInfo.IDEType == IDEType.VSCode;
            startInfo.UseShellExecute = false;
        }
        else if (ideInfo.ProgramPath != null)
        {
            startInfo.FileName = ideInfo.ProgramPath;
            startInfo.Arguments = $"\"{ideArguments}\"";
            startInfo.CreateNoWindow = ideInfo.IDEType == IDEType.VSCode;
            startInfo.UseShellExecute = false;
        }
        else
        {
            startInfo.FileName = new UFile(solutionPath).ToOSPath();
            startInfo.UseShellExecute = true;
        }

        return startInfo;
    }

    public static string? ResolveExecutablePath(string? programPathOrName)
    {
        if (string.IsNullOrWhiteSpace(programPathOrName))
            return null;

        if (File.Exists(programPathOrName))
            return Path.GetFullPath(programPathOrName);

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var directories = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var directory in directories)
            {
                var candidate = Path.Combine(directory, programPathOrName);
                if (File.Exists(candidate))
                    return candidate;

                if (OperatingSystem.IsWindows())
                {
                    if (File.Exists(candidate + ".exe"))
                        return candidate + ".exe";
                    if (File.Exists(candidate + ".cmd"))
                        return candidate + ".cmd";
                    if (File.Exists(candidate + ".bat"))
                        return candidate + ".bat";
                }
            }
        }

        if (OperatingSystem.IsLinux())
        {
            string[] commonLinuxDirs = ["/usr/bin", "/usr/local/bin", "/snap/bin", "/var/lib/flatpak/exports/bin"];
            foreach (var directory in commonLinuxDirs)
            {
                var candidate = Path.Combine(directory, programPathOrName);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    internal static async Task<bool> CheckCanOpenSolution(SessionViewModel session, IDEInfo? ideInfo)
    {
        if (string.IsNullOrEmpty(session.SolutionPath))
        {
            var dialogService = session.ServiceProvider.TryGet<IDialogService>();
            if (dialogService != null)
            {
                await dialogService.MessageBoxAsync(string.Format(Tr._p("Message", "The session currently open is not a {0} session."), ideInfo?.IDEType), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return false;
        }
        return true;
    }
}
