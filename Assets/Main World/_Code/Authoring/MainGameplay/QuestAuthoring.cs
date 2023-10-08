using System;
using System.Collections;
using System.Collections.Generic;
using FreeParking.Authoring;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace FreeParking.MainWorld.MainGameplay.Authoring
{
    [CreateAssetMenu(menuName = "Free Parking/Main World/Quest", fileName = "NewQuest")]
    public class QuestAuthoring : ScriptableObject
    {
        [Serializable]
        public struct SubquestAuthoring
        {
            public GameFlagAuthoring       subquestCompletionFlag;
            public string                  preChecklistText;
            public List<GameFlagAuthoring> checklist;
            public string                  postChecklistText;
        }

        public string                  questName;
        public List<SubquestAuthoring> subquests;

        public unsafe BlobAssetReference<QuestBlob> CreateBlob(IBaker baker)
        {
            baker.DependsOn(this);

            if (questName == null || subquests == null || subquests.Count == 0)
                return default;

            var     builder   = new BlobBuilder(Allocator.Temp);
            ref var root      = ref builder.ConstructRoot<QuestBlob>();
            root.questName    = questName;
            var textCache     = new NativeText(Allocator.Temp);
            var subquestArray = builder.Allocate(ref root.subquests, subquests.Count);
            int i             = 0;
            foreach (var subquest in subquests)
            {
                if (subquest.subquestCompletionFlag != null)
                {
                    subquestArray[i].completionFlag = subquest.subquestCompletionFlag;
                }

                textCache.Clear();
                if (subquest.preChecklistText != null)
                {
                    textCache.Append(subquest.preChecklistText);
                    var ptr = builder.Allocate(ref subquestArray[i].prelistText, textCache.Length).GetUnsafePtr();
                    UnsafeUtility.MemCpy(ptr, textCache.GetUnsafePtr(), textCache.Length);
                }
                if (subquest.checklist != null && subquest.checklist.Count > 0)
                {
                    var flags = builder.Allocate(ref subquestArray[i].checklistFlags, subquest.checklist.Count);
                    int j     = 0;
                    foreach (var f in subquest.checklist)
                    {
                        if (f == null)
                            flags[j] = default;
                        else
                            flags[j] = f;
                        j++;
                    }
                }
                textCache.Clear();
                if (subquest.postChecklistText != null)
                {
                    textCache.Append(subquest.postChecklistText);
                    var ptr = builder.Allocate(ref subquestArray[i].postlistText, textCache.Length).GetUnsafePtr();
                    UnsafeUtility.MemCpy(ptr, textCache.GetUnsafePtr(), textCache.Length);
                }
                i++;
            }
            var result = builder.CreateBlobAssetReference<QuestBlob>(Allocator.Persistent);
            baker.AddBlobAsset(ref result, out _);
            return result;
        }
    }
}

