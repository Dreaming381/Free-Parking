using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// DreamingImLatios usually writes this
namespace CharacterAdventures.Authoring
{
    public class ImpMovementAuthoring : MonoBehaviour
    {
        [Tooltip("Maximum speed the character can move.")]
        public float maxSpeed     = 5f;
        [Tooltip("Acceleration rate when increasing speed over ground.")]
        public float acceleration = 25f;
        [Tooltip("Acceleration rate when slowing to a halt.")]
        public float deceleration = 25f;
    }

    public class ImpMovementAuthoringBaker : Baker<ImpMovementAuthoring>
    {
        public override void Bake(ImpMovementAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ImpMovementStats
            {
                acceleration = authoring.acceleration,
                deceleration = authoring.deceleration,
                maxSpeed     = authoring.maxSpeed,
            });
            AddComponent<ImpMovementState>(entity);
        }
    }
}

