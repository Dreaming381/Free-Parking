using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace NetForce.Systems
{
    [BurstCompile]
    public partial struct SpinSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job { dt = Time.DeltaTime }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float dt;

            public void Execute(TransformAspect transform, in Spin spin)
            {
                var rotation = quaternion.AxisAngle(spin.axis, spin.radPerSecCW * dt);
                if (spin.applyInWorldSpace)
                    transform.RotateWorld(rotation);
                else
                    transform.RotateLocal(rotation);
            }
        }
    }
}

