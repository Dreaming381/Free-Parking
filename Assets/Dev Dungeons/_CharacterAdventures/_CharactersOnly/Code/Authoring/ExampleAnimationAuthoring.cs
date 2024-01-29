using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// You write this
namespace CharacterAdventures.Authoring
{
    public class ExampleAnimationAuthoring : MonoBehaviour
    {
        public float maxRotationSpeed               = 540f;  // deg/s
        public float rotationAccelerationMultiplier = 10f;  // deg/s^2 per meter
        public float rotationConstantDeceleration   = 15f;  // deg/s^2 per second
    }

    public class ExampleAnimationAuthoringBaker : Baker<ExampleAnimationAuthoring>
    {
        public override void Bake(ExampleAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExampleAnimationStats
            {
                maxRotationSpeed               = math.radians(authoring.maxRotationSpeed),
                rotationAccelerationMultiplier = math.radians(authoring.rotationAccelerationMultiplier),
                rotationConstantDeceleration   = math.radians(authoring.rotationConstantDeceleration)
            });
            AddComponent<ExampleAnimationMovementOutput>(entity);
            AddComponent<ExampleAnimationState>(         entity);
        }
    }
}

