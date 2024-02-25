using Latios;
using Latios.Kinemation;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace CharacterAdventures.Systems
{
    [BurstCompile]
    public partial struct TacticalGuyIKSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job { transformLookup = GetComponentLookup<WorldTransform>(true) }.Run();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            [ReadOnly] public ComponentLookup<WorldTransform> transformLookup;

            public void Execute(OptimizedSkeletonAspect skeleton, in DynamicBuffer<TacticalGuyArmIKStats> armStatsBuffer, in SkeletonBindingPathsBlobReference boneNames)
            {
                foreach (var armStats in armStatsBuffer)
                {
                    if (!boneNames.blob.Value.TryGetFirstPathIndexThatStartsWith(armStats.handBoneName, out var handIndex))
                        continue;

                    var handTargetTransform     = qvvs.inversemulqvvs(skeleton.skeletonWorldTransform, transformLookup[armStats.handTarget].worldTransform);
                    var handBone                = skeleton.bones[handIndex];
                    handTargetTransform.scale   = handBone.rootScale;
                    handTargetTransform.stretch = handBone.stretch;

                    handBone.rootTransform = handTargetTransform;
                }
            }
        }
    }
}

