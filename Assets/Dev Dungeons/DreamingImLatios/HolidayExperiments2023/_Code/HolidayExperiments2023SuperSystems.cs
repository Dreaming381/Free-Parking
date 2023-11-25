using FreeParking;
using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DreamingImLatios.Holiday2023.Systems
{
    public partial class Holiday2023SuperSystem : RootSuperSystem
    {
        bool m_isActive           = false;
        bool m_requiresEvaluation = true;

        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<MeshDeformTestSystem>();
        }

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
                m_isActive      = description.CurrentDevDungeonPathStartsWith("DreamingImLatios/HolidayExperiments2023");
            }

            return m_isActive;
        }
    }
}

