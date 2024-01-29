using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterAdventures.Authoring
{
    public class ImpInputAuthoring : MonoBehaviour
    {
    }

    public class ImpInputAuthoringBaker : Baker<ImpInputAuthoring>
    {
        public override void Bake(ImpInputAuthoring authoring)
        {
            AddComponent<ImpDesiredActions>(GetEntity(TransformUsageFlags.Dynamic));
        }
    }
}

