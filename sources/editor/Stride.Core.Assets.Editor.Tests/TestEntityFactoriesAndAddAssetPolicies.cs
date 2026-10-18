// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Engine.Processors;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.ViewModels.AddAssetPolicies;
using Stride.Assets.Editor.AssetEditors.SceneEditor.ViewModels.AddAssetPolicies;
using Stride.Engine;
using Stride.Rendering.Lights;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestEntityFactoriesAndAddAssetPolicies
    {
        [Fact]
        public void TestEntityFactoryCategoriesRegistration()
        {
            // Verify categories registration
            EntityFactoryCategory.RegisterCategory(10, "Model");
            EntityFactoryCategory.RegisterCategory(20, "Light");
            EntityFactoryCategory.RegisterCategory(30, "Camera");
            EntityFactoryCategory.RegisterCategory(40, "2D");
            EntityFactoryCategory.RegisterCategory(50, "Audio");
            EntityFactoryCategory.RegisterCategory(60, "Navigation");

            var catModel = new EntityFactoryCategory("Model");
            var catLight = new EntityFactoryCategory("Light");

            var dummyFactory = new EmptyParticleSystemEntityFactory();
            catModel.AddFactory(dummyFactory, "Particle", 1);

            Assert.Equal("Model", catModel.Name);
            Assert.Single(catModel.Factories);
            Assert.True(((IComparable<EntityFactoryCategory>)catModel).CompareTo(catLight) != 0);
        }

        [Fact]
        public async Task TestEntityFactoriesCreation()
        {
            var audioEmitterFactory = new AudioEmitterFactory();
            var audioListenerFactory = new AudioListenerFactory();
            var dirLightFactory = new DirectionalLightEntityFactory();
            var pointLightFactory = new PointLightEntityFactory();
            var orthoCamFactory = new OrthographicCameraEntityFactory();
            var navFactory = new NavigationEntityFactory();
            var spriteFactory = new SpriteEntityFactory();
            var emptyParticleFactory = new EmptyParticleSystemEntityFactory();

            // None of these require session/dialog picker for simple creation:
            var audioEntity = await audioEmitterFactory.CreateEntity(null!);
            Assert.NotNull(audioEntity);
            Assert.NotNull(audioEntity.Get<AudioEmitterComponent>());

            var listenerEntity = await audioListenerFactory.CreateEntity(null!);
            Assert.NotNull(listenerEntity);
            Assert.NotNull(listenerEntity.Get<AudioListenerComponent>());

            var dirLightEntity = await dirLightFactory.CreateEntity(null!);
            Assert.NotNull(dirLightEntity);
            var lightComp = dirLightEntity.Get<LightComponent>();
            Assert.NotNull(lightComp);

            var pointLightEntity = await pointLightFactory.CreateEntity(null!);
            Assert.NotNull(pointLightEntity);
            var pointLightComp = pointLightEntity.Get<LightComponent>();
            Assert.NotNull(pointLightComp);
            Assert.IsType<LightPoint>(pointLightComp.Type);

            var orthoCamEntity = await orthoCamFactory.CreateEntity(null!);
            Assert.NotNull(orthoCamEntity);
            var camComp = orthoCamEntity.Get<CameraComponent>();
            Assert.NotNull(camComp);
            Assert.Equal(CameraProjectionMode.Orthographic, camComp.Projection);

            var navEntity = await navFactory.CreateEntity(null!);
            Assert.NotNull(navEntity);
            Assert.NotNull(navEntity.Get<Stride.Navigation.NavigationBoundingBoxComponent>());

            var spriteEntity = await spriteFactory.CreateEntity(null!);
            Assert.NotNull(spriteEntity);
            Assert.NotNull(spriteEntity.Get<SpriteComponent>());

            var particleEntity = await emptyParticleFactory.CreateEntity(null!);
            Assert.NotNull(particleEntity);
            Assert.NotNull(particleEntity.Get<Stride.Particles.Components.ParticleSystemComponent>());
        }

        [Fact]
        public void TestAddAssetPoliciesAcceptance()
        {
            var modelPolicy = new AddModelAssetPolicy();
            Assert.True(modelPolicy.Accept(typeof(Stride.Assets.Models.ModelAsset)));
            Assert.False(modelPolicy.Accept(typeof(Stride.Assets.Materials.MaterialAsset)));

            var prefabPolicy = new AddPrefabAssetPolicy();
            Assert.True(prefabPolicy.Accept(typeof(Stride.Assets.Entities.PrefabAsset)));

            var scenePolicy = new AddSceneAssetPolicy();
            Assert.True(scenePolicy.Accept(typeof(Stride.Assets.Entities.SceneAsset)));
        }
    }
}
