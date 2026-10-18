// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Views;

#nullable enable

namespace Stride.Core.Assets.Editor;

public sealed class CoreAssetsEditorPlugin : AssetsEditorPlugin
{
    private readonly Dictionary<object, object> enumImagesDictionary = [];

    /// <inheritdoc />
    public override void InitializePlugin(ILogger logger)
    {
        enumImagesDictionary.Clear();
    }

    /// <inheritdoc />
    public override void InitializeSession(SessionViewModel session)
    {
    }

    /// <inheritdoc />
    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
    }

    /// <inheritdoc />
    public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
    }

    /// <inheritdoc />
    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
    }

    /// <inheritdoc />
    public override void RegisterEnumImages(IDictionary<object, object> enumImages)
    {
        foreach (var pair in enumImagesDictionary)
        {
            enumImages.Add(pair);
        }
    }

    /// <inheritdoc />
    public override void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
    {
    }

    /// <inheritdoc />
    public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
        pasteProcessors.Add(new AssetPropertyPasteProcessor());
        pasteProcessors.Add(new AssetItemPasteProcessor(session));
    }

    /// <inheritdoc />
    public override void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postPasteProcessors, SessionViewModel session)
    {
    }

    /// <inheritdoc />
    public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
    }
}
