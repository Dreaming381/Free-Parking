using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using System;

namespace CharacterAdventures.Authoring
{
    [DisallowMultipleComponent]
    public class ImpAnimationAuthoring : MonoBehaviour
    {   
        [Tooltip("Adjust to make sure footsteps don't drift when walking.")]
        public float walkStepLength;
        [Tooltip("Adjust to make sure footsteps don't drift when running.")]
        public float runStepLength;
        [Tooltip("Fraction of max speed to transition to running.")]
        public float runThreshold;
        [Tooltip("Maximum horizontal angle to twist before the character moves their feet when aiming (radians)")]
        public float maxAimRotation;
        [Tooltip("Use the two-handed aim animation variant.")]
        public bool twoHands;

        [HideInInspector]
        public AnimationClip[] animations = new AnimationClip[Enum.GetValues(typeof(EImpAnimation)).Length];
    }

    [TemporaryBakingType]
    public struct ImpAnimationsSmartBakeItem : ISmartBakeItem<ImpAnimationAuthoring>
    {
        public SmartBlobberHandle<SkeletonClipSetBlob> animationBlob;

        public bool Bake(ImpAnimationAuthoring authoring, IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddComponent<ImpAnimationMovementOutput>(entity);
            baker.AddComponent<ImpAnimations>(entity);
            baker.AddComponent<ImpAnimationState>(entity);

            baker.AddComponent(entity, new ImpAnimationSettings {
                walkStepLength = authoring.walkStepLength,
                runStepLength = authoring.runStepLength,
                runThreshold = authoring.runThreshold,
                maxAimRotation = authoring.maxAimRotation,
                twoHands = authoring.twoHands
            });

            var animationClips = new NativeArray<SkeletonClipConfig>(
                    System.Enum.GetNames(typeof(EImpAnimation)).Length, Allocator.Temp
                );

            foreach (var i in Enum.GetValues(typeof(EImpAnimation)))
            {
                animationClips[(int) i] = new SkeletonClipConfig {
                    clip = authoring.animations[(int) i],
                    settings = SkeletonClipCompressionSettings.kDefaultSettings
                };
            }

            animationBlob = baker.RequestCreateBlobAsset(baker.GetComponent<Animator>(), animationClips);

            return true;
        }

        public void PostProcessBlobRequests(EntityManager entityManager, Entity entity)
        {
            entityManager.SetComponentData(entity,
                new ImpAnimations {
                    animations = animationBlob.Resolve(entityManager)
                }
            );
        }
    }

    class ImpAnimationsBaker: SmartBaker<ImpAnimationAuthoring, ImpAnimationsSmartBakeItem>
    {
    }

    [CustomEditor(typeof(ImpAnimationAuthoring))]
    public class ImpAnimationAuthoringInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new VisualElement();

            InspectorElement.FillDefaultInspector(inspector, serializedObject, this);

            var animationClipsHeader = new Label("Animation clips");

            animationClipsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            animationClipsHeader.style.marginTop = new Length(10);
            animationClipsHeader.style.marginBottom = new Length(5);
            animationClipsHeader.style.fontSize = new Length(14);

            inspector.Add(animationClipsHeader);

            var clipsProperty = serializedObject.FindProperty("animations");

            int i = 0;
            ObjectField clipField;
            foreach (string animation in Enum.GetNames(typeof(EImpAnimation)) )
            {
                clipField = new ObjectField();
                clipField.objectType=typeof(AnimationClip);
                clipField.label = animation;
                clipField.BindProperty(clipsProperty.GetArrayElementAtIndex(i));
                inspector.Add(clipField);
                ++i;
            }

            return inspector;
        }
    }
}

