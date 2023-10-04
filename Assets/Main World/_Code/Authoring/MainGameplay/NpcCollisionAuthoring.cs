using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Authoring
{
    public class NpcCollisionAuthoring : MonoBehaviour
    {
    }

    public class NpcCollisionAuthoringBaker : Baker<NpcCollisionAuthoring>
    {
        public override void Bake(NpcCollisionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<NpcCollisionTag>(entity);
        }
    }
}

