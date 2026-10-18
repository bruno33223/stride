// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Avalonia.AssetEditors.UIEditor.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<UILibraryEditorViewModel>]
public partial class UILibraryEditorView : UIEditorView
{
    public UILibraryEditorView()
    {
        InitializeComponent();
    }
}
