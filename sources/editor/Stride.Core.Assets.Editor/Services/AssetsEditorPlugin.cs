// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Views;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract class AssetsEditorPlugin : AssetsPlugin
{
    protected static readonly Dictionary<Type, object> TypeImages = [];

    // TODO: give access to this differently
    public readonly List<PackageSettingsEntry> ProfileSettings = [];

    public static IReadOnlyDictionary<Type, object> TypeImagesDictionary => TypeImages;

    public void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if ((typeof(IAssetEditorViewModel).IsAssignableFrom(type) || typeof(AssetEditorViewModel).IsAssignableFrom(type)) &&
                type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } attribute)
            {
                assetEditorViewModelTypes.Add(attribute.ViewModelType, type);
            }
        }
    }

    public virtual void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(IAssetEditorView).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetEditorViewAttribute>(inherit: false) is { } attribute)
            {
                assetEditorViewTypes.TryAdd(attribute.EditorViewModelType, type);
            }
        }
    }

    public abstract void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes);

    public abstract void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes);

    public virtual void RegisterEnumImages(IDictionary<object, object> enumImages)
    {
    }

    public virtual void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
    {
    }

    public virtual void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
    }

    public virtual void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postPasteProcessors, SessionViewModel session)
    {
    }

    public virtual void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
    }
}
