using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace CharacterAdventures.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct ExampleAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            new Job { dt = SystemAPI.Time.DeltaTime }.Run();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float dt;

            public void Execute(TransformAspect transform, ref ExampleAnimationState state, in ExampleAnimationStats stats, in ExampleAnimationMovementOutput movement)
            {
                state.rotationalSpeed   += stats.rotationAccelerationMultiplier * math.csum(movement.decleration) * dt;
                state.rotationalSpeed   -= stats.rotationConstantDeceleration * dt;
                state.rotationalSpeed    = math.clamp(state.rotationalSpeed, 0f, stats.maxRotationSpeed);
                transform.worldRotation  = quaternion.LookRotationSafe(transform.forwardDirection.xz.x0y(), math.up());
                transform.RotateWorld(quaternion.RotateY(state.rotationalSpeed * dt));
            }
        }
    }
}

