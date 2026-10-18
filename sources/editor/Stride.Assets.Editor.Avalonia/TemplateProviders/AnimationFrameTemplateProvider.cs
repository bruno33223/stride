// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.TemplateProviders;

public class AnimationFrameTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "AnimationFrameTemplateProvider";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Name.Equals(nameof(AnimationAssetDuration.StartAnimationTime), StringComparison.Ordinal) ||
               node.Name.Equals(nameof(AnimationAssetDuration.EndAnimationTime), StringComparison.Ordinal);
    }
}
