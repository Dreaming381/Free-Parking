using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace FreeParking.Systems
{
    [BurstCompile]
    public partial struct BuildEnvironmentCollisionLayerSystem : ISystem, ISystemNewScene
    {
        LatiosWorldUnmanaged           latiosWorld;
        BuildCollisionLayerTypeHandles m_handles;
        EntityQuery                    m_environmentQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            m_handles   = new BuildCollisionLayerTypeHandles(ref state);

            m_environmentQuery = state.Fluent().With<EnvironmentTag>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnNewScene(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<EnvironmentCollisionLayer>(default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_handles.Update(ref state);

            state.Dependency = Physics.BuildCollisionLayer(m_environmentQuery, in m_handles)
                               .ScheduleParallel(out var environmentLayer, Allocator.Persistent, state.Dependency);
            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new EnvironmentCollisionLayer { layer = environmentLayer });
        }
    }
}

