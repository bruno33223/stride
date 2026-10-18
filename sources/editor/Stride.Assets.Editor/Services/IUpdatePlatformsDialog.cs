// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Services;

/// <summary>
/// This interface represents a modal dialog used to select and update package target platforms.
/// </summary>
public interface IUpdatePlatformsDialog : IModalDialog
{
    /// <summary>
    /// Gets the list of available platforms to choose from.
    /// </summary>
    IReadOnlyList<SolutionPlatformViewModel> AvailablePlatforms { get; }

    /// <summary>
    /// Gets or sets whether the force platform regeneration option is visible.
    /// </summary>
    bool ForcePlatformRegenerationVisible { get; set; }

    /// <summary>
    /// Gets or sets whether all platform projects should be forcefully regenerated.
    /// </summary>
    bool ForcePlatformRegeneration { get; set; }

    /// <summary>
    /// Gets the sequence of platforms selected by the user.
    /// </summary>
    IEnumerable<SelectedSolutionPlatform> SelectedPlatforms { get; }
}
