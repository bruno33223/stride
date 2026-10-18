// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Stride.Assets.Editor.AssetEditors;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Assets.Presentation.AssetEditors.ScriptEditor;
using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using DirectoryBaseViewModel = Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel;
using Project = Microsoft.CodeAnalysis.Project;
using RoslynWorkspace = Stride.Assets.Editor.AssetEditors.ScriptEditor.RoslynWorkspace;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// Manages source code project and files, including change tracking, Roslyn workspace updates, etc.
/// </summary>
public class CodeViewModel : DispatcherViewModel, IDisposable
{
    /// <summary>
    /// Gets the logger dedicated to code project events.
    /// </summary>
    public LoggerResult Logger { get; } = new("Project");

    /// <summary>
    /// The editor minimum font size.
    /// </summary>
    public const int MinimumEditorFontSize = 8;

    /// <summary>
    /// The editor maximum font size.
    /// </summary>
    public const int MaximumEditorFontSize = 72;

    private readonly TaskCompletionSource<ProjectWatcher> projectWatcherCompletion = new();
    private readonly TaskCompletionSource<RoslynWorkspace> workspaceCompletion = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private int editorFontSize = ScriptEditorSettings.FontSize.GetValue(); // default size

    private readonly Color keywordColor;
    private readonly Color typeColor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeViewModel"/> class.
    /// </summary>
    /// <param name="strideAssetsViewModel">The parent assets view model.</param>
    public CodeViewModel(StrideAssetsViewModel strideAssetsViewModel)
        : base(strideAssetsViewModel.SafeArgument(nameof(strideAssetsViewModel)).ServiceProvider)
    {
        _ = InitializeAsync(strideAssetsViewModel);

        // Apply syntax highlighting colors for classifications and tooltips
        keywordColor = ClassificationHighlightColorsDark.KeywordColor;
        typeColor = ClassificationHighlightColorsDark.TypeColor;
    }

    /// <summary>
    /// Gets the color used for keyword highlighting.
    /// </summary>
    public Color KeywordColor => keywordColor;

    /// <summary>
    /// Gets the color used for type name highlighting.
    /// </summary>
    public Color TypeColor => typeColor;

    /// <summary>
    /// Gets the project watcher which tracks source code changes on disk; it is created asynchronously.
    /// </summary>
    public Task<ProjectWatcher> ProjectWatcher => projectWatcherCompletion.Task;

    /// <summary>
    /// Gets the Roslyn workspace; it is created asynchronously.
    /// </summary>
    public Task<RoslynWorkspace> Workspace => workspaceCompletion.Task;

    /// <summary>
    /// The editor current font size. It will be saved in the settings.
    /// </summary>
    public int EditorFontSize
    {
        get => editorFontSize;
        set
        {
            if (value < MinimumEditorFontSize || value > MaximumEditorFontSize)
                return;

            if (SetValue(ref editorFontSize, value))
            {
                ScriptEditorSettings.FontSize.SetValue(editorFontSize);
                ScriptEditorSettings.Save();
            }
        }
    }

