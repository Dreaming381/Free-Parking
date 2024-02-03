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
    [BurstCompile]
    public partial struct ImpMovementSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

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
                                ref ImpAnimationMovementOutput animationOutput,
                                ref ImpMovementState state,
                                in ImpMovementStats stats,
                                in ImpDesiredActions desiredActions)
            {
                // There's no collision right now, so just move the character without any changes along the y-axis.
                // There's also no rotation speeds or strafing, so auto-snap rotation in the desired direction.
                var direction = math.normalizesafe(desiredActions.move, float2.zero);
                if (!direction.Equals(float2.zero))
                    transform.worldRotation = quaternion.LookRotation(direction.x0y(), math.up());

                // Remap [0, 1f] to [-1f, 1f] for velocity stepping.
                var inputMagnitude = math.remap(0f, 1f, -1f, 1f, math.length(desiredActions.move));
                // The character is not allowed to move when aiming.
                inputMagnitude    = math.select(inputMagnitude, 0f, desiredActions.aim);
                var previousSpeed = math.length(state.velocity);
                var newSpeed      = Physics.StepVelocityWithInput(inputMagnitude,
                                                                  previousSpeed,
                                                                  stats.acceleration,
                                                                  stats.deceleration,
                                                                  stats.maxSpeed,
                                                                  stats.acceleration,
                                                                  stats.deceleration,
                                                                  0f,
                                                                  dt);
                state.velocity = newSpeed * direction;
                transform.TranslateWorld(state.velocity.x0y() * dt);

                // Don't start aiming until the character reaches a standstill, or else animation will "slide".
                bool isAiming = newSpeed < math.EPSILON && desiredActions.aim;
                var  flags    = EImpMovementFlags.Grounded;
                if (isAiming)
                    flags          |= EImpMovementFlags.Aiming;
                state.aimDirection  = desiredActions.aimDirection;

                animationOutput = new ImpAnimationMovementOutput
                {
                    flags = flags,
                    speed = newSpeed
                };
            }
        }
    }
}

