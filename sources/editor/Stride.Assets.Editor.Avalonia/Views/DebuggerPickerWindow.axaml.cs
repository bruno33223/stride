// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.CodeEditorSupport;
using Stride.Core.CodeEditorSupport.VisualStudio;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Avalonia.Views
{
    public partial class DebuggerPickerWindow : Window, INotifyPropertyChanged, IDebuggerPickerDialog, IModalDialog
    {
        private readonly List<Process> runningInstances;
        private IPickedDebugger? selectedDebugger;
        private DialogResult modalResult = DialogResult.Cancel;

        public class Debugger : IPickedDebugger
        {
            private readonly IDEInfo? ideInfo;
            private Process? process;

            public Debugger(IDEInfo ideInfo) => this.ideInfo = ideInfo;
            public Debugger(Process process) => this.process = process;

            public string Name => process?.MainWindowTitle ?? $"New instance of {ideInfo?.DisplayName}";

            public async Task<Process?> Launch(SessionViewModel session)
            {
                if (process == null && ideInfo != null)
                {
                    process = await CodeEditorOpenerService.StartInstance(session, ideInfo);
                    process?.WaitForInputIdle();
                }
                return process;
            }
        }

        public DebuggerPickerWindow() : this(Enumerable.Empty<Process>()) { }

        public DebuggerPickerWindow(IEnumerable<Process> runningInstances)
        {
            this.runningInstances = runningInstances.ToList();
            InitializeComponent();
            DataContext = this;
            Debuggers = new ObservableList<Debugger>();
        }

        public ObservableList<Debugger> Debuggers { get; private set; }

        public IPickedDebugger? SelectedDebugger { get => selectedDebugger; set { selectedDebugger = value; OnPropertyChanged(nameof(SelectedDebugger), nameof(SelectionValid)); } }

        public bool SelectionValid => IsSelectionValid();

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(params string[] propertyNames)
        {
            var handler = PropertyChanged;
            if (handler != null)
                foreach (var name in propertyNames) handler(this, new PropertyChangedEventArgs(name));
        }

        private bool IsSelectionValid() => SelectedDebugger != null;

        public void RequestClose(DialogResult result)
        {
            modalResult = result;
            if (modalResult != DialogResult.Ok) SelectedDebugger = null;
            Close();
        }

        private void OnOkClick(object? sender, RoutedEventArgs e) { if (SelectionValid) RequestClose(DialogResult.Ok); }
        private void OnCancelClick(object? sender, RoutedEventArgs e) => RequestClose(DialogResult.Cancel);

        public async Task<DialogResult> ShowModal()
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return await Dispatcher.UIThread.InvokeAsync(ShowModal);

            Debuggers.Clear();
            Debuggers.AddRange(VisualStudioVersions.AvailableInstances.Select(ideInfo => new Debugger(ideInfo)));
            Debuggers.AddRange(runningInstances.Select(process => new Debugger(process)));

            var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner != null && owner != this && owner.IsVisible)
                await ShowDialog(owner);
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                Closed += (_, _) => tcs.TrySetResult(true);
                try { Show(); } catch { return modalResult; }
                await tcs.Task;
            }

            if (modalResult != DialogResult.Ok) SelectedDebugger = null;
            return modalResult;
        }
    }
}
