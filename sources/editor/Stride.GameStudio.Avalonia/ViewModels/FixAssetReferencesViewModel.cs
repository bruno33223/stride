// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.GameStudio.Avalonia.ViewModels;

public sealed class FixAssetReferenceItemViewModel : ViewModelBase
{
    private string sourceAssetName = string.Empty;
    private string originalPath = string.Empty;
    private string targetReference = string.Empty;
    private bool ignore;

    public FixAssetReferenceItemViewModel()
    {
    }

    public FixAssetReferenceItemViewModel(AssetViewModel? sourceAsset, string originalPath, string targetReference = "", bool ignore = false)
    {
        SourceAsset = sourceAsset;
        SourceAssetName = sourceAsset?.Url ?? string.Empty;
        OriginalPath = originalPath;
        TargetReference = targetReference;
        Ignore = ignore;
    }

    public AssetViewModel? SourceAsset { get; set; }

    public IGraphNode? Node { get; set; }

    public NodeIndex Index { get; set; } = NodeIndex.Empty;

    public object? TargetAsset { get; set; }

    public string SourceAssetName
    {
        get => sourceAssetName;
        set => SetValue(ref sourceAssetName, value);
    }

    public string OriginalPath
    {
        get => originalPath;
        set => SetValue(ref originalPath, value);
    }

    public string TargetReference
    {
        get => targetReference;
        set => SetValue(ref targetReference, value);
    }

    public bool Ignore
    {
        get => ignore;
        set => SetValue(ref ignore, value);
    }

    public ICommandBase? BrowseCommand { get; set; }
}

public sealed class FixAssetReferencesViewModel : ViewModelBase
{
    private readonly IViewModelServiceProvider serviceProvider;
    private readonly SessionViewModel? session;
    private ObservableCollection<FixAssetReferenceItemViewModel> items = new();
    private bool useSingleReplace;
    private string singleReplacement = string.Empty;

    public FixAssetReferencesViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel? session = null)
    {
        this.serviceProvider = serviceProvider;
        this.session = session;

        BrowseSingleReplacementCommand = new AnonymousTaskCommand(serviceProvider, OnBrowseSingleReplacement);
        IgnoreAllCommand = new AnonymousCommand(serviceProvider, OnIgnoreAll);
        ClearAllCommand = new AnonymousCommand(serviceProvider, OnClearAll);
    }

    public SessionViewModel? Session => session;

    public ObservableCollection<FixAssetReferenceItemViewModel> Items
    {
        get => items;
        set => SetValue(ref items, value);
    }

    public bool UseSingleReplace
    {
        get => useSingleReplace;
        set
        {
            if (SetValue(ref useSingleReplace, value))
            {
                if (value && !string.IsNullOrEmpty(SingleReplacement))
                {
                    ApplySingleReplacement();
                }
            }
        }
    }

    public string SingleReplacement
    {
        get => singleReplacement;
        set
        {
            if (SetValue(ref singleReplacement, value))
            {
                if (UseSingleReplace)
                {
                    ApplySingleReplacement();
                }
            }
        }
    }

    public ICommandBase BrowseSingleReplacementCommand { get; }
    public ICommandBase IgnoreAllCommand { get; }
    public ICommandBase ClearAllCommand { get; }

    public void AddItem(FixAssetReferenceItemViewModel item)
    {
        item.BrowseCommand ??= new AnonymousTaskCommand(serviceProvider, async () => await OnBrowseItem(item));
        Items.Add(item);
    }

    private void ApplySingleReplacement()
    {
        foreach (var item in Items)
        {
            if (!item.Ignore)
            {
                item.TargetReference = SingleReplacement;
            }
        }
    }

    private void OnIgnoreAll()
    {
        foreach (var item in Items)
        {
            item.Ignore = true;
        }
    }

    private void OnClearAll()
    {
        foreach (var item in Items)
        {
            item.TargetReference = string.Empty;
            item.Ignore = false;
        }
    }

    private async Task OnBrowseSingleReplacement()
    {
        var dialogService = serviceProvider.Get<IDialogService>();
        if (dialogService is null) return;

        var file = await dialogService.OpenFilePickerAsync();
        if (file is not null)
        {
            SingleReplacement = file.FullPath;
        }
    }

    private async Task OnBrowseItem(FixAssetReferenceItemViewModel item)
    {
        var dialogService = serviceProvider.Get<IDialogService>();
        if (dialogService is null) return;

        var file = await dialogService.OpenFilePickerAsync();
        if (file is not null)
        {
            item.TargetReference = file.FullPath;
        }
    }

    /// <summary>
    /// Preenche as referências quebradas para os assets excluídos informados.
    /// </summary>
    public void PopulateFromDeletedAssets(IEnumerable<AssetViewModel> deletedAssets)
    {
        if (session is null) return;

        var deletedList = deletedAssets.ToList();
        var deletedSet = new HashSet<AssetId>(deletedList.Select(a => a.Id));

        foreach (var deletedAsset in deletedList)
        {
            var dependencies = session.DependencyManager?.ComputeDependencies(
                deletedAsset.AssetItem.Id,
                AssetDependencySearchOptions.In,
                ContentLinkType.Reference);

            if (dependencies?.LinksIn is null) continue;

            foreach (var link in dependencies.LinksIn)
            {
                if (deletedSet.Contains(link.Item.Id)) continue;

                var referencer = session.GetAssetById(link.Item.Id);
                var item = new FixAssetReferenceItemViewModel(
                    referencer,
                    deletedAsset.Url,
                    string.Empty,
                    false);

                AddItem(item);
            }
        }
    }

    /// <summary>
    /// Aplica as correções de referências aos nós Quantum e aos assets.
    /// </summary>
    public void ApplyFixes()
    {
        foreach (var item in Items)
        {
            if (item.Ignore)
                continue;

            if (item.Node is null)
                continue;

            object? newReference = null;
            if (!string.IsNullOrEmpty(item.TargetReference) || item.TargetAsset is not null)
            {
                if (item.TargetAsset is AssetViewModel targetAssetVm)
                {
                    var referenceType = item.Node.Descriptor.GetInnerCollectionType();
                    newReference = ContentReferenceHelper.CreateReference(targetAssetVm, referenceType) ?? item.TargetAsset;
                }
                else if (item.TargetAsset is not null)
                {
                    newReference = item.TargetAsset;
                }
                else if (session is not null && !string.IsNullOrEmpty(item.TargetReference))
                {
                    var foundAsset = session.AllAssets.FirstOrDefault(a => a.Url == item.TargetReference);
                    if (foundAsset is not null)
                    {
                        var referenceType = item.Node.Descriptor.GetInnerCollectionType();
                        newReference = ContentReferenceHelper.CreateReference(foundAsset, referenceType) ?? foundAsset;
                    }
                    else
                    {
                        newReference = item.TargetReference;
                    }
                }
                else
                {
                    newReference = item.TargetReference;
                }
            }

            if (item.Node is IMemberNode memberNode)
            {
                memberNode.Update(newReference);
            }
            else if (item.Node is IObjectNode objectNode && item.Index != NodeIndex.Empty)
            {
                objectNode.Update(newReference, item.Index);
            }
        }
    }
}
