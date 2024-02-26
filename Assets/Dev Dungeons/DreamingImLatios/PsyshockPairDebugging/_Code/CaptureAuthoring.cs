using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.PsyshockPairDebugging.Authoring
{
    public class CaptureAuthoring : MonoBehaviour
    {
        public GameObject    colliderA;
        public GameObject    colliderB;
        public float         maxDistance   = 10.0f;
        public DrawOperation drawOperation = DrawOperation.UnityContactsBetweenClosest;
        public bool          rebakeToggle;
    }

    public class CaptureAuthoringBaker : Baker<CaptureAuthoring>
    {
        public override void Bake(CaptureAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new CapturePairTarget
            {
                colliderA     = GetEntity(authoring.colliderA, TransformUsageFlags.Renderable),
                colliderB     = GetEntity(authoring.colliderB, TransformUsageFlags.Renderable),
                maxDistance   = authoring.maxDistance,
                drawOperation = authoring.drawOperation
            });
        }
    }
}

