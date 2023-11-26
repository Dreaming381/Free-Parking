using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.Holiday2023.Authoring
{
    class ClothAuthoring : OverrideMeshRendererBase
    {
        public float friction = 0.1f;
        public float mass = 5f;
        public float thickness = 0.02f;
        public float stiffness = 0.5f;
    }

    /*[TemporaryBakingType]
    struct ClothAuthoringBakeItem : ISmartBakeItem<ClothAuthoring>
    {
        SmartBlobberHandle<ClothBlob> clothBlobHandle;
        SmartBlobberHandle<MeshDeformDataBlob> meshDeformDataBlobHandle;

        public bool Bake(ClothAuthoring authoring, IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            var renderer = baker.GetComponent<MeshRenderer>();
            var filter = baker.GetComponent<MeshFilter>();
            var materials = new List<Material>();
            renderer.GetSharedMaterials(materials);
            baker.BakeDeformMeshAndMaterial(renderer, filter.sharedMesh, materials);
            baker.AddBuffer<DynamicMeshVertex>(entity);
            baker.AddComponent<DynamicMeshState>(entity);
            baker.AddComponent<DynamicMeshMaxVertexDisplacement>(entity);
            baker.AddComponent<MeshDeformDataBlobReference>(entity);
            meshDeformDataBlobHandle = baker.RequestCreateBlobAsset(filter.sharedMesh);
            
        }



    }*/
}