// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for ItemTemplatesWindow.axaml
    /// </summary>
    public partial class ItemTemplatesWindow : Window, IItemTemplateDialog, IModalDialog
    {
        public ItemTemplatesWindow() : this(null!)
        {
        }

        public ItemTemplatesWindow(AssetTemplatesViewModel? viewModel)
        {
            InitializeComponent();
            if (viewModel != null)
            {
                ViewModel = viewModel;
            }
            Loaded += OnLoaded;
            KeyDown += OnKeyDown;
            if (Screens?.Primary is { } s)
            {
                Width = Math.Min(Width, s.WorkingArea.Width / s.Scaling);
                Height = Math.Min(Height, s.WorkingArea.Height / s.Scaling);
            }
        }

        public AssetTemplatesViewModel ViewModel
        {
            get => (AssetTemplatesViewModel)DataContext!;
            set => DataContext = value;
        }

        public ITemplateDescriptionViewModel? SelectedTemplate { get; private set; }

        public DialogResult Result { get; set; } = DialogResult.Cancel;

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Validate();
                e.Handled = true;
            }
        }

        private void TemplateListBox_OnDoubleTapped(object? sender, TappedEventArgs e)
        {
            Validate();
        }

        private void Validate()
        {
            if (ViewModel?.SelectedTemplate.Key != null)
            {
                SelectedTemplate = ViewModel.SelectedTemplate.Key;
                Result = DialogResult.Ok;
            }
            else
            {
                SelectedTemplate = null;
                Result = DialogResult.Cancel;
            }
            Close();
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            Activate();
            if (TemplateListBox != null && TemplateListBox.ItemCount > 0)
            {
                TemplateListBox.SelectedIndex = 0;
            }
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
