using Latios;
using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Authoring
{
    [AddComponentMenu("Free Parking/Main World/Player Motion")]
    public class PlayerMotionAuthoring : MonoBehaviour
    {
        public float capsuleRadius     = 0.3f;
        public float capsuleHeight     = 1f;
        public float targetHoverHeight = 0.5f;
        public float skinWidth         = 0.01f;
        public float maxSlopeAngle     = 60f;

        public float maxSpeed     = 4f;
        public float maxTurnSpeed = 180f;
        public float gravity      = -9.81f;
        public float acceleration = 25f;
        public float hoverKpM     = 1f;  // k/mass

        public CameraControllerAuthoring        cameraController;
        public PlayerInteractionSensorAuthoring playerInteractionSensor;
    }

    public class PlayerMotionAuthoringBaker : Baker<PlayerMotionAuthoring>
    {
        public override void Bake(PlayerMotionAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerMotionStats
            {
                collider = new Latios.Psyshock.CapsuleCollider
                {
                    pointA      = new float3(0f, authoring.capsuleRadius, 0f),
                    pointB      = new float3(0f, authoring.capsuleHeight - authoring.capsuleRadius, 0f),
                    radius      = authoring.capsuleRadius,
                    stretchMode = Latios.Psyshock.CapsuleCollider.StretchMode.StretchPoints
                },
                targetHoverHeight = authoring.targetHoverHeight,
                skinWidth         = authoring.skinWidth,
                cosMaxSlope       = math.cos(math.radians(authoring.maxSlopeAngle)),
                maxSpeed          = authoring.maxSpeed,
                maxTurnSpeed      = math.radians(authoring.maxTurnSpeed),
                gravity           = -math.abs(authoring.gravity),
                acceleration      = authoring.acceleration,
                hoverKpM          = authoring.hoverKpM,
                cameraEntity      = GetEntity(authoring.cameraController, TransformUsageFlags.Dynamic),
                interactionEntity = GetEntity(authoring.playerInteractionSensor, TransformUsageFlags.Renderable),
            });
            AddComponent<PlayerMotionState>(         entity);
            AddComponent<PlayerMotionDesiredActions>(entity);
        }
    }
}

