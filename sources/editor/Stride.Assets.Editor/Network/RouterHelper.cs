// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Packages;
using Stride.Engine.Network;

namespace Stride.ConnectionRouter
{
    public static class RouterHelper
    {
        private static string VersionWithoutSpecialPart(string version)
        {
            var indexOfDash = version.IndexOf('-');
            if (indexOfDash == -1)
                return version;

            return version.Substring(0, indexOfDash);
        }

        public static bool EnsureRouterLaunched(bool attachChildJob = false, bool checkIfPortOpen = true)
        {
            try
            {
                // Try to connect to router
                FileVersionInfo? runningRouterVersion = null;
                Process? runningRouterProcess = null;
                foreach (var process in Process.GetProcessesByName("Stride.ConnectionRouter"))
                {
                    try
                    {
                        runningRouterVersion = process.MainModule?.FileVersionInfo;
                        runningRouterProcess = process;
                        break;
                    }
                    catch (Exception)
                    {
                    }
                }

                string? routerAssemblyLocation = null;
                try
                {
                    var package = PackageStore.Instance.FindLocalPackage("Stride.ConnectionRouter", new PackageVersionRange(new PackageVersion(StrideVersion.NuGetVersion)));
                    if (package != null)
                    {
                        var exeName = OperatingSystem.IsWindows() ? "Stride.ConnectionRouter.exe" : "Stride.ConnectionRouter";
                        routerAssemblyLocation = package.GetFiles().FirstOrDefault(x => string.Compare(Path.GetFileName(x.Path), exeName, true) == 0)?.FullPath;
                    }
                }
                catch
                {
                }

                if (routerAssemblyLocation == null)
                {
                    var exeName = OperatingSystem.IsWindows() ? "Stride.ConnectionRouter.exe" : "Stride.ConnectionRouter";
                    var candidate = Path.Combine(AppContext.BaseDirectory, exeName);
                    if (File.Exists(candidate))
                        routerAssemblyLocation = candidate;
                }

                if (runningRouterVersion != null && routerAssemblyLocation != null)
                {
                    var routerAssemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(routerAssemblyLocation);
                    if (new PackageVersion(routerAssemblyFileVersionInfo.FileVersion) <= new PackageVersion(runningRouterVersion.FileVersion))
                        return true;
                }

                if (runningRouterProcess != null)
                {
                    runningRouterProcess.Kill();
                    runningRouterProcess.WaitForExit();
                }

                if (routerAssemblyLocation == null || !File.Exists(routerAssemblyLocation))
                    return false;

                var spawnedRouterProcess = Process.Start(routerAssemblyLocation);

                if (checkIfPortOpen)
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        for (int i = 0; i < 10; ++i)
                        {
                            try
                            {
                                socket.Connect("localhost", RouterClient.DefaultPort);
                            }
                            catch (SocketException)
                            {
                                Thread.Sleep(500);
                                continue;
                            }
                            break;
                        }
                    }
                }

                return spawnedRouterProcess != null;
            }
            catch
            {
                return false;
            }
        }

        public static void ParseUrl(string url, out string[] segments, out string parameters)
        {
            var parameterIndex = url.IndexOf('?');
            parameters = parameterIndex != -1 ? url.Substring(parameterIndex + 1) : string.Empty;
            var urlWithoutParameters = parameterIndex != -1 ? url.Substring(0, parameterIndex) : url;
            segments = urlWithoutParameters.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            return System.Web.HttpUtility.ParseQueryString(query);
        }
    }
}
