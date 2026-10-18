// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.ViewModels;
using Stride.Engine;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Empty particle system", "Particle system")]
    public class EmptyParticleSystemEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(50, "Particle system");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Empty particle system") : "Empty particle system";
            var component = new ParticleSystemComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "Simple particle system", "Particle system")]
    public class SimpleParticleSystemEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Simple particle system") : "Simple particle system";
            var component = new ParticleSystemComponent();
            var emitter = new ParticleEmitter { ParticleLifetime = new Vector2(1, 2) };

            var spawner = new SpawnerPerSecond();
            emitter.Spawners.Add(spawner);

            var randSize = new InitialSizeSeed { RandomSize = new Vector2(0.05f, 0.1f) };
            emitter.Initializers.Add(randSize);
            emitter.Initializers.Add(new InitialPositionSeed());

            var randVel = new InitialVelocitySeed
            {
                VelocityMin = new Vector3(-0.5f, 1f, -0.5f),
                VelocityMax = new Vector3(0.5f, 3, 0.5f)
            };
            emitter.Initializers.Add(randVel);

            var gravity = new UpdaterForceField
            {
                EnergyConservation = 1f,
                FieldShape = null
            };
            gravity.FieldFalloff.StrengthInside = gravity.FieldFalloff.StrengthOutside = 1f;
            gravity.ForceFixed = new Vector3(0, -9.81f, 0);
            gravity.ForceDirected = gravity.ForceRepulsive = gravity.ForceVortex = 0f;
            emitter.Updaters.Add(gravity);

            component.ParticleSystem.Emitters.Add(emitter);
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(30, "Fountain particle system", "Particle system")]
    public class SmokeParticleSystemEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = parent != null ? ComputeNewName(parent, "Smoke particle system") : "Smoke particle system";
            var component = new ParticleSystemComponent();
            var emitter = new ParticleEmitter { ParticleLifetime = new Vector2(1, 2) };
            var spawner = new SpawnerPerSecond { SpawnCount = 20 };
            emitter.Spawners.Add(spawner);

            var randSize = new InitialSizeSeed { RandomSize = new Vector2(0.1f, 0.2f) };
            emitter.Initializers.Add(randSize);
            emitter.Initializers.Add(new InitialPositionSeed());

            component.ParticleSystem.Emitters.Add(emitter);
            return CreateEntityWithComponent(name, component);
        }
    }
}
