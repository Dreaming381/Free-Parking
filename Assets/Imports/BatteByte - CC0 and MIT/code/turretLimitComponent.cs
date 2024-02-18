using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct turretLimitComponent : IComponentData
{
    public float minimumY; // -85.0F for infantry
    public float maximumY; // 90.0F for infantry
    public float minimumX; // -10.0F for infantry
    public float maximumX; // 10.0F for infantry

    public float rotationSpeedDegreesPerSec; //360 for infantry
    public float rotationSpeedDegreesPerSecElevator; //380 for infantry
}
