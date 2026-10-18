// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Tracking;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Presentation.ViewModels;

public sealed class AssetSourcesViewModel : DispatcherViewModel
{
    private readonly AssetViewModel asset;
    private readonly HashSet<UFile> currentSourceFiles = [];
    private bool needUpdateFromSource;
    private IReadOnlyDictionary<UFile, ObjectId> updatedHashes;

    public AssetSourcesViewModel(AssetViewModel asset)
        : base(asset.ServiceProvider)
    {
        this.asset = asset;
        updatedHashes = SourceHashesHelper.GetAllHashes(asset.Asset);
        currentSourceFiles.AddRange(updatedHashes.Keys);
    }

    /// <summary>
    /// Gets or sets whether the asset related to this view model needs to be updated from its source files.
    /// </summary>
    public bool NeedUpdateFromSource
    {
        get => needUpdateFromSource;
        private set => SetValue(ref needUpdateFromSource, value);
    }

    public void ComputeNeedUpdateFromSource()
    {
        var result = false;
        foreach (var file in currentSourceFiles)
        {
            if (!updatedHashes.TryGetValue(file, out var hash))
            {
                result = true;
                break;
            }

            var latestHash = asset.Session.SourceTracker.GetCurrentHash(file);
            if (hash != latestHash)
            {
                result = true;
                break;
            }
        }

        var notifyTracker = NeedUpdateFromSource != result;

        NeedUpdateFromSource = result;

        if (notifyTracker)
            asset.Session.SourceTracker?.UpdateAssetStatus(asset);
    }

    public void UpdateUsedHashes(IReadOnlyList<UFile> files)
    {
        currentSourceFiles.Clear();
        currentSourceFiles.AddRange(files);
        ComputeNeedUpdateFromSource();
    }

    public async Task UpdateAssetFromSource(LoggerResult logger)
    {
        await asset.UpdateAssetFromSource(logger);

        var oldHashes = new Dictionary<UFile, ObjectId>(updatedHashes);
        var newHashes = new Dictionary<UFile, ObjectId>();
        foreach (var file in currentSourceFiles)
        {
            var hash = asset.Session.SourceTracker.GetCurrentHash(file);
            newHashes[file] = hash;
        }
        // Add an operation to update hashes in the SourceHashHelper
        var operation = new AnonymousDirtyingOperation(asset.Dirtiables,
            () =>
            {
                SourceHashesHelper.UpdateHashes(asset.Asset, oldHashes);
                updatedHashes = oldHashes;
                ComputeNeedUpdateFromSource();
            },
            () =>
            {
                SourceHashesHelper.UpdateHashes(asset.Asset, newHashes);
                updatedHashes = newHashes;
                ComputeNeedUpdateFromSource();
            });
        asset.UndoRedoService.PushOperation(operation);

        updatedHashes = newHashes;
        SourceHashesHelper.UpdateHashes(asset.Asset, updatedHashes);

        NeedUpdateFromSource = false;
        asset.Session.SourceTracker?.UpdateAssetStatus(asset);
    }
}
