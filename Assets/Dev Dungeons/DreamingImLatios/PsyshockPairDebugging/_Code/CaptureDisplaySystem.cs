using System.Diagnostics;
using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace DreamingImLatios.PsyshockPairDebugging.Systems
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    public partial struct CaptureDisplaySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var debug                   = new EntityManager.EntityManagerDebug(state.EntityManager);
            var allDistanceResultsCache = new NativeList<ColliderDistanceResult>(state.WorldUpdateAllocator);
            var allContactsResultsCache = new NativeList<UnitySim.ContactsBetweenResult>(state.WorldUpdateAllocator);
            var allCollector            = new AllCollector { results = allDistanceResultsCache };

            foreach ((var pair, Entity entity) in Query<CapturePairTarget>().WithEntityAccess())
            {
                if (!Exists(pair.colliderA) || !Exists(pair.colliderB))
                    continue;
                if (!HasComponent<Collider>(pair.colliderA) || !HasComponent<Collider>(pair.colliderB))
                    continue;
                if (!HasComponent<WorldTransform>(pair.colliderA) || !HasComponent<WorldTransform>(pair.colliderB))
                    continue;

                var go         = debug.GetAuthoringObjectForEntity(entity) as UnityEngine.GameObject;
                var playbacker = go?.GetComponent<Playbacker>();
                if (pair.drawOperation == DrawOperation.None && playbacker == null)
                    continue;

                var colliderA  = GetComponent<Collider>(pair.colliderA);
                var transformA = GetComponent<WorldTransform>(pair.colliderA).worldTransform;
                var colliderB  = GetComponent<Collider>(pair.colliderB);
                var transformB = GetComponent<WorldTransform>(pair.colliderB).worldTransform;

                var aabbA = Physics.AabbFrom(colliderA, transformA);
                PhysicsDebug.DrawAabb(aabbA, UnityEngine.Color.red);
                PhysicsDebug.DrawCollider(colliderA, transformA, UnityEngine.Color.green);
                PhysicsDebug.DrawCollider(colliderB, transformB, UnityEngine.Color.blue);
                //if (colliderA.type == ColliderType.Compound)
                //{
                //    CompoundCollider compound = colliderA;
                //    if (compound.compoundColliderBlob.Value.colliders[1].type == ColliderType.Sphere)
                //    {
                //        compound                      = Physics.ScaleStretchCollider(compound, transformA.scale, transformA.stretch);
                //        SphereCollider localSphere    = compound.compoundColliderBlob.Value.colliders[1];
                //        var            localTransform = compound.compoundColliderBlob.Value.transforms[1];
                //        compound.GetScaledStretchedSubCollider(1, out var scaledCollider, out var scaledTransform);
                //        SphereCollider scaledSphere   = scaledCollider;
                //        var            worldTransform = math.mul(new RigidTransform(transformA.rotation, transformA.position), scaledTransform);
                //        var            worldCenter    = math.transform(worldTransform, scaledSphere.center);
                //        UnityEngine.Debug.Log(
                //            $"localSphere: {localSphere.center}, scaledSphere: {scaledSphere.center}, worldSphere: {worldCenter}, localTransform: {localTransform.rot}, {localTransform.pos}, scaledTransform: {scaledTransform.rot}, {scaledTransform.pos}, worldTransform: {worldTransform.rot}, {worldTransform.pos}");
                //    }
                //}
                if (colliderA.type == ColliderType.Compound && colliderB.type == ColliderType.Compound)
                {
                    CompoundCollider compoundA = colliderA;
                    CompoundCollider compoundB = colliderB;
                    UnityEngine.Debug.Log(
                        $"tensorA: {compoundA.compoundColliderBlob.Value.inertiaTensor}, tensorB: {compoundB.compoundColliderBlob.Value.inertiaTensor * transformB.scale * transformB.scale}");
                }

                allDistanceResultsCache.Clear();
                allContactsResultsCache.Clear();
                if (playbacker != null)
                {
                    // We do this now in case something later crashes.
                    playbacker.isLive = true;
                    var hexString     = PhysicsDebug.LogDistanceBetween(colliderA, transformA, colliderB, transformB, pair.maxDistance, state.WorldUpdateAllocator);
                    playbacker.hex    = hexString.ToString();  // Todo: Need to not allocate every time, but unfortuantely the comparison allocates too. Thanks Unity!
                }

                UnitySim.ContactsBetweenResult closestContactsResult = default;
                var                            hit                   = Physics.DistanceBetween(colliderA,
                                                                    transformA,
                                                                    colliderB,
                                                                    transformB,
                                                                    pair.maxDistance,
                                                                    out var closestHitResult);
                if (hit)
                {
                    closestContactsResult = UnitySim.ContactsBetween(colliderA, transformA, colliderB, transformB, closestHitResult);
                    Physics.DistanceBetweenAll(colliderA, transformA, colliderB, transformB, pair.maxDistance, ref allCollector);
                    foreach (var distanceResult in allDistanceResultsCache)
                    {
                        allContactsResultsCache.Add(UnitySim.ContactsBetween(colliderA, transformA, colliderB, transformB, distanceResult));
                    }

                    if (pair.drawOperation == DrawOperation.DistanceBetweenClosest)
                    {
                        DebugDisplayDrawer.DrawColliderDistanceResult(closestHitResult);
                    }
                    else if (pair.drawOperation == DrawOperation.UnityContactsBetweenClosest)
                    {
                        DebugDisplayDrawer.DrawContactsResult(closestContactsResult, UnityEngine.Color.green);
                    }
                    else if (pair.drawOperation == DrawOperation.DistanceBetweenAll)
                    {
                        foreach (var distance in allDistanceResultsCache)
                            DebugDisplayDrawer.DrawColliderDistanceResult(distance);
                    }
                    else if (pair.drawOperation == DrawOperation.UnityContactsBetweenAll)
                    {
                        int i = 0;
                        foreach (var set in  allContactsResultsCache)
                        {
                            DebugDisplayDrawer.DrawContactsResult(set, DebugDisplayDrawer.GetContactResultColor(i));
                            i++;
                        }
                    }

                    if (playbacker != null)
                    {
                        playbacker.closestColliderDistance = new SerializedColliderDistanceResult
                        {
                            distance          = closestHitResult.distance,
                            hitpointA         = closestHitResult.hitpointA,
                            hitpointB         = closestHitResult.hitpointB,
                            normalA           = closestHitResult.normalA,
                            normalB           = closestHitResult.normalB,
                            subColliderIndexA = closestHitResult.subColliderIndexA,
                            subColliderIndexB = closestHitResult.subColliderIndexB,
                            hit               = true
                        };

                        if (playbacker.allColliderDistances == null)
                            playbacker.allColliderDistances = new System.Collections.Generic.List<SerializedColliderDistanceResult>();

                        playbacker.allColliderDistances.Clear();
                        foreach (var hitResult in allDistanceResultsCache)
                        {
                            playbacker.allColliderDistances.Add(new SerializedColliderDistanceResult
                            {
                                distance          = hitResult.distance,
                                hitpointA         = hitResult.hitpointA,
                                hitpointB         = hitResult.hitpointB,
                                normalA           = hitResult.normalA,
                                normalB           = hitResult.normalB,
                                subColliderIndexA = hitResult.subColliderIndexA,
                                subColliderIndexB = hitResult.subColliderIndexB,
                                hit               = true
                            });
                        }

                        if (playbacker.closestContacts.contactPointPairs == null)
                            playbacker.closestContacts.contactPointPairs = new System.Collections.Generic.List<Float3Pair>();

                        playbacker.closestContacts.contactPointPairs.Clear();
                        playbacker.closestContacts.contactNormal = closestContactsResult.contactNormal;
                        playbacker.closestContacts.hit           = true;
                        var closestFlipped                       = closestContactsResult.ToFlipped();
                        for (int i = 0; i < closestContactsResult.contactCount; i++)
                        {
                            playbacker.closestContacts.contactPointPairs.Add(new Float3Pair { a = closestContactsResult[i].location, b = closestFlipped[i].location });
                        }

                        if (playbacker.allContacts == null)
                            playbacker.allContacts = new System.Collections.Generic.List<SerializedUnityContactsResult>();

                        if (playbacker.allContacts.Count > allContactsResultsCache.Length)
                            playbacker.allContacts.RemoveRange(allContactsResultsCache.Length, playbacker.allContacts.Count - allContactsResultsCache.Length);

                        for (int i = playbacker.allContacts.Count; i < allContactsResultsCache.Length; i++)
                            playbacker.allContacts.Add(new SerializedUnityContactsResult { contactPointPairs = new System.Collections.Generic.List<Float3Pair>() });

                        for (int setIndex = 0; setIndex < allContactsResultsCache.Length; setIndex++)
                        {
                            var set        = allContactsResultsCache[setIndex];
                            var flipped    = set.ToFlipped();
                            var serialized = playbacker.allContacts[setIndex];
                            serialized.contactPointPairs.Clear();
                            serialized.contactNormal = set.contactNormal;
                            serialized.hit           = true;
                            for (int i = 0; i < set.contactCount; i++)
                                serialized.contactPointPairs.Add(new Float3Pair { a = set[i].location, b = flipped[i].location });
                            playbacker.allContacts[setIndex]                                             = serialized;
                        }
                    }
                }
                else if (playbacker != null)
                {
                    playbacker.closestColliderDistance = default;

                    var tempList = playbacker.closestContacts.contactPointPairs;
                    tempList.Clear();
                    playbacker.closestContacts                   = default;
                    playbacker.closestContacts.contactPointPairs = tempList;

                    playbacker.allColliderDistances.Clear();
                    playbacker.allContacts.Clear();
                }
            }
        }

        struct AllCollector : IDistanceBetweenAllProcessor
        {
            public NativeList<ColliderDistanceResult> results;

            public void Execute(in ColliderDistanceResult result)
            {
                results.Add(result);
            }
        }
    }

    static class DebugDisplayDrawer
    {
        public static void DrawColliderDistanceResult(ColliderDistanceResult result)
        {
            float3 x = new float3(0.05f, 0f, 0f);
            float3 y = x.yxz;
            float3 z = x.yzx;

            UnityEngine.Debug.DrawRay(result.hitpointA, result.normalA, UnityEngine.Color.red, 0f, false);
            UnityEngine.Debug.DrawLine(result.hitpointA - x, result.hitpointA + x, UnityEngine.Color.red, 0f, false);
            UnityEngine.Debug.DrawLine(result.hitpointA - y, result.hitpointA + y, UnityEngine.Color.red, 0f, false);
            UnityEngine.Debug.DrawLine(result.hitpointA - z, result.hitpointA + z, UnityEngine.Color.red, 0f, false);

            UnityEngine.Debug.DrawRay(result.hitpointB, result.normalB, UnityEngine.Color.blue, 0f, false);
            UnityEngine.Debug.DrawLine(result.hitpointB - x, result.hitpointB + x, UnityEngine.Color.blue, 0f, false);
            UnityEngine.Debug.DrawLine(result.hitpointB - y, result.hitpointB + y, UnityEngine.Color.blue, 0f, false);
            UnityEngine.Debug.DrawLine(result.hitpointB - z, result.hitpointB + z, UnityEngine.Color.blue, 0f, false);
        }

        public static void DrawContactsResult(UnitySim.ContactsBetweenResult result, UnityEngine.Color color)
        {
            var flipped   = result.ToFlipped();
            var darkColor = GetDarkenedColor(color);
            for (int i = 0; i < result.contactCount; i++)
            {
                if (result[i].distanceToA < 0f)
                    UnityEngine.Debug.DrawLine(result[i].location, flipped[i].location, darkColor, 0f, false);
                else
                    UnityEngine.Debug.DrawLine(result[i].location, flipped[i].location, color, 0f, false);
            }
        }

        public static UnityEngine.Color colliderColorA => new UnityEngine.Color(0.5f, 0f, 0f);
        public static UnityEngine.Color colliderColorB => new UnityEngine.Color(0.0f, 0f, 0.5f);

        public static UnityEngine.Color GetContactResultColor(int index)
        {
            return UnityEngine.Color.HSVToRGB((math.PI * index) % 1f, 1f, 1f);
        }

        static UnityEngine.Color GetDarkenedColor(UnityEngine.Color color)
        {
            color.r *= 0.25f;
            color.g *= 0.25f;
            color.b *= 0.25f;
            return color;
        }
    }
}

