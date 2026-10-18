// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using ThumbnailData = Stride.Core.Assets.Editor.Services.ThumbnailData;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Interface for session objects.
/// </summary>
public interface ISessionObjectViewModel : Stride.Core.Assets.Presentation.ViewModels.ISessionObjectViewModel
{
    bool IsEditable { get; }

    string Name { get; set; }

    new SessionViewModel Session { get; }

    ThumbnailData? ThumbnailData { get; }

    string TypeDisplayName { get; }

    ISessionViewModel Stride.Core.Assets.Presentation.ViewModels.ISessionObjectViewModel.Session => Session;
}
