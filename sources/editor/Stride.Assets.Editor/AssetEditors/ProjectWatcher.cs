// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable VSTHRD011

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Stride.Assets.Editor.AssetEditors.ScriptEditor;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Project = Microsoft.CodeAnalysis.Project;

namespace Stride.Assets.Editor.AssetEditors
{
    public enum AssemblyChangeType
    {
        Binary,
        Project,
        Source,
    }

    public class AssemblyChangedEvent
    {
        public AssemblyChangedEvent(PackageLoadedAssembly assembly, AssemblyChangeType changeType, string changedFile, Project project)
        {
            Assembly = assembly;
            ChangeType = changeType;
            ChangedFile = changedFile;
            Project = project;
        }

        public AssemblyChangeType ChangeType { get; set; }

        public PackageLoadedAssembly Assembly { get; set; }

        public string ChangedFile { get; set; }

        public Project Project { get; set; }
    }

    public class ProjectWatcher : IDisposable
    {
        private readonly LoggerResult logger;
        private readonly TrackingCollection<TrackedAssembly> trackedAssemblies;
        private readonly BufferBlock<FileEvent> fileChanged = new BufferBlock<FileEvent>();
        private readonly IDisposable fileChangedLink1;
        private readonly IDisposable fileChangedLink2;
        private readonly DirectoryWatcher directoryWatcher;
        private readonly SessionViewModel session;
        private readonly bool trackBinaries;
        private TaskCompletionSource<bool> initializedTaskSource;
        private Project gameExecutable;

        private readonly CancellationTokenSource batchChangesCancellationTokenSource = new CancellationTokenSource();
        public IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChange { get; }

        private MSBuildWorkspace msbuildWorkspace;

        private readonly Lazy<Task<RoslynHost>> roslynHost;

        private RoslynHost CreateRoslynHost()
        {
            try
            {
                return new RoslynHost();
            }
            catch (Exception e)
            {
                logger.Error($"Could not create {nameof(RoslynHost)}", e);
                throw;
            }
        }

        public ProjectWatcher(SessionViewModel session, LoggerResult logger, bool trackBinaries = true)
        {
            trackedAssemblies = new TrackingCollection<TrackedAssembly>();

            this.trackBinaries = trackBinaries;
            this.session = session;
            this.logger = logger;

            roslynHost = new Lazy<Task<RoslynHost>>(() => Task.Run(CreateRoslynHost));

            session.LocalPackages.CollectionChanged += LocalPackagesChanged;

            directoryWatcher = new DirectoryWatcher();
            directoryWatcher.Modified += directoryWatcher_Modified;

            var fileChangedTransform = new TransformBlock<FileEvent, AssemblyChangedEvent>(x => FileChangeTransformation(x));
            fileChangedLink1 = fileChanged.LinkTo(fileChangedTransform);
            fileChangedLink2 = fileChangedTransform.LinkTo(AssemblyChangedBroadcast);

            BatchChange = BatchChanges();
        }

