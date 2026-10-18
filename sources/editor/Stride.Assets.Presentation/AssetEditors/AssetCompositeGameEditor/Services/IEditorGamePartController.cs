// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.Services
{
    public interface IEditorGamePartController
    {
        Task AddPart(EntityHierarchyElementViewModel parent, Entity assetSidePart);

        Task RemovePart(EntityHierarchyElementViewModel parent, Entity assetSidePart);
    }
}
