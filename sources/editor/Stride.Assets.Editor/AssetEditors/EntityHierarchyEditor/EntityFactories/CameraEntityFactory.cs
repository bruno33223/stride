// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Rendering;
using Stride.Editor.Build;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Perspective camera", "Camera")]
    public class PerspectiveCameraEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(30, "Camera");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var slotId = default(SceneCameraSlotId);
            if (parent != null && parent.Asset != null && parent.Asset.ServiceProvider != null)
            {
                var gameSettingsService = parent.Asset.ServiceProvider.TryGet<GameSettingsProviderService>();
                var gameSettings = gameSettingsService?.CurrentGameSettings;
                if (gameSettings != null)
                {
                    var assetFinder = (IAssetFinder)parent.Asset.Session;
                    var compositor = assetFinder.FindAssetFromProxyObject(gameSettings.GraphicsCompositor)?.Asset as GraphicsCompositorAsset;
                    slotId = compositor?.Cameras.FirstOrDefault()?.ToSlotId() ?? default(SceneCameraSlotId);
                }
            }
            var name = parent != null ? ComputeNewName(parent, "Camera") : "Camera";
            var component = new CameraComponent
            {
                Projection = CameraProjectionMode.Perspective,
                Slot = slotId,
            };
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "Orthographic camera", "Camera")]
    public class OrthographicCameraEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Camera") : "Camera";
            var component = new CameraComponent
            {
                Projection = CameraProjectionMode.Orthographic,
            };
            return CreateEntityWithComponent(name, component);
        }
    }
}