        private async IAsyncEnumerable<List<AssemblyChangedEvent>> BatchChanges()
        {
            var buffer = new BufferBlock<AssemblyChangedEvent>();
            using (AssemblyChangedBroadcast.LinkTo(buffer))
            {
                while (!batchChangesCancellationTokenSource.IsCancellationRequested)
                {
                    var hasChanged = false;
                    var assemblyChanges = new List<AssemblyChangedEvent>();
                    do
                    {
                        AssemblyChangedEvent assemblyChange;
                        try
                        {
                            assemblyChange = await buffer.ReceiveAsync(batchChangesCancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            yield break;
                        }

                        if (assemblyChange == null)
                            continue;

                        assemblyChanges.Add(assemblyChange);
                        var project = assemblyChange.Project;
                        if (project != null && msbuildWorkspace?.CurrentSolution != null)
                        {
                            var referencedProjects = msbuildWorkspace.CurrentSolution.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(project.Id);
                            foreach (var referenceProject in referencedProjects)
                            {
                                var foundProject = msbuildWorkspace.CurrentSolution.GetProject(referenceProject);
                                if (foundProject is null)
                                    continue;
                                var assemblyName = foundProject.AssemblyName;
                                var target = trackedAssemblies.FirstOrDefault(x => x.Project != null && x.Project.AssemblyName == assemblyName);
                                if (target != null)
                                {
                                    string file = assemblyChange.ChangedFile;
                                    if (assemblyChange.ChangeType == AssemblyChangeType.Binary)
                                    {
                                        file = target.LoadedAssembly.Path;
                                    }
                                    else if (assemblyChange.ChangeType == AssemblyChangeType.Project)
                                    {
                                        file = target.Project.FilePath;
                                    }
                                    assemblyChanges.Add(new AssemblyChangedEvent(target.LoadedAssembly, assemblyChange.ChangeType, file, target.Project));
                                }
                            }
                        }

                        if (!hasChanged)
                        {
                            // After the first change, wait for more changes
                            try
                            {
                                await Task.Delay(TimeSpan.FromMilliseconds(500), batchChangesCancellationTokenSource.Token);
                            }
                            catch (OperationCanceledException)
                            {
                                yield break;
                            }
                            hasChanged = true;
                        }

                    } while (!hasChanged || buffer.Count > 0);

                    // Merge files that were modified multiple time
                    assemblyChanges = assemblyChanges.GroupBy(x => x.ChangedFile).Select(x => x.Last()).ToList();

                    yield return assemblyChanges;
                }
            }
        }

        public BroadcastBlock<AssemblyChangedEvent> AssemblyChangedBroadcast { get; } = new BroadcastBlock<AssemblyChangedEvent>(null);

        public Project CurrentGameLibrary
        {
            get
            {
                var project = session.CurrentProject as ProjectViewModel;
                if (project == null || project.Type != ProjectType.Library)
                    return null;

                return TrackedAssemblies.FirstOrDefault(x => x.Project != null && new UFile(x.Project.FilePath) == project.ProjectPath)?.Project;
            }
        }

        public Task<RoslynHost> RoslynHost => roslynHost.Value;

        public Project CurrentGameExecutable => gameExecutable;

        public TrackingCollection<TrackedAssembly> TrackedAssemblies => trackedAssemblies;

        public void Dispose()
        {
            batchChangesCancellationTokenSource.Cancel();

            directoryWatcher.Dispose();
            fileChangedLink1?.Dispose();
            fileChangedLink2?.Dispose();
            msbuildWorkspace?.Dispose();
            batchChangesCancellationTokenSource.Dispose();
        }

        public async Task Initialize()
        {
            if (initializedTaskSource == null)
            {
                initializedTaskSource = new TaskCompletionSource<bool>();

                // Track all packages
                foreach (var package in session.LocalPackages)
                    await TrackPackage(package);

                // Locate current package's game executable
                // TODO: Handle current package changes. Detect this as part of the package solution.
                var gameExecutableViewModel = (session.CurrentProject as ProjectViewModel)?.Type == ProjectType.Executable ? session.CurrentProject : null;
                if (gameExecutableViewModel != null && gameExecutableViewModel.IsLoaded)
                    gameExecutable = await OpenProject(gameExecutableViewModel.ProjectPath);

                initializedTaskSource.SetResult(true);
            }
            else
            {
                await initializedTaskSource.Task;
            }
        }

        public async Task ReceiveAndDiscardChanges(TimeSpan batchInterval, CancellationToken cancellationToken)
        {
            var hasChanged = false;
            var buffer = new BufferBlock<AssemblyChangedEvent>();
            using (AssemblyChangedBroadcast.LinkTo(buffer))
            {
                do
                {
                    var assemblyChange = await buffer.ReceiveAsync(cancellationToken);

                    if (assemblyChange != null && !hasChanged)
                    {
                        // After the first change, wait for more changes
                        await Task.Delay(batchInterval, cancellationToken);
                        hasChanged = true;
                    }

                } while (!hasChanged || fileChanged.Count > 0);
            }
        }

        private async Task<AssemblyChangedEvent> FileChangeTransformation(FileEvent e)
        {
            string changedFile;
            var renameEvent = e as FileRenameEvent;
            changedFile = renameEvent?.OldFullPath ?? e.FullPath;

            foreach (var trackedAssembly in trackedAssemblies)
            {
                // Report change of the assembly binary
                if (string.Equals(trackedAssembly.LoadedAssembly?.Path, changedFile, StringComparison.OrdinalIgnoreCase))
                    return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Binary, changedFile, trackedAssembly.Project);

                if (trackedAssembly.Project == null)
                    continue;

                var needProjectReload = string.Equals(trackedAssembly.Project.FilePath, changedFile, StringComparison.OrdinalIgnoreCase);

                var directoryName = Path.GetDirectoryName(trackedAssembly.Project.FilePath) + Path.DirectorySeparatorChar;
                var changedFileDirectoryName = Path.GetDirectoryName(changedFile) + Path.DirectorySeparatorChar;

                // Also check for .cs file changes (DefaultItems auto import *.cs, with some excludes such as obj subfolder)
                // TODO: Check actual unevaluated .csproj to get the auto includes/excludes?
                if (needProjectReload == false
                    && ((e.ChangeType == FileEventChangeType.Deleted || e.ChangeType == FileEventChangeType.Renamed || e.ChangeType == FileEventChangeType.Created)
                    && Path.GetExtension(changedFile)?.ToLowerInvariant() == ".cs"
                    && changedFileDirectoryName.StartsWith(directoryName, StringComparison.OrdinalIgnoreCase)))
                {
                    needProjectReload = true;
                }

                // Reparse the project file and report source changes
                if (needProjectReload)
                {
                    // Reparse the project file and report source changes
                    await UpdateProject(trackedAssembly);
                    return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Project, trackedAssembly.Project.FilePath, trackedAssembly.Project);
                }

                // Only deal with file changes
                if (e.ChangeType != FileEventChangeType.Changed)
                    continue;

                // Check if we have a matching document
                var document = trackedAssembly.Project.Documents.FirstOrDefault(x => string.Equals(x.FilePath, changedFile, StringComparison.OrdinalIgnoreCase));
                if (document == null)
                    continue;

                string source = null;
                // Try multiple times
                for (int i = 0; i < 10; ++i)
                {
                    try
                    {
                        using (var streamReader = new StreamReader(File.Open(changedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8, true))
                        {
                            source = await streamReader.ReadToEndAsync();
                        }
                        break;
                    }
                    catch (IOException)
                    {
                    }
                    await Task.Delay(1);
                }

                if (source == null)
                {
                    // Something went wrong reading the file
                    return null;
                }

                // Remove and readd new source
                trackedAssembly.Project = trackedAssembly.Project.RemoveDocument(document.Id);
                var documentId = DocumentId.CreateNewId(trackedAssembly.Project.Id);
                trackedAssembly.Project = trackedAssembly.Project.Solution.AddDocument(documentId, document.Name, SourceText.From(source, Encoding.UTF8), null, document.FilePath).GetDocument(documentId).Project;

                return new AssemblyChangedEvent(trackedAssembly.LoadedAssembly, AssemblyChangeType.Source, changedFile, trackedAssembly.Project);
            }

            return null;
        }

