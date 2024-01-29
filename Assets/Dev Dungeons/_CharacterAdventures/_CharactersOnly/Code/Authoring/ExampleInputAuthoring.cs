using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// You write this
namespace CharacterAdventures.Authoring
{
    public class ExampleInputAuthoring : MonoBehaviour
    {
    }

    public class ExampleInputAuthoringBaker : Baker<ExampleInputAuthoring>
    {
        public override void Bake(ExampleInputAuthoring authoring)
        {
            AddComponent<ExampleDesiredActions>(GetEntity(TransformUsageFlags.Dynamic));
        }
    }
}

