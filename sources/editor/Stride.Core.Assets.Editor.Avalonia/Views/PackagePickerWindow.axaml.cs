// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for PackagePickerWindow.axaml
    /// </summary>
    public partial class PackagePickerWindow : Window, IPackagePickerDialog, IModalDialog
    {
        private readonly SessionViewModel? session;
        private readonly List<PickablePackageViewModel> selectedPackages = new();
        private DialogResult Result = DialogResult.Cancel;

        public PackagePickerWindow() : this(null) { }

        public PackagePickerWindow(SessionViewModel? session)
        {
            this.session = session;
            InitializeComponent();
            Packages = new List<PickablePackageViewModel>();
            DataContext = this;
            if (Screens?.Primary is { } s)
            {
                Width = Math.Min(Width, s.WorkingArea.Width / s.Scaling);
                Height = Math.Min(Height, s.WorkingArea.Height / s.Scaling);
            }
        }

        public List<PickablePackageViewModel> Packages { get; set; }
        public bool AllowMultiSelection { get; set; }
        public Func<PickablePackageViewModel, bool>? Filter { get; set; }
        IReadOnlyCollection<PickablePackageViewModel> IPackagePickerDialog.SelectedPackages => selectedPackages;

        public void RequestClose(DialogResult result) { Result = result; Close(); }
        private void OnOkClick(object? s, RoutedEventArgs e) => RequestClose(DialogResult.Ok);
        private void OnCancelClick(object? s, RoutedEventArgs e) => RequestClose(DialogResult.Cancel);

        public async Task<DialogResult> ShowModal()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                return await Dispatcher.UIThread.InvokeAsync(ShowModal);
            }

            if (session != null)
                foreach (var pkg in await session.SuggestPackagesToAdd())
                    if (Filter == null || Filter(pkg)) Packages.Add(pkg);

            selectedPackages.Clear();
            var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner != null && owner != this && owner.IsVisible)
                await ShowDialog(owner);
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                Closed += (_, _) => tcs.TrySetResult(true);
                try { Show(); } catch { return Result; }
                await tcs.Task;
            }

            if (Result == DialogResult.Ok && PackageListBox?.SelectedItem is PickablePackageViewModel sel)
                selectedPackages.Add(sel);
            return Result;
        }
    }
}
