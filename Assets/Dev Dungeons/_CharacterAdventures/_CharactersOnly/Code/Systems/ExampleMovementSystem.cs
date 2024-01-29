using Latios;
using Latios.Psyshock;
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
    public partial struct ExampleMovementSystem : ISystem
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

            public void Execute(TransformAspect transform,
                                ref ExampleAnimationMovementOutput animationOutput,
                                ref ExampleIKMovementOutput ikOutput,
                                ref ExampleMovementState state,
                                in ExampleMovementStats stats,
                                in ExampleDesiredActions input)
            {
                var previousVelocity = state.velocity;
                var newVelocity      = Physics.StepVelocityWithInput(input.move,
                                                                     previousVelocity,
                                                                     stats.acceleration,
                                                                     stats.deceleration,
                                                                     stats.maxSpeed,
                                                                     stats.acceleration,
                                                                     stats.deceleration,
                                                                     stats.maxSpeed,
                                                                     dt);
                var accel                   = (math.abs(newVelocity) - math.abs(previousVelocity)) / dt;
                animationOutput.decleration = -math.min(accel, 0f);
                ikOutput.acceleration       = math.max(accel, 0f);
                ikOutput.deceleration       = animationOutput.decleration;
                ikOutput.velocity           = newVelocity;
                state.velocity              = newVelocity;
                transform.TranslateWorld(dt * newVelocity.x0y());
            }
        }
    }
}

