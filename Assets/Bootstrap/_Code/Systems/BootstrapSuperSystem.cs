using Latios;
using Latios.Systems;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace FreeParking.Bootstrap.Systems
{
    [UpdateInGroup(typeof(LatiosWorldSyncGroup), OrderFirst = true)]
    [UpdateAfter(typeof(MergeBlackboardsSystem))]
    public partial class BootstrapSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddManagedSystem<ProcessBootstrapInputCommandsSystem>();
            GetOrCreateAndAddUnmanagedSystem<DisableMecanimOnPauseSystem>();
            GetOrCreateAndAddUnmanagedSystem<ShowPauseMenuSystem>();
        }
    }
}

