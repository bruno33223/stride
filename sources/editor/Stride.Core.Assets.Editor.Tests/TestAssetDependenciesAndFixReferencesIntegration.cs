// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.FixReferences;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using SessionObjectViewModel = Stride.Core.Assets.Presentation.ViewModels.SessionObjectViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests;

public class TestAssetDependenciesAndFixReferencesIntegration
{
    private class DummyDialog : IFixReferencesDialog
    {
        public object? DataContext { get; set; }
        public DialogResult Result { get; private set; }
        public bool ReferenceFixesApplied { get; private set; }

        public void ApplyReferenceFixes()
        {
            ReferenceFixesApplied = true;
        }

        public void RequestClose(DialogResult result)
        {
            Result = result;
        }

        public Task<DialogResult> ShowModal()
        {
            return Task.FromResult(Result);
        }
    }

    public class DummyFixTargetViewModel : ViewModelBase
    {
        public DummyFixTargetViewModel(string name, IViewModelServiceProvider sp) : base(sp)
        {
            Name = name;
        }

        public string Name { get; set; }
        public DummyFixTargetViewModel? TargetReference { get; set; }
    }

    public class DummyReplacementViewModel : ReferenceReplacementViewModel<DummyFixTargetViewModel>
    {
        public bool WasFixed { get; private set; }
        public bool WasCleared { get; private set; }

        public DummyReplacementViewModel(
            FixReferencesViewModel<DummyFixTargetViewModel> fixReferences,
            DummyFixTargetViewModel objectToFix,
            DummyFixTargetViewModel referencer,
            Type propertyType)
            : base(fixReferences, objectToFix, referencer, propertyType)
        {
        }

        public override void ClearReference()
        {
            WasCleared = true;
            Referencer.TargetReference = null;
        }

        protected override void ReplaceReference()
        {
            WasFixed = true;
            Referencer.TargetReference = ReplacementObject;
        }
    }

    public class ConcreteFixReferencesViewModel : FixReferencesViewModel<DummyFixTargetViewModel>
    {
        public Dictionary<DummyFixTargetViewModel, List<DummyFixTargetViewModel>> ReferencersStore { get; } = new();
        public DummyFixTargetViewModel? ObjectToPick { get; set; }

        public ConcreteFixReferencesViewModel(IViewModelServiceProvider serviceProvider, IFixReferencesDialog dialog)
            : base(serviceProvider, dialog)
        {
        }

        public override Task<DummyFixTargetViewModel?> PickupObject(DummyFixTargetViewModel objectToFix, Type propertyType)
        {
            return Task.FromResult(ObjectToPick);
        }

        protected override IEnumerable<KeyValuePair<object, List<DummyFixTargetViewModel>>> FindReferencers(DummyFixTargetViewModel deletedObject)
        {
            if (ReferencersStore.TryGetValue(deletedObject, out var list))
            {
                yield return new KeyValuePair<object, List<DummyFixTargetViewModel>>(deletedObject, list);
            }
        }

        protected override IEnumerable<ReferenceReplacementViewModel<DummyFixTargetViewModel>> GetReplacementsForReferencer(DummyFixTargetViewModel referencer, object referencedMember)
        {
            yield return new DummyReplacementViewModel(this, (DummyFixTargetViewModel)referencedMember, referencer, typeof(DummyFixTargetViewModel));
        }
    }

