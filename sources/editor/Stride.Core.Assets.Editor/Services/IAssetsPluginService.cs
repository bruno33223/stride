// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public interface IAssetsPluginService
{
    IReadOnlyCollection<AssetsPlugin> Plugins { get; }

    bool HasImagesForEnum(SessionViewModel? session, Type enumType) => false;

    object? GetImageForEnum(SessionViewModel? session, object value) => null;

    IEnumerable<Type> GetPrimitiveTypes(SessionViewModel? session) => GetPrimitiveTypes();

    IReadOnlyList<Type> GetPrimitiveTypes();

    bool HasEditorView(SessionViewModel session, Type viewModelType) => GetEditorViewModelType(viewModelType) != null;

    Type? GetAssetViewModelType(Type assetType);

    Type? GetEditorViewModelType(Type viewModelType);

    Type? GetEditorViewType(Type editorViewModelType);

    Type? GetPreviewViewModelType(Type previewType);

    Type? GetPreviewViewType(Type previewType);

    void RegisterSession(SessionViewModel session, ILogger logger) => EnsureInitialized(logger);

    void EnsureInitialized(ILogger logger);
}
