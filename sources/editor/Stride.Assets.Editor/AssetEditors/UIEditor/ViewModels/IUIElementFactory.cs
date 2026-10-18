// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.UI;
using Stride.Core.Assets;
using Stride.UI;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

public interface IUIElementFactory
{
    string Category { get; }

    string Name { get; }

    Type? ElementType { get; }

    string? IconData { get; }

    AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset);
}
