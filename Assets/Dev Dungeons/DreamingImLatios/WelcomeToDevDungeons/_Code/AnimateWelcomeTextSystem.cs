using FreeParking;
using Latios;
using Latios.Calligraphics;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace DreamingImLatios.Welcome.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct AnimateWelcomeTextSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job
            {
                descriptionBlob = latiosWorld.sceneBlackboardEntity.GetComponentData<CurrentDevDungeonDescription>().currentDevDungeonDescriptionBlob,
                dt              = Time.DeltaTime
            }.Schedule();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public BlobAssetReference<DevDungeonDescriptionBlob> descriptionBlob;
            public float                                         dt;

            public unsafe void Execute(TextRendererAspect textAspect, ref TextAnimationState state, in TextAnimationStats stats)
            {
                // Note: This is in no way meant to be fast, but honestly, it really doesn't have to be in this scenario.
                FixedString4096Bytes description = default;
                ref var              blobArray   = ref descriptionBlob.Value.description;
                description.Append((byte*)blobArray.GetUnsafePtr(), blobArray.Length);

                if (state.charactersPlayed == 0)
                    textAspect.text.Clear();

                if (state.pauseTimeRemaining > 0f)
                {
                    state.pauseTimeRemaining -= dt;
                    return;
                }

                var text           = textAspect.text;
                int characterIndex = 0;
                text.Clear();
                foreach (var character in description)
                {
                    text.Append(character);

                    if (characterIndex == state.charactersPlayed)
                    {
                        if (character.value == 10) // Linefeed
                            state.pauseTimeRemaining = stats.pauseDuration;

                        FixedString32Bytes s = "<alpha=#00>";
                        text.Append(s);
                    }
                    characterIndex++;
                }
                state.charactersPlayed++;
            }
        }
    }
}

