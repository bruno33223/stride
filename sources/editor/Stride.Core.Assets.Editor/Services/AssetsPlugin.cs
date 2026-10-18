// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract class AssetsPlugin
{
    private static readonly List<AssetsPlugin> registeredPlugins = [];

    public static IReadOnlyList<AssetsPlugin> RegisteredPlugins => registeredPlugins;

    public abstract void InitializePlugin(ILogger logger);

    public virtual void InitializeSession(ISessionViewModel session)
    {
        if (session is SessionViewModel sessionVm)
        {
            InitializeSession(sessionVm);
        }
    }

    public virtual void InitializeSession(SessionViewModel session)
    {
        // Intentionally does nothing
    }

    public static AssetsPlugin RegisterPlugin(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) is null)
            throw new ArgumentException("The given type does not have a parameterless constructor.");

        if (!typeof(AssetsPlugin).IsAssignableFrom(type))
            throw new ArgumentException($"The given type does not inherit from {nameof(AssetsPlugin)}.");

        if (RegisteredPlugins.Any(x => x.GetType() == type))
            throw new InvalidOperationException("The plugin type is already registered.");

        var plugin = (AssetsPlugin)Activator.CreateInstance(type)!;
        registeredPlugins.Add(plugin);
        return plugin;
    }

    public void RegisterAssetViewModelTypes(IDictionary<Type, Type> assetViewModelTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(AssetViewModel).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetViewModelAttribute>() is { } attribute)
            {
                assetViewModelTypes[attribute.AssetType] = type;
            }
        }
    }

    public abstract void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes);

    protected internal virtual void SessionLoaded(SessionViewModel session)
    {
        // Intentionally does nothing
    }

    protected internal virtual void SessionDisposed(SessionViewModel session)
    {
        // Intentionally does nothing
    }
}
