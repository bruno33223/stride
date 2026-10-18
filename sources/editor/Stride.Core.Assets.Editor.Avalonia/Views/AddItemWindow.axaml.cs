// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for AddItemWindow.axaml
    /// </summary>
    public partial class AddItemWindow : Window, IItemTemplateDialog, IModalDialog
    {
        private bool validated;

        public AddItemWindow() : this(new ViewModelServiceProvider(), null!)
        {
        }

        public AddItemWindow(IViewModelServiceProvider serviceProvider, TemplateDescriptionCollectionViewModel? templateDescriptions)
        {
            DataContext = templateDescriptions;
            InitializeComponent();
            AddItemCommand = new AnonymousCommand<ITemplateDescriptionViewModel>(serviceProvider, ValidateSelectedTemplate);
            if (Screens?.Primary is { } s)
            {
                Width = Math.Min(Width, s.WorkingArea.Width / s.Scaling);
                Height = Math.Min(Height, s.WorkingArea.Height / s.Scaling);
            }
        }

        public AddItemTemplateCollectionViewModel? TemplateDescriptions => DataContext as AddItemTemplateCollectionViewModel;

        public ICommand AddItemCommand { get; }

        public ITemplateDescriptionViewModel? SelectedTemplate { get; private set; }

        public DialogResult Result { get; set; } = DialogResult.Cancel;

        private void ValidateSelectedTemplate(ITemplateDescriptionViewModel? template)
        {
            if (validated)
                return;

            validated = true;
            SelectedTemplate = template;
            Result = SelectedTemplate != null ? DialogResult.Ok : DialogResult.Cancel;
            Close();
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
