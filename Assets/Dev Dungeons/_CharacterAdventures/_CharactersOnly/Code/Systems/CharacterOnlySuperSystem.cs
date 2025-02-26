using FreeParking;
using FreeParking.Systems;
using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CharacterAdventures.Systems
{
    [UpdateBefore(typeof(Latios.Transforms.Systems.TransformSuperSystem))]
    public partial class CharacterOnlySuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildEnvironmentCollisionLayerSystem>();

            GetOrCreateAndAddManagedSystem<ExampleInputSystem>();
            GetOrCreateAndAddUnmanagedSystem<ExampleMovementSystem>();
            GetOrCreateAndAddUnmanagedSystem<ExampleAnimationSystem>();

            // Add your input and animation systems here.
            GetOrCreateAndAddUnmanagedSystem<ImpMovementSystem>();
            GetOrCreateAndAddUnmanagedSystem<ImpAnimationSystem>();
        }

        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("CharacterAdventures/CharactersOnly");

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }

    [UpdateBefore(typeof(Latios.Transforms.Systems.TransformSuperSystem))]
    [UpdateAfter(typeof(CharacterOnlySuperSystem))]
    public partial class CharacterOnlyIKSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<ExampleIKSystem>();
            GetOrCreateAndAddUnmanagedSystem<TacticalGuyIKSystem>();
        }

        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("CharacterAdventures/CharactersOnly");

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

