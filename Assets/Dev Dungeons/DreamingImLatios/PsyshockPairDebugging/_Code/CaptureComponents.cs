using Latios;
using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamingImLatios.PsyshockPairDebugging
{
    public enum DrawOperation
    {
        None,
        DistanceBetweenClosest,
        DistanceBetweenAll,
        UnityContactsBetweenClosest,
        UnityContactsBetweenAll,
    }

    struct CapturePairTarget : IComponentData
    {
        public EntityWith<Collider> colliderA;
        public EntityWith<Collider> colliderB;
        public float                maxDistance;
        public DrawOperation        drawOperation;
    }
}

