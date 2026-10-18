// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class DebugWindow : Window
{
    private DebugWindowViewModel? viewModel;

    public DebugWindow()
    {
        InitializeComponent();
    }

    public DebugWindow(IViewModelServiceProvider serviceProvider)
        : this()
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        viewModel = new DebugWindowViewModel(serviceProvider);
        DataContext = viewModel;
    }

    public DebugWindowViewModel? ViewModel
    {
        get => viewModel;
        init => DataContext = viewModel = value;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (viewModel is not null)
        {
            EditorDebugService.RegisterDebugWindow(viewModel);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (viewModel is not null)
        {
            EditorDebugService.UnregisterDebugWindow(viewModel);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
