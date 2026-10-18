// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using NewProjectTemplateCollectionViewModel = Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.NewProjectTemplateCollectionViewModel;

namespace Stride.GameStudio.Avalonia.Views;

public partial class NewProjectWindow : Window, INewProjectDialog
{
    private DialogResult dialogResult = DialogResult.Cancel;

    public NewProjectWindow()
    {
        InitializeComponent();
    }

    public UDirectory DefaultOutputDirectory { get; set; } = string.Empty;

    public NewPackageParameters Parameters { get; private set; } = new();

    private NewProjectTemplateCollectionViewModel? ViewModel => DataContext as NewProjectTemplateCollectionViewModel;

    public async Task<DialogResult> ShowModal()
    {
        if (!string.IsNullOrWhiteSpace(DefaultOutputDirectory) && ViewModel is not null)
        {
            ViewModel.Location = DefaultOutputDirectory;
        }

        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var owner = desktop?.MainWindow;

        if (owner is not null && owner != this && owner.IsVisible)
        {
            await ShowDialog(owner);
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            Closed += (_, _) => tcs.TrySetResult(true);
            Show();
            await tcs.Task;
        }

        return dialogResult;
    }

    public void RequestClose(DialogResult result)
    {
        dialogResult = result;
        Close();
    }

    public void ValidateAndConfirm()
    {
        if (ViewModel?.SelectedTemplate is null)
            return;

        if (!ValidateProperties())
            return;

        dialogResult = DialogResult.Ok;
        Parameters = new NewPackageParameters
        {
            TemplateDescription = ViewModel.SelectedTemplate.GetTemplate(),
            OutputName = ViewModel.Name,
            OutputDirectory = ViewModel.Location,
        };
        Close();
    }

    private void SelectButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ValidateAndConfirm();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RequestClose(DialogResult.Cancel);
    }

    private void TemplateBrowser_OnTemplateConfirmed(object? sender, EventArgs e)
    {
        ValidateAndConfirm();
    }

    private bool ValidateProperties()
    {
        if (ViewModel is null)
            return false;

        if (!ViewModel.ValidateProperties(out var error))
        {
            if (!string.IsNullOrEmpty(error))
            {
                var dialogService = ViewModel.ServiceProvider.TryGet<IDialogService>();
                dialogService?.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }

        return true;
    }
}
