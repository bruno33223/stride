// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Services;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class FixAssetReferencesWindow : Window, IFixReferencesDialog
{
    private Stride.Core.Presentation.Services.DialogResult modalResult = Stride.Core.Presentation.Services.DialogResult.Cancel;

    public FixAssetReferencesWindow()
    {
        InitializeComponent();
    }

    public FixAssetReferencesWindow(FixAssetReferencesViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }

    public FixAssetReferencesViewModel? ViewModel
    {
        get => DataContext as FixAssetReferencesViewModel;
        set => DataContext = value;
    }

    public bool? DialogResult => modalResult == Stride.Core.Presentation.Services.DialogResult.Ok;

    public async Task<Stride.Core.Presentation.Services.DialogResult> ShowModal()
    {
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

        return modalResult;
    }

    public void RequestClose(Stride.Core.Presentation.Services.DialogResult result)
    {
        modalResult = result;
        Close(result == Stride.Core.Presentation.Services.DialogResult.Ok);
    }

    public void ApplyReferenceFixes()
    {
        ViewModel?.ApplyFixes();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestClose(Stride.Core.Presentation.Services.DialogResult.Cancel);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private void OnClick_Ok(object? sender, RoutedEventArgs e)
    {
        ApplyReferenceFixes();
        RequestClose(Stride.Core.Presentation.Services.DialogResult.Ok);
    }

    private void OnClick_Cancel(object? sender, RoutedEventArgs e)
    {
        RequestClose(Stride.Core.Presentation.Services.DialogResult.Cancel);
    }
}
