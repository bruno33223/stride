// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Stride.Core.IO;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Services;

internal class EditorDialogService : DialogService, IEditorDialogService
{
    private DebugWindow? debugWindow;
    private readonly IViewModelServiceProvider serviceProvider;
    private readonly List<NotificationWindow> notificationWindows = new();

    public EditorDialogService(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider.Get<IDispatcherService>())
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task ShowAboutWindowAsync()
    {
        if (MainWindow is null) return;

        await Dispatcher.InvokeTask(async () =>
        {
            await new AboutWindow().ShowDialog(MainWindow);
        });
    }

    public async Task ShowDebugWindowAsync()
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (debugWindow is null)
            {
                debugWindow = new DebugWindow
                {
                    ViewModel = new DebugWindowViewModel(serviceProvider)
                };
                debugWindow.Show();
                debugWindow.Closed += (_, _) => debugWindow = null;
            }
            else
            {
                if (debugWindow.WindowState == WindowState.Minimized)
                {
                    debugWindow.WindowState = WindowState.Normal;
                }
                debugWindow.Activate();
            }
        });
    }

    public void ShowProgressWindow(WorkProgressViewModel workProgress)
    {
        if (!workProgress.WorkDone || workProgress.ShouldStayOpen())
        {
            Dispatcher.Invoke(() =>
            {
                var progressWindow = new ProgressWindow
                {
                    DataContext = workProgress
                };
                workProgress.WorkFinished += (_, _) =>
                {
                    if (!workProgress.ShouldStayOpen()) progressWindow.Close();
                };
                progressWindow.Closing += (_, e) =>
                {
                    if (!workProgress.WorkDone) e.Cancel = true;
                };
                progressWindow.Closed += (_, _) =>
                {
                    workProgress.NotifyWindowClosed();
                };
                workProgress.NotifyWindowWillOpen();

                if (MainWindow is not null) progressWindow.ShowDialog(MainWindow);
                else progressWindow.Show();
            });
        }
        else
        {
            workProgress.NotifyWindowClosed();
        }
    }

    public async Task ShowSettingsWindowAsync()
    {
        if (MainWindow is null) return;

        await Dispatcher.InvokeTask(async () =>
        {
            await new SettingsWindow
            {
                DataContext = new SettingsViewModel(serviceProvider, EditorSettings.SettingsContainer.CurrentProfile)
            }.ShowDialog(MainWindow);
        });
    }

    public void ShowNotificationWindow(string title, string message, ICommandBase? command = null, object? commandParameter = null)
    {
        Dispatcher.Invoke(() =>
        {
            var notificationWindow = new NotificationWindow(title, message, command, commandParameter);
            lock (notificationWindows)
            {
                notificationWindows.Add(notificationWindow);
            }

            notificationWindow.Closed += (_, _) =>
            {
                lock (notificationWindows)
                {
                    notificationWindows.Remove(notificationWindow);
                    UpdateNotificationPositions();
                }
            };

            notificationWindow.Show();
            UpdateNotificationPositions();
        });
    }

    public void CloseAllNotificationWindows()
    {
        Dispatcher.Invoke(() =>
        {
            NotificationWindow[] windowsToClose;
            lock (notificationWindows)
            {
                windowsToClose = notificationWindows.ToArray();
                notificationWindows.Clear();
            }

            foreach (var window in windowsToClose)
            {
                window.Close();
            }
        });
    }

    private void UpdateNotificationPositions()
    {
        lock (notificationWindows)
        {
            var targetScreen = (MainWindow is not null ? MainWindow.Screens.ScreenFromWindow(MainWindow) : null)
                               ?? (notificationWindows.Count > 0 ? notificationWindows[0].Screens.Primary : null);

            if (targetScreen is null) return;

            var scaling = targetScreen.Scaling;
            const int spacingDip = 8;
            var spacingPx = (int)(spacingDip * scaling);

            int currentOffsetPx = 0;
            for (int i = 0; i < notificationWindows.Count; i++)
            {
                var win = notificationWindows[i];
                win.SetPosition(currentOffsetPx);

                var heightDip = win.Bounds.Height > 0 ? win.Bounds.Height : (!double.IsNaN(win.Height) ? win.Height : 120);
                var heightPx = (int)Math.Ceiling(heightDip * scaling);
                currentOffsetPx += heightPx + spacingPx;
            }
        }
    }

    public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null)
    {
        var viewModel = new FixAssetReferencesViewModel(serviceProvider, session);
        if (assets is not null && assets.Count > 0)
        {
            viewModel.PopulateFromDeletedAssets(assets);
        }

        return new FixAssetReferencesWindow(viewModel);
    }

    public IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IEnumerable<FixAssetReferenceItemViewModel> items)
    {
        var viewModel = new FixAssetReferencesViewModel(serviceProvider, session);
        foreach (var item in items)
        {
            viewModel.AddItem(item);
        }

        return new FixAssetReferencesWindow(viewModel);
    }

    public async Task<bool> ShowFixAssetReferencesDialogAsync(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null)
    {
        return await Dispatcher.InvokeTask(async () =>
        {
            var dialog = CreateFixAssetReferencesDialog(session, assets);
            if (dialog is Window window && MainWindow is not null)
            {
                return await window.ShowDialog<bool>(MainWindow);
            }
            var result = await dialog.ShowModal();
            return result == Stride.Core.Presentation.Services.DialogResult.Ok;
        });
    }

    public INewProjectDialog CreateNewProjectDialog(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var viewModel = new Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.NewProjectTemplateCollectionViewModel(session.ServiceProvider, session);
        var window = new NewProjectWindow
        {
            DataContext = viewModel
        };
        return window;
    }

    public IAssetPickerDialog CreateAssetPickerDialog(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var assetPickerWindow = new AssetPickerWindow(session);
        var assetView = new AssetCollectionViewModel(session);
        assetPickerWindow.AssetView = assetView;
        return assetPickerWindow;
    }

    public IPackagePickerDialog CreatePackagePickerDialog(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new PackagePickerWindow(session);
    }

    public async Task<(NewSessionParameters? newParams, UFile? existingPath)?> ShowProjectSelectionWindowAsync()
    {
        return await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync<(NewSessionParameters?, UFile?)?>(async () =>
        {
            var window = new ProjectSelectionWindow
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            var viewModel = new Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels.NewOrOpenSessionTemplateCollectionViewModel(serviceProvider, window);
            window.Templates = viewModel;

            var result = await window.ShowModal();
            if (result == Stride.Core.Presentation.Services.DialogResult.Ok)
            {
                return (window.NewSessionParameters, window.ExistingSessionPath);
            }
            return null;
        });
    }
}

