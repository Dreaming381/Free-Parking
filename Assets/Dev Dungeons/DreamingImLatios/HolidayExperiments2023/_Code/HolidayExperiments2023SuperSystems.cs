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
        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("DreamingImLatios/HolidayExperiments2023");

        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<MeshDeformTestSystem>();
        }

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

