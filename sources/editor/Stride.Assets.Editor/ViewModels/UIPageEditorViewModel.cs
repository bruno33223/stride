// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Assets.Editor.AssetEditors.UIPageEditor.Services;
using Stride.Assets.Editor.AssetEditors.UIPageEditor.ViewModels;
using Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.ViewModels;

using UIPageRootViewModel = Stride.Assets.Editor.AssetEditors.UIPageEditor.ViewModels.UIPageRootViewModel;
using UIHierarchyItemViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIHierarchyItemViewModel;
using UIElementViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIElementViewModel;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<UIPageViewModel>]
public sealed class UIPageEditorViewModel : UIEditorBaseViewModel, IAssetEditorViewModel<UIPageViewModel>
{
    public UIPageEditorViewModel(UIPageViewModel asset)
        : this(asset, x => new UIPageEditorController(asset, (UIPageEditorViewModel)x))
    {
    }

    public UIPageEditorViewModel(UIPageViewModel asset, Func<UIEditorBaseViewModel, IEditorGameController>? controllerFactory)
        : base(asset, controllerFactory)
    {
    }

    /// <inheritdoc />
    public override UIPageViewModel Asset => (UIPageViewModel)base.Asset;

    private UIPageRootViewModel? UIPage => RootPart as UIPageRootViewModel;

    protected override bool CanPaste(bool asRoot)
    {
        if (!base.CanPaste(asRoot))
            return false;

        return !asRoot || UIPage?.RootElement == null;
    }

    /// <inheritdoc />
    protected override UIHierarchyItemViewModel CreateRootPartViewModel()
    {
        var rootParts = Asset.Asset.Hierarchy.EnumerateRootPartDesigns();
        return new UIPageRootViewModel(this, Asset, rootParts.SingleOrDefault());
    }

    /// <inheritdoc />
    public override async Task<bool> InitializeEditor()
    {
        if (!await base.InitializeEditor())
            return false;

        ActiveRoot = UIPage?.RootElement;
        return true;
    }
}
