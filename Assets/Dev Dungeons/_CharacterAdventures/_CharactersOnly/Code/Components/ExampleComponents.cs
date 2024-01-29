using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CharacterAdventures
{
    #region You write
    // Populate with all "abilities" the character can perform.
    public struct ExampleDesiredActions : IComponentData
    {
        public float2 move;  // math.length(move) should be in the range [0.0, 1.0]
    }

    // Populate with data you would like to receive from the movement system
    public struct ExampleAnimationMovementOutput : IComponentData
    {
        public float2 decleration;
    }

    // This is an example of a component you may attach to perform animation
    public struct ExampleAnimationStats : IComponentData
    {
        public float maxRotationSpeed;  // rad/s
        public float rotationAccelerationMultiplier;  // rad/s^2 per meter
        public float rotationConstantDeceleration;  // rad/s^2
    }

    // This is an example of a component you may attach to perform animation
    public struct ExampleAnimationState : IComponentData
    {
        public float rotationalSpeed;
    }
    #endregion

    #region DreamingImLatios writes
    public struct ExampleMovementStats : IComponentData
    {
        public float maxSpeed;
        public float acceleration;
        public float deceleration;
    }

    public struct ExampleMovementState : IComponentData
    {
        public float2 velocity;
    }

    public struct ExampleIKMovementOutput : IComponentData
    {
        public float2 acceleration;
        public float2 deceleration;
        public float2 velocity;
    }

    public struct ExampleIKStats : IComponentData
    {
        public float tiltFactor;
        public float gravityFactor;
    }

    public struct ExampleIKState : IComponentData
    {
        public quaternion tiltPrevious;
    }
    #endregion
}

