using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamingImLatios.PsyshockRigidBodies
{
    struct RigidBody : IComponentData
    {
        public UnitySim.Velocity        velocity;
        public UnitySim.MotionExpansion motionExpansion;
        public RigidTransform           inertialPoseWorldTransform;
        public UnitySim.Mass            mass;
        public float                    coefficientOfFriction;
        public float                    coefficientOfRestitution;
    }
}

