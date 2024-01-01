using System.Runtime.CompilerServices;
using Latios.Kinemation;
using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Mimic.Mecanim
{
    internal static class MecanimInternalUtilities
    {
        internal static bool Approximately(float a, in float b)
        {
            return math.abs(b - a) < math.max(0.000001f * math.max(math.abs(a), math.abs(b)), math.EPSILON * 8f);
        }

        internal static void AddClipEvents(NativeList<TimedMecanimClipInfo> currentClipWeights,
                                           DynamicBuffer<TimedMecanimClipInfo> previousClipWeights,
                                           ref SkeletonClipSetBlob clipSet,
                                           ref DynamicBuffer<MecanimActiveClipEvent> clipEvents,
                                           float deltaTime)
        {
            // Todo: I think this implementation can generate double events if the motionTime is exactly equal to the eventTime.
            //Check for events in the current clips

            for (int i = 0; i < currentClipWeights.Length; i++)
            {
                var clipWeight = currentClipWeights[i];
                ref var clip = ref clipSet.clips[clipWeight.mecanimClipIndex];

                for (int j = 0; j < clip.events.times.Length; j++)
                {
                    var eventTime = clip.events.times[j];
                    var motionTime = clip.LoopToClipTime(clipWeight.motionTime);
                    var previousMotionTime = motionTime - deltaTime;
                    var hasLooped = motionTime < clipWeight.motionTime && clipWeight.motionTime > clip.duration;
                    if ((eventTime >= previousMotionTime && eventTime <= motionTime) ||
                        (hasLooped && eventTime >= clip.duration + previousMotionTime && eventTime <= clip.duration))
                    {
                        clipEvents.Add(new MecanimActiveClipEvent
                        {
                            nameHash = clip.events.nameHashes[j],
                            parameter = clip.events.parameters[j],
                            clipIndex = (short)clipWeight.mecanimClipIndex,
                            eventIndex = (short)j,
                        });
                    }
                }
            }

            //Check for clip events in the previous clips
            for (int i = 0; i < previousClipWeights.Length; i++)
            {
                var clipWeight = previousClipWeights[i];
                bool isPlaying = clipWeight.timeFragment == deltaTime;

                if (isPlaying)
                {
                    continue;
                }

                ref var clip = ref clipSet.clips[clipWeight.mecanimClipIndex];
                for (int j = 0; j < clip.events.times.Length; j++)
                {
                    var eventTime = clip.events.times[j];
                    var previousMotionTime = clip.LoopToClipTime(clipWeight.motionTime);
                    var motionTime = clip.LoopToClipTime(clipWeight.motionTime + clipWeight.timeFragment);
                    var hasLooped = motionTime < clipWeight.motionTime && clipWeight.motionTime > clip.duration;
                    if ((eventTime >= previousMotionTime && eventTime <= motionTime) ||
                        (hasLooped && eventTime >= clip.duration + previousMotionTime && eventTime <= clip.duration))
                    {
                        clipEvents.Add(new MecanimActiveClipEvent
                        {
                            nameHash = clip.events.nameHashes[j],
                            parameter = clip.events.parameters[j],
                            clipIndex = (short)clipWeight.mecanimClipIndex,
                            eventIndex = (short)j,
                        });
                    }
                }
            }
        }

        internal static void AddLayerClipWeights(ref NativeList<TimedMecanimClipInfo> clipWeights,
                                                 ref MecanimControllerLayerBlob layer,
                                                 short layerIndex,
                                                 short currentStateIndex,
                                                 short lastStateIndex,
                                                 NativeArray<MecanimParameter> parameters,
                                                 float timeInState,
                                                 float lastTransitionDuration,
                                                 float lastExitTime,
                                                 float layerWeight,
                                                 NativeList<float> floatCache = default)
        {
            MotionWeightCache weightCache;
            if (floatCache.IsCreated)
                weightCache = new MotionWeightCache(ref floatCache, layer.childMotions.Length);
            else
                weightCache = new MotionWeightCache(layer.childMotions.Length);

            if (lastStateIndex > -1)
            {
                ref var lastState = ref layer.states[lastStateIndex];

                if (lastTransitionDuration > timeInState)
                {
                    var lastStateTime = timeInState + lastExitTime;
                    var normalizedTime = timeInState / lastTransitionDuration;

                    AddStateClipWeights(layerIndex,
                                        ref clipWeights,
                                        ref lastState,
                                        lastStateIndex,
                                        ref layer.childMotions,
                                        parameters,
                                        lastStateTime,
                                        layerWeight * (1 - normalizedTime),
                                        weightCache);
                    AddStateClipWeights(layerIndex,
                                        ref clipWeights,
                                        ref layer.states[currentStateIndex],
                                        currentStateIndex,
                                        ref layer.childMotions,
                                        parameters,
                                        timeInState,
                                        layerWeight * normalizedTime,
                                        weightCache);
                }
                else
                {
                    AddStateClipWeights(layerIndex,
                                        ref clipWeights,
                                        ref layer.states[currentStateIndex],
                                        currentStateIndex,
                                        ref layer.childMotions,
                                        parameters,
                                        timeInState,
                                        layerWeight,
                                        weightCache);
                }
            }
            else
            {
                AddStateClipWeights(layerIndex,
                                    ref clipWeights,
                                    ref layer.states[currentStateIndex],
                                    currentStateIndex,
                                    ref layer.childMotions,
                                    parameters,
                                    timeInState,
                                    layerWeight,
                                    weightCache);
            }
        }

        private static void AddStateClipWeights(short layerIndex,
                                                ref NativeList<TimedMecanimClipInfo> clipWeights,
                                                ref MecanimStateBlob state,
                                                short stateIndex,
                                                ref BlobArray<MecanimStateBlob> childMotions,
                                                NativeArray<MecanimParameter> parameters,
                                                float timeInState,
                                                float weightFactor,
                                                MotionWeightCache motionWeightCache)
        {
            if (state.isBlendTree)
            {
                switch (state.blendTreeType)
                {
                    case MecanimStateBlob.BlendTreeType.Direct:
                        {
                            //Each motion has their own parameter

                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                ref var childMotion = ref childMotions[state.childMotionIndices[i]];
                                var directBlendParameterValue = parameters[state.directBlendParameterIndices[i]].floatParam;
                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref childMotion,
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor * directBlendParameterValue,
                                                    motionWeightCache);
                            }

                            break;
                        }

                    case MecanimStateBlob.BlendTreeType.Simple1D:
                        {
                            if (state.childMotionIndices.Length < 2)
                            {
                                if (state.childMotionIndices.Length == 1)
                                {
                                    ref var childMotion = ref childMotions[state.childMotionIndices[0]];
                                    clipWeights.Add(new TimedMecanimClipInfo(ref childMotion, parameters, weightFactor,
                                                                             timeInState, layerIndex, stateIndex));
                                }

                                break;
                            }
                            var blendParameter = parameters[state.blendParameterIndex].floatParam;

                            //This will depend on the editor automatically reordering the child motions according to threshold
                            int stateChildMotionStartIndex = -1;
                            for (int i = 0; i < state.childMotionIndices.Length - 1; i++)
                            {
                                if (state.childMotionThresholds[i] <= blendParameter &&
                                    state.childMotionThresholds[i + 1] >= blendParameter)
                                {
                                    //Blend these two
                                    stateChildMotionStartIndex = i;
                                    break;
                                }
                            }

                            if (stateChildMotionStartIndex > -1)
                            {
                                // Calculate the blend factor.
                                float blendFactor = (blendParameter - state.childMotionThresholds[stateChildMotionStartIndex]) /
                                                    (state.childMotionThresholds[stateChildMotionStartIndex + 1] - state.childMotionThresholds[stateChildMotionStartIndex]);

                                ref var startChildMotion =
                                    ref childMotions[state.childMotionIndices[stateChildMotionStartIndex]];
                                ref var endChildMotion =
                                    ref childMotions[state.childMotionIndices[stateChildMotionStartIndex + 1]];

                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref startChildMotion,
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor * (1f - blendFactor),
                                                    motionWeightCache);
                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref endChildMotion,
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor * blendFactor,
                                                    motionWeightCache);
                            }
                            else
                            {
                                //Find the motion at the edge of the blend
                                var childMotionIndex = blendParameter < state.childMotionThresholds[0] ? 0 : state.childMotionIndices.Length - 1;
                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref childMotions[state.childMotionIndices[childMotionIndex]],
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor,
                                                    motionWeightCache);
                            }

                            break;
                        }
                    case MecanimStateBlob.BlendTreeType.SimpleDirectional2D:
                        {
                            var x = parameters[state.blendParameterIndex].floatParam;
                            var y = parameters[state.blendParameterYIndex].floatParam;
                            var blendParameter = new float2(x, y);

                            float totalWeight = 0;
                            NativeArray<float> childMotionWeights = motionWeightCache.GetCacheSubArray(state.childMotionIndices.Length);

                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                var motionPosition = state.childMotionPositions[i];

                                float dotProduct = math.dot(motionPosition, blendParameter);
                                // Use the dot product as the weight. This will be close to 1 for small angles and close to 0 for large angles.
                                float weight = math.clamp(dotProduct, 0f, 1f);  // Clamp the weight to [0, 1] to prevent negative weights for vectors pointing in opposite directions.

                                childMotionWeights[i] = weight;

                                totalWeight += weight;
                            }

                            //Get clip weights by their normalized weight stableSeed
                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                int childMotionIndex = state.childMotionIndices[i];
                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref childMotions[childMotionIndex],
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor * childMotionWeights[i] / totalWeight,
                                                    motionWeightCache);
                            }

                            break;
                        }
                    case MecanimStateBlob.BlendTreeType.FreeformCartesian2D:
                        {
                            // ref: https://runevision.com/thesis/rune_skovbo_johansen_thesis.pdf page 58 - Gradient Band Interpolation
                            // The interpolation specifies an influence function hi(p).  (hi(p) = min(1-(pip)*(pipj)/abs(pipj)^2))  The influence function hi(p) of a example i creates gradient bands between the
                            // example point pi and each of the other example points pj with j = 1...n, j 6= i. Note
                            // that the fraction inside the parenthesis is identical to a standard vector projection of
                            // pip onto pipj, except for a missing multiplication with the vector pipj. The fraction
                            // instead evaluates to a scalar that is 0 when the would-be projected vector would
                            // have zero length and is 1 when the would-be projected vector would be equal to
                            // pipj. Thus, after this fraction is subtracted from one, each gradient band decrease
                            // from the stableSeed of 1 at pi to 0 at pj
                            // The influence function evaluates to the minimum stableSeed of those gradient bands.
                            // These influence functions are normalized to get the weight functions wi(p) associated with each example i
                            // Due to the normalization the weight functions are not entirely identical to the influence functions:
                            // In some areas the influence functions may sum to more than one,
                            // and in those areas all the weights are divided by the sum (normalization). While
                            // the exact weights are altered by this, the overall shapes are quite similar and the
                            // extends of the weight functions (the areas in which they have non-zero weight) are not altered.
                            var x = parameters[state.blendParameterIndex].floatParam;
                            var y = parameters[state.blendParameterYIndex].floatParam;
                            var blendParameter = new float2(x, y);

                            var childMotionWeights = motionWeightCache.GetCacheSubArray(state.childMotionIndices.Length);
                            var totalWeight = 0f;
                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                var motionPosition = state.childMotionPositions[i];
                                var minWeight = float.PositiveInfinity;

                                float2 parametricVector = blendParameter - motionPosition;

                                for (int j = 0; j < state.childMotionIndices.Length; j++)
                                {
                                    if (i == j)
                                    {
                                        continue;
                                    }

                                    var referenceMotionPosition = state.childMotionPositions[j];
                                    float2 referenceVector = referenceMotionPosition - motionPosition;
                                    float weight = math.max(1f - math.dot(parametricVector, referenceVector) / math.lengthsq(referenceVector), 0);
                                    if (weight < minWeight)
                                    {
                                        minWeight = weight;
                                    }
                                }

                                childMotionWeights[i] = minWeight;
                                totalWeight += minWeight;
                            }

                            //Get clip weights by their normalized weight stableSeed
                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                int childMotionIndex = state.childMotionIndices[i];
                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref childMotions[childMotionIndex],
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor * childMotionWeights[i] / totalWeight,
                                                    motionWeightCache);
                            }

                            break;
                        }
                    case MecanimStateBlob.BlendTreeType.FreeformDirectional2D:
                        {
                            // ref: https://runevision.com/thesis/rune_skovbo_johansen_thesis.pdf page 59 - Gradient Bands in Polar Space
                            // Initially, the influence functions were defined in standard Cartesian space with the
                            //vectors pjpi and pjp specifying the differences in the x, y, and z components of the
                            //points.  The Polar gradient band interpolation method is based on the reasoning that in
                            // order to get a more desirable behavior for the weight functions of example points
                            // that represent velocities, the space in which the interpolation takes place should
                            // take on some of the properties of a polar coordinate system. This means that this
                            // interpolation method is far less general in that it is based on a defined origin in the
                            // interpolation space. However, in the specific case of handling velocities, this can be
                            // an advantage, since it allows for dealing with differences in direction and magnitude
                            // rather than differences in the Cartesian vector coordinate components. In this polar
                            // space, the gradient bands behaves somewhat differently:
                            // • Two example points placed equally far away from the origin should have a
                            // wedge shaped gradient band between them that extends from the origin and
                            // outwards (figure 6.12a). In this case the direction of the new point relative to
                            // the two example points determine the respective weights of the two examples.
                            // • Two example points placed successively further away from the origin in the
                            // same direction should have a circle shaped gradient band between them with
                            // its center in the origin (figure 6.12b). In this case the magnitude of the new
                            // point relative to the two example points determine the respective weights of
                            // the two examples.
                            // pipj = ((math.length(pj) - math.length(pi))/(math.length(pj) + math.length(pi))/2, math.angle(pj, pi))
                            // pip = ((math.length(p) - math.length(pi))/(math.length(pj) + math.length(pi))/2, math.angle(p, pi))
                            var x = parameters[state.blendParameterIndex].floatParam;
                            var y = parameters[state.blendParameterYIndex].floatParam;
                            var blendParameter = new float2(x, y);

                            var childMotionWeights = motionWeightCache.GetCacheSubArray(state.childMotionIndices.Length);
                            float totalWeight = 0;
                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                var motionPosition = state.childMotionPositions[i];
                                var minWeight = float.PositiveInfinity;

                                for (int j = 0; j < state.childMotionIndices.Length; j++)
                                {
                                    if (i == j)
                                    {
                                        continue;
                                    }
                                    var referenceMotionPosition = state.childMotionPositions[j];

                                    float2 parametricVector =
                                        new float2((math.length(blendParameter) - math.length(motionPosition)) /
                                                   (math.length(referenceMotionPosition) + math.length(motionPosition)) / 2f, blendParameter.angleSigned(motionPosition));
                                    float2 referenceVector =
                                        new float2((math.length(referenceMotionPosition) - math.length(motionPosition)) /
                                                   (math.length(referenceMotionPosition) + math.length(motionPosition)) / 2f, referenceMotionPosition.angleSigned(motionPosition));
                                    float dot = math.dot(parametricVector, referenceVector);
                                    float weight = math.abs(1f - dot);
                                    if (weight < minWeight)
                                    {
                                        minWeight = weight;
                                    }
                                }

                                childMotionWeights[i] = minWeight;
                                totalWeight += minWeight;
                            }

                            //Get clip weights by their normalized weight stableSeed
                            for (int i = 0; i < state.childMotionIndices.Length; i++)
                            {
                                int childMotionIndex = state.childMotionIndices[i];
                                AddStateClipWeights(layerIndex,
                                                    ref clipWeights,
                                                    ref childMotions[childMotionIndex],
                                                    stateIndex,
                                                    ref childMotions,
                                                    parameters,
                                                    timeInState,
                                                    weightFactor * childMotionWeights[i] / totalWeight,
                                                    motionWeightCache);
                            }

                            break;
                        }
                }
            }
            else
            {
                clipWeights.Add(new TimedMecanimClipInfo(ref state, parameters, weightFactor, timeInState, layerIndex, stateIndex));
            }
        }

        internal static TransformQvvs GetRootMotionDelta(ref MecanimControllerBlob controllerBlob,
                                             ref SkeletonClipSetBlob clipSet,
                                             NativeArray<MecanimParameter> parameters,
                                             float deltaTime,
                                             NativeList<TimedMecanimClipInfo> clipWeights,
                                             DynamicBuffer<TimedMecanimClipInfo> previousFrameClipInfo,
                                             float totalWeight,
                                             float weightCullingThreshold)
        {
            //Get the current clip deltas
            var currentRoot = TransformQvvs.identity;
            for (int i = 0; i < clipWeights.Length; i++)
            {
                var clipWeight = clipWeights[i];
                ref var clip = ref clipSet.clips[clipWeight.mecanimClipIndex];
                var blendWeight = clipWeight.weight / totalWeight;
                //Cull clips with negligible weight
                if (blendWeight < weightCullingThreshold)
                    continue;

                ref var state = ref controllerBlob.layers[clipWeight.layerIndex].states[clipWeight.stateIndex];
                var time = state.isLooping ? clip.LoopToClipTime(clipWeight.motionTime) : math.min(clipWeight.motionTime, clip.duration);
                var stateSpeed = state.speedMultiplierParameterIndex != -1 ?
                                 parameters[state.speedMultiplierParameterIndex].floatParam * state.speed :
                                 state.speed;
                var speedModifiedDeltaTime = deltaTime * stateSpeed;
                var deltaTransform = TransformQvvs.identity;
                var hasLooped = state.isLooping && time - deltaTime < 0f;

                //If the clip has looped, get a sample of the end of the clip to incorporate it into the delta
                if (hasLooped)
                {
                    deltaTransform = clip.SampleBone(0, time);
                    var previousClipSample = clip.SampleBone(0, time - speedModifiedDeltaTime);

                    var sampleEnd = math.select(clip.duration, -clip.duration, stateSpeed < 0f);
                    var endClipSample = clip.SampleBone(0, sampleEnd);

                    deltaTransform.position += endClipSample.position - previousClipSample.position;
                    deltaTransform.rotation = math.mul(deltaTransform.rotation, math.mul(math.inverse(endClipSample.rotation), previousClipSample.rotation));
                }
                else if (time < clip.duration)
                {
                    //Get the delta as normal
                    var currentClipSample = clip.SampleBone(0, time);
                    var previousClipSample = clip.SampleBone(0, time - speedModifiedDeltaTime);

                    deltaTransform.position += currentClipSample.position - previousClipSample.position;
                    deltaTransform.rotation = math.mul(currentClipSample.rotation, math.inverse(previousClipSample.rotation));
                }

                currentRoot.position += deltaTransform.position * blendWeight;
                currentRoot.rotation = math.slerp(currentRoot.rotation, math.mul(currentRoot.rotation, deltaTransform.rotation), blendWeight);
            }

            //Get the previous clip deltas
            var previousFrameTotalWeight = 0f;
            for (int i = 0; i < previousFrameClipInfo.Length; i++)
            {
                var clipWeight = previousFrameClipInfo[i].weight;
                if (clipWeight < weightCullingThreshold)
                    continue;
                previousFrameTotalWeight += clipWeight;
            }
            var previousRoot = TransformQvvs.identity;
            for (int i = 0; i < previousFrameClipInfo.Length; i++)
            {
                var clipWeight = previousFrameClipInfo[i];
                //We can tell if the clip is playing still by comparing the timeFragment to deltaTime
                //If the clip is no longer playing, we need to capture the fragmented delta by sampling at the motion time and at the motion time + time fragment
                var isPlaying = clipWeight.timeFragment == deltaTime;
                if (isPlaying)
                    continue;

                ref var clip = ref clipSet.clips[clipWeight.mecanimClipIndex];
                var blendWeight = clipWeight.weight / totalWeight;
                //Cull clips with negligible weight
                if (blendWeight < weightCullingThreshold)
                    continue;

                ref var state = ref controllerBlob.layers[clipWeight.layerIndex].states[clipWeight.stateIndex];
                var time = state.isLooping ? clip.LoopToClipTime(clipWeight.motionTime) : math.min(clipWeight.motionTime, clip.duration);

                var sampleTransform = clip.SampleBone(0, time);

                //If the clip has looped, we need the previous sample to capture the delta of the clip end
                var hasLooped = state.isLooping && time - deltaTime < 0f;
                if (hasLooped)
                {
                    var endClipSample = clip.SampleBone(0, clip.duration);

                    sampleTransform.position -= endClipSample.position;
                    sampleTransform.rotation = math.mul(endClipSample.rotation, math.inverse(sampleTransform.rotation));

                    var remainderSample = clip.SampleBone(0, clipWeight.timeFragment - (clip.duration - time));

                    sampleTransform.position -= remainderSample.position;
                    sampleTransform.rotation = math.mul(sampleTransform.rotation, math.inverse(remainderSample.rotation));
                }
                else
                {
                    //need to get the sample at the time fragment
                    var timeFragmentSample = clip.SampleBone(0, time + clipWeight.timeFragment);

                    sampleTransform.position -= timeFragmentSample.position;
                    sampleTransform.rotation = math.mul(sampleTransform.rotation, math.inverse(timeFragmentSample.rotation));
                }

                previousRoot.position += sampleTransform.position * blendWeight;
                previousRoot.rotation = math.slerp(previousRoot.rotation, math.mul(previousRoot.rotation, sampleTransform.rotation), blendWeight);
            }

            var rootDelta = TransformQvvs.identity;

            rootDelta.position = currentRoot.position - previousRoot.position;
            rootDelta.rotation = math.mul(currentRoot.rotation, math.inverse(previousRoot.rotation));

            return rootDelta;
        }

        internal static void ApplyBlendShapeBlends(ref MecanimControllerBlob controllerBlob, in DynamicBuffer<BlendShapeClipSet> blendShapeClipSets, ref BlendShapesAspect.Lookup blendShapesLookup, NativeList<TimedMecanimClipInfo> clipWeights, float totalWeight, float weightCullingThreshold)
        {
            //Blend shapes
            for (int i = 0; i < blendShapeClipSets.Length; i++)
            {
                var meshEntity = blendShapeClipSets[i].meshEntity;
                var blendShapeWeights = blendShapesLookup[meshEntity].weightsRW;
                ref var clipSetBlob = ref blendShapeClipSets[i].clips.Value;

                for (int j = 0; j < clipWeights.Length; j++)
                {
                    ref var clip = ref clipSetBlob.clips[clipWeights[j].mecanimClipIndex];
                    var clipWeight = clipWeights[j];
                    var blendWeight = clipWeight.weight / totalWeight;
                    //Cull clips with negligible weight
                    if (blendWeight < weightCullingThreshold)
                        continue;

                    ref var state = ref controllerBlob.layers[clipWeight.layerIndex].states[clipWeight.stateIndex];
                    var time = state.isLooping ? clip.LoopToClipTime(clipWeight.motionTime) : math.min(clipWeight.motionTime, clip.duration);

                    NativeArray<float> parameterValues = new NativeArray<float>(clip.parameterCount, Allocator.Temp);

                    //Get samples at normalized time
                    clip.SampleAllParameters(parameterValues, time / state.averageDuration);

                    for (int k = 0; k < clip.parameterCount; k++)
                    {
                        var parameterValue = parameterValues[k];

                        //Skip values without animations - denoted by specific float value
                        if (parameterValue > float.MinValue + float.Epsilon)
                        {
                            var blendedValue = parameterValue * blendWeight;
                            blendShapeWeights[k] = blendedValue;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float angleSigned(in this float2 from, in float2 to)
        {
            float unsignedAngle = angle(from, to);
            float sign = math.sign(from.x * to.y - from.y * to.x);
            return unsignedAngle * sign;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float angle(this float2 from, float2 to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = math.sqrt(math.lengthsq(from) * math.lengthsq(to));

            if (denominator < 1E-15F)
                return 0F;

            float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
            return math.radians(math.acos(dot));
        }

        // Always pass by stableSeed in recursive contexts
        struct MotionWeightCache
        {
            NativeArray<float> m_cache;
            public int m_usedCount;

            public MotionWeightCache(int motionTotalCount)
            {
                m_cache = new NativeArray<float>(motionTotalCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                m_usedCount = 0;
            }

            public MotionWeightCache(ref NativeList<float> sourceCache, int motionTotalCount)
            {
                sourceCache.ResizeUninitialized(motionTotalCount);
                m_cache = sourceCache.AsArray();
                m_usedCount = 0;
            }

            public NativeArray<float> GetCacheSubArray(int motionCount)
            {
                var result = m_cache.GetSubArray(m_usedCount, motionCount);
                m_usedCount += motionCount;
                return result;
            }
        }
    }
}