    private static (SessionViewModel Session, AssetCollectionViewModel Collection, IViewModelServiceProvider ServiceProvider) CreateMockSessionEnvironment()
    {
        var sp = new ViewModelServiceProvider();
        var dispatcher = new NullDispatcherService();
        var undoRedo = new UndoRedoService(100);
        sp.RegisterService(dispatcher);
        sp.RegisterService(undoRedo);

        var sessionVm = (SessionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(SessionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(sessionVm, [sp]);
        typeof(DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sessionVm, dispatcher);

        var packageSession = new PackageSession();
        typeof(SessionViewModel).GetField("session", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sessionVm, packageSession);

        var collection = (AssetCollectionViewModel)RuntimeHelpers.GetUninitializedObject(typeof(AssetCollectionViewModel));
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(collection, [sp]);
        typeof(DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(collection, dispatcher);
        typeof(AssetCollectionViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(collection, sessionVm);

        var selectedAssetsField = typeof(AssetCollectionViewModel).GetField("selectedAssets", BindingFlags.Instance | BindingFlags.NonPublic);
        var selectedAssets = new ObservableSet<AssetViewModel>();
        selectedAssetsField?.SetValue(collection, selectedAssets);

        return (sessionVm, collection, sp);
    }

    private static void SelectAsset(AssetCollectionViewModel collection, AssetViewModel asset)
    {
        var selected = (ObservableSet<AssetViewModel>)typeof(AssetCollectionViewModel).GetField("selectedAssets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(collection)!;
        selected.Add(asset);
    }

    private static void ClearSelectedAssets(AssetCollectionViewModel collection)
    {
        var selected = (ObservableSet<AssetViewModel>)typeof(AssetCollectionViewModel).GetField("selectedAssets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(collection)!;
        selected.Clear();
    }

    private static AssetViewModel CreateMockAssetViewModel(SessionViewModel session, IViewModelServiceProvider sp, string url)
    {
        var assetVm = (AssetViewModel)RuntimeHelpers.GetUninitializedObject(typeof(AssetViewModel<RawAsset>));
        typeof(ViewModelBase).GetProperty("ServiceProvider")!.GetSetMethod(true)!.Invoke(assetVm, [sp]);
        typeof(DispatcherViewModel).GetField("<Dispatcher>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, sp.Get<IDispatcherService>());
        typeof(SessionObjectViewModel).GetField("<Session>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, session);
        var assetItem = new AssetItem(url, new RawAsset());
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, assetItem);

        var tags = new ObservableList<string>();
        typeof(AssetViewModel).GetField("<Tags>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, tags);

        var deps = new AssetDependenciesViewModel(assetVm, false);
        typeof(AssetViewModel).GetField("<Dependencies>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, deps);

        var sources = new AssetSourcesViewModel(assetVm);
        typeof(AssetViewModel).GetField("<Sources>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(assetVm, sources);

        return assetVm;
    }

    [Fact]
    public void TestFixReferencesViewModelLifecycleAndReplacements()
    {
        var sp = new ViewModelServiceProvider();
        var dispatcher = new NullDispatcherService();
        sp.RegisterService(dispatcher);

        var dialog = new DummyDialog();
        var fixVm = new ConcreteFixReferencesViewModel(sp, dialog);

        var itemToDelete1 = new DummyFixTargetViewModel("Deleted1", sp);
        var itemToDelete2 = new DummyFixTargetViewModel("Deleted2", sp);
        var referencerA = new DummyFixTargetViewModel("ReferencerA", sp) { TargetReference = itemToDelete1 };
        var referencerB = new DummyFixTargetViewModel("ReferencerB", sp) { TargetReference = itemToDelete2 };
        var replacementItem = new DummyFixTargetViewModel("Replacement", sp);

        fixVm.ReferencersStore[itemToDelete1] = new List<DummyFixTargetViewModel> { referencerA };
        fixVm.ReferencersStore[itemToDelete2] = new List<DummyFixTargetViewModel> { referencerB };

        fixVm.Initialize(new[] { itemToDelete1, itemToDelete2 });

        Assert.Equal(2, fixVm.ObjectsToFixCount);
        Assert.Equal(1, fixVm.FixedObjectsCount);
        Assert.Equal("1/2", fixVm.FixedObjectsCounter);
        Assert.Same(itemToDelete1, fixVm.CurrentObjectToReplace);
        Assert.True(fixVm.IsInObjectsToFixList(itemToDelete1));
        Assert.True(fixVm.IsInObjectsToFixList(itemToDelete2));

        // Use Single Replace mode
        fixVm.UseSingleReplace = true;
        fixVm.SingleReplacementObject = replacementItem;
        Assert.True(fixVm.FixReferencesCommand.IsEnabled);

        // Fix first reference
        fixVm.FixReferencesCommand.Execute();

        // Should have advanced to itemToDelete2
        Assert.Equal(2, fixVm.FixedObjectsCount);
        Assert.Equal("2/2", fixVm.FixedObjectsCounter);
        Assert.Same(itemToDelete2, fixVm.CurrentObjectToReplace);

        // Clear second reference
        Assert.True(fixVm.ClearReferencesCommand.IsEnabled);
        fixVm.ClearReferencesCommand.Execute();

        // All issues resolved -> Dialog closed
        Assert.Equal(DialogResult.Ok, dialog.Result);

        // Process fixes
        fixVm.ProcessFixes();

        Assert.Same(replacementItem, referencerA.TargetReference);
        Assert.Null(referencerB.TargetReference);
    }

    [Fact]
    public void TestFixReferencesViewModelClearAllWorkflow()
    {
        var sp = new ViewModelServiceProvider();
        sp.RegisterService(new NullDispatcherService());

        var dialog = new DummyDialog();
        var fixVm = new ConcreteFixReferencesViewModel(sp, dialog);

        var item1 = new DummyFixTargetViewModel("Del1", sp);
        var item2 = new DummyFixTargetViewModel("Del2", sp);
        var ref1 = new DummyFixTargetViewModel("Ref1", sp) { TargetReference = item1 };
        var ref2 = new DummyFixTargetViewModel("Ref2", sp) { TargetReference = item2 };

        fixVm.ReferencersStore[item1] = new List<DummyFixTargetViewModel> { ref1 };
        fixVm.ReferencersStore[item2] = new List<DummyFixTargetViewModel> { ref2 };

        fixVm.Initialize(new[] { item1, item2 });
        fixVm.ClearAllReferencesCommand.Execute();

        Assert.Equal(DialogResult.Ok, dialog.Result);

        fixVm.ProcessFixes();
        Assert.Null(ref1.TargetReference);
        Assert.Null(ref2.TargetReference);
    }

    [Fact]
    public void TestTagsViewModelAddRemoveAndCounter()
    {
        var (session, collection, sp) = CreateMockSessionEnvironment();
        var asset1 = CreateMockAssetViewModel(session, sp, "Assets/Texture1");
        var asset2 = CreateMockAssetViewModel(session, sp, "Assets/Texture2");

        var tagsVm = new TagsViewModel(collection);
        Assert.NotNull(tagsVm.Tags);
        Assert.Empty(tagsVm.Tags);

        // Select asset1
        SelectAsset(collection, asset1);

        // Add Tag "LevelDesign"
        Assert.True(tagsVm.AddTagCommand.IsEnabled);
        tagsVm.AddTagCommand.Execute("LevelDesign");

        Assert.Single(tagsVm.Tags);
        var tagVm = tagsVm.Tags.First();
        Assert.Equal("LevelDesign", tagVm.Name);
        Assert.Contains("LevelDesign", asset1.Tags);
        Assert.Equal("", tagVm.Counter); // Single asset selected: counter is empty

        // Select asset2 as well
        SelectAsset(collection, asset2);
        // After selecting asset2, only asset1 has the tag, so counter should be " (1)"
        tagVm = tagsVm.Tags.FirstOrDefault(t => t.Name == "LevelDesign");
        Assert.NotNull(tagVm);
        Assert.Equal(" (1)", tagVm.Counter);

        // Add tag to all
        tagVm.AddTagToAllCommand.Execute();
        Assert.Contains("LevelDesign", asset2.Tags);
        Assert.Equal(" (all)", tagVm.Counter);

        // Remove tag
        tagVm.RemoveTagCommand.Execute();
        Assert.DoesNotContain("LevelDesign", asset1.Tags);
        Assert.DoesNotContain("LevelDesign", asset2.Tags);
        Assert.Empty(tagVm.Assets);
        Assert.Equal(" (0)", tagVm.Counter);

        // When selection is refreshed, the tag without assets is cleared from Tags collection
        ClearSelectedAssets(collection);
        SelectAsset(collection, asset1);
        Assert.Empty(tagsVm.Tags);
    }

    [Fact]
    public void TestAssetSourceTrackerViewModelCommandsAndStatus()
    {
        var (session, _, sp) = CreateMockSessionEnvironment();
        var tracker = new AssetSourceTrackerViewModel(session);

        Assert.NotNull(tracker.UpdateSelectedAssetsFromSourceCommand);
        Assert.NotNull(tracker.UpdateAllAssetsWithModifiedSourceCommand);
        Assert.NotNull(tracker.AssetsToUpdate);
        Assert.Empty(tracker.AssetsToUpdate);
        Assert.False(tracker.UpdateAllAssetsWithModifiedSourceCommand.IsEnabled);

        var asset = CreateMockAssetViewModel(session, sp, "Assets/Model1");

        // Set NeedUpdateFromSource to true via reflection
        typeof(AssetSourcesViewModel).GetProperty("NeedUpdateFromSource")!.GetSetMethod(true)!.Invoke(asset.Sources, [true]);

        tracker.UpdateAssetStatus(asset);
        Assert.Single(tracker.AssetsToUpdate);
        Assert.Contains(asset, tracker.AssetsToUpdate);
        Assert.True(tracker.UpdateAllAssetsWithModifiedSourceCommand.IsEnabled);

        // Set NeedUpdateFromSource to false
        typeof(AssetSourcesViewModel).GetProperty("NeedUpdateFromSource")!.GetSetMethod(true)!.Invoke(asset.Sources, [false]);

        tracker.UpdateAssetStatus(asset);
        Assert.Empty(tracker.AssetsToUpdate);
        Assert.False(tracker.UpdateAllAssetsWithModifiedSourceCommand.IsEnabled);
    }

    [Fact]
    public void TestAssetDependenciesViewModelRootsAndExclusion()
    {
        var (session, _, sp) = CreateMockSessionEnvironment();
        var asset = CreateMockAssetViewModel(session, sp, "Assets/Material1");

        var deps = new AssetDependenciesViewModel(asset, forcedRoot: false);
        Assert.False(deps.ForcedRoot);
        Assert.False(deps.IsRoot);
        Assert.True(deps.IsExcluded);
        Assert.False(deps.IsIndirectlyIncluded);

        var forcedDeps = new AssetDependenciesViewModel(asset, forcedRoot: true);
        Assert.True(forcedDeps.ForcedRoot);

        Assert.NotNull(deps.ToggleIsRootOnSelectedAssetCommand);
        Assert.NotNull(deps.ReferencerAssets);
        Assert.NotNull(deps.ReferencedAssets);
        Assert.NotNull(deps.RecursiveReferencerAssets);
        Assert.NotNull(deps.RecursiveReferencedAssets);

        deps.NotifyRootAssetChange(notifyReferencedAssets: false);
    }
}
