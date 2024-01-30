using Latios;
using Latios.Kinemation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterAdventures.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct ImpAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Debug.Log("Running imp animation system.");
            state.CompleteDependency();
            new Job {
                dt = SystemAPI.Time.DeltaTime,
                time = (float) SystemAPI.Time.ElapsedTime,
            }.Run();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float dt;
            public float time;

            public void Execute(ref ImpAnimationState animationState, in ImpAnimationSettings animationSettings, in ImpAnimations animations, in ImpAnimationMovementOutput movement, OptimizedSkeletonAspect skeletonAspect)
            {
                EImpAnimation clipIndex;
                //replace this with more complex logic if we end up with more clips
                if ((movement.flags & EImpMovementFlags.Aiming) != 0) {
                    clipIndex = animationSettings.twoHands ? EImpAnimation.AimTwoHands : EImpAnimation.AimOneHand;
                } else {
                    if (movement.speed == 0)
                    {
                        clipIndex = EImpAnimation.Idle;
                    } else {
                        clipIndex = movement.speed > animationSettings.runThreshold ? EImpAnimation.Run : EImpAnimation.Walk;
                    }
                }

                ref var clip = ref animations.animations.Value.clips[(int) clipIndex];

                if (clipIndex != animationState.currentAnimation)
                {
                    animationState.startTime = time;
                    animationState.currentAnimation = clipIndex;
                    animationState.inInertialBlend = true;
                    clip.SamplePose(ref skeletonAspect, 0.0f, 1f);
                    skeletonAspect.SyncHistory();
                    skeletonAspect.StartNewInertialBlend(animationState.previousDt, animationSettings.maxInertialBlendDuration + dt);
                } else {
                    float evaluationTime = clip.LoopToClipTime(time - animationState.startTime);
                    clip.SamplePose(ref skeletonAspect, evaluationTime, 1f);
                }

                if (animationState.inInertialBlend)
                {
                    float inertialBlendTimeElapsed = time - animationState.startTime;
                    if (inertialBlendTimeElapsed > animationSettings.maxInertialBlendDuration)
                    {
                        animationState.inInertialBlend = false;                        
                    } else {
                        skeletonAspect.InertialBlend(inertialBlendTimeElapsed);
                    }
                }

                skeletonAspect.EndSamplingAndSync();

                animationState.previousDt = dt;
            }
        }
    }
}

