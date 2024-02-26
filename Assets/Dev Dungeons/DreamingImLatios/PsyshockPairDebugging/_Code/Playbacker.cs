using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DreamingImLatios.PsyshockPairDebugging
{
    [Serializable]
    public struct SerializedColliderDistanceResult
    {
        public bool   hit;
        public float3 hitpointA;
        public float3 hitpointB;
        public float3 normalA;
        public float3 normalB;
        public float  distance;
        public int    subColliderIndexA;
        public int    subColliderIndexB;
    }

    [Serializable]
    public struct SerializedUnityContactsResult
    {
        public bool             hit;
        public float3           contactNormal;
        public List<Float3Pair> contactPointPairs;
    }

    [Serializable]
    public struct Float3Pair
    {
        public float3 a;
        public float3 b;
    }

    public class Playbacker : MonoBehaviour
    {
        [Tooltip("If this is set, then the inspector string is being live generated from the CaptureAuthoring on this same GameObject")]
        public bool isLive;
        [Tooltip("Check this to cause everything to be cleared and potentially recalculated")]
        public bool                                   reset;
        public string                                 hex;
        public SerializedColliderDistanceResult       closestColliderDistance;
        public SerializedUnityContactsResult          closestContacts;
        public List<SerializedColliderDistanceResult> allColliderDistances;
        public List<SerializedUnityContactsResult>    allContacts;

        private void OnValidate()
        {
            if (reset)
            {
                reset                   = false;
                hex                     = default;
                closestColliderDistance = default;
                closestContacts         = default;
                allColliderDistances    = default;
                allContacts             = default;
            }
        }
    }
}

