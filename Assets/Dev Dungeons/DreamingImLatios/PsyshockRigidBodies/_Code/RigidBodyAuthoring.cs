using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.PsyshockRigidBodies.Authoring
{
    public class RigidBodyAuthoring : MonoBehaviour
    {
        public float mass = 1f;

        [Range(0, 1)] public float coefficientOfFriction    = 0.5f;
        [Range(0, 1)] public float coefficientOfRestitution = 0.5f;
    }

    public class RigidBodyAuthoringBaker : Baker<RigidBodyAuthoring>
    {
        public override void Bake(RigidBodyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RigidBody
            {
                mass = new UnitySim.Mass
                {
                    inverseMass = math.rcp(authoring.mass)
                },
                coefficientOfFriction    = authoring.coefficientOfFriction,
                coefficientOfRestitution = authoring.coefficientOfRestitution,
            });
        }
    }
}

