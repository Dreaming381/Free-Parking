using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace NetForce.Systems
{
    [BurstCompile]
    public partial struct ForwardCharacterControllerV1System : ISystem
    {
        LatiosWorldUnmanaged         latiosWorld;
        EntityQuery                  m_query;
        PhysicsTransformAspectLookup m_transformLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld       = state.GetLatiosWorldUnmanaged();
            m_query           = state.Fluent().WithAll<ForwardCharacterControllerV1State>().Build();
            m_transformLookup = new PhysicsTransformAspectLookup(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_transformLookup.Update(ref state);
            var environmentLayer = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<EnvironmentCollisionLayer>(true).layer;
            var count            = m_query.CalculateEntityCountWithoutFiltering();
            var bodies           = CollectionHelper.CreateNativeArray<ColliderBody>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var aabbs            = CollectionHelper.CreateNativeArray<Aabb>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            state.Dependency     = new Job
            {
                aabbs            = aabbs,
                bodies           = bodies,
                dt               = Time.DeltaTime,
                environmentLayer = environmentLayer
            }.ScheduleParallel(state.Dependency);
            state.Dependency = Physics.BuildCollisionLayer(bodies, aabbs).ScheduleParallel(out var enemyLayer, state.WorldUpdateAllocator, state.Dependency);

            var charCharProcessor = new CharacterCharacterProcessor
            {
                transformLookup = m_transformLookup
            };
            var charEnvironmentProcessor = new CharacterEnvironmentProcessor
            {
                transformLookup = m_transformLookup
            };

            state.Dependency = Physics.FindPairs(enemyLayer, charCharProcessor).ScheduleParallel(state.Dependency);
            state.Dependency = Physics.FindPairs(enemyLayer, environmentLayer, charEnvironmentProcessor).ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            [ReadOnly] public CollisionLayer environmentLayer;

            [NativeDisableParallelForRestriction] public NativeArray<ColliderBody> bodies;
            [NativeDisableParallelForRestriction] public NativeArray<Aabb>         aabbs;

            public float dt;

            public void Execute(Entity entity,
                                [EntityIndexInQuery] int index,
                                TransformAspect transform,
                                ref ForwardCharacterControllerV1State state,
                                in ForwardCharacterControllerV1Stats stats,
                                in ForwardDesiredActions desiredActions)
            {
                // Check ground state first
                bool   isGround      = false;
                float3 supportNormal = float3.zero;
                {
                    var sphere              = new SphereCollider(0f, stats.radius);
                    var sphereStart         = transform.worldTransform;
                    sphereStart.position.y += stats.height - stats.radius;
                    var sphereEnd           = transform.worldPosition;
                    sphereEnd.y            += stats.radius - stats.groundCheckDistance;
                    isGround                = Physics.ColliderCast(sphere, sphereStart, sphereEnd, environmentLayer, out var hitResult, out _);
                    if (isGround)
                    {
                        supportNormal = hitResult.normalOnTarget;
                        transform.TranslateWorld(new float3(0f, -(hitResult.distance - (sphereStart.position.y - sphereEnd.y)), 0f));
                    }
                }

                // Integrate turn
                var heading      = math.normalize(transform.forwardDirection.xz);
                var headingAngle = math.atan2(heading.y, heading.x);
                var targetAngle  = math.atan2(desiredActions.direction.y, desiredActions.direction.x);
                var delta        = targetAngle - headingAngle;
                if (delta > math.PI)
                    delta -= 2f * math.PI;
                if (delta < -math.PI)
                    delta           += 2f * math.PI;
                var allowedRotation  = math.radians(stats.turnSpeed) * dt;
                delta                = math.clamp(delta, -allowedRotation, allowedRotation);
                transform.RotateWorld(quaternion.Euler(0f, -delta, 0f));
                heading = math.normalize(transform.forwardDirection.xz);

                // Integrate motion
                if (isGround)
                {
                    state.horizontalVelocity = Physics.StepVelocityWithInput(desiredActions.forwardInput,
                                                                             state.horizontalVelocity,
                                                                             stats.acceleration,
                                                                             stats.deceleration,
                                                                             stats.maxSpeed,
                                                                             stats.acceleration,
                                                                             stats.deceleration,
                                                                             stats.maxSpeed,
                                                                             dt);
                    var forward3D =
                        math.normalize(math.cross(math.cross(supportNormal, new float3(heading.x, 0f, heading.y)), supportNormal));
                    transform.TranslateWorld(forward3D * state.horizontalVelocity * dt);
                    state.verticalVelocity = 0f;
                }
                else
                {
                    state.verticalVelocity -= stats.gravity * dt;
                    transform.TranslateWorld(new float3(heading.x * state.horizontalVelocity, state.verticalVelocity, heading.y * state.horizontalVelocity) * dt);
                }

                // Build Collision Layers
                var capsule   = new CapsuleCollider(new float3(0f, stats.radius, 0f), new float3(0f, stats.height - stats.radius, 0f), stats.radius);
                bodies[index] = new ColliderBody { collider = capsule, entity = entity, transform = transform.worldTransform };
                var aabb      = Physics.AabbFrom(capsule, transform.worldTransform);
                Physics.GetCenterExtents(aabb, out var center, out var extents);
                aabb.min     -= extents;
                aabb.max     += extents;
                aabbs[index]  = aabb;
            }
        }

        struct CharacterCharacterProcessor : IFindPairsProcessor
        {
            public PhysicsTransformAspectLookup transformLookup;

            public void Execute(in FindPairsResult result)
            {
                var transformA = transformLookup[result.entityA];
                var transformB = transformLookup[result.entityB];

                if (Physics.DistanceBetween(result.colliderA, transformA.worldTransform, result.colliderB, transformB.worldTransform, 0f, out var hitResult))
                {
                    var negativeHalfDistance = hitResult.distance / 2f;
                    transformA.TranslateWorld(negativeHalfDistance * hitResult.normalA);
                    transformB.TranslateWorld(negativeHalfDistance * hitResult.normalB);
                }
            }
        }

        struct CharacterEnvironmentProcessor : IFindPairsProcessor
        {
            public PhysicsTransformAspectLookup transformLookup;

            public void Execute(in FindPairsResult result)
            {
                var transform = transformLookup[result.entityA];

                if (Physics.DistanceBetween(result.colliderA, transform.worldTransform, result.colliderB, result.transformB, 0f, out var hitResult))
                {
                    transform.TranslateWorld(-hitResult.normalB * hitResult.distance);
                }
            }
        }
    }
}

