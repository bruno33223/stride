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
using Stride.Assets.Editor.Templates;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Interaction logic for ModelAssetTemplateWindow.axaml
/// </summary>
public partial class ModelAssetTemplateWindow : Window, IModelAssetTemplateDialog, IModalDialog, INotifyPropertyChanged
{
    private DialogResult modalResult = DialogResult.Cancel;
    private ImportModelFromFileViewModel parameters;

    public ModelAssetTemplateWindow()
    {
        var session = SessionViewModel.Instance;
        var viewModelService = session?.AssetNodeContainer != null ? new GraphViewModelService(session.AssetNodeContainer) : null;
        var services = session?.ServiceProvider != null
            ? (viewModelService != null ? new ViewModelServiceProvider(session.ServiceProvider, new object[] { viewModelService }) : session.ServiceProvider)
            : new ViewModelServiceProvider(new object[] { new NullDispatcherService() });

        parameters = new ImportModelFromFileViewModel(services);
        InitializeComponent();
        DataContext = this;
    }

    public ImportModelFromFileViewModel Parameters
    {
        get => parameters;
        set => SetField(ref parameters, value);
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
