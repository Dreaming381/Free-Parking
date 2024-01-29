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
    [UpdateBefore(typeof(Latios.Mimic.Addons.Mecanim.Systems.MecanimSuperSystem))]
    public partial class CharacterOnlySuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildEnvironmentCollisionLayerSystem>();

            GetOrCreateAndAddManagedSystem<ExampleInputSystem>();
            GetOrCreateAndAddUnmanagedSystem<ExampleMovementSystem>();
            GetOrCreateAndAddUnmanagedSystem<ExampleAnimationSystem>();

            // Add your input and animation systems here.
        }

        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("CharacterAdventures/CharactersOnly");

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }

    [UpdateBefore(typeof(Latios.Transforms.Systems.TransformSuperSystem))]
    [UpdateAfter(typeof(Latios.Mimic.Addons.Mecanim.Systems.MecanimSuperSystem))]
    public partial class CharacterOnlyIKSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<ExampleIKSystem>();
        }

        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("CharacterAdventures/CharactersOnly");

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

