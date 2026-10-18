// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Avalonia.Controls;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Settings.ViewModels;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public class SettingsTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "SettingsEntry";

    public override bool MatchNode(NodeViewModel node)
    {
        return node?.NodeValue is PackageSettingsWrapper.SettingsKeyWrapper;
    }
}

public class SettingsStringFromAcceptableValuesTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "StringFromAcceptableValues";

    public override bool MatchNode(NodeViewModel node)
    {
        return node?.Parent != null && node.Parent.AssociatedData.TryGetValue("HasAcceptableValues", out var hasAcceptableValues) && hasAcceptableValues is bool b && b;
    }
}

public class SettingsCommandTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "SettingsCommand";

    public override bool MatchNode(NodeViewModel node)
    {
        return node?.NodeValue is global::Stride.Core.Assets.Editor.Settings.SettingsCommand;
    }
}

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(IViewModelServiceProvider serviceProvider)
        : this()
    {
        DataContext = new SettingsViewModel(serviceProvider, EditorSettings.SettingsContainer.CurrentProfile);
    }
}
