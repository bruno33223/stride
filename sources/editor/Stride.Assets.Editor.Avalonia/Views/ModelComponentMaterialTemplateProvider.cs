// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.Avalonia.Views;

public class ModelComponentMaterialTemplateProvider : ContentReferenceTemplateProvider
{
    public ModelComponentMaterialTemplateProvider()
    {
        DynamicThumbnail = true;
    }

    public override string Name => "ModelComponentMaterial";

    public override bool MatchNode(NodeViewModel node)
    {
        return base.MatchNode(node)
            && node.NodeValue is Material
            && node.Parent?.Parent?.NodeValue is ModelComponent;
    }
}
