// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class AssetExplorerView : UserControl
{
    public AssetExplorerView()
    {
        InitializeComponent();
    }

    private void OnListBoxDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AssetCollectionViewModel vm)
        {
            if (vm.Session.EditSelectedContentCommand.CanExecute(null))
            {
                vm.Session.EditSelectedContentCommand.Execute(null);
            }
        }
    }

    private void OnListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is AssetCollectionViewModel vm)
        {
            if (vm.Session.EditSelectedContentCommand.CanExecute(null))
            {
                vm.Session.EditSelectedContentCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
