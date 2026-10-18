// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

public interface IEditorDialogService : IDialogService
{
    IAssetPickerDialog? CreateAssetPickerDialog(SessionViewModel session) => null;

    IPackagePickerDialog? CreatePackagePickerDialog(SessionViewModel session) => null;

    void ShowProgressWindow(WorkProgressViewModel workProgress);

    INewProjectDialog CreateNewProjectDialog(SessionViewModel session);

    IFixReferencesDialog CreateFixAssetReferencesDialog(SessionViewModel session, IReadOnlyCollection<AssetViewModel>? assets = null);

    void ShowNotificationWindow(string title, string message, ICommandBase? command = null, object? commandParameter = null) { }

    void CloseAllNotificationWindows() { }

    void ShowSettingsWindow(IViewModelServiceProvider serviceProvider) { }
}
