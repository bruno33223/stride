// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for UpdatePlatformsWindow.axaml
/// </summary>
public partial class UpdatePlatformsWindow : Window, IUpdatePlatformsDialog, IModalDialog, INotifyPropertyChanged
{
    private readonly ViewModelServiceProvider services;
    private DialogResult modalResult = DialogResult.Cancel;
    private bool forcePlatformRegenerationVisible = true;
    private bool forcePlatformRegeneration;

    public UpdatePlatformsWindow() : this(Array.Empty<PlatformType>(), true) { }

    public UpdatePlatformsWindow(ICollection<PlatformType> installedPlatforms, bool forcePlatformRegenerationVisible = true)
    {
        ArgumentNullException.ThrowIfNull(installedPlatforms);

        var dispatcher = new NullDispatcherService();
        services = new ViewModelServiceProvider(new object[] { dispatcher });

        AvailablePlatforms = new List<SolutionPlatformViewModel>();
        foreach (var platform in AssetRegistry.SupportedPlatforms)
        {
            var isInstalled = installedPlatforms.Contains(platform.Type);
            var solutionPlatform = new SolutionPlatformViewModel(services, platform, isInstalled, isInstalled);
            AvailablePlatforms.Add(solutionPlatform);
        }

        this.forcePlatformRegenerationVisible = forcePlatformRegenerationVisible;
        InitializeComponent();
        DataContext = this;
    }

    public List<SolutionPlatformViewModel> AvailablePlatforms { get; }

    IReadOnlyList<SolutionPlatformViewModel> IUpdatePlatformsDialog.AvailablePlatforms => AvailablePlatforms;

    public bool ForcePlatformRegenerationVisible
    {
        get => forcePlatformRegenerationVisible;
        set => SetField(ref forcePlatformRegenerationVisible, value);
    }

    public bool ForcePlatformRegeneration
    {
        get => forcePlatformRegeneration;
        set => SetField(ref forcePlatformRegeneration, value);
    }

    public IEnumerable<SelectedSolutionPlatform> SelectedPlatforms
    {
        get { return AvailablePlatforms.Where(x => x.IsSelected).Select(x => new SelectedSolutionPlatform(x.SolutionPlatform, x.SelectedTemplate)); }
    }

    public DialogResult Result => modalResult;

    public async Task<bool> ValidateAsync()
    {
        if (!SelectedPlatforms.Any())
        {
            var dialogService = services.TryGet<IDialogService>() ?? SessionViewModel.Instance?.ServiceProvider?.TryGet<IDialogService>();
            if (dialogService != null)
            {
                await dialogService.MessageBoxAsync(Tr._p("Message", "You must select at least one platform."), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return false;
        }

        if (AvailablePlatforms.Any(x => x.MarkedToRemove))
        {
            var buttons = DialogHelper.CreateButtons(new[]
            {
                Tr._p("Button", "Remove"),
                Tr._p("Button", "Cancel")
            }, 1, 2);
            var msg = string.Format(Tr._p("Message", "Are you sure you want to remove these {0} platform(s) from the package?"), AvailablePlatforms.Count(x => x.MarkedToRemove));
            var dialogService = services.TryGet<IDialogService>() ?? SessionViewModel.Instance?.ServiceProvider?.TryGet<IDialogService>();
            if (dialogService != null)
            {
                var result = await dialogService.MessageBoxAsync(msg, buttons, MessageBoxImage.Question);
                if (result != 1)
                    return false;
            }

            RequestClose(DialogResult.Ok);
            return true;
        }

        RequestClose(DialogResult.Ok);
        return true;
    }

    public void RequestClose(DialogResult result)
    {
        modalResult = result;
        Close();
    }

    public void Cancel()
    {
        RequestClose(DialogResult.Cancel);
    }

    private void ButtonOk(object? sender, RoutedEventArgs e)
    {
        _ = ValidateAsync();
    }

    private void ButtonCancel(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }

    public async Task<DialogResult> ShowModal()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return await Dispatcher.UIThread.InvokeAsync(ShowModal);

        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner != null && owner != this && owner.IsVisible)
        {
            await ShowDialog(owner);
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            Closed += (_, _) => tcs.TrySetResult(true);
            try
            {
                Show();
            }
            catch
            {
                return modalResult;
            }
            await tcs.Task;
        }

        return modalResult;
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
