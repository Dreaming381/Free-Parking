using FreeParking;
using FreeParking.Systems;
using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace NetForce.Systems
{
    [UpdateBefore(typeof(Latios.Transforms.Systems.TransformSuperSystem))]
    public partial class PreTransformsMecanimSuperSystem : BaseInjectSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildEnvironmentCollisionLayerSystem>();
            GetOrCreateAndAddManagedSystem<PlayerInputSystem>();
            GetOrCreateAndAddUnmanagedSystem<ForwardCharacterControllerV1System>();
            GetOrCreateAndAddUnmanagedSystem<SpinSystem>();
        }
    }

    public abstract partial class BaseInjectSuperSystem : RootSuperSystem
    {
        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("NetForce/ITWN");

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

