// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.UI;

using UIRootViewModel = Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels.UIRootViewModel;

namespace Stride.Assets.Editor.AssetEditors.UILibraryEditor.ViewModels;

public sealed class UILibraryRootViewModel : UIRootViewModel
{
    public UILibraryRootViewModel([NotNull] UIEditorBaseViewModel editor, [NotNull] UILibraryViewModel asset, IEnumerable<UIElementDesign> rootElements)
        : base(editor, asset, rootElements)
    {
        NotifyGameSidePartAdded().Forget();
    }

    /// <inheritdoc />
    [NotNull]
    public override string Name { get => "UI Library"; set => throw new NotSupportedException("Can't change the name of a UILibrary object."); }

    /// <inheritdoc />
    public override void ReplaceRootElement([NotNull] UIElement sourcePanel, [NotNull] AssetCompositeHierarchyData<UIElementDesign, UIElement> hierarchy, Guid targetPanelId)
    {
        if (sourcePanel == null) throw new ArgumentNullException(nameof(sourcePanel));
        var index = Asset.Asset.Hierarchy.RootParts.IndexOf(x => x.Id == sourcePanel.Id);
        if (index < 0)
            throw new ArgumentException(@"The given source panel is not a root element of this asset.", nameof(sourcePanel));

        Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(Asset.Asset.Hierarchy.Parts[sourcePanel.Id]);
        Asset.AssetHierarchyPropertyGraph.AddPartToAsset(hierarchy.Parts, hierarchy.Parts[targetPanelId], null, index);
    }

    /// <inheritdoc />
    protected override bool CanAddOrInsertChildren(IReadOnlyCollection<object> children, ref string message)
    {
        return true;
    }
}
