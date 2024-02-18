using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using Unity.Entities;

public struct bodyAimingComponent : IComponentData
{
    public quaternion rotationExtraAiming; //start with quaternion.identity note only used for inf and tanks but always present
    public float rotationExtraYAiming;
}
