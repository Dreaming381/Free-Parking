using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace FreeParking
{
    public struct DevDungeonSystemFilter
    {
        bool                m_isActive;
        bool                m_requiresEvaluation;
        FixedString512Bytes m_nameToFilter;

        public DevDungeonSystemFilter(FixedString512Bytes startsWith)
        {
            m_isActive           = false;
            m_requiresEvaluation = true;
            m_nameToFilter       = startsWith;
        }

        public void OnNewScene() => m_requiresEvaluation = true;

        public bool ShouldUpdateSystem(BlackboardEntity sceneBlackboardEntity)
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
                m_isActive      = description.CurrentDevDungeonPathStartsWith(m_nameToFilter);
            }

            return m_isActive;
        }
    }
}

