using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// DreamingImLatios typically writes this
namespace CharacterAdventures.Authoring
{
    public class ExampleIKAuthoring : MonoBehaviour
    {
        public float tiltFactor    = 1f;
        public float graivtyFactor = 9.81f;
    }

    public class ExampleIKAuthoringBaker : Baker<ExampleIKAuthoring>
    {
        public override void Bake(ExampleIKAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExampleIKStats
            {
                tiltFactor    = math.radians(authoring.tiltFactor),
                gravityFactor = authoring.graivtyFactor
            });
            AddComponent(entity, new ExampleIKState
            {
                tiltPrevious = quaternion.identity,
            });
            AddComponent<ExampleIKMovementOutput>(entity);
        }
    }
}

