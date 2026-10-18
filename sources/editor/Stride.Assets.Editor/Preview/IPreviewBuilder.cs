// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Preview;

/// <summary>
/// An interface that represents an object which is capable of building previews for assets.
/// </summary>
public interface IPreviewBuilder : Stride.Editor.Preview.IPreviewBuilder
{
    /// <summary>
    /// Gets the asset builder service used to build asset.
    /// </summary>
    new GameStudioBuilderService AssetBuilderService { get; }

    /// <summary>
    /// Gets the <see cref="IDispatcherService"/> to use to update UI.
    /// </summary>
    new IDispatcherService Dispatcher { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/> to use for preview logs.
    /// </summary>
    new Logger Logger { get; }

    /// <summary>
    /// Gets the instance of <see cref="PreviewGame"/> to use for preview.
    /// </summary>
    new PreviewGame PreviewGame { get; }

    /// <summary>
    /// Compiles the given asset (and its dependencies).
    /// </summary>
    /// <param name="asset">The asset to compile.</param>
    /// <returns>An <see cref="AssetCompilerResult"/> containing the generated build steps.</returns>
    new AssetCompilerResult Compile(AssetItem asset);

    /// <summary>
    /// Gets the framework element that contains the stride viewport.
    /// </summary>
    /// <returns></returns>
    new object GetStrideView();
}
