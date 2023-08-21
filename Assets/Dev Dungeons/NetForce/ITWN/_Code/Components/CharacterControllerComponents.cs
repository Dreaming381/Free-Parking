using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace NetForce
{
    #region Forward
    struct ForwardDesiredActions : IComponentData
    {
        public float2 direction;
        public float  forwardInput;  // Should be -1 to 1
    }

    struct ForwardCharacterControllerV1Stats : IComponentData
    {
        public float turnSpeed;
        public float maxSpeed;
        public float acceleration;
        public float deceleration;
        public float height;
        public float radius;
        public float gravity;
        public float groundCheckDistance;
    }

    struct ForwardCharacterControllerV1State : IComponentData
    {
        public float horizontalVelocity;
        public float verticalVelocity;
    }
    #endregion
}

