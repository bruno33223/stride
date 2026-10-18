// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.Status;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel;

public abstract class EditorViewModel : ViewModelBase
{
    public const string PackageFileExtension = Package.PackageFileExtension;
    public const string SolutionFileExtension = ".sln";
    private SessionViewModel? session;

    protected EditorViewModel(IViewModelServiceProvider serviceProvider, MostRecentlyUsedFileCollection? mru, string editorName, string editorVersionMajor)
        : base(serviceProvider)
    {
        ClearMRUCommand = new AnonymousCommand(serviceProvider, ClearRecentFiles);
        OpenSettingsWindowCommand = new AnonymousCommand(serviceProvider, OpenSettingsWindow);
        OpenWebPageCommand = new AnonymousTaskCommand<string>(serviceProvider, OpenWebPage);

        MRU = mru;
        if (MRU != null)
        {
            MRU.MostRecentlyUsedFiles.CollectionChanged += MostRecentlyUsedFiles_CollectionChanged;
        }

        EditorName = editorName;
        EditorVersionMajor = editorVersionMajor;
        UpdateRecentFiles();

        Instance = this;

        Status = new StatusViewModel(ServiceProvider);
        Status.PushStatus("Ready");
    }

    private void MostRecentlyUsedFiles_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateRecentFiles();
    }

    /// <summary>
    /// Gets the current instance of <see cref="EditorViewModel"/>.
    /// </summary>
    public static EditorViewModel? Instance { get; protected set; }

    /// <summary>
    /// Gets the name of this editor.
    /// </summary>
    public string EditorName { get; set; }

    /// <summary>
    /// Gets the version major of this editor.
    /// </summary>
    public string EditorVersionMajor { get; set; }

    /// <summary>
    /// Gets the current active session.
    /// </summary>
    public SessionViewModel? Session { get => session; protected set => SetValue(ref session, value); }

    public StatusViewModel Status { get; }

    public MostRecentlyUsedFileCollection? MRU { get; }

    public ObservableList<MostRecentlyUsedFile> RecentFiles { get; } = new();

    public ICommandBase ClearMRUCommand { get; }

    public ICommandBase OpenSettingsWindowCommand { get; }

    public ICommandBase OpenWebPageCommand { get; }

    protected internal virtual IEnumerable<string> TextAssetTypes => Enumerable.Empty<string>();

    public void RemoveRecentFile(UFile filePath)
    {
        var strideVersions = RecentFiles?.Select(x => x.Version).Distinct().ToList();
        if (strideVersions != null && MRU != null)
        {
            foreach (var item in strideVersions)
            {
                MRU.RemoveFile(filePath, item);
            }
        }
    }

    private void ClearRecentFiles()
    {
        var strideVersions = RecentFiles?.Select(x => x.Version).ToList();
        if (strideVersions != null && MRU != null)
        {
            foreach (var item in strideVersions)
            {
                MRU.Clear(item);
            }
        }
    }

    private void UpdateRecentFiles()
    {
        RecentFiles.Clear();
        if (MRU == null)
            return;

        foreach (var item in MRU.MostRecentlyUsedFiles.Where(x => string.Compare(x.Version, EditorVersionMajor, StringComparison.Ordinal) <= 0).Take(10))
        {
            RecentFiles.Add(new MostRecentlyUsedFile { FilePath = item.FilePath, Timestamp = item.Timestamp, Version = item.Version });
        }
    }

    private void OpenSettingsWindow()
    {
        ServiceProvider.TryGet<IEditorDialogService>()?.ShowSettingsWindow(ServiceProvider);
    }

    private async Task OpenWebPage(string url)
    {
        try
        {
            var process = new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true } };
            process.Start();
        }
        catch (Exception ex)
        {
            var message = $"{Tr._p("Message", "An error occurred while opening the file.")}{ex.FormatSummary(true)}";
            var dialogService = ServiceProvider.TryGet<IDialogService>();
            if (dialogService != null)
            {
                await dialogService.MessageBoxAsync(message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
