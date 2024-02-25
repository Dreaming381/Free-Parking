using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using Unity.Entities;

public struct bodyAimingComponent : IComponentData
{
    //some units types can rotate thier whole body to face the target. 
    public quaternion rotationExtraAiming; //start with quaternion.identity
    public float rotationExtraYAiming;
}
