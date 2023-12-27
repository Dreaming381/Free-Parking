using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

namespace DreamingImLatios.Holiday2023.Authoring
{
    class ClothAuthoring : MonoBehaviour
    {
        public float friction = 0.1f;
        public float mass = 5f;
        public float thickness = 0.02f;
        public float stiffness = 0.5f;

        public int2 edgesPerAxis = new int2(200, 200);
        public Material material;
    }

    [BurstCompile]
    struct BuildMeshWithQuadsJob : IJob
    {
        public NativeList<float3> positions;
        public NativeList<float3> normals;
        public NativeList<float4> tangents;
        public NativeList<float2> uvs;
        public NativeList<int> indices;
        public NativeList<int2> edges;
        public NativeList<int2> bendSegments;

        public int2 dimensions;
        public float minDistanceBetweenVertices;

        public void Execute()
        {
            float2 offset = -(float2)dimensions * minDistanceBetweenVertices / 2f;
            float2 uvStride = 1f / (float2)dimensions;
            
            for (int i = 0; i <= dimensions.y; i++)
            {
                for (int j = 0; j <= dimensions.x; j++)
                {
                    positions.Add(new float3(offset.x + j * dimensions.x, 0f, offset.y + i * dimensions.y));
                    normals.Add(new float3(0f, 1f, 0f));
                    tangents.Add(new float4(1f, 0f, 0f, 1f));
                    uvs.Add(new float2(uvStride.x * j, uvStride.y * i));
                }
            }


            for (int i = 0; i < dimensions.y; i++)
            {
                var rowBase = i * (dimensions.x + 1);
                for (int j = 0; j < dimensions.x; j++)
                {
                    var tl = rowBase + j;
                    var tr = tl + 1;
                    var bl = tl + dimensions.x + 1;
                    var br = bl + 1;

                    edges.Add(new int2(tl, tr));
                    edges.Add(new int2(tl, bl));
                    edges.Add(new int2(tl, br));

                    indices.Add(tl);
                    indices.Add(tr);
                    indices.Add(bl);
                    indices.Add(bl);
                    indices.Add(tr);
                    indices.Add(br);
                }

                edges.Add(new int2(rowBase + dimensions.x, rowBase + dimensions.x + dimensions.x + 1));
            }


            for (int i = 0; i < dimensions.x; i++)
            {
                edges.Add(new int2(dimensions.y * (dimensions.x + 1) + i, dimensions.y * (dimensions.x + 1) + i + 1));
            }

            for (int i = 0; i < dimensions.y - 1; i++)
            {
                for (int j = 0; j < dimensions.x - 1; j++)
                {
                    var tl = i * (dimensions.x + 1) + j;
                    var tm = tl + 1;
                    var ml = tl + dimensions.x + 1;
                    var mm = ml + 1;
                    var mr = mm + 1;
                    var bm = mm + dimensions.x + 1;
                    bendSegments.Add(new int2(tm, ml));
                    bendSegments.Add(new int2(tl, mr));
                    bendSegments.Add(new int2(tl, bm));
                }
            }
        }
    }

    /*[TemporaryBakingType]
    struct ClothAuthoringBakeItem : ISmartBakeItem<ClothAuthoring>
    {
        SmartBlobberHandle<ClothBlob> clothBlobHandle;
        SmartBlobberHandle<MeshDeformDataBlob> meshDeformDataBlobHandle;

        public bool Bake(ClothAuthoring authoring, IBaker baker)
        {
            var buildMeshJob = new BuildMeshWithQuadsJob
            {
                bendSegments = new NativeList<int2>(Allocator.TempJob),
                dimensions = authoring.edgesPerAxis,
                edges = new NativeList<int2>(Allocator.TempJob),
                indices = new NativeList<int>(Allocator.TempJob),
                minDistanceBetweenVertices = authoring.thickness,
                normals = new NativeList<float3>(Allocator.TempJob),
                positions = new NativeList<float3>(Allocator.TempJob),
                tangents = new NativeList<float4>(Allocator.TempJob),
                uvs = new NativeList<float2>(Allocator.TempJob)
            };
            buildMeshJob.Run();
            var mesh = new Mesh();
            mesh.SetVertices(buildMeshJob.positions.AsArray());
            mesh.SetNormals(buildMeshJob.normals.AsArray());
            mesh.SetTangents(buildMeshJob.tangents.AsArray());
            mesh.SetUVs(0, buildMeshJob.uvs.AsArray());
            mesh.SetIndices(buildMeshJob.indices.AsArray(), MeshTopology.Triangles, 0);

            
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            //baker.BakeDeformMeshAndMaterial()
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