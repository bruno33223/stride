// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Thumbnails;

/// <summary>
/// Service responsible for asynchronous thumbnail compilation, prioritization and lifecycle events in GameStudio.
/// </summary>
[Stride.Core.DataContractIgnore]
public class GameStudioThumbnailService : Stride.Editor.Thumbnails.GameStudioThumbnailService
{
    public GameStudioThumbnailService(SessionViewModel session, GameSettingsProviderService settingsProvider, GameStudioBuilderService assetBuilderService)
        : base(session, settingsProvider, assetBuilderService)
    {
    }

    /// <summary>
    /// Generates a placeholder thumbnail indicating a broken asset.
    /// </summary>
    public static new byte[] HandleBrokenThumbnail()
    {
        return Stride.Editor.Thumbnails.GameStudioThumbnailService.HandleBrokenThumbnail();
    }
}
