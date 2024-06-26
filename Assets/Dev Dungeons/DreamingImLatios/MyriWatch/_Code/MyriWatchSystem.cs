using FreeParking;
using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace DreamingImLatios.MyriWatch.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct MyriWatchSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;
        Rng                  rng;

        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            rng         = new Rng(new FixedString128Bytes("MyriWatchSystem"));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            rng.Shuffle();
            int i   = 0;
            var icb = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<WorldTransform>();
            foreach (var spawner in Query<RefRW<MyriWatchSpawner> >())
            {
                spawner.ValueRW.timeUntilNextSpawn -= Time.DeltaTime;
                var random                          = rng.GetSequence(i);
                while (spawner.ValueRW.timeUntilNextSpawn <= 0f)
                {
                    var position                                                              = random.NextFloat2(-spawner.ValueRW.radius, spawner.ValueRW.radius);
                    var transform                                                             = new TransformQvvs(new float3(position.x, 0f, position.y), quaternion.identity);
                    icb.Add(spawner.ValueRW.audioEntity, new WorldTransform { worldTransform  = transform });
                    spawner.ValueRW.timeUntilNextSpawn                                       += spawner.ValueRW.spawnInterval;
                }
                i++;
            }
        }
    }

    public partial class MyriWatchSuperSystem : RootSuperSystem
    {
        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("DreamingImLatios/MyriWatch");

        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<MyriWatchSystem>();
        }

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

