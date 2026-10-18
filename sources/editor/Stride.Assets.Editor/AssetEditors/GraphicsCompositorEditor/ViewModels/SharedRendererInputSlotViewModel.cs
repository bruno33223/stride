// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.AssetEditors.GraphicsCompositorEditor.ViewModels;

public class SharedRendererInputSlotViewModel : GraphicsCompositorSlotViewModel
{
    public SharedRendererInputSlotViewModel(SharedRendererBlockViewModel block)
        : base(block, string.Empty)
    {
    }

    public ISharedRenderer GetSharedRenderer() => Block.GetSharedRenderer();

    internal new SharedRendererBlockViewModel Block => (SharedRendererBlockViewModel)base.Block;

    public override void UpdateLink()
    {
    }

    public override bool CanLinkTo(GraphicsCompositorSlotViewModel target)
    {
        return false;
    }

    public override void LinkTo(IGraphicsCompositorSlotViewModel target)
    {
        throw new NotImplementedException();
    }

    public override void ClearLink()
    {
        throw new NotImplementedException();
    }
}
