// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.DebugTools.UndoRedo;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services
{
    public static class EditorDebugTools
    {
        internal static readonly List<IDebugPage> DebugPages = new List<IDebugPage>();
        private static readonly List<DebugWindowViewModel> DebugWindows = new List<DebugWindowViewModel>();

        public static void RegisterDebugPage(IDebugPage page)
        {
            lock (DebugPages)
            {
                DebugPages.Add(page);
                foreach (var debugWindow in DebugWindows)
                {
                    debugWindow.Pages.Add(page);
                }
            }
        }

        public static void UnregisterDebugPage(IDebugPage page)
        {
            lock (DebugPages)
            {
                DebugPages.Remove(page);
                foreach (var debugWindow in DebugWindows)
                {
                    debugWindow.Pages.Remove(page);
                }
                var disposable = page as IDestroyable;
                disposable?.Destroy();
            }
        }

        public static IDebugPage CreateLogDebugPage(IViewModelServiceProvider serviceProvider, Logger logger, string title, bool register = true)
        {
            var dispatcher = serviceProvider.Get<IDispatcherService>();
            dispatcher.EnsureAccess();
            // Activate all log levels
            logger.ActivateLog(LogMessageType.Debug);
            var page = new LoggerViewModel(serviceProvider, logger) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        }

        public static IDebugPage CreateLogDebugPage(Logger logger, string title, bool register = true)
        {
            return CreateLogDebugPage(SessionViewModel.Instance.ServiceProvider, logger, title, register);
        }

        public static IDebugPage CreateUndoRedoDebugPage(IViewModelServiceProvider serviceProvider, IUndoRedoService service, string title, bool register = true)
        {
            var dispatcher = serviceProvider.Get<IDispatcherService>();
            dispatcher.EnsureAccess();
            var page = new DebugUndoRedoViewModel(serviceProvider, service) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        }

        public static IDebugPage CreateUndoRedoDebugPage(IUndoRedoService service, string title, bool register = true)
        {
            return CreateUndoRedoDebugPage(SessionViewModel.Instance.ServiceProvider, service, title, register);
        }

        public static IDebugPage CreateAssetNodesDebugPage(SessionViewModel session, string title, bool register = true)
        {
            session.Dispatcher.EnsureAccess();
            var page = new DebugAssetNodeCollectionViewModel(session) { Title = title };
            if (register)
            {
                RegisterDebugPage(page);
            }
            return page;
        }

        public static void RegisterDebugWindow(DebugWindowViewModel debugWindow)
        {
            lock (DebugPages)
            {
                DebugWindows.Add(debugWindow);
                debugWindow.Pages.AddRange(DebugPages);
            }
        }

        public static void UnregisterDebugWindow(DebugWindowViewModel debugWindow)
        {
            lock (DebugPages)
            {
                debugWindow.Pages.Clear();
                DebugWindows.Remove(debugWindow);
            }
        }
    }
}
