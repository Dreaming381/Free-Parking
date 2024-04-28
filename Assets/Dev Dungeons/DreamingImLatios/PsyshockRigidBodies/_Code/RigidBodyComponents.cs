using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamingImLatios.PsyshockRigidBodies
{
    struct RigidBody : IComponentData
    {
        public UnitySim.Velocity         velocity;
        public UnitySim.MotionExpansion  motionExpansion;
        public RigidTransform            inertialPoseWorldTransform;
        public UnitySim.Mass             mass;
        public UnitySim.MotionStabilizer motionStabilizer;
        public float                     angularExpansion;
        public int                       numOtherSignificantBodiesInContact;
        public float                     coefficientOfFriction;
        public float                     coefficientOfRestitution;
        public float                     linearDamping;
        public float                     angularDamping;
    }
}

