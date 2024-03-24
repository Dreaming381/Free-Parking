using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Psyshock
{
    public static partial class UnitySim
    {
        /// <summary>   A contact jacobian angular. </summary>
        public struct ContactJacobianAngular
        {
            /// <summary>   The angular a. </summary>
            public float3 angularA;
            /// <summary>   The angular b. </summary>
            public float3 angularB;
            /// <summary>   The effective mass. </summary>
            public float effectiveMass;
        }

        /// <summary>   A contact jacobian angle and velocity to reach the contact plane. </summary>
        public struct ContactJacobianContactParameters
        {
            /// <summary>   The jacobian. </summary>
            public ContactJacobianAngular jacobianAngular;

            /// <summary>
            /// Velocity needed to reach the contact plane in one frame, both if approaching (negative) and
            /// depenetrating (positive)
            /// </summary>
            public float velocityToReachContactPlane;
        }

        public struct ContactJacobianBodyParameters
        {
            // Linear friction jacobians.  Only store the angular part, linear part can be recalculated from BaseJacobian.Normal
            public ContactJacobianAngular friction0;  // effectiveMass stores friction effective mass matrix element (0, 0)
            public ContactJacobianAngular friction1;  // effectiveMass stores friction effective mass matrix element (1, 1)
            public float3                 frictionDirection0;
            public float3                 frictionDirection1;

            // Angular friction about the contact normal, no linear part
            public ContactJacobianAngular angularFriction;  // effectiveMass stores friction effective mass matrix element (2, 2)
            public float3                 frictionEffectiveMassOffDiag;  // Effective mass matrix (0, 1), (0, 2), (1, 2) == (1, 0), (2, 0), (2, 1)

            public float3 contactNormal;
            public float3 surfaceVelocityDv;

            public float coefficientOfFriction;

            public void SetSurfaceVelocity(Velocity surfaceVelocity)
            {
                surfaceVelocityDv = default;
                if (!surfaceVelocity.Equals(float3.zero))
                {
                    float linVel0 = math.dot(surfaceVelocity.linear, frictionDirection0);
                    float linVel1 = math.dot(surfaceVelocity.linear, frictionDirection1);

                    float angVelProj  = math.dot(surfaceVelocity.angular, contactNormal);
                    surfaceVelocityDv = new float3(linVel0, linVel1, angVelProj);
                }
            }
        }

        // Internal motion data input for the solver stabilization
        public struct MotionStabilizationInput
        {
            public Velocity inputVelocity;
            public float    inverseInertiaScale;

            public static readonly MotionStabilizationInput kDefault = new MotionStabilizationInput
            {
                inputVelocity       = default,
                inverseInertiaScale = 1.0f
            };
        }

        public struct ContactJacobianImpulses
        {
            public float combinedContactPointsImpulse;
            public float friction0Impulse;
            public float friction1Impulse;
            public float frictionAngularImpulse;
        }

        public const float kMaxDepenetrationVelocityDynamicStatic  = float.MaxValue;
        public const float kMaxDepenetrationVelocityDynamicDynamic = 3f;

        // Notes: In Unity Physics, inertialPoseWorldTransformA/B is identity if the body is static.
        // perContactParameters can be uninitialized.
        // gravityAgainstContactNormal uses the magnitude of global gravity. Would a dot product with the contact normal be more appropriate?
        public static void BuildJacobian(Span<ContactJacobianContactParameters> perContactParameters, out ContactJacobianBodyParameters bodyParameters,
                                         RigidTransform inertialPoseWorldTransformA, in Velocity velocityA, in Mass massA,
                                         RigidTransform inertialPoseWorldTransformB, in Velocity velocityB, in Mass massB,
                                         float3 contactNormal, ReadOnlySpan<ContactsBetweenResult.ContactOnB> contacts,
                                         float coefficientOfRestitution, float coefficientOfFriction,
                                         float maxDepenetrationVelocity, float gravityAgainstContactNormal,
                                         float deltaTime, float inverseDeltaTime)
        {
            CheckContactAndJacobianSpanLengthsEqual(perContactParameters.Length, contacts.Length);

            var    negContactRestingVelocity = -gravityAgainstContactNormal * deltaTime;
            var    sumInverseMasses          = massA.inverseMass + massB.inverseMass;
            var    inverseRotationA          = math.conjugate(inertialPoseWorldTransformA.rot);
            var    inverseRotationB          = math.conjugate(inertialPoseWorldTransformB.rot);
            float3 centerA                   = 0f;
            float3 centerB                   = 0f;

            // Indicator whether restitution will be applied,
            // used to scale down friction on bounce.
            bool applyRestitution = false;

            for (int i = 0; i < perContactParameters.Length; i++)
            {
                // Build the jacobian
                ref var jacAngular = ref perContactParameters[i];
                var     contact    = contacts[i];
                float3  pointOnB   = contact.location;
                float3  pointOnA   = contact.location + contactNormal * contact.distanceToA;
                float3  armA       = pointOnA - inertialPoseWorldTransformA.pos;
                float3  armB       = pointOnB - inertialPoseWorldTransformB.pos;
                BuildJacobianAngular(inverseRotationA, inverseRotationB, contactNormal, armA, armB, massA.inverseInertia, massB.inverseInertia, sumInverseMasses,
                                     out jacAngular.jacobianAngular.angularA, out jacAngular.jacobianAngular.angularB, out float invEffectiveMass);
                jacAngular.jacobianAngular.effectiveMass = 1.0f / invEffectiveMass;

                float solveDistance = contact.distanceToA;
                float solveVelocity = solveDistance * inverseDeltaTime;

                solveVelocity = math.max(-maxDepenetrationVelocity, solveVelocity);

                jacAngular.velocityToReachContactPlane = -solveVelocity;

                // Calculate average position for friction
                centerA += armA;
                centerB += armB;

                // Restitution (optional)
                if (coefficientOfRestitution > 0.0f)
                {
                    float relativeVelocity = GetJacVelocity(contactNormal, jacAngular.jacobianAngular,
                                                            velocityA.linear, velocityA.angular, velocityB.linear, velocityB.angular);
                    float dv = jacAngular.velocityToReachContactPlane - relativeVelocity;
                    if (dv > 0.0f && relativeVelocity < negContactRestingVelocity)
                    {
                        // Note: The following comment comes from Unity Physics. However, gravityAcceleration was renamed to
                        // gravityAgainstContactNormal.

                        // Restitution impulse is applied as if contact point is on the contact plane.
                        // However, it can (and will) be slightly away from contact plane at the moment restitution is applied.
                        // So we have to apply vertical shot equation to make sure we don't gain energy:
                        // effectiveRestitutionVelocity^2 = restitutionVelocity^2 - 2.0f * gravityAcceleration * distanceToGround
                        // From this formula we calculate the effective restitution velocity, which is the velocity
                        // that the contact point needs to reach the same height from current position
                        // as if it was shot with the restitutionVelocity from the contact plane.
                        // ------------------------------------------------------------
                        // This is still an approximation for 2 reasons:
                        // - We are assuming the contact point will hit the contact plane with its current velocity,
                        // while actually it would have a portion of gravity applied before the actual hit. However,
                        // that velocity increase is quite small (less than gravity in one step), so it's safe
                        // to use current velocity instead.
                        // - gravityAcceleration is the actual value of gravity applied only when contact plane is
                        // directly opposite to gravity direction. Otherwise, this value will only be smaller.
                        // However, since this can only result in smaller bounce than the "correct" one, we can
                        // safely go with the default gravity value in all cases.
                        float restitutionVelocity          = (relativeVelocity - negContactRestingVelocity) * coefficientOfRestitution;
                        float distanceToGround             = math.max(-jacAngular.velocityToReachContactPlane * deltaTime, 0.0f);
                        float effectiveRestitutionVelocity =
                            math.sqrt(math.max(restitutionVelocity * restitutionVelocity - 2.0f * gravityAgainstContactNormal * distanceToGround, 0.0f));

                        jacAngular.velocityToReachContactPlane =
                            math.max(jacAngular.velocityToReachContactPlane - effectiveRestitutionVelocity, 0.0f) +
                            effectiveRestitutionVelocity;

                        // Remember that restitution should be applied
                        applyRestitution = true;
                    }
                }
            }

            // Build friction jacobians
            {
                // Clear accumulated impulse
                bodyParameters                       = default;
                bodyParameters.coefficientOfFriction = coefficientOfFriction;
                bodyParameters.contactNormal         = contactNormal;

                // Calculate average position
                float invNumContacts  = math.rcp(contacts.Length);
                centerA              *= invNumContacts;
                centerB              *= invNumContacts;

                // Choose friction axes
                mathex.GetDualPerpendicularNormalized(contactNormal, out float3 frictionDir0, out float3 frictionDir1);
                bodyParameters.frictionDirection0 = frictionDir0;
                bodyParameters.frictionDirection1 = frictionDir1;

                // Build linear jacobian
                float invEffectiveMass0, invEffectiveMass1;
                {
                    float3 armA = centerA;
                    float3 armB = centerB;
                    BuildJacobianAngular(inverseRotationA, inverseRotationB, frictionDir0, armA, armB, massA.inverseInertia, massB.inverseInertia, sumInverseMasses,
                                         out bodyParameters.friction0.angularA, out bodyParameters.friction0.angularB, out invEffectiveMass0);
                    BuildJacobianAngular(inverseRotationA, inverseRotationB, frictionDir1, armA, armB, massA.inverseInertia, massB.inverseInertia, sumInverseMasses,
                                         out bodyParameters.friction1.angularA, out bodyParameters.friction1.angularB, out invEffectiveMass1);
                }

                // Build angular jacobian
                float invEffectiveMassAngular;
                {
                    bodyParameters.angularFriction.angularA  = math.mul(inverseRotationA, contactNormal);
                    bodyParameters.angularFriction.angularB  = math.mul(inverseRotationB, -contactNormal);
                    float3 temp                              = bodyParameters.angularFriction.angularA * bodyParameters.angularFriction.angularA * massA.inverseInertia;
                    temp                                    += bodyParameters.angularFriction.angularB * bodyParameters.angularFriction.angularB * massB.inverseInertia;
                    invEffectiveMassAngular                  = math.csum(temp);
                }

                // Build effective mass
                {
                    // Build the inverse effective mass matrix
                    var invEffectiveMassDiag    = new float3(invEffectiveMass0, invEffectiveMass1, invEffectiveMassAngular);
                    var invEffectiveMassOffDiag = new float3(  // (0, 1), (0, 2), (1, 2)
                        CalculateInvEffectiveMassOffDiag(bodyParameters.friction0.angularA, bodyParameters.friction1.angularA,       massA.inverseInertia,
                                                         bodyParameters.friction0.angularB, bodyParameters.friction1.angularB, massB.inverseInertia),
                        CalculateInvEffectiveMassOffDiag(bodyParameters.friction0.angularA, bodyParameters.angularFriction.angularA, massA.inverseInertia,
                                                         bodyParameters.friction0.angularB, bodyParameters.angularFriction.angularB, massB.inverseInertia),
                        CalculateInvEffectiveMassOffDiag(bodyParameters.friction1.angularA, bodyParameters.angularFriction.angularA, massA.inverseInertia,
                                                         bodyParameters.friction1.angularB, bodyParameters.angularFriction.angularB, massB.inverseInertia));

                    // Invert the matrix and store it to the jacobians
                    if (!InvertSymmetricMatrix(invEffectiveMassDiag, invEffectiveMassOffDiag, out float3 effectiveMassDiag, out float3 effectiveMassOffDiag))
                    {
                        // invEffectiveMass can be singular if the bodies have infinite inertia about the normal.
                        // In that case angular friction does nothing so we can regularize the matrix, set col2 = row2 = (0, 0, 1)
                        invEffectiveMassOffDiag.y = 0.0f;
                        invEffectiveMassOffDiag.z = 0.0f;
                        invEffectiveMassDiag.z    = 1.0f;
                        bool success              = InvertSymmetricMatrix(invEffectiveMassDiag,
                                                                          invEffectiveMassOffDiag,
                                                                          out effectiveMassDiag,
                                                                          out effectiveMassOffDiag);
                        Unity.Assertions.Assert.IsTrue(success);  // it should never fail, if it does then friction will be disabled
                    }
                    bodyParameters.friction0.effectiveMass       = effectiveMassDiag.x;
                    bodyParameters.friction1.effectiveMass       = effectiveMassDiag.y;
                    bodyParameters.angularFriction.effectiveMass = effectiveMassDiag.z;
                    bodyParameters.frictionEffectiveMassOffDiag  = effectiveMassOffDiag;
                }

                // Reduce friction to 1/4 of the impulse if there will be restitution
                if (applyRestitution)
                {
                    bodyParameters.friction0.effectiveMass       *= 0.25f;
                    bodyParameters.friction1.effectiveMass       *= 0.25f;
                    bodyParameters.angularFriction.effectiveMass *= 0.25f;
                    bodyParameters.frictionEffectiveMassOffDiag  *= 0.25f;
                }
            }
        }

        // Returns true if a collision event was detected.
        // perContactImpulses should be initialized to 0f prior to the first solver iteration call (unless you know what you are doing).
        public static bool SolveJacobian(ref Velocity velocityA, in Mass massA, in MotionStabilizationInput motionStabilizationSolverInputA,
                                         ref Velocity velocityB, in Mass massB, in MotionStabilizationInput motionStabilizationSolverInputB,
                                         ReadOnlySpan<ContactJacobianContactParameters> perContactParameters, Span<float> perContactImpulses,
                                         in ContactJacobianBodyParameters bodyParameters,
                                         bool enableFrictionVelocitiesHeuristic, float InvNumSolverIterations,
                                         out ContactJacobianImpulses outputImpulses)
        {
            // Copy velocity data
            Velocity tempVelocityA = velocityA;
            Velocity tempVelocityB = velocityB;

            // Solve normal impulses
            bool  hasCollisionEvent = false;
            float sumImpulses       = 0.0f;
            outputImpulses          = default;

            for (int j = 0; j < perContactParameters.Length; j++)
            {
                ref readonly ContactJacobianContactParameters jacAngular     = ref perContactParameters[j];
                var                                           contactImpulse = perContactImpulses[j];

                // Solve velocity so that predicted contact distance is greater than or equal to zero
                float relativeVelocity = GetJacVelocity(bodyParameters.contactNormal, jacAngular.jacobianAngular,
                                                        tempVelocityA.linear, tempVelocityA.angular, tempVelocityB.linear, tempVelocityB.angular);
                float dv = jacAngular.velocityToReachContactPlane - relativeVelocity;

                float impulse            = dv * jacAngular.jacobianAngular.effectiveMass;
                float accumulatedImpulse = math.max(contactImpulse + impulse, 0.0f);
                if (accumulatedImpulse != contactImpulse)
                {
                    float deltaImpulse = accumulatedImpulse - contactImpulse;
                    ApplyImpulse(deltaImpulse, bodyParameters.contactNormal, jacAngular.jacobianAngular, ref tempVelocityA, ref tempVelocityB, in massA, in massB,
                                 motionStabilizationSolverInputA.inverseInertiaScale, motionStabilizationSolverInputB.inverseInertiaScale);
                }

                contactImpulse                               = accumulatedImpulse;
                perContactImpulses[j]                        = contactImpulse;
                sumImpulses                                 += accumulatedImpulse;
                outputImpulses.combinedContactPointsImpulse += contactImpulse;

                // Force contact event even when no impulse is applied, but there is penetration.
                hasCollisionEvent |= jacAngular.velocityToReachContactPlane > 0.0f;
            }

            // Export collision event
            hasCollisionEvent |= outputImpulses.combinedContactPointsImpulse > 0.0f;

            // Solve friction
            if (sumImpulses > 0.0f)
            {
                // Choose friction axes
                mathex.GetDualPerpendicularNormalized(bodyParameters.contactNormal, out float3 frictionDir0, out float3 frictionDir1);

                // Calculate impulses for full stop
                float3 imp;
                {
                    // Take velocities that produce minimum energy (between input and solver velocity) as friction input
                    float3 frictionLinVelA = tempVelocityA.linear;
                    float3 frictionAngVelA = tempVelocityA.angular;
                    float3 frictionLinVelB = tempVelocityB.linear;
                    float3 frictionAngVelB = tempVelocityB.angular;
                    if (enableFrictionVelocitiesHeuristic)
                    {
                        GetFrictionVelocities(motionStabilizationSolverInputA.inputVelocity.linear, motionStabilizationSolverInputA.inputVelocity.angular,
                                              tempVelocityA.linear, tempVelocityA.angular,
                                              math.rcp(massA.inverseInertia), math.rcp(massA.inverseMass),
                                              out frictionLinVelA, out frictionAngVelA);
                        GetFrictionVelocities(motionStabilizationSolverInputB.inputVelocity.linear, motionStabilizationSolverInputB.inputVelocity.angular,
                                              tempVelocityB.linear, tempVelocityB.angular,
                                              math.rcp(massB.inverseInertia), math.rcp(massB.inverseMass),
                                              out frictionLinVelB, out frictionAngVelB);
                    }

                    // Calculate the jacobian dot velocity for each of the friction jacobians
                    float dv0 = bodyParameters.surfaceVelocityDv.x - GetJacVelocity(frictionDir0,
                                                                                    bodyParameters.friction0,
                                                                                    frictionLinVelA,
                                                                                    frictionAngVelA,
                                                                                    frictionLinVelB,
                                                                                    frictionAngVelB);
                    float dv1 = bodyParameters.surfaceVelocityDv.y - GetJacVelocity(frictionDir1,
                                                                                    bodyParameters.friction1,
                                                                                    frictionLinVelA,
                                                                                    frictionAngVelA,
                                                                                    frictionLinVelB,
                                                                                    frictionAngVelB);
                    float dva = bodyParameters.surfaceVelocityDv.z - math.csum(
                        bodyParameters.angularFriction.angularA * frictionAngVelA + bodyParameters.angularFriction.angularB * frictionAngVelB);

                    // Reassemble the effective mass matrix
                    float3 effectiveMassDiag = new float3(bodyParameters.friction0.effectiveMass,
                                                          bodyParameters.friction1.effectiveMass,
                                                          bodyParameters.angularFriction.effectiveMass);
                    float3x3 effectiveMass = BuildSymmetricMatrix(effectiveMassDiag, bodyParameters.frictionEffectiveMassOffDiag);

                    // Calculate the impulse
                    imp = math.mul(effectiveMass, new float3(dv0, dv1, dva));
                }

                // Clip TODO.ma calculate some contact radius and use it to influence balance between linear and angular friction
                float maxImpulse              = sumImpulses * bodyParameters.coefficientOfFriction * InvNumSolverIterations;
                float frictionImpulseSquared  = math.lengthsq(imp);
                imp                          *= math.min(1.0f, maxImpulse * math.rsqrt(frictionImpulseSquared));

                // Apply impulses
                ApplyImpulse(imp.x, frictionDir0, bodyParameters.friction0, ref tempVelocityA, ref tempVelocityB,
                             in massA, in massB,
                             motionStabilizationSolverInputA.inverseInertiaScale, motionStabilizationSolverInputB.inverseInertiaScale);
                ApplyImpulse(imp.y, frictionDir1, bodyParameters.friction1, ref tempVelocityA, ref tempVelocityB,
                             in massA, in massB,
                             motionStabilizationSolverInputA.inverseInertiaScale, motionStabilizationSolverInputB.inverseInertiaScale);

                tempVelocityA.angular += imp.z * bodyParameters.angularFriction.angularA * motionStabilizationSolverInputA.inverseInertiaScale * massA.inverseInertia;
                tempVelocityB.angular += imp.z * bodyParameters.angularFriction.angularB * motionStabilizationSolverInputB.inverseInertiaScale * massB.inverseInertia;

                // Accumulate them
                outputImpulses.friction0Impulse       = imp.x;
                outputImpulses.friction1Impulse       = imp.y;
                outputImpulses.frictionAngularImpulse = imp.z;
            }

            // Write back linear and angular velocities. Changes to other properties, like InverseMass, should not be persisted.
            velocityA = tempVelocityA;
            velocityB = tempVelocityB;

            return hasCollisionEvent;
        }

        static void BuildJacobianAngular(quaternion inverseRotationA, quaternion inverseRotationB, float3 normal, float3 armA, float3 armB,
                                         float3 invInertiaA, float3 invInertiaB, float sumInvMass, out float3 angularA, out float3 angularB, out float invEffectiveMass)
        {
            float3 crossA = math.cross(armA, normal);
            angularA      = math.mul(inverseRotationA, crossA).xyz;

            float3 crossB = math.cross(normal, armB);
            angularB      = math.mul(inverseRotationB, crossB).xyz;

            float3 temp      = angularA * angularA * invInertiaA + angularB * angularB * invInertiaB;
            invEffectiveMass = temp.x + temp.y + temp.z + sumInvMass;
        }

        static float GetJacVelocity(float3 linear, ContactJacobianAngular jacAngular,
                                    float3 linVelA, float3 angVelA, float3 linVelB, float3 angVelB)
        {
            float3 temp  = (linVelA - linVelB) * linear;
            temp        += angVelA * jacAngular.angularA;
            temp        += angVelB * jacAngular.angularB;
            return math.csum(temp);
        }

        private static void ApplyImpulse(
            float impulse, float3 linear, ContactJacobianAngular jacAngular,
            ref Velocity velocityA, ref Velocity velocityB,
            in Mass massA, in Mass massB,
            float inverseInertiaScaleA = 1.0f, float inverseInertiaScaleB = 1.0f)
        {
            velocityA.linear += impulse * linear * massA.inverseMass;
            velocityB.linear -= impulse * linear * massB.inverseMass;

            // Scale the impulse with inverseInertiaScale
            velocityA.angular += impulse * jacAngular.angularA * inverseInertiaScaleA * massA.inverseInertia;
            velocityB.angular += impulse * jacAngular.angularB * inverseInertiaScaleB * massB.inverseInertia;
        }

        static void GetFrictionVelocities(
            float3 inputLinearVelocity, float3 inputAngularVelocity,
            float3 intermediateLinearVelocity, float3 intermediateAngularVelocity,
            float3 inertia, float mass,
            out float3 frictionLinearVelocityOut, out float3 frictionAngularVelocityOut)
        {
            float inputEnergy;
            {
                float linearEnergySq  = mass * math.lengthsq(inputLinearVelocity);
                float angularEnergySq = math.dot(inertia * inputAngularVelocity, inputAngularVelocity);
                inputEnergy           = linearEnergySq + angularEnergySq;
            }

            float intermediateEnergy;
            {
                float linearEnergySq  = mass * math.lengthsq(intermediateLinearVelocity);
                float angularEnergySq = math.dot(inertia * intermediateAngularVelocity, intermediateAngularVelocity);
                intermediateEnergy    = linearEnergySq + angularEnergySq;
            }

            if (inputEnergy < intermediateEnergy)
            {
                // Make sure we don't change the sign of intermediate velocity when using the input one.
                // If sign was to be changed, zero it out since it produces less energy.
                bool3 changedSignLin       = inputLinearVelocity * intermediateLinearVelocity < float3.zero;
                bool3 changedSignAng       = inputAngularVelocity * intermediateAngularVelocity < float3.zero;
                frictionLinearVelocityOut  = math.select(inputLinearVelocity, float3.zero, changedSignLin);
                frictionAngularVelocityOut = math.select(inputAngularVelocity, float3.zero, changedSignAng);
            }
            else
            {
                frictionLinearVelocityOut  = intermediateLinearVelocity;
                frictionAngularVelocityOut = intermediateAngularVelocity;
            }
        }

        // Calculate the inverse effective mass for a pair of jacobians with perpendicular linear parts
        static float CalculateInvEffectiveMassOffDiag(float3 angA0, float3 angA1, float3 invInertiaA,
                                                      float3 angB0, float3 angB1, float3 invInertiaB)
        {
            return math.csum(angA0 * angA1 * invInertiaA + angB0 * angB1 * invInertiaB);
        }

        // Inverts a symmetric 3x3 matrix with diag = (0, 0), (1, 1), (2, 2), offDiag = (0, 1), (0, 2), (1, 2) = (1, 0), (2, 0), (2, 1)
        public static bool InvertSymmetricMatrix(float3 diag, float3 offDiag, out float3 invDiag, out float3 invOffDiag)
        {
            float3 offDiagSq      = offDiag.zyx * offDiag.zyx;
            float  determinant    = (mathex.cproduct(diag) + 2.0f * mathex.cproduct(offDiag) - math.csum(offDiagSq * diag));
            bool   determinantOk  = (determinant != 0);
            float  invDeterminant = math.select(0.0f, 1.0f / determinant, determinantOk);
            invDiag               = (diag.yxx * diag.zzy - offDiagSq) * invDeterminant;
            invOffDiag            = (offDiag.yxx * offDiag.zzy - diag.zyx * offDiag) * invDeterminant;
            return determinantOk;
        }

        // Builds a symmetric 3x3 matrix from diag = (0, 0), (1, 1), (2, 2), offDiag = (0, 1), (0, 2), (1, 2) = (1, 0), (2, 0), (2, 1)
        static float3x3 BuildSymmetricMatrix(float3 diag, float3 offDiag)
        {
            return new float3x3(
                new float3(diag.x, offDiag.x, offDiag.y),
                new float3(offDiag.x, diag.y, offDiag.z),
                new float3(offDiag.y, offDiag.z, diag.z)
                );
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckContactAndJacobianSpanLengthsEqual(int parametersLength, int contactsLength)
        {
            if (parametersLength != contactsLength)
                throw new ArgumentException($"Span<ContactJacobianContactParameters> length of {parametersLength} does not match the number of contacts {contactsLength}");
        }
    }
}

