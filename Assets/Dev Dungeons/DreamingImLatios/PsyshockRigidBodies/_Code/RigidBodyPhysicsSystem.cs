using FreeParking;
using FreeParking.Systems;
using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace DreamingImLatios.PsyshockRigidBodies.Systems
{
    [BurstCompile]
    public partial struct RigidBodyPhysicsSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rigidBodyCount         = QueryBuilder().WithAll<RigidBody, Collider, WorldTransform>().Build().CalculateEntityCountWithoutFiltering();
            var rigidBodyColliderArray = CollectionHelper.CreateNativeArray<ColliderBody>(rigidBodyCount, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var rigidBodyAabbArray     = CollectionHelper.CreateNativeArray<Aabb>(rigidBodyCount, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            new BuildRigidBodiesJob
            {
                timeScaledGravity = Time.DeltaTime * -9.81f,
                deltaTime         = Time.DeltaTime,
                colliderArray     = rigidBodyColliderArray,
                aabbArray         = rigidBodyAabbArray
            }.ScheduleParallel();
            state.Dependency = Physics.BuildCollisionLayer(rigidBodyColliderArray, rigidBodyAabbArray)
                               .ScheduleParallel(out var rigidBodyLayer, state.WorldUpdateAllocator, state.Dependency);

            var pairStream            = new PairStream(rigidBodyLayer, state.WorldUpdateAllocator);
            var findBodyBodyProcessor = new FindBodyVsBodyProcessor
            {
                bodyLookup       = GetComponentLookup<RigidBody>(true),
                pairStream       = pairStream.AsParallelWriter(),
                deltaTime        = Time.DeltaTime,
                inverseDeltaTime = math.rcp(Time.DeltaTime)
            };
            state.Dependency = Physics.FindPairs(in rigidBodyLayer, in findBodyBodyProcessor).ScheduleParallelUnsafe();

            var environmentLayer             = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<EnvironmentCollisionLayer>(true);
            var findBodyEnvironmentProcessor = new FindBodyVsEnvironmentProcessor
            {
                bodyLookup       = GetComponentLookup<RigidBody>(true),
                pairStream       = pairStream.AsParallelWriter(),
                deltaTime        = Time.DeltaTime,
                inverseDeltaTime = math.rcp(Time.DeltaTime)
            };
            state.Dependency = Physics.FindPairs(in rigidBodyLayer, in environmentLayer.layer, in findBodyEnvironmentProcessor).ScheduleParallelUnsafe();
        }

        [BurstCompile]
        partial struct BuildRigidBodiesJob : IJobEntity
        {
            public float timeScaledGravity;
            public float deltaTime;

            [NativeDisableParallelForRestriction] public NativeArray<ColliderBody> colliderArray;
            [NativeDisableParallelForRestriction] public NativeArray<Aabb>         aabbArray;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, ref RigidBody rigidBody, in Collider collider, in WorldTransform transform)
            {
                rigidBody.velocity.linear.y += timeScaledGravity;

                var aabb                  = Physics.AabbFrom(in collider, in transform.worldTransform);
                var angularExpansion      = UnitySim.AngularExpansionFactorFrom(in collider);
                var motionExpansion       = new UnitySim.MotionExpansion(in rigidBody.velocity, deltaTime, angularExpansion);
                aabb                      = motionExpansion.ExpandAabb(aabb);
                rigidBody.motionExpansion = motionExpansion;

                colliderArray[index] = new ColliderBody
                {
                    collider  = collider,
                    transform = transform.worldTransform,
                    entity    = entity
                };
                aabbArray[index] = aabb;

                var localCenterOfMass = UnitySim.LocalCenterOfMassFrom(in collider);
                var localInertia      = UnitySim.LocalInertiaTensorFrom(in collider, transform.stretch);
                UnitySim.ConvertToWorldMassInertia(in transform.worldTransform,
                                                   in localInertia,
                                                   localCenterOfMass,
                                                   rigidBody.mass.inverseMass,
                                                   out rigidBody.mass,
                                                   out rigidBody.inertialPoseWorldTransform);
            }
        }

        struct FindBodyVsBodyProcessor : IFindPairsProcessor
        {
            [ReadOnly] public ComponentLookup<RigidBody> bodyLookup;
            public PairStream.ParallelWriter             pairStream;
            public float                                 deltaTime;
            public float                                 inverseDeltaTime;

            DistanceBetweenAllCache distanceBetweenAllCache;

            public void Execute(in FindPairsResult result)
            {
                ref readonly var rigidBodyA = ref bodyLookup.GetRefRO(result.entityA).ValueRO;
                ref readonly var rigidBodyB = ref bodyLookup.GetRefRO(result.entityB).ValueRO;

                var maxDistance = UnitySim.MotionExpansion.GetMaxDistance(in rigidBodyA.motionExpansion, in rigidBodyB.motionExpansion);
                Physics.DistanceBetweenAll(result.colliderA, result.transformA, result.colliderB, result.transformB, maxDistance, ref distanceBetweenAllCache);
                foreach (var distanceResult in distanceBetweenAllCache)
                {
                    var contacts = UnitySim.ContactsBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, in distanceResult);

                    var coefficientOfFriction    = math.sqrt(rigidBodyA.coefficientOfFriction * rigidBodyB.coefficientOfFriction);
                    var coefficientOfRestitution = math.sqrt(rigidBodyA.coefficientOfRestitution * rigidBodyB.coefficientOfRestitution);

                    ref var streamData           = ref pairStream.AddPairAndGetRef<ContactStreamData>(result.pairStreamKey, true, true, out var pair);
                    streamData.contactParameters = pair.Allocate<UnitySim.ContactJacobianContactParameters>(contacts.contactCount, NativeArrayOptions.UninitializedMemory);
                    streamData.contactImpulses   = pair.Allocate<float>(contacts.contactCount, NativeArrayOptions.ClearMemory);

                    UnitySim.BuildJacobian(streamData.contactParameters.AsSpan(),
                                           out streamData.bodyParameters,
                                           rigidBodyA.inertialPoseWorldTransform,
                                           in rigidBodyA.velocity,
                                           in rigidBodyA.mass,
                                           rigidBodyB.inertialPoseWorldTransform,
                                           in rigidBodyB.velocity,
                                           in rigidBodyB.mass,
                                           contacts.contactNormal,
                                           contacts.AsSpan(),
                                           coefficientOfRestitution,
                                           coefficientOfFriction,
                                           UnitySim.kMaxDepenetrationVelocityDynamicDynamic,
                                           9.81f,
                                           deltaTime,
                                           inverseDeltaTime);
                }
            }
        }

        struct FindBodyVsEnvironmentProcessor : IFindPairsProcessor
        {
            [ReadOnly] public ComponentLookup<RigidBody> bodyLookup;
            public PairStream.ParallelWriter             pairStream;
            public float                                 deltaTime;
            public float                                 inverseDeltaTime;

            DistanceBetweenAllCache distanceBetweenAllCache;

            public void Execute(in FindPairsResult result)
            {
                ref readonly var rigidBodyA = ref bodyLookup.GetRefRO(result.entityA).ValueRO;

                var maxDistance = UnitySim.MotionExpansion.GetMaxDistance(in rigidBodyA.motionExpansion);
                Physics.DistanceBetweenAll(result.colliderA, result.transformA, result.colliderB, result.transformB, maxDistance, ref distanceBetweenAllCache);
                foreach (var distanceResult in distanceBetweenAllCache)
                {
                    var contacts = UnitySim.ContactsBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, in distanceResult);

                    ref var streamData           = ref pairStream.AddPairAndGetRef<ContactStreamData>(result.pairStreamKey, true, true, out var pair);
                    streamData.contactParameters = pair.Allocate<UnitySim.ContactJacobianContactParameters>(contacts.contactCount, NativeArrayOptions.UninitializedMemory);
                    streamData.contactImpulses   = pair.Allocate<float>(contacts.contactCount, NativeArrayOptions.ClearMemory);

                    UnitySim.BuildJacobian(streamData.contactParameters.AsSpan(),
                                           out streamData.bodyParameters,
                                           rigidBodyA.inertialPoseWorldTransform,
                                           in rigidBodyA.velocity,
                                           in rigidBodyA.mass,
                                           RigidTransform.identity,
                                           default,
                                           default,
                                           contacts.contactNormal,
                                           contacts.AsSpan(),
                                           rigidBodyA.coefficientOfRestitution,
                                           rigidBodyA.coefficientOfFriction,
                                           UnitySim.kMaxDepenetrationVelocityDynamicDynamic,
                                           9.81f,
                                           deltaTime,
                                           inverseDeltaTime);
                }
            }
        }

        struct ContactStreamData
        {
            public UnitySim.ContactJacobianBodyParameters                bodyParameters;
            public StreamSpan<UnitySim.ContactJacobianContactParameters> contactParameters;
            public StreamSpan<float>                                     contactImpulses;
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class PsyshockRigidBodiesSuperSystem : RootSuperSystem
    {
        DevDungeonSystemFilter m_filter = new DevDungeonSystemFilter("DreamingImLatios/PsyshockRigidBodies");

        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildEnvironmentCollisionLayerSystem>();
            GetOrCreateAndAddUnmanagedSystem<RigidBodyPhysicsSystem>();
        }

        public override void OnNewScene() => m_filter.OnNewScene();

        public override bool ShouldUpdateSystem() => m_filter.ShouldUpdateSystem(sceneBlackboardEntity);
    }
}

