using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// DreamingImLatios usually writes this
namespace CharacterAdventures.Authoring
{
    public class ExampleMovementAuthoring : MonoBehaviour
    {
        public float maxSpeed     = 5f;
        public float acceleration = 25f;
        public float deceleration = 25f;
    }

    public class ExampleMovementAuthoringBaker : Baker<ExampleMovementAuthoring>
    {
        public override void Bake(ExampleMovementAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExampleMovementStats
            {
                acceleration = authoring.acceleration,
                deceleration = authoring.deceleration,
                maxSpeed     = authoring.maxSpeed,
            });
            AddComponent<ExampleMovementState>(entity);
        }
    }
}

