// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia.Controls;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views.DebugTools;

/// <summary>
/// Interaction logic for DebugLogUserControl.axaml
/// </summary>
public partial class DebugLogUserControl : UserControl, IDebugPage
{
    public DebugLogUserControl()
    {
        InitializeComponent();
    }

    public DebugLogUserControl(LoggerViewModel loggerViewModel)
    {
        ArgumentNullException.ThrowIfNull(loggerViewModel);
        Logger = loggerViewModel;
        DataContext = loggerViewModel;
        InitializeComponent();
    }

    public string Title { get; set; } = "Debug Log";

    public LoggerViewModel? Logger { get; private set; }
}
