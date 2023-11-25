using System.Collections.Generic;
using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.Holiday2023.Authoring
{
    class MeshDeformTestAuthoring : OverrideMeshRendererBase
    {
        public float speed     = 5f;
        public float magnitude = 0.5f;
        public float frequency = 4f;
    }

    [TemporaryBakingType]
    struct MeshDeformTestAuthoringBakeItem : ISmartBakeItem<MeshDeformTestAuthoring>
    {
        SmartBlobberHandle<MeshDeformDataBlob> blobHandle;

        public bool Bake(MeshDeformTestAuthoring authoring, IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddComponent(entity, new MeshDeformTest
            {
                frequency = authoring.frequency,
                magnitude = authoring.magnitude,
                speed     = authoring.speed
            });

            var renderer  = baker.GetComponent<MeshRenderer>();
            var filter    = baker.GetComponent<MeshFilter>();
            var materials = new List<Material>();
            renderer.GetSharedMaterials(materials);
            baker.BakeDeformMeshAndMaterial(renderer, filter.sharedMesh, materials);
            baker.AddBuffer<DynamicMeshVertex>(entity);
            baker.AddComponent<DynamicMeshState>(                entity);
            baker.AddComponent<DynamicMeshMaxVertexDisplacement>(entity);
            baker.AddComponent<MeshDeformDataBlobReference>(     entity);

            blobHandle = baker.RequestCreateBlobAsset(filter.sharedMesh);
            return true;
        }

        public void PostProcessBlobRequests(EntityManager entityManager, Entity entity)
        {
            entityManager.SetComponentData(entity, new MeshDeformDataBlobReference
            {
                blob = blobHandle.Resolve(entityManager)
            });
        }
    }

    class MeshDeformTestBaker : SmartBaker<MeshDeformTestAuthoring, MeshDeformTestAuthoringBakeItem>
    {
    }
}

