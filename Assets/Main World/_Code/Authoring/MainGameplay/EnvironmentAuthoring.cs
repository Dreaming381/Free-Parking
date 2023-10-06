using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Authoring
{
    [AddComponentMenu("Free Parking/Main World/Environment")]
    [DisallowMultipleComponent]
    public class EnvironmentAuthoring : MonoBehaviour
    {
        public bool includeDescendants;
    }

    [BakeDerivedTypes]
    public class EnvironmentAuthoringBaker : Baker<Collider>
    {
        static List<EnvironmentAuthoring> s_environmentCache = new List<EnvironmentAuthoring>();
        static List<Collider>             s_colliderCache    = new List<Collider>();

        public override void Bake(Collider authoring)
        {
            s_colliderCache.Clear();
            GetComponents(s_colliderCache);
            if (s_colliderCache[0] != authoring)
                return;

            s_environmentCache.Clear();
            GetComponentsInParent(s_environmentCache);
            foreach (var env in s_environmentCache)
            {
                if (env.gameObject == authoring.gameObject || env.includeDescendants)
                {
                    var entity = GetEntity(TransformUsageFlags.Renderable);
                    AddComponent<EnvironmentTag>(entity);
                    break;
                }
            }
        }
    }
}

