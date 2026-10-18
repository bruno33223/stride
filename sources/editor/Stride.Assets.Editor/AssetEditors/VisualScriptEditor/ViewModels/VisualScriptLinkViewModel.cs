// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels;

public class VisualScriptLinkViewModel : DispatcherViewModel, IAssetPropertyProviderViewModel
{
    private readonly Link link;

    public VisualScriptLinkViewModel(VisualScriptMethodEditorViewModel method, Link link, VisualScriptSlotViewModel sourceSlot, VisualScriptSlotViewModel targetSlot)
        : base(method.SafeArgument(nameof(method)).ServiceProvider)
    {
        Method = method;
        Editor = method.Editor;
        this.link = link;
        SourceSlot = sourceSlot;
        TargetSlot = targetSlot;
    }

    public VisualScriptMethodEditorViewModel Method { get; }

    public VisualScriptSlotViewModel SourceSlot { get; }

    public VisualScriptSlotViewModel TargetSlot { get; }

    public VisualScriptEditorViewModel Editor { get; }

    public SlotKind Kind => Link.Source.Kind;

    public ObservableList<VisualScriptDiagnostic> Diagnostics { get; } = new();

    public Link Link => link;

    /// <inheritdoc/>
    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    /// <inheritdoc/>
    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Editor.Asset;

    /// <inheritdoc/>
    GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
    {
        var asset = Method.Editor.Asset.Asset;
        var path = new GraphNodePath(Method.Editor.Session.AssetNodeContainer.GetNode(asset));
        path.PushMember(nameof(VisualScriptAsset.Methods));
        path.PushTarget();
        path.PushIndex(new NodeIndex(asset.Methods.IndexOf(Method.Method.Method)));
        path.PushMember(nameof(Scripts.Method.Links));
        path.PushTarget();
        path.PushIndex(new NodeIndex(link.Id));
        path.PushTarget();
        return path;
    }

    IObjectNode IPropertyProviderViewModel.GetRootNode() => Editor.Session.AssetNodeContainer.GetNode(link);

    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => true;

    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => true;
}
