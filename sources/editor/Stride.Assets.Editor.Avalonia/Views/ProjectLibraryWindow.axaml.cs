// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Assets.Editor.Services;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for ProjectLibraryWindow.axaml
/// </summary>
public partial class ProjectLibraryWindow : Window, IProjectLibraryDialog, IModalDialog, INotifyPropertyChanged
{
    private readonly IViewModelServiceProvider services;
    private bool hasError;
    private string libraryName = string.Empty;
    private string namespaceName = string.Empty;
    private Func<string, bool>? libNameInputValidator;
    private DialogResult modalResult = DialogResult.Cancel;

    public ProjectLibraryWindow() : this(string.Empty) { }

    public ProjectLibraryWindow(string defaultLibraryName)
    {
        var dispatcher = new NullDispatcherService();
        services = new ViewModelServiceProvider(new object[] { dispatcher });

        libraryName = defaultLibraryName ?? string.Empty;
        namespaceName = defaultLibraryName ?? string.Empty;

        InitializeComponent();
        DataContext = this;

        var libBox = this.FindControl<TextBox>("LibBox");
        if (libBox != null)
        {
            libBox.TextChanged += (sender, args) =>
            {
                if (libNameInputValidator != null && libBox.Text != null)
                {
                    HasError = libNameInputValidator(libBox.Text);
                }
            };
        }
    }

    public bool HasError
    {
        get => hasError;
        set => SetField(ref hasError, value);
    }

    public Func<string, bool> LibNameInputValidator
    {
        get => libNameInputValidator ?? (_ => false);
        set => libNameInputValidator = value;
    }

    public string LibraryName
    {
        get => libraryName;
        set => SetField(ref libraryName, value);
    }

    public string Namespace
    {
        get => namespaceName;
        set => SetField(ref namespaceName, value);
    }

    public DialogResult Result
    {
        get => modalResult;
        set => modalResult = value;
    }

    public void RequestClose(DialogResult result)
    {
        modalResult = result;
        Close();
    }

    private void ButtonOk(object? sender, RoutedEventArgs e)
    {
        _ = ValidateAndCloseAsync();
    }

    private async Task ValidateAndCloseAsync()
    {
        if (HasError)
            return;

        if (!NamingHelper.IsValidNamespace(LibraryName, out var error))
        {
            var dialogService = services.TryGet<IDialogService>() ?? SessionViewModel.Instance?.ServiceProvider?.TryGet<IDialogService>();
            if (dialogService != null)
            {
                await dialogService.MessageBoxAsync(string.Format(Tr._p("Message", "Type a valid library name. Error with {0}"), error), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return;
        }

        if (!NamingHelper.IsValidNamespace(Namespace, out error))
        {
            var dialogService = services.TryGet<IDialogService>() ?? SessionViewModel.Instance?.ServiceProvider?.TryGet<IDialogService>();
            if (dialogService != null)
            {
                await dialogService.MessageBoxAsync(string.Format(Tr._p("Message", "Type a valid namespace name. Error with {0}"), error), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return;
        }

        RequestClose(DialogResult.Ok);
    }

    private void ButtonCancel(object? sender, RoutedEventArgs e)
    {
        RequestClose(DialogResult.Cancel);
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
