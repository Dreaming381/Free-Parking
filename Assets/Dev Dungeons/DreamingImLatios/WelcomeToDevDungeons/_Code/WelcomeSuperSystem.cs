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
        // This is a utility to help set up root systems to only run when they are supposed to.
        // Pass in the path to your dev dungeon.
        // Note that this is a "starts with" filter.
        // If I were to only pass in "DreamingImLatios" as the path,
        // then this group would update for all my dev dungeons.
        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("DreamingImLatios/WelcomeToDevDungeons");

        protected override void CreateSystems()
        {
            // This is where you explicitly specify the systems you want to update in this group.
            // They will be updated in the order specified.
            GetOrCreateAndAddUnmanagedSystem<AnimateWelcomeTextSystem>();
        }

        // These are both optional overrides for RootSuperSystem, but are required in Free Parking for the filter to function correctly.
        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

