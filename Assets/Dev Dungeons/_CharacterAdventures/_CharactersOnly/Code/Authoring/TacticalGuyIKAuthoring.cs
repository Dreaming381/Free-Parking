using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CharacterAdventures.Authoring
{
    public class TacticalGuyIKAuthoring : MonoBehaviour
    {
        public string    leftHandName;
        public Transform leftHandTarget;
        public string    rightHandName;
        public Transform rightHandTarget;
    }

    public class TacticalGuyIKAuthoringBaker : Baker<TacticalGuyIKAuthoring>
    {
        public override void Bake(TacticalGuyIKAuthoring authoring)
        {
            var entity           = GetEntity(TransformUsageFlags.None);
            var armIKStatsBuffer = AddBuffer<TacticalGuyArmIKStats>(entity);
            armIKStatsBuffer.Add(new TacticalGuyArmIKStats
            {
                handBoneName = authoring.leftHandName,
                handTarget   = GetEntity(authoring.leftHandTarget, TransformUsageFlags.Renderable)
            });
            armIKStatsBuffer.Add(new TacticalGuyArmIKStats
            {
                handBoneName = authoring.rightHandName,
                handTarget   = GetEntity(authoring.rightHandTarget, TransformUsageFlags.Renderable)
            });
        }
    }
}

