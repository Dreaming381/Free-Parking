using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Authoring
{
    public class CameraControllerAuthoring : MonoBehaviour
    {
    }

    public class CameraControllerAuthoringBaker : Baker<CameraControllerAuthoring>
    {
        public override void Bake(CameraControllerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<PreviousRequest>(entity);
            AddComponent<TwoAgoRequest>(  entity);
        }

        [BakingType]
        struct PreviousRequest : IRequestPreviousTransform { }
        [BakingType]
        struct TwoAgoRequest : IRequestTwoAgoTransform { }
    }
}

