using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NetForce.Authoring
{
    public class SpinAuthoring : MonoBehaviour
    {
        public float3 axis               = new float3(0f, 1f, 0f);
        public float  degreesPerSecondCW = 75f;
        public bool   applyInWorldSpace  = false;
    }

    public class SpinAuthoringBaker : Baker<SpinAuthoring>
    {
        public override void Bake(SpinAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Spin
            {
                axis              = math.normalizesafe(authoring.axis, math.up()),
                radPerSecCW       = math.radians(authoring.degreesPerSecondCW),
                applyInWorldSpace = authoring.applyInWorldSpace
            });

            if (authoring.applyInWorldSpace)
                this.AddHiearchyModeFlags(entity, Latios.Transforms.HierarchyUpdateMode.Flags.WorldRotation);
        }
    }
}

