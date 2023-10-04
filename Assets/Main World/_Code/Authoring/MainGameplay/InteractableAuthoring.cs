using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Authoring
{
    public class InteractableAuthoring : MonoBehaviour
    {
    }

    public class InteractableAuthoringBaker : Baker<InteractableAuthoring>
    {
        public override void Bake(InteractableAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<InteractableTargetTag>(entity);
        }
    }
}

