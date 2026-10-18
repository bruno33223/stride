// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

public sealed class SpriteFontAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    private static readonly IEnumerable<string> InstalledFonts;

    static SpriteFontAssetNodeUpdater()
    {
        // Safe cross-platform fonts list for Linux / Windows
        InstalledFonts = new[]
        {
            "Arial",
            "Calibri",
            "Courier New",
            "DejaVu Sans",
            "DejaVu Serif",
            "Liberation Sans",
            "Liberation Serif",
            "Segoe UI",
            "Tahoma",
            "Times New Roman",
            "Trebuchet MS",
            "Verdana"
        };
    }

    protected override void UpdateNode(IAssetNodePresenter node)
    {
        // Root node
        if (typeof(SpriteFontAsset).IsAssignableFrom(node.Type))
        {
            node.AttachedProperties.Add(SpriteFontData.Key, InstalledFonts);
        }
    }
}
