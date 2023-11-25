using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamingImLatios.Holiday2023
{
    struct MeshDeformTest : IComponentData
    {
        public float speed;
        public float magnitude;
        public float frequency;
    }
}

