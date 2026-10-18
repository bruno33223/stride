// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace Stride.Assets.Editor.AssetEditors.UIEditor.ViewModels;

public sealed class UIElementViewModelFactory
{
    private static readonly Lazy<UIElementViewModelFactory> LazyInstance = new(() => new UIElementViewModelFactory());

    private readonly Dictionary<Type, Type> elementViewModelTypes = new();

    private UIElementViewModelFactory()
    {
        elementViewModelTypes[typeof(ContentControl)] = typeof(UIElementViewModel);
        elementViewModelTypes[typeof(Panel)] = typeof(UIElementViewModel);
    }

    public static UIElementViewModelFactory Instance => LazyInstance.Value;

    public UIElementViewModel ProvideViewModel(UIEditorBaseViewModel editor, UIBaseViewModel asset, UIElementDesign elementDesign)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(elementDesign);

        return new UIElementViewModel(editor, asset, elementDesign);
    }
}
