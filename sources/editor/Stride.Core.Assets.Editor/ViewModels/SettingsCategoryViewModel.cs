// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Forwarding class for backwards-compatibility.
/// </summary>
public class SettingsCategoryViewModel : Settings.ViewModels.SettingsCategoryViewModel
{
    public SettingsCategoryViewModel(IViewModelServiceProvider serviceProvider, SettingsProfile profile, string name, Settings.ViewModels.SettingsCategoryViewModel? parent, AssetNodeContainer nodeContainer)
        : base(serviceProvider, profile, name, parent, nodeContainer)
    {
    }
}
