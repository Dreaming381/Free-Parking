using FreeParking;
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
    [UpdateBefore(typeof(Latios.Mimic.Mecanim.Systems.MecanimSuperSystem))]
    public partial class PreTransformsMecanimSuperSystem : BaseInjectSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildCollisionLayersSystem>();
            GetOrCreateAndAddManagedSystem<PlayerInputSystem>();
            GetOrCreateAndAddUnmanagedSystem<ForwardCharacterControllerV1System>();
            GetOrCreateAndAddUnmanagedSystem<SpinSystem>();
        }
    }

    public abstract partial class BaseInjectSuperSystem : RootSuperSystem
    {
        bool m_isActive           = false;
        bool m_requiresEvaluation = true;

        public override void OnNewScene()
        {
            m_requiresEvaluation = true;
        }

        public override bool ShouldUpdateSystem()
        {
            if (sceneBlackboardEntity.HasComponent<PausedTag>())
                return false;

            if (m_requiresEvaluation)
            {
                m_requiresEvaluation = false;
                if (!sceneBlackboardEntity.HasComponent<CurrentDevDungeonDescription>())
                {
                    m_isActive = false;
                    return false;
                }

                var description = sceneBlackboardEntity.GetComponentData<CurrentDevDungeonDescription>();

                m_isActive = description.CurrentDevDungeonPathStartsWith("NetForce/ITWN");
            }

            return m_isActive;
        }
    }
}

