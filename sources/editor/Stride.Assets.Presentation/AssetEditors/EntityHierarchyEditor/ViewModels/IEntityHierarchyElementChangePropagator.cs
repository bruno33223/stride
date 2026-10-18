// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;

public interface IEntityHierarchyElementChangePropagator : IDisposable
{
    Task Initialized { get; }

    void NotifyGameSidePartAdded();

    void Destroy();
}

public interface IEntityHierarchyElementChangePropagatorFactory
{
    IEntityHierarchyElementChangePropagator Create([NotNull] EntityViewModel owner, [NotNull] Entity assetSidePart);
}
