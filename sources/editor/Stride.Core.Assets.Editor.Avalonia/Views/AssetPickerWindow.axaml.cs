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
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using SessionObjectViewModel = Stride.Core.Assets.Presentation.ViewModels.SessionObjectViewModel;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for AssetPickerWindow.axaml
    /// </summary>
    public partial class AssetPickerWindow : Window, IAssetPickerDialog, IModalDialog
    {
        public static readonly StyledProperty<string> MessageProperty =
            AvaloniaProperty.Register<AssetPickerWindow, string>(nameof(Message), string.Empty);
        private static readonly List<SessionObjectViewModel> ExpandedObjects = new List<SessionObjectViewModel>();
        private readonly List<AssetViewModel> selectedAssets = new List<AssetViewModel>();
        private DialogResult Result = DialogResult.Cancel;

        public AssetPickerWindow() : this(null!) { }

        public AssetPickerWindow(SessionViewModel session)
        {
            InitializeComponent();
            Session = session;
            AcceptedTypes = new List<Type>();
            AssetDoubleClickCommand = new AnonymousCommand(session?.ServiceProvider!, OnAssetDoubleClick);
            DataContext = this;
            Loaded += AssetPickerWindowLoaded;
            if (Screens?.Primary is { } s)
            {
                Width = Math.Min(Width, s.WorkingArea.Width / s.Scaling);
                Height = Math.Min(Height, s.WorkingArea.Height / s.Scaling);
            }
        }

        private void AssetPickerWindowLoaded(object? sender, RoutedEventArgs e)
        {
            var initialDirectory = InitialLocation ?? Session?.LocalPackages.FirstOrDefault()?.AssetMountPoint;
            if (initialDirectory != null && DirectoryTreeView != null)
                DirectoryTreeView.SelectedItem = initialDirectory;

            if (InitialAsset != null && AssetView != null)
                AssetView.SelectAssets([InitialAsset]);
        }

        public string Message { get => GetValue(MessageProperty); set => SetValue(MessageProperty, value); }
        public DirectoryBaseViewModel? InitialLocation { get; set; }
        public AssetViewModel? InitialAsset { get; set; }
        public bool AllowMultiSelection { get; set; }
        public List<Type> AcceptedTypes { get; }
        public IReadOnlyCollection<AssetViewModel> SelectedAssets => selectedAssets;
        public SessionViewModel Session { get; }
        public AssetCollectionViewModel AssetView { get; set; } = null!;
        public ICommandBase AssetDoubleClickCommand { get; private set; }
        public Func<AssetViewModel, bool>? Filter { get => AssetView?.CustomFilter; set { if (AssetView != null) AssetView.CustomFilter = value; } }

        public async Task<DialogResult> ShowModal()
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return await Dispatcher.UIThread.InvokeAsync(ShowModal);

            if (AcceptedTypes.Count > 0 && AssetView != null)
            {
                var assetTypes = AssetRegistry.GetPublicTypes().Where(x => AcceptedTypes.Any(y => y.IsAssignableFrom(x))).ToList();
                var activeFilters = AssetView.TypeFilters.Where(f => assetTypes.Any(t => string.Equals(t.FullName, f.Filter)));
                foreach (var filter in activeFilters)
                {
                    filter.IsReadOnly = true;
                    AssetView.AddAssetFilter(filter);
                }
            }
            selectedAssets.Clear();

            var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var owner = desktop?.MainWindow;
            if (owner is not null && owner != this && owner.IsVisible)
                await ShowDialog(owner);
            else
            {
                var tcs = new TaskCompletionSource<bool>();
                Closed += (_, _) => tcs.TrySetResult(true);
                try { Show(); } catch { return Result; }
                await tcs.Task;
            }

            if (Result == DialogResult.Ok && AssetView != null)
                selectedAssets.AddRange(AssetView.SelectedAssets);

            return Result;
        }

        public void RequestClose(DialogResult result) { Result = result; Close(); }
        private void OnOkClick(object? sender, RoutedEventArgs e) => RequestClose(DialogResult.Ok);
        private void OnCancelClick(object? sender, RoutedEventArgs e) => RequestClose(DialogResult.Cancel);

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            lock (ExpandedObjects)
            {
                ExpandedObjects.Clear();
                var items = DirectoryTreeView?.GetVisualDescendants().OfType<TreeViewItem>();
                if (items != null)
                {
                    foreach (var obj in items.Where(x => x.IsExpanded).Select(x => x.DataContext).OfType<SessionObjectViewModel>())
                        ExpandedObjects.Add(obj);
                }
            }
            base.OnClosing(e);
        }

        private void OnAssetDoubleClick()
        {
            var folder = AssetView?.SingleSelectedContent as DirectoryViewModel;
            if (folder != null && AssetView != null)
            {
                AssetView.SelectedLocations.Clear();
                AssetView.SelectedLocations.Add(folder);
            }
            var asset = AssetView?.SingleSelectedAsset;
            if (asset != null)
            {
                Result = DialogResult.Ok;
                Close();
            }
        }
    }
}