        private async void LocalPackagesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (PackageViewModel oldItem in e.OldItems)
                    UntrackPackage(oldItem);
            }

            if (e.NewItems != null)
            {
                foreach (PackageViewModel newItem in e.NewItems)
                    await TrackPackage(newItem);
            }
        }

        private void UntrackPackage(PackageViewModel package)
        {
            // TODO: Properly untrack all files
            trackedAssemblies.RemoveWhere(trackedAssembly => trackedAssembly.Package == package);
        }

        private async Task TrackPackage(PackageViewModel package)
        {
            if (!package.IsLoaded)
                return;

            foreach (var loadedAssembly in package.LoadedAssemblies)
            {
                // Track the assembly binary
                if (trackBinaries && !string.IsNullOrEmpty(loadedAssembly.Path))
                    directoryWatcher.Track(loadedAssembly.Path);

                // Track the project file
                if (loadedAssembly.ProjectReference == null)
                    continue;

                directoryWatcher.Track(loadedAssembly.ProjectReference.Location);

                var trackedAssembly = new TrackedAssembly { Package = package, LoadedAssembly = loadedAssembly };

                // Track project source code
                if (await UpdateProject(trackedAssembly))
                    trackedAssemblies.Add(trackedAssembly);
            }

            // TODO: Detect changes to loaded assemblies?
        }

        // TODO: Properly untrack removed documents
        private async Task<bool> UpdateProject(TrackedAssembly trackedAssembly)
        {
            if (trackedAssembly.LoadedAssembly.ProjectReference == null)
                return false;

            var location = trackedAssembly.LoadedAssembly.ProjectReference.Location;
            if (location.IsRelative)
            {
                var packageDir = trackedAssembly.Package.PackagePath?.GetFullDirectory() ?? trackedAssembly.Package.RootDirectory;
                location = UPath.Combine(packageDir, location);
            }
            var project = await OpenProject(location);
            if (project == null)
                return false;

            trackedAssembly.Project = project;

            var packageDirectory = trackedAssembly.Package.PackagePath?.GetFullDirectory() ?? trackedAssembly.Package.RootDirectory;
            var projectDirectory = location.GetFullDirectory();

            foreach (var document in project.Documents)
            {
                if (document.FilePath == null)
                    continue;

                // Limit ourselves to our package subfolders or project folders
                var docFile = new UFile(document.FilePath);
                if ((packageDirectory == null || !packageDirectory.Contains(docFile))
                    && (projectDirectory == null || !projectDirectory.Contains(docFile)))
                    continue;

                directoryWatcher.Track(document.FilePath);
            }

            return true;
        }

        private async Task<Project> OpenProject(UFile projectPath)
        {
            if (msbuildWorkspace == null)
            {
                var host = await RoslynHost;
                msbuildWorkspace = MSBuildWorkspace.Create(ImmutableDictionary<string, string>.Empty, host.HostServices);
            }

            var solutionPath = session.SolutionPath?.ToOSPath();
            if (solutionPath != null && File.Exists(solutionPath))
            {
                await msbuildWorkspace.OpenSolutionAsync(solutionPath);
            }
            else if (File.Exists(projectPath.ToOSPath()))
            {
                await msbuildWorkspace.OpenProjectAsync(projectPath.ToOSPath());
            }

            // Try up to 10 times (1 second)
            const int retryCount = 10;
            for (var i = retryCount - 1; i >= 0; --i)
            {
                try
                {
                    var project = msbuildWorkspace.CurrentSolution?.Projects.FirstOrDefault(x => string.Equals(x.FilePath, projectPath.ToOSPath(), StringComparison.OrdinalIgnoreCase));
                    if (project != null)
                    {
                        if (msbuildWorkspace.Diagnostics.Count > 0)
                        {
                            foreach (var diagnostic in msbuildWorkspace.Diagnostics)
                                Debug.WriteLine(diagnostic.Message, category: nameof(ProjectWatcher));
                        }
                        return project;
                    }

                    if (i == 0)
                        return null;

                    await Task.Delay(100);
                }
                catch (IOException)
                {
                    // File might still be locked, let's wait little bit more
                    await Task.Delay(100);

                    if (i == 0)
                        throw;
                }
            }

            return null;
        }

        private void directoryWatcher_Modified(object sender, FileEvent e)
        {
            fileChanged.Post(e);
        }

        public class TrackedAssembly
        {
            public PackageViewModel Package { get; set; }

            public PackageLoadedAssembly LoadedAssembly { get; set; }

            public Project Project { get; set; }
        }
    }
}