    /// <summary>
    /// Initializes the project watcher and Roslyn workspace asynchronously.
    /// </summary>
    /// <param name="strideAssetsViewModel">The parent assets view model.</param>
    private async Task InitializeAsync(StrideAssetsViewModel strideAssetsViewModel)
    {
        var projectWatcher = new ProjectWatcher(strideAssetsViewModel.Session, Logger);
        await projectWatcher.Initialize();
        var workspace = (await projectWatcher.RoslynHost).Workspace;

        // Load and update Roslyn workspace with latest compiled version
        foreach (var trackedAssembly in projectWatcher.TrackedAssemblies)
        {
            if (trackedAssembly.Project is { } project)
            {
                workspace.AddOrUpdateProject(project);
            }
        }

        projectWatcher.TrackedAssemblies.CollectionChanged += TrackedAssembliesCollectionChanged;

        _ = UpdateProjects(projectWatcher, workspace, strideAssetsViewModel);

        projectWatcherCompletion.SetResult(projectWatcher);
        workspaceCompletion.SetResult(workspace);

        void TrackedAssembliesCollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)
        {
            if (e.Item is ProjectWatcher.TrackedAssembly trackedAssembly && trackedAssembly.Project is { } project)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        workspace.AddOrUpdateProject(project);
                        break;
                    }
                    case NotifyCollectionChangedAction.Remove:
                    {
                        workspace.RemoveProject(project.Id);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Processes batch changes asynchronously from the project watcher with cooperative cancellation.
    /// </summary>
    /// <param name="projectWatcher">The project watcher source.</param>
    /// <param name="workspace">The target Roslyn workspace.</param>
    /// <param name="strideAssetsViewModel">The assets view model session.</param>
    private async Task UpdateProjects(ProjectWatcher projectWatcher, RoslynWorkspace workspace, StrideAssetsViewModel strideAssetsViewModel)
    {
        try
        {
            await foreach (var events in projectWatcher.BatchChange.WithCancellation(cancellationTokenSource.Token))
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    break;

                await Dispatcher.InvokeAsync(async () =>
                {
                    // Update projects
                    foreach (var e in events.Where(x => x.ChangeType == AssemblyChangeType.Project))
                    {
                        var project = e.Project;
                        if (project != null)
                        {
                            await ReloadProject(strideAssetsViewModel.Session, project);
                        }
                    }

                    // Update files
                    foreach (var e in events.Where(x => x.ChangeType == AssemblyChangeType.Source))
                    {
                        var documentId = workspace.CurrentSolution.GetDocumentIdsWithFilePath(e.ChangedFile).FirstOrDefault();
                        if (documentId != null)
                        {
                            workspace.HostDocumentTextLoaderChanged(documentId, new FileTextLoader(e.ChangedFile, null));
                        }
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Expected cooperative cancellation during disposal
        }
        catch (Exception ex)
        {
            Logger.Warning("Error while updating projects from batch change stream", ex);
        }
    }

    /// <summary>
    /// Reloads a project when a .csproj file changes on the hard drive.
    /// </summary>
    /// <param name="session">The active session view model.</param>
    /// <param name="project">The Roslyn project that changed.</param>
    /// <remarks>
    /// In case of destructive changes (i.e. dirty files that disappeared), user confirmation will be asked to proceed.
    /// </remarks>
    public async Task ReloadProject(SessionViewModel session, Project project)
    {
        var workspace = await Workspace;

        // Get assets and namespace from csproj
        // TODO: Use roslyn list of files? not sure we could have non .cs files easily though
        // However, if we load from file, it might not be in sync with Roslyn state
        var projectFiles = Package.FindAssetsInProject(project.FilePath, out _);

        // Find associated ProjectViewModel
        var projectViewModel = session.LocalPackages.FirstOrDefault(y => y.Name == project.Name) as ProjectViewModel;
        if (projectViewModel == null)
            return;

        // List current assets
        var projectAssets = new List<AssetViewModel>();
        var isProjectDirty = GetAssets(projectViewModel.Code, projectAssets);

        // Project is dirty, ask user if he really wants to auto-reload
        if (isProjectDirty)
        {
            var dialogService = session.ServiceProvider.TryGet<IDialogService>();
            if (dialogService != null)
            {
                var dialogResult = await dialogService.MessageBoxAsync(
                    string.Format(
                        Tr._p("Message", "Game Studio can't auto-reload the project file {0} because you have local changes such as new or deleted scripts.\r\n\r\nClick OK to keep reloading or Cancel to keep the current version."),
                        Path.GetFileName(project.FilePath)), MessageBoxButton.OKCancel);
                if (dialogResult == MessageBoxResult.Cancel)
                    return;
            }
        }

        // Remove deleted assets (and ask user if he really wants to proceed in case some of them were dirty?)
        bool continueReload = await DeleteRemovedProjectAssets(projectViewModel, projectAssets, project, projectFiles);
        if (!continueReload)
            return;

        // Update Roslyn project
        workspace.AddOrUpdateProject(project);

        // Add new assets
        await AddNewProjectAssets(projectViewModel, projectAssets, project, projectFiles);

        // Mark project as non dirty
        // TODO: Does that work properly with Undo/Redo?
        UpdateDirtiness(projectViewModel.Code, false);
    }

    /// <summary>
    /// Handles Script asset deletion (from external changes to Game Studio).
    /// </summary>
    /// <param name="projectViewModel">The project view model.</param>
    /// <param name="projectAssets">The current project assets collection.</param>
    /// <param name="project">The Roslyn project.</param>
    /// <param name="projectFiles">The discovered project files.</param>
    /// <returns><c>false</c> if the user refused to continue (when deleted assets were dirty); otherwise, <c>true</c>.</returns>
    public static async Task<bool> DeleteRemovedProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> projectAssets, Project project, List<(UFile FilePath, UFile? Link)> projectFiles)
    {
        // List IProjectAsset
        var currentProjectAssets = projectAssets.Where(x => x.AssetItem.Asset is IProjectAsset);

        var assetsToDelete = new List<AssetViewModel>();
        foreach (var asset in currentProjectAssets)
        {
            // Note: if file doesn't exist on HDD anymore (i.e. automatic csproj tracking for *.cs), no need to delete it anymore
            bool isDeleted = !projectFiles.Any(x => x.FilePath == asset.AssetItem.FullPath);
            if (isDeleted)
            {
                assetsToDelete.Add(asset);
            }
        }

        var dirtyAssetsToDelete = assetsToDelete.Where(x => x.AssetItem.IsDirty).ToList();
        if (dirtyAssetsToDelete.Count > 0)
        {
            var dialogService = projectViewModel.Session.ServiceProvider.TryGet<IDialogService>();
            if (dialogService != null)
            {
                // Check if user is OK with deleting those dirty assets
                var dialogResult = await dialogService.MessageBoxAsync(
                    string.Format(
                        Tr._p("Message", "The following source files in the {0} project have been deleted externally, but have unsaved changes in Game Studio. Do you want to delete these files?\r\n\r\n{1}"),
                        Path.GetFileName(project.FilePath), string.Join("\r\n", dirtyAssetsToDelete.Select(x => x.AssetItem.FullPath.ToOSPath()))),
                    MessageBoxButton.OKCancel);
                if (dialogResult == MessageBoxResult.Cancel)
                    return false;
            }
        }

        // Delete removed assets using an undo/redo transaction
        if (assetsToDelete.Count > 0)
        {
            var undoRedoService = projectViewModel.ServiceProvider.TryGet<IUndoRedoService>();
            using (var transaction = undoRedoService?.CreateTransaction())
            {
                foreach (var asset in assetsToDelete)
                {
                    asset.IsDeleted = true;
                }

                if (transaction != null && undoRedoService != null)
                {
                    undoRedoService.SetName(transaction, Tr._p("Transaction", "Delete removed script assets"));
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Handles project asset addition (from external changes to Game Studio).
    /// </summary>
    /// <param name="projectViewModel">The project view model.</param>
    /// <param name="projectAssets">The current project assets collection.</param>
    /// <param name="project">The Roslyn project.</param>
    /// <param name="projectFiles">The discovered project files.</param>
    public static async Task AddNewProjectAssets(ProjectViewModel projectViewModel, List<AssetViewModel> projectAssets, Project project, List<(UFile FilePath, UFile? Link)> projectFiles)
    {
        // Nothing to add?
        if (projectFiles.Count == 0)
            return;

        var scriptAssets = projectAssets.Where(x => x.AssetItem.Asset is IProjectAsset).Select(x => x.AssetItem);

        var documentsToIgnore = (from scriptAsset in scriptAssets
                                 from document in projectFiles
                                 let ufileDoc = document.FilePath
                                 where ufileDoc == scriptAsset.FullPath
                                 select document).ToList();

        // Remove what we already have
        var documentsCopy = new List<(UFile FilePath, UFile? Link)>(projectFiles);
        foreach (var document in documentsToIgnore)
        {
            documentsCopy.Remove(document);
        }

        // Add what we are missing
        if (documentsCopy.Count > 0)
        {
            var newScriptAssets = new List<AssetViewModel>();
            foreach (var document in documentsCopy)
            {
                var docFile = new UFile(document.FilePath);

                var assetName = docFile.MakeRelative(projectViewModel.Package.RootDirectory).GetDirectoryAndFileNameWithoutExtension();

                var asset = new ScriptSourceFileAsset();
                var assetItem = new AssetItem(assetName, asset)
                {
                    IsDirty = true, // Important in case of renaming to propagate the change from VS to Game Studio
                    SourceFolder = projectViewModel.Package.RootDirectory,
                };

                var directory = projectViewModel.GetOrCreateProjectDirectory(assetItem.Location.GetFullDirectory().FullPath);
                var newScriptAsset = projectViewModel.CreateAsset(directory, assetItem, false, null);
                newScriptAssets.Add(newScriptAsset);
            }

            // We're out of any transaction in this context so we manually notify that new assets were created.
            if (projectViewModel.Session is SessionViewModel sessionViewModel)
            {
                await sessionViewModel.NotifyAssetPropertiesChangedAsync(newScriptAssets);
            }
        }
    }

    /// <summary>
    /// Enumerates assets in the given directory hierarchy.
    /// </summary>
    /// <param name="dir">The root directory.</param>
    /// <param name="list">The target list of collected assets.</param>
    /// <returns><c>true</c> if any directory in the hierarchy is dirty; otherwise, <c>false</c>.</returns>
    public static bool GetAssets(Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel dir, List<AssetViewModel> list)
    {
        bool dirDirty = dir.IsDirty;

        foreach (var directory in dir.SubDirectories)
        {
            dirDirty |= GetAssets(directory, list);
        }

        // Check is dirty to avoid unsaved scripts being deleted
        list.AddRange(dir.Assets.Where(x => x.Asset is IProjectAsset));

        return dirDirty;
    }

    /// <summary>
    /// Recursively updates dirtiness of the specified directory hierarchy.
    /// </summary>
    /// <param name="dir">The root directory to update.</param>
    /// <param name="isDirty">The dirty state to set.</param>
    public void UpdateDirtiness(Stride.Core.Assets.Presentation.ViewModels.DirectoryBaseViewModel dir, bool isDirty)
    {
        if (dir.IsDirty)
            ((IDirtiable)dir).UpdateDirtiness(isDirty);

        foreach (var subdir in dir.SubDirectories)
        {
            UpdateDirtiness(subdir, isDirty);
        }
    }

    /// <summary>
    /// Resolves the classification color for the specified <see cref="SymbolDisplayPartKind"/>.
    /// </summary>
    /// <param name="partKind">The symbol display part kind.</param>
    /// <returns>The corresponding color, or <c>null</c> if not highlighted.</returns>
    public Color? GetColorFromSymbolDisplayPartKind(SymbolDisplayPartKind partKind)
    {
        switch (partKind)
        {
            case SymbolDisplayPartKind.Keyword:
                return keywordColor;
            case SymbolDisplayPartKind.StructName:
            case SymbolDisplayPartKind.EnumName:
            case SymbolDisplayPartKind.TypeParameterName:
            case SymbolDisplayPartKind.ClassName:
            case SymbolDisplayPartKind.DelegateName:
            case SymbolDisplayPartKind.InterfaceName:
                return typeColor;
            default:
                return null;
        }
    }

    /// <summary>
    /// Resolves the classification color for the specified text tag.
    /// </summary>
    /// <param name="textTag">The classification text tag.</param>
    /// <returns>The corresponding color, or <c>null</c> if not highlighted.</returns>
    public Color? GetColorFromTextTag(string textTag)
    {
        switch (textTag)
        {
            case TextTags.Keyword:
                return keywordColor;
            case TextTags.Struct:
            case TextTags.Enum:
            case TextTags.TypeParameter:
            case TextTags.Class:
            case TextTags.Delegate:
            case TextTags.Interface:
                return typeColor;
            default:
                return null;
        }
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(CodeViewModel));
        Cleanup();
        base.Destroy();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Cleanup();
    }

    /// <summary>
    /// Cleans up resources and cancels cooperative background watchers.
    /// </summary>
    private void Cleanup()
    {
        if (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
