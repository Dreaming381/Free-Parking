using Latios;
using Latios.Kinemation;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace FreeParking.Bootstrap.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct DisableMecanimOnPauseSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;
        NativeList<Entity>   m_entitiesToResume;
        bool                 m_previouslyPaused;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld        = state.GetLatiosWorldUnmanaged();
            m_entitiesToResume = new NativeList<Entity>(Allocator.Persistent);
            m_previouslyPaused = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            state.CompleteDependency();
            m_entitiesToResume.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var paused = latiosWorld.sceneBlackboardEntity.HasComponent<PausedTag>();
            if (paused && !m_previouslyPaused)
            {
                m_entitiesToResume.Clear();
                new DisableJob { entities = m_entitiesToResume }.Schedule();
            }
            else if (!paused && m_previouslyPaused)
            {
                state.Dependency = new EnableJob
                {
                    entities      = m_entitiesToResume.AsDeferredJobArray(),
                    enabledLookup = GetComponentLookup<MecanimControllerEnabledFlag>(),
                }.Schedule(state.Dependency);
            }
            m_previouslyPaused = paused;
        }

        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        [BurstCompile]
        partial struct DisableJob : IJobEntity
        {
            public NativeList<Entity> entities;

            public void Execute(Entity entity, EnabledRefRW<MecanimControllerEnabledFlag> enabled)
            {
                entities.Add(entity);
                enabled.ValueRW = false;
            }
        }

        [BurstCompile]
        struct EnableJob : IJob
        {
            [ReadOnly] public NativeArray<Entity>                entities;
            public ComponentLookup<MecanimControllerEnabledFlag> enabledLookup;

            public void Execute()
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var enabled = enabledLookup.GetComponentEnabledRefRWOptional<MecanimControllerEnabledFlag>(entities[i]);
                    if (enabled.IsValid)
                        enabled.ValueRW = true;
                }
            }
        }
    }
}

