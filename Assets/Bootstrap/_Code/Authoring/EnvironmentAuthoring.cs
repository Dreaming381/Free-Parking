using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.Authoring
{
    [AddComponentMenu("Free Parking/Environment")]
    [DisallowMultipleComponent]
    public class EnvironmentAuthoring : MonoBehaviour
    {
        public enum Mode
        {
            IncludeRecursively,
            ExcludeRecursively
        }

        public Mode mode;
    }

    [BakeDerivedTypes]
    public class EnvironmentAuthoringBaker : Baker<Collider>
    {
        static List<Collider> s_colliderCache = new List<Collider>();

        public override void Bake(Collider authoring)
        {
            s_colliderCache.Clear();
            GetComponents(s_colliderCache);
            if (s_colliderCache[0] != authoring)
                return;

            var ancencestor = GetComponentInParent<EnvironmentAuthoring>();
            if (ancencestor == null)
                return;
            if (ancencestor.mode == EnvironmentAuthoring.Mode.IncludeRecursively)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<EnvironmentTag>(entity);
            }
        }
    }
}

