using Latios;
using Latios.Kinemation;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace DreamingImLatios.Holiday2023.Systems
{
    [BurstCompile]
    partial struct MeshDeformTestSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                time      = (float)SystemAPI.Time.ElapsedTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float time;
            public float deltaTime;

            public void Execute(DynamicMeshAspect dynamicMesh,
                                ref DynamicMeshMaxVertexDisplacement displacement,
                                in MeshDeformTest testParams,
                                in MeshDeformDataBlobReference meshBlobRef)
            {
                var deformVertices      = dynamicMesh.verticesRW;
                dynamicMesh.previousVertices.CopyTo(deformVertices);
                var uniqueVerticesCount = meshBlobRef.blob.Value.uniqueVertexPositionsCount;
                var uniqueVertices      = new NativeArray<float3>(uniqueVerticesCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                SkinningAlgorithms.ExtractUniquePositions(ref uniqueVertices, ref deformVertices, ref meshBlobRef.blob.Value.normalizationData);
                for (int i = 0; i < uniqueVertices.Length; i++)
                {
                    var vertex  = uniqueVertices[i];
                    var dist    = math.length(vertex.xz);
                    vertex.y   += math.cos(testParams.frequency * time + dist * testParams.speed) * deltaTime * testParams.magnitude;
                    uniqueVertices[i] = vertex;
                }
                SkinningAlgorithms.ApplyPositionsWithUniqueNormals(ref deformVertices, uniqueVertices.AsReadOnly(), ref meshBlobRef.blob.Value.normalizationData);
                SkinningAlgorithms.NormalizeMesh(ref deformVertices, ref meshBlobRef.blob.Value.normalizationData, true);
                displacement.maxDisplacement = SkinningAlgorithms.FindMaxDisplacement(deformVertices.AsReadOnly(), ref meshBlobRef.blob.Value);
            }
        }
    }
}

