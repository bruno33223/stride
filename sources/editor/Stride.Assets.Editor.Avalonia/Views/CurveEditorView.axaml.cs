// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;

namespace Stride.Assets.Editor.Avalonia.Views;

public partial class CurveEditorView : UserControl
{
    public CurveEditorView()
    {
        InitializeComponent();
        Focusable = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
    }
}
