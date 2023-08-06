using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.Welcome.Authoring
{
    public class WelcomeTextAnimatorAuthoring : MonoBehaviour
    {
        public float pauseTime = 2.5f;
    }

    public class WelcomeTextAnimatorAuthoringBaker : Baker<WelcomeTextAnimatorAuthoring>
    {
        public override void Bake(WelcomeTextAnimatorAuthoring authoring)
        {
            var entity                                                     = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TextAnimationStats { pauseDuration    = authoring.pauseTime });
            AddComponent(entity, new TextAnimationState { charactersPlayed = 0, pauseTimeRemaining = authoring.pauseTime });
        }
    }
}

