// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views.DebugTools;

/// <summary>
/// Interaction logic for DebugAssetNodesUserControl.axaml
/// </summary>
public partial class DebugAssetNodesUserControl : UserControl, IDebugPage
{
    public DebugAssetNodesUserControl()
    {
        InitializeComponent();
    }

    public DebugAssetNodesUserControl(SessionViewModel session)
    {
        InitializeComponent();
        DataContext = new DebugAssetNodeCollectionViewModel(session);
    }

    public string Title { get; set; } = "Quantum Nodes";

    public DebugAssetNodeCollectionViewModel? ViewModel => DataContext as DebugAssetNodeCollectionViewModel;
}
