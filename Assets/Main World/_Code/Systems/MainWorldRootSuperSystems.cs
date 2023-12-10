using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace FreeParking.MainWorld.Systems
{
    [UpdateBefore(typeof(Latios.Transforms.Systems.TransformSuperSystem))]
    [UpdateBefore(typeof(Latios.Mimic.Mecanim.Systems.MecanimSuperSystem))]
    public partial class PreTransformsMecanimSuperSystem : BaseInjectSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddManagedSystem<MainGameplay.Systems.MainGameplayPreAnimationSuperSystem>();
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

                var currentScene = worldBlackboardEntity.GetComponentData<CurrentScene>();

                m_isActive = currentScene.current == "Main World";
            }

            return m_isActive;
        }
    }
}

