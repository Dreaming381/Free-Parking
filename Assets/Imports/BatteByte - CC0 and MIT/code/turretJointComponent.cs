using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

//Mount should be attached to elevator. Rotator should be parent of elevator. This is different than monobehavior because of the way parent system works.
public struct turretJointComponent : IComponentData
{
    public Entity rotator; // parent of elevator
    public quaternion startingLocalRotationRotator;
    public Entity elevatorMount; // same entity as mount
    public quaternion startingLocalRotationElevator;
}