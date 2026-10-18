// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia.Views
{
    /// <summary>
    /// Interaction logic for ProjectSelectionWindow.axaml
    /// </summary>
    public partial class ProjectSelectionWindow : Window, IModalDialog
    {
        public ProjectSelectionWindow()
        {
            InitializeComponent();
            if (Screens?.Primary is { } s)
            {
                Width = Math.Min(Width, s.WorkingArea.Width / s.Scaling);
                Height = Math.Min(Height, s.WorkingArea.Height / s.Scaling);
            }
            Title = string.Format(Tr._p("Title", "Project selection - {0}"), EditorPath.EditorTitle);
        }

        public NewSessionParameters? NewSessionParameters { get; private set; }

        public UFile? ExistingSessionPath { get; private set; }

        public NewOrOpenSessionTemplateCollectionViewModel? Templates
        {
            get => DataContext as NewOrOpenSessionTemplateCollectionViewModel;
            set => DataContext = value;
        }

        public DialogResult Result { get; set; } = DialogResult.Cancel;

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (Result == DialogResult.Ok)
            {
                if (!ValidateProperties())
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (Result == DialogResult.Ok && Templates != null)
            {
                var recentProject = Templates.SelectedTemplate as ExistingProjectViewModel;
                var newPackageTemplate = Templates.SelectedTemplate as TemplateDescriptionViewModel;
                if (recentProject != null)
                {
                    ExistingSessionPath = recentProject.Path;
                }
                else if (newPackageTemplate != null)
                {
                    NewSessionParameters = new NewSessionParameters
                    {
                        TemplateDescription = Templates.SelectedTemplate != null ? newPackageTemplate.GetTemplate() : null,
                        OutputName = Templates.Name,
                        OutputDirectory = Templates.Location,
                        SolutionName = Templates.SolutionName,
                        SolutionLocation = Templates.SolutionLocation,
                    };
                }
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (Result == DialogResult.Ok && Templates != null)
            {
                EditorSettings.Save();
                if (Templates.Location != null)
                {
                    InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.SetValue(Templates.Location.FullPath);
                }
                InternalSettings.Save();
            }
        }

        public bool ValidateProperties()
        {
            if (Templates == null)
                return false;

            if (!Templates.ValidateProperties(out string error))
            {
                var dialogService = Templates.ServiceProvider?.TryGet<IDialogService>();
                dialogService?.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void OnSelectClick(object? sender, RoutedEventArgs e)
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
