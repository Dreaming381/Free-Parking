using UnityEngine;
using System.Collections;
using System;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using System.Runtime.CompilerServices;
//using Unity.Physics;
//using Unity.Physics.Extensions;
using Unity.Entities;

/*
Copyright 2024 BattleByte Games LLC.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

public static class turretFunctions
{
    public static void resetMountRotation(in turretJointComponent turretData, out quaternion rotTurret, out quaternion rotElevator)
    {
        rotTurret = math.mul(turretData.startingLocalRotationRotator, quaternion.AxisAngle(math.up(), 0.0F)); //sideways
        rotElevator = math.mul(turretData.startingLocalRotationElevator, quaternion.AxisAngle(new float3(1.0F, 0.0F, 0.0F), 0.0F)); //up
    }

    public static float extractAxisAngleRadiansFromQuaternion(quaternion q, float3 axis)
    {
        axis = math.normalize(axis);

        //get the plane the axis is a normal of
        float3 orthonormal1;
        float3 orthonormal2;
        FindOrthonormals(axis, out orthonormal1, out orthonormal2);

        float3 transformed = math.mul(q, orthonormal1);

        //project transformed vector onto plane
        float3 flattened = transformed - (math.dot(transformed, axis) * axis);
        flattened = math.normalize(flattened);

        float3 cross = math.cross(orthonormal1, flattened);
        //get angle between original vector and projected transform to get angle around normal
        float angle = math.atan2(math.length(cross), math.dot(orthonormal1, flattened)); //see highPrecisionAngle

        if (math.dot(axis, cross) < 0.0F)
        {
            angle = -angle;
        }

        return angle;
    }

    public static void FindOrthonormals(float3 normal, out float3 orthonormal1, out float3 orthonormal2)
    {
        quaternion rotationX = quaternion.Euler(90.0F, 0.0F, 0.0F);
        float4x4 OrthoX = float4x4.TRS(float3.zero, rotationX, new float3(1.0F, 1.0F, 1.0F));

        quaternion rotationY = quaternion.Euler(0.0F, 90.0F, 0.0F);
        float4x4 OrthoY = float4x4.TRS(float3.zero, rotationY, new float3(1.0F, 1.0F, 1.0F));

        float3 w = math.mul(OrthoX, new float4(normal, 0.0F)).xyz;
        float dot = math.dot(normal, w);
        if (math.abs(dot) > 0.6F)
        {
            w = math.mul(OrthoY, new float4(normal, 0.0F)).xyz;
        }
        w = math.normalize(w);

        orthonormal1 = math.cross(normal, w);
        orthonormal1 = math.normalize(orthonormal1);
        orthonormal2 = math.cross(normal, orthonormal1);
        orthonormal2 = math.normalize(orthonormal2);
    }

    public static quaternion RotateTowards(quaternion from, quaternion to, float maxDegreesDelta)
    {
        if (maxDegreesDelta <= 0.0F)
        {
            return from;
        }

        float num = Angle(from, to);

        if (maxDegreesDelta >= num)
        {
            return to;
        }

        return math.slerp(from, to, maxDegreesDelta / num);
    }

    public static quaternion RotateTowards(quaternion from, quaternion to, float maxDegreesDelta, out bool onTarget)
    {
        if (maxDegreesDelta <= 0.0F)
        {
            onTarget = true;
            return from;
        }

        float num = Angle(from, to);

        if (maxDegreesDelta >= num)
        {
            onTarget = true;
            return to;
        }

        onTarget = false;
        return math.slerp(from, to, maxDegreesDelta / num);
    }

    public const float Epsilon = 0.00001F;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(quaternion q1, quaternion q2)
    {
        float dot = math.dot(q1, q2);

        if (dot >= (1.0F - Epsilon))
        {
            return 0.0F;
        }
        else
        {
            return (math.acos(math.min(math.abs(dot), 1.0F)) * 2.0F);
        }
    }

    private const float rotationSpeedThreeSixtyDegreesPerSec = 360.0F;

    private static quaternion snapLeft(float degrees)
    {
        return quaternion.AxisAngle(math.up(), math.radians(-degrees));
    }

    private static quaternion snapRight(float degrees)
    {
        return quaternion.AxisAngle(math.up(), math.radians(degrees));
    }

    private static quaternion rotateLeft(float tick, float rotationSpeed)
    {
        return quaternion.AxisAngle(math.up(), math.radians(-rotationSpeed * tick));
    }

    private static quaternion rotateRight(float tick, float rotationSpeed)
    {
        return quaternion.AxisAngle(math.up(), math.radians(rotationSpeed * tick));
    }


    public static void aimingRotation(ref bodyAimingComponent bodyAiming, float rotAroundX, float tick)
    {
        float rotABS = math.abs(rotAroundX);
        bool shouldSnapRotation = (rotABS <= (rotationSpeedThreeSixtyDegreesPerSec * tick));
        bool aimRight = (rotAroundX >= 0.0F);

        if (shouldSnapRotation)
        {
            if (aimRight)
            {
                bodyAiming.rotationExtraAiming = snapRight(rotABS);
                bodyAiming.rotationExtraYAiming = rotABS;
            }
            else
            {
                bodyAiming.rotationExtraAiming = snapLeft(rotABS);
                bodyAiming.rotationExtraYAiming = -rotABS;
            }
        }
        else
        {
            if (aimRight)
            {
                bodyAiming.rotationExtraAiming = rotateRight(tick, rotationSpeedThreeSixtyDegreesPerSec);
                bodyAiming.rotationExtraYAiming = rotationSpeedThreeSixtyDegreesPerSec * tick;
            }
            else
            {
                bodyAiming.rotationExtraAiming = rotateLeft(tick, rotationSpeedThreeSixtyDegreesPerSec);
                bodyAiming.rotationExtraYAiming = -rotationSpeedThreeSixtyDegreesPerSec * tick;
            }
        }
    }

    /*
     * Shoddy code warning
     * This code uses LocalTransform
     * This code was extracted from multiple systems and rewarned/recombined should only be used as an example for tactical guy aiming. Untested and probably has issues.   
     
    public static float3 transformDirectionWorldSpaceToLocalSpace(in Entity e, ref ComponentLookup<LocalTransform> localTransformLookup, ref ComponentLookup<Parent> parentLookup,
        ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup, in float3 dirWorld)
    {
        float4x4 worldSpaceTransformationMatrix;
        TransformHelpers.ComputeWorldTransformMatrix(in e, out worldSpaceTransformationMatrix, ref localTransformLookup, ref parentLookup, ref postTransformMatrixLookup);

        return math.transform(math.inverse(float4x4.TRS(float3.zero, Unity.Physics.Math.DecomposeRigidBodyOrientation(in worldSpaceTransformationMatrix), new float3(1.0F, 1.0F, 1.0F))), dirWorld);
    }

    public static void aim(in Entity entityTurret, in Parent elevatorParent, in float3 worldPosElevator, in float3 targetPosWorld, ref ComponentLookup<LocalTransform> localTransformLookup, ref ComponentLookup<Parent> parentLookup,
        ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup, in turretJointComponent turretJointComponentMount, ref bodyAimingComponent bodyAiming, in turretLimitComponent wLimit,
        out bool aimingOnTarget, float timeStep)
    {
        LocalTransform localTransformElevator = localTransformLookup[entityTurret];

        //y and x
        float3 worldDir = math.normalize(targetPosWorld - worldPosElevator);
        float3 relativePos = math.normalize(transformFunctionsDOTS.transformDirectionWorldSpaceToLocalSpace(in entityTurret, ref localTransformLookup, ref parentLookup, ref postTransformMatrixLookup, worldDir)); //math.normalize prob not needed

        quaternion rot;

        if (relativePos.Equals(float3.zero) != false)
        {
            rot = quaternion.LookRotation(relativePos, math.up()); //world space
        }
        else
        {
            rot = quaternion.identity;
        }

        float rotationX = math.degrees(extractAxisAngleRadiansFromQuaternion(rot, math.up()));

        float3 axisRight;
        if (rotationX > 90.0F || rotationX < -90.0F)
        {
            axisRight = -new float3(1.0F, 0.0F, 0.0F);
        }
        else
        {
            axisRight = new float3(1.0F, 0.0F, 0.0F);
        }

        float rotationY = math.degrees(extractAxisAngleRadiansFromQuaternion(rot, axisRight));
        float rotationExtraAiming = rotationX;

        //soft limit
        rotationX = math.clamp(rotationX, wLimit.minimumX, wLimit.maximumX);
        rotationY = math.clamp(rotationY, wLimit.maximumY, wLimit.minimumY); //note min is max

        //some units types can rotate to face target
        rotationExtraAiming = rotationExtraAiming - rotationX;

        if (rotationExtraAiming != 0.0F)
        {
            aimingRotation(ref bodyAiming, rotationExtraAiming, timeStep);// extra rotation needs to rotate the character controller
        }

        float rotationRateDegrees = wLimit.rotationSpeedDegreesPerSec * timeStep;

        quaternion rotTurretNew = math.mul(turretJointComponentMount.startingLocalRotationRotator, quaternion.AxisAngle(math.up(), math.radians(rotationX)));
        quaternion rotatorTurret = localTransformLookup[elevatorParent.Value].Rotation;
        LocalTransform turretTrans = localTransformLookup[elevatorParent.Value];

        bool onTargetTurret;
        rotTurretNew = RotateTowards(rotatorTurret, rotTurretNew, rotationRateDegrees, out onTargetTurret); //limit rot speed
        turretTrans.Rotation = rotTurretNew;  //sideways
        localTransformLookup[elevatorParent.Value] = turretTrans; //set new rot

        quaternion rotElevator = math.mul(turretJointComponentMount.startingLocalRotationElevator, quaternion.AxisAngle(new float3(1.0F, 0.0F, 0.0F), math.radians(rotationY)));

        bool onTargetElevator;
        rotElevator = RotateTowards(localTransformElevator.Rotation, rotElevator, rotationRateDegrees, out onTargetElevator); //limit rot speed
        localTransformElevator.Rotation = rotElevator; // up
        localTransformLookup[entityTurret] = localTransformElevator;

        if (onTargetTurret && onTargetElevator)
        {
            aimingOnTarget = true;
        }
        else
        {
            aimingOnTarget = false;
        }
    }
    */
}
