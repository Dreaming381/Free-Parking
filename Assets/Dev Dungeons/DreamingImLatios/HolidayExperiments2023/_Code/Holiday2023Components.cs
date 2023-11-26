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

    struct ClothBlob
    {
        public BlobArray<int3> triangles;
        
        public struct DistanceConstraint
        {
            public int indexA;
            public int indexB;
            public float targetDistance;
            public float stiffness;
        }

        public BlobArray<DistanceConstraint> distanceConstraints;
    }

    struct Cloth : IComponentData
    {
        public BlobAssetReference<ClothBlob> blob;
        public float friction;
        public float invMassPerParticle;
        public float halfThickness;
    }
}

