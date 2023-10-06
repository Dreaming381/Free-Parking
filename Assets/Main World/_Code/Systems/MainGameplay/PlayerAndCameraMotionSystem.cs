using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace FreeParking.MainWorld.MainGameplay.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct PlayerAndCameraMotionSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        EntityQuery                    m_collisionQuery;
        BuildCollisionLayerTypeHandles m_handles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();

            m_collisionQuery = state.Fluent().WithAny<EnvironmentTag>().WithAny<NpcCollisionTag>().PatchQueryForBuildingCollisionLayer().Build();
            m_handles        = new BuildCollisionLayerTypeHandles(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_handles.Update(ref state);
            state.Dependency = Physics.BuildCollisionLayer(m_collisionQuery, in m_handles).ScheduleParallel(out var layer, state.WorldUpdateAllocator, state.Dependency);
            new PlayerJob
            {
                layer                        = layer,
                twoAgoTransformLookup        = GetComponentLookup<TwoAgoTransform>(true),
                playerInteractionStateLookup = GetComponentLookup<PlayerInteractionState>(true),
                deltaTime                    = Time.DeltaTime
            }.Schedule();
        }

        [BurstCompile]
        partial struct PlayerJob : IJobEntity
        {
            [ReadOnly] public CollisionLayer                          layer;
            [ReadOnly] public ComponentLookup<TwoAgoTransform>        twoAgoTransformLookup;
            [ReadOnly] public ComponentLookup<PlayerInteractionState> playerInteractionStateLookup;

            public float deltaTime;

            public void Execute(TransformAspect transform, ref PlayerMotionState state, in PlayerMotionStats stats, in PlayerMotionDesiredActions desiredActions)
            {
                // Step 1: Perform discrete depenetration. Should be unnecessary most of the time, but helps solve special cases.
                state.isSafeToInteract                          = false;
                var disableCollisionForStuckCollidersIndicesSet = new NativeHashSet<int>(8, Allocator.Temp);
                for (int i = 0; i < 100; i++)
                {
                    if (!Physics.DistanceBetween(stats.collider, transform.worldTransform, in layer, stats.skinWidth, out var hit, out var body))
                    {
                        disableCollisionForStuckCollidersIndicesSet.Clear();
                        state.isSafeToInteract = true;
                        break;
                    }
                    disableCollisionForStuckCollidersIndicesSet.Add(body.bodyIndex);
                    transform.TranslateWorld(hit.normalA * (hit.distance - stats.skinWidth));
                }

                // Step 2: Everything horizontal.
                {
                    var  groundCastEnd      = transform.worldPosition - new float3(0f, stats.targetHoverHeight + stats.skinWidth, 0f);
                    bool isOnGround         = Physics.ColliderCast(stats.collider, transform.worldTransform, groundCastEnd, in layer, out var hit, out _);
                    state.isSafeToInteract &= isOnGround;
                    float3 initialNormal    = math.up();
                    if (isOnGround)
                    {
                        // We only update rotation and velocity if we are on the ground.
                        var cameraRot = twoAgoTransformLookup[stats.cameraEntity].rotation;
                        // Pick forward or up as camera forward depending on which is more horizontal
                        var cameraRawForward = math.mul(cameraRot, math.forward());
                        var cameraRawUp      = math.mul(cameraRot, math.up());
                        var cameraForward3D  = math.select(cameraRawForward, cameraRawUp, math.abs(cameraRawForward.y) > math.abs(cameraRawUp.y));
                        var cameraForward2D  = math.normalize(cameraForward3D.xz);
                        var cameraRight2D    = math.normalize(math.mul(cameraRot, math.right()).xz);
                        var worldInput       = cameraForward2D * desiredActions.cameraRelativeMovement.y + cameraRight2D * desiredActions.cameraRelativeMovement.x;

                        // Integrate turn
                        var heading             = math.normalize(transform.forwardDirection.xz);
                        var worldInputDirection = math.normalizesafe(worldInput, heading);
                        var headingAngle        = math.atan2(heading.y, heading.x);
                        var targetAngle         = math.atan2(worldInputDirection.y, worldInputDirection.x);
                        var delta               = targetAngle - headingAngle;
                        if (delta > math.PI)
                            delta -= 2f * math.PI;
                        if (delta < -math.PI)
                            delta           += 2f * math.PI;
                        var allowedRotation  = math.radians(stats.maxTurnSpeed) * deltaTime;
                        delta                = math.clamp(delta, -allowedRotation, allowedRotation);
                        transform.RotateWorld(quaternion.Euler(0f, -delta, 0f));
                        heading = math.normalize(transform.forwardDirection.xz);

                        state.speed = Physics.StepVelocityWithInput(math.dot(worldInput, heading),
                                                                    state.speed,
                                                                    stats.acceleration,
                                                                    stats.acceleration,
                                                                    stats.maxSpeed,
                                                                    stats.acceleration,
                                                                    stats.acceleration,
                                                                    0f,
                                                                    deltaTime);
                        initialNormal = -hit.normalOnCaster;
                    }

                    // Time to collide and slide.
                    var initialPosition   = transform.worldPosition;
                    var initialDirection  = transform.forwardDirection;
                    var direction         = initialDirection;
                    var distanceRemaining = state.speed * deltaTime;
                    var slideNormal       = initialNormal;
                    for (int i = 0; i < 100; i++)
                    {
                        if (math.abs(slideNormal.y) < stats.cosMaxSlope)
                        {
                            slideNormal        = math.normalize(new float3(slideNormal.x, 0f, slideNormal.z));
                            distanceRemaining *= -math.dot(slideNormal, initialDirection);
                        }

                        direction -= math.project(direction, slideNormal);
                        direction  = math.normalizesafe(direction, float3.zero);
                        if (direction.Equals(float3.zero))
                            break;

                        var slideCastEnd = transform.worldPosition + direction * distanceRemaining;
                        if (Physics.ColliderCast(stats.collider, transform.worldTransform, slideCastEnd, in layer, out var slideHit, out var body))
                        {
                            bool ignore     = disableCollisionForStuckCollidersIndicesSet.Contains(body.bodyIndex);
                            var  skinOffset = math.select(-stats.skinWidth, stats.skinWidth, ignore);
                            transform.TranslateWorld((slideHit.distance + skinOffset) * direction);
                            distanceRemaining -= slideHit.distance + skinOffset;
                            if (!ignore)
                                slideNormal = -slideHit.normalOnCaster;
                        }
                        else
                            break;
                    }
                    var actualSpeed = math.length(transform.worldPosition - initialPosition) / deltaTime;
                    state.speed     = math.min(state.speed, actualSpeed);
                }

                // Step 3: Integrate hover.
                {
                    var  groundCastEnd      = transform.worldPosition - new float3(0f, stats.targetHoverHeight + stats.skinWidth, 0f);
                    bool isOnGround         = Physics.ColliderCast(stats.collider, transform.worldTransform, groundCastEnd, in layer, out var hit, out _);
                    state.isSafeToInteract &= isOnGround;
                    // Apply either gravity or Hooke's law
                    var verticalAcceleration  = math.select(stats.gravity, stats.hoverKpM * math.max(stats.targetHoverHeight + stats.skinWidth - hit.distance, 0f), isOnGround);
                    state.verticalSpeed      += verticalAcceleration * deltaTime;
                    // If completely bottomed out, kill all downward momentum so that the player doesn't have to wait long to recover.
                    state.verticalSpeed = math.select(state.verticalSpeed,
                                                      math.max(state.verticalSpeed, 0f),
                                                      isOnGround && hit.distance <= stats.skinWidth + math.EPSILON);
                    var desiredVerticalDisplacement = state.verticalSpeed * deltaTime;
                    if (desiredVerticalDisplacement < 0f)
                    {
                        desiredVerticalDisplacement = math.min(desiredVerticalDisplacement, -hit.distance + stats.skinWidth);
                    }
                    else
                    {
                        var headCastEnd = transform.worldPosition + new float3(0f, desiredVerticalDisplacement + stats.skinWidth, 0f);
                        if (Physics.ColliderCast(stats.collider, transform.worldTransform, headCastEnd, in layer, out var headHit, out _))
                            desiredVerticalDisplacement = headHit.distance - stats.skinWidth;
                    }
                    transform.TranslateWorld(new float3(0f, desiredVerticalDisplacement, 0f));
                }
            }
        }
    }
}

