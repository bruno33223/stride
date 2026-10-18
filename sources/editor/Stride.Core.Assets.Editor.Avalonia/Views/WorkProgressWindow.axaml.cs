// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for WorkProgressWindow.axaml
    /// </summary>
    public partial class WorkProgressWindow : Window, IModalDialog
    {
        private readonly WorkProgressViewModel workProgress;
        private bool callbackRegistered;

        public static IntPtr CurrentHandle;

        public WorkProgressWindow() : this(new WorkProgressViewModel(new ViewModelServiceProvider(), new LoggerResult()))
        {
        }

        public WorkProgressWindow(WorkProgressViewModel workProgress)
        {
            InitializeComponent();
            this.workProgress = workProgress ?? throw new ArgumentNullException(nameof(workProgress));
            DataContext = workProgress;
            Loaded += WindowLoaded;
            Closing += WindowClosing;
            if (Screens?.Primary is { } s)
            {
                Width = Math.Min(Width, s.WorkingArea.Width / s.Scaling);
                Height = Math.Min(Height, s.WorkingArea.Height / s.Scaling);
            }
        }

        public WorkProgressViewModel WorkProgress => workProgress;

        public DialogResult Result { get; set; } = DialogResult.Cancel;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (workProgress.WorkDone && !workProgress.ShouldStayOpen())
            {
                if (!IsLoaded)
                {
                    return;
                }
                Close();
                return;
            }

            if (!callbackRegistered)
            {
                workProgress.WorkFinished += WorkFinished;
                callbackRegistered = true;
            }
        }

        private void WindowLoaded(object? sender, RoutedEventArgs e)
        {
            if (workProgress.WorkDone && !workProgress.ShouldStayOpen())
            {
                Close();
                return;
            }
            DataContext = workProgress;
            CurrentHandle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            ShowInTaskbar = Owner == null;
        }

        private void WindowClosing(object? sender, WindowClosingEventArgs e)
        {
            // Prevent closing the window while the work is still in progress, unless it has been user cancelled.
            if (!workProgress.WorkDone && Result != DialogResult.Cancel)
            {
                e.Cancel = true;
            }
            else
            {
                CurrentHandle = IntPtr.Zero;
            }
        }

        private void WorkFinished(object? sender, WorkProgressNotificationEventArgs e)
        {
            if (!workProgress.ShouldStayOpen())
            {
                Dispatcher.UIThread.Post(Close);
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            RequestClose(DialogResult.Ok);
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            RequestClose(DialogResult.Cancel);
        }

        public async Task<DialogResult> ShowModal()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                return await Dispatcher.UIThread.InvokeAsync(ShowModal);
            }

            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var owner = desktop?.MainWindow;
            if (owner != null && owner != this && owner.IsVisible)
            {
                await ShowDialog(owner);
            }
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                Closed += (_, _) => tcs.TrySetResult(true);
                try { Show(); } catch { return Result; }
                await tcs.Task;
            }

            return Result;
        }

        public void RequestClose(DialogResult result)
        {
            Result = result;
            Close();
        }
    }
}
