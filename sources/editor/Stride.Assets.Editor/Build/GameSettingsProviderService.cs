// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Extensions;
using Stride.Assets;
using Stride.Data;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Build
{
    /// <summary>
    /// Service that provides access to the current game settings in <see cref="Stride.Assets.Editor"/>.
    /// </summary>
    public class GameSettingsProviderService : Stride.Editor.Build.GameSettingsProviderService
    {
        public GameSettingsProviderService(SessionViewModel session)
            : base(session)
        {
        }
    }
}
