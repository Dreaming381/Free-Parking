using System;
using System.Collections.Generic;
using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace DreamingImLatios.Holiday2023.Authoring
{
    class MeshDeformTestAuthoring : MonoBehaviour, IOverrideMeshRenderer
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
            baker.AddBuffer<DynamicMeshVertex>(entity);
            baker.AddComponent<DynamicMeshState>(                entity);
            baker.AddComponent<DynamicMeshMaxVertexDisplacement>(entity);
            baker.AddComponent<MeshDeformDataBlobReference>(     entity);

            blobHandle = baker.RequestCreateBlobAsset(filter.sharedMesh);

            Span<MeshMaterialSubmeshSettings> mms = stackalloc MeshMaterialSubmeshSettings[materials.Count];
            RenderingBakingTools.ExtractMeshMaterialSubmeshes(mms, filter.sharedMesh, materials);
            var opaqueMaterialCount = RenderingBakingTools.GroupByDepthSorting(mms);

            RenderingBakingTools.GetLOD(baker, renderer, out var lodSettings);
            RenderingBakingTools.BakeLodMaskForEntity(baker, entity, lodSettings);

            var rendererSettings = new MeshRendererBakeSettings
            {
                targetEntity                = entity,
                renderMeshDescription       = new RenderMeshDescription(renderer),
                isDeforming                 = true,
                suppressDeformationWarnings = false,
                useLightmapsIfPossible      = true,
                lightmapIndex               = renderer.lightmapIndex,
                lightmapScaleOffset         = renderer.lightmapScaleOffset,
                isStatic                    = baker.IsStatic(),
                localBounds                 = filter.sharedMesh != null ? filter.sharedMesh.bounds : default,
            };

            if (opaqueMaterialCount == mms.Length || opaqueMaterialCount == 0)
            {
                Span<MeshRendererBakeSettings> renderers = stackalloc MeshRendererBakeSettings[1];
                renderers[0]                             = rendererSettings;
                Span<int> count                          = stackalloc int[1];
                count[0]                                 = mms.Length;
                baker.BakeMeshAndMaterial(renderers, mms, count);
            }
            else
            {
                var                            additionalEntity = baker.CreateAdditionalEntity(TransformUsageFlags.Renderable, false, $"{baker.GetName()}-TransparentRenderEntity");
                Span<MeshRendererBakeSettings> renderers        = stackalloc MeshRendererBakeSettings[2];
                renderers[0]                                    = rendererSettings;
                renderers[1]                                    = rendererSettings;
                renderers[1].targetEntity                       = additionalEntity;
                Span<int> counts                                = stackalloc int[2];
                counts[0]                                       = opaqueMaterialCount;
                counts[1]                                       = mms.Length - opaqueMaterialCount;
                baker.BakeMeshAndMaterial(renderers, mms, counts);
            }

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

