// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;
using Stride.Navigation;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Bounding box", "Navigation")]
    public class NavigationEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(60, "Navigation");
        }

        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Navigation bounding box") : "Navigation bounding box";
            var component = new NavigationBoundingBoxComponent();
            Entity result = await CreateEntityWithComponent(name, component);
            component.Size = new Vector3(1.0f);
            return result;
        }
    }
}
