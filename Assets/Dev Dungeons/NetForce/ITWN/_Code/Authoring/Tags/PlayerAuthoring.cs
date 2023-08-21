using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NetForce.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
    }

    public class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<PlayerTag>(entity);
        }
    }
}

