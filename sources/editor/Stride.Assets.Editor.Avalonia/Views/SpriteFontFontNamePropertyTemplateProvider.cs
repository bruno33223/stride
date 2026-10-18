// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

public class SpriteFontFontNamePropertyTemplateProvider : NodeViewModelTemplateProvider
{
    public static readonly string FontNamePropertyName = nameof(SystemFontProvider.FontName);

    public override string Name => "SpriteFontFontName";

    public override bool MatchNode(NodeViewModel node)
    {
        if (!typeof(SpriteFontAsset).IsAssignableFrom(node.Root.Type))
            return false;

        return node.Name == FontNamePropertyName;
    }
}
