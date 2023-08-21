using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NetForce.Authoring
{
    public class ForwardCharacterControllerV1Authoring : MonoBehaviour
    {
        public float turnSpeed           = 200f;
        public float maxSpeed            = 5f;
        public float acceleration        = 10f;
        public float deceleration        = 10f;
        public float height              = 3f;
        public float radius              = 0.5f;
        public float gravity             = 9.81f;
        public float groundCheckDistance = 0.01f;
    }

    public class ForwardCharacterControllerV1AuthoringBaker : Baker<ForwardCharacterControllerV1Authoring>
    {
        public override void Bake(ForwardCharacterControllerV1Authoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new ForwardCharacterControllerV1Stats
            {
                turnSpeed           = authoring.turnSpeed,
                maxSpeed            = authoring.maxSpeed,
                acceleration        = authoring.acceleration,
                deceleration        = authoring.deceleration,
                height              = authoring.height,
                radius              = authoring.radius,
                gravity             = authoring.gravity,
                groundCheckDistance = authoring.groundCheckDistance,
            });
            AddComponent(entity, new ForwardDesiredActions
            {
                direction    = new float2(1f, 0f),
                forwardInput = 0.5f
            });
            AddComponent<ForwardCharacterControllerV1State>(entity);
        }
    }
}

