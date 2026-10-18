// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Stride.Assets.Editor.AssetEditors;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestProjectWatcherIntegration
{
    private static SessionViewModel CreateTestSession()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());
        sp.RegisterService(new UndoRedoService(100));

        var session = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .GetSetMethod(true)!.Invoke(session, [sp]);

        // Initialize packageCategories dictionary
        var categoriesField = typeof(SessionViewModel).GetField("packageCategories", BindingFlags.Instance | BindingFlags.NonPublic);
        var categories = new Dictionary<string, PackageCategoryViewModel>
        {
            [SessionViewModel.LocalPackageCategoryName] = new PackageCategoryViewModel(SessionViewModel.LocalPackageCategoryName, session),
            [SessionViewModel.StorePackageCategoryName] = new PackageCategoryViewModel(SessionViewModel.StorePackageCategoryName, session)
        };
        categoriesField?.SetValue(session, categories);

        return session;
    }

    [Fact]
    public void TestProjectWatcherContractAndParity()
    {
        // 1. AssemblyChangedEvent.Assembly must be PackageLoadedAssembly, NOT object
        var assemblyProp = typeof(AssemblyChangedEvent).GetProperty(nameof(AssemblyChangedEvent.Assembly));
        Assert.NotNull(assemblyProp);
        Assert.Equal(typeof(PackageLoadedAssembly), assemblyProp.PropertyType);

        // 2. ProjectWatcher must expose BatchChange of type IAsyncEnumerable<List<AssemblyChangedEvent>>
        var batchChangeProp = typeof(ProjectWatcher).GetProperty("BatchChange");
        Assert.NotNull(batchChangeProp);
        Assert.Equal(typeof(IAsyncEnumerable<List<AssemblyChangedEvent>>), batchChangeProp.PropertyType);

        // 3. ProjectWatcher must expose AssemblyChangedBroadcast of type BroadcastBlock<AssemblyChangedEvent>
        var broadcastProp = typeof(ProjectWatcher).GetProperty("AssemblyChangedBroadcast");
        Assert.NotNull(broadcastProp);
        Assert.Equal(typeof(BroadcastBlock<AssemblyChangedEvent>), broadcastProp.PropertyType);

        // 4. ProjectWatcher must expose TrackedAssemblies
        var trackedAssembliesProp = typeof(ProjectWatcher).GetProperty("TrackedAssemblies");
        Assert.NotNull(trackedAssembliesProp);
        Assert.Equal(typeof(TrackingCollection<ProjectWatcher.TrackedAssembly>), trackedAssembliesProp.PropertyType);

        // 5. ProjectWatcher must have nested TrackedAssembly class
        var trackedAssemblyType = typeof(ProjectWatcher).GetNestedType("TrackedAssembly");
        Assert.NotNull(trackedAssemblyType);
        Assert.NotNull(trackedAssemblyType.GetProperty("Package"));
        Assert.NotNull(trackedAssemblyType.GetProperty("LoadedAssembly"));
        Assert.NotNull(trackedAssemblyType.GetProperty("Project"));

        // 6. ProjectWatcher must have ReceiveAndDiscardChanges method
        var receiveMethod = typeof(ProjectWatcher).GetMethod("ReceiveAndDiscardChanges");
        Assert.NotNull(receiveMethod);

        // 7. ProjectWatcher must have CurrentGameLibrary and CurrentGameExecutable
        var gameLibProp = typeof(ProjectWatcher).GetProperty("CurrentGameLibrary");
        Assert.NotNull(gameLibProp);

        var gameExeProp = typeof(ProjectWatcher).GetProperty("CurrentGameExecutable");
        Assert.NotNull(gameExeProp);
    }

    [Fact]
    public async Task TestProjectWatcherBatchChangesDebounceAndBroadcast()
    {
        var session = CreateTestSession();
        var logger = new LoggerResult();
        using var watcher = new ProjectWatcher(session, logger, trackBinaries: true);

        Assert.NotNull(watcher.AssemblyChangedBroadcast);
        Assert.NotNull(watcher.BatchChange);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var enumerator = watcher.BatchChange.GetAsyncEnumerator(cts.Token);

        // Start MoveNextAsync to enter BatchChanges() and link AssemblyChangedBroadcast to buffer
        var moveNextTask = enumerator.MoveNextAsync();
        await Task.Delay(50);

        // Broadcast multiple events: duplicate files should be merged to the last event
        var dummyAssembly = new PackageLoadedAssembly(new ProjectReference(Guid.NewGuid(), "TestProject.csproj", ProjectType.Library), "/path/to/dummy.dll");
        var event1 = new AssemblyChangedEvent(dummyAssembly, AssemblyChangeType.Binary, "/path/to/dummy.dll", null!);
        var event2 = new AssemblyChangedEvent(dummyAssembly, AssemblyChangeType.Binary, "/path/to/second.dll", null!);
        var event3 = new AssemblyChangedEvent(dummyAssembly, AssemblyChangeType.Binary, "/path/to/dummy.dll", null!);

        watcher.AssemblyChangedBroadcast.Post(event1);
        watcher.AssemblyChangedBroadcast.Post(event2);
        watcher.AssemblyChangedBroadcast.Post(event3);

        // Await next batch (with 500ms debounce)
        var moved = await moveNextTask;
        Assert.True(moved, "BatchChange should yield a batch after debounce");
        var currentBatch = enumerator.Current;
        Assert.NotNull(currentBatch);

        // Duplicate "/path/to/dummy.dll" must be merged to 1 entry, plus "/path/to/second.dll" -> total 2
        Assert.Equal(2, currentBatch.Count);
        Assert.Contains(currentBatch, x => x.ChangedFile == "/path/to/dummy.dll");
        Assert.Contains(currentBatch, x => x.ChangedFile == "/path/to/second.dll");
    }

    [Fact]
    public async Task TestProjectWatcherReceiveAndDiscardChanges()
    {
        var session = CreateTestSession();
        var logger = new LoggerResult();
        using var watcher = new ProjectWatcher(session, logger, trackBinaries: true);

        var dummyAssembly = new PackageLoadedAssembly(new ProjectReference(Guid.NewGuid(), "TestProject.csproj", ProjectType.Library), "/path/to/dummy.dll");
        var dummyEvent = new AssemblyChangedEvent(dummyAssembly, AssemblyChangeType.Binary, "/path/to/dummy.dll", null!);

        // Start discarding in background task
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var discardTask = Task.Run(async () =>
        {
            await watcher.ReceiveAndDiscardChanges(TimeSpan.FromMilliseconds(50), cts.Token);
        });

        // Post event to be discarded
        watcher.AssemblyChangedBroadcast.Post(dummyEvent);

        await discardTask;
        Assert.True(discardTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task TestProjectWatcherFileChangeTransformationBinary()
    {
        var session = CreateTestSession();
        var logger = new LoggerResult();
        using var watcher = new ProjectWatcher(session, logger, trackBinaries: true);

        var dummyAssembly = new PackageLoadedAssembly(new ProjectReference(Guid.NewGuid(), "TestProject.csproj", ProjectType.Library), "/path/to/tracked.dll");
        var tracked = new ProjectWatcher.TrackedAssembly
        {
            Package = null!,
            LoadedAssembly = dummyAssembly,
            Project = null!
        };
        watcher.TrackedAssemblies.Add(tracked);

        // Access private FileChangeTransformation via reflection
        var transformMethod = typeof(ProjectWatcher).GetMethod("FileChangeTransformation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(transformMethod);

        var fileEvent = new FileEvent(FileEventChangeType.Changed, "tracked.dll", "/path/to/tracked.dll");
        var resultTask = (Task<AssemblyChangedEvent>?)transformMethod.Invoke(watcher, [fileEvent]);
        Assert.NotNull(resultTask);

        var changedEvent = await resultTask!;
        Assert.NotNull(changedEvent);
        Assert.Equal(AssemblyChangeType.Binary, changedEvent.ChangeType);
        Assert.Equal("/path/to/tracked.dll", changedEvent.ChangedFile);
        Assert.Same(dummyAssembly, changedEvent.Assembly);
    }

    [Fact]
    public void TestProjectWatcherDisposalCancelsBatchChange()
    {
        var session = CreateTestSession();
        var logger = new LoggerResult();
        var watcher = new ProjectWatcher(session, logger, trackBinaries: true);

        // Dispose should cancel BatchChange token source and clean up
        watcher.Dispose();

        // Check cancellation source state via reflection
        var ctsField = typeof(ProjectWatcher).GetField("batchChangesCancellationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(ctsField);
        var cts = ctsField.GetValue(watcher) as CancellationTokenSource;
        Assert.NotNull(cts);
        Assert.True(cts.IsCancellationRequested);
    }
}
