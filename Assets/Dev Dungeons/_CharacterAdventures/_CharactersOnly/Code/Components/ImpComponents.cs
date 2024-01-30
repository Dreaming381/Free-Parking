using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Latios.Kinemation;
using System;

namespace CharacterAdventures
{
    #region You write
    // Populate with all "abilities" the character can perform.
    public struct ImpDesiredActions : IComponentData
    {
        public float2 move;  // math.length(move) should be in the range [0.0, 1.0]
        public bool aim;
        public float3 aimDirection;
    }

    [Flags]
    public enum EImpMovementFlags : short
    {
        Aiming = 1 << 0,
        Grounded = 1 << 1,
        Jumping =  1 << 2,
    }

    // Populate with data you would like to receive from the movement system
    public struct ImpAnimationMovementOutput : IComponentData
    {
        public float speed; //speed should be between 0.0 (stationary) and 1.0 (full run)
        public EImpMovementFlags flags;
    }

    public struct ImpAnimationSettings : IComponentData
    {
        public float maxAimRotation; //max horizontal angle before character should rotate to realign
        public float2 aimElevationRange; //max and min angles the character can aim vertically; 0 is horizontal
        public float walkStepLength; //adjust to make sure footsteps don't drift when walking
        public float runStepLength; //adjust to make sure footsteps don't drift when running
        public bool twoHands; //use the two-handed aiming animation
        public float runThreshold; //fractional speed at which to transition from the walk to the run animation
        public float maxInertialBlendDuration; //max time for inertial blends
    }

    public enum EImpAnimation : byte
    {
        Idle,
        Walk,
        Run,
        AimOneHand,
        AimTwoHands,
    }

    public struct ImpAnimations : IComponentData
    {
        public BlobAssetReference<SkeletonClipSetBlob> animations;
    }

    public struct ImpAnimationState : IComponentData
    {
        public EImpAnimation currentAnimation;
        public float startTime;
        public float previousDt;
        public bool inInertialBlend;
    }
    #endregion

    #region DreamingImLatios writes
    public struct ImpMovementStats : IComponentData
    {
        public float maxSpeed;
        public float acceleration;
        public float deceleration;
    }

    public struct ImpMovementState : IComponentData
    {
        public float2 velocity;
        public float3 aimDirection;
    }
    #endregion
}

