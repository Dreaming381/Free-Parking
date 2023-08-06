using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamingImLatios.Welcome
{
    struct TextAnimationState : IComponentData
    {
        public float pauseTimeRemaining;
        public int   charactersPlayed;
    }

    struct TextAnimationStats : IComponentData
    {
        public float pauseDuration;
    }
}

