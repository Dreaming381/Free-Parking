using FreeParking;
using Latios;
using Latios.Transforms;
using Unity.Entities;

namespace DreamingImLatios.Welcome.Systems
{
    // You can use normal [UpdateInGroup()] and [UpdateBefore/After()] attributes here
    // to position this group relative to other Latios Framework and Unity systems.
    // RootSuperSystem is a derived class of ComponentSystemGroup.
    public partial class WelcomeSuperSystem : RootSuperSystem
    {
        bool m_isActive           = false;
        bool m_requiresEvaluation = true;

        protected override void CreateSystems()
        {
            // This is where you explicitly specify the systems you want to update in this group.
            // They will be updated in the order specified.
            GetOrCreateAndAddUnmanagedSystem<AnimateWelcomeTextSystem>();
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

                // Pass in the path to your dev dungeon.
                // Note that this is a "starts with" filter.
                // If I were to only pass in "DreamingImLatios" as the path,
                // then this group would update for all my dev dungeons.
                m_isActive = description.CurrentDevDungeonPathStartsWith("DreamingImLatios/WelcomeToDevDungeons");
            }

            return m_isActive;
        }
    }
}

