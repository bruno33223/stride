// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Sprite", "2D")]
    public class SpriteEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(40, "2D");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Sprite") : "Sprite";
            var component = new SpriteComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "UI", "2D")]
    public class UIEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "UI") : "UI";
            var component = new UIComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(30, "Background", "2D")]
    public class BackgroundEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Background") : "Background";
            var component = new BackgroundComponent();
            return CreateEntityWithComponent(name, component);
        }
    }
}
