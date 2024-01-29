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
    public partial struct ExampleIKSystem : ISystem
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

            public void Execute(TransformAspect transform, ref ExampleIKState state, in ExampleIKStats stats, in ExampleIKMovementOutput movement)
            {
                // This is a bit jank because I'm faking angular momentum by applying surface velocity impulses.

                var        leanPower     = math.length(movement.deceleration) - math.length(movement.acceleration);
                var        leanDirection = math.normalizesafe(movement.velocity, float2.zero);
                quaternion tiltTarget;
                if (leanDirection.Equals(float2.zero))
                {
                    tiltTarget = quaternion.identity;
                }
                else
                {
                    var axis   = math.cross(leanDirection.x0y(), math.up());
                    var angle  = -stats.tiltFactor * leanPower * dt;
                    tiltTarget = quaternion.AxisAngle(axis, angle);
                }

                var newTilt            = math.mul(tiltTarget, state.tiltPrevious);
                var localUp            = math.rotate(newTilt, math.up());
                var totalLeanDirection = math.normalizesafe(localUp.xz.x0y(), float3.zero);
                if (totalLeanDirection.Equals(float3.zero))
                {
                    state.tiltPrevious = quaternion.identity;
                    return;
                }

                var gravityAxis  = math.cross(totalLeanDirection, math.up());
                var gravityAngle = stats.gravityFactor * dt * dt;
                newTilt          = math.normalize(math.mul(quaternion.AxisAngle(gravityAxis, gravityAngle), newTilt));
                if (math.dot(math.rotate(newTilt, math.up()), totalLeanDirection) < 0f)
                    newTilt        = quaternion.identity;
                state.tiltPrevious = newTilt;
                transform.RotateWorld(newTilt);
            }
        }
    }
}

