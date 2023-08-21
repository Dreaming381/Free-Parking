using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace NetForce
{
    struct Spin : IComponentData
    {
        public float3 axis;
        public float  radPerSecCW;
        public bool   applyInWorldSpace;
    }
}

