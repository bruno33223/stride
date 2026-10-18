// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public class SessionNodeContainer : AssetNodeContainer
{
    private readonly IReadOnlyList<Type> additionalPrimitiveTypes;

    public SessionNodeContainer()
    {
        additionalPrimitiveTypes = [];
    }

    public SessionNodeContainer(SessionViewModel session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Apply primitive types, commands and associated data providers that come from plugins
        var pluginService = session.ServiceProvider.Get<IAssetsPluginService>();
        additionalPrimitiveTypes = pluginService.GetPrimitiveTypes(session).ToList();
    }

    public SessionNodeContainer(IViewModelServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Apply primitive types
        var pluginService = serviceProvider.Get<IAssetsPluginService>();
        additionalPrimitiveTypes = pluginService.GetPrimitiveTypes();
    }

    public override bool IsPrimitiveType(Type type)
    {
        return base.IsPrimitiveType(type) || additionalPrimitiveTypes.Any(x => x.IsAssignableFrom(type));
    }
}
