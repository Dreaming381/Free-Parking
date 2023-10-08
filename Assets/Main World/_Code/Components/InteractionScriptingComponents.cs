using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Mathematics;

namespace FreeParking.MainWorld.MainGameplay
{
    public struct InteractionScriptEntityReference : IBufferElementData
    {
        public Entity entity;
    }

    public struct InteractionScriptBlobReference : IBufferElementData
    {
        public UnsafeUntypedBlobAssetReference blob;
    }

    public struct InteractionScriptPlayer : IComponentData
    {
        public int taskIndex;
    }

    public struct InteractionScriptBlob
    {
        public enum TaskType
        {
            SetFlag,
            ClearFlag,
            GoTo,
            GoToIfFlagSet,
            GoToIfFlagUnset,
            PromptOption,
            ShowDevDungeonPreview,
            HideDevDungeonPreview,
            EnterDevDungeon,
            AddQuest,
            FinishQuest,
            PrintText,
            WaitForTextAcknowledge,
            CloseTextbox,
            InstantiateEntity,
            InstantiateEntityWithParent,
            // Todo: Much more can be added
        }

        public BlobArray<TaskType> taskTypes;
        public BlobArray<int>      typedTaskIndices;

        public BlobArray<GameFlagHandle> setFlagTasks;
        public BlobArray<GameFlagHandle> clearFlagTasks;

        public BlobArray<int> goToTasks;  // untyped task indices
        public struct GoToWithFlag
        {
            public GameFlagHandle flag;
            public int            destinationTaskIndex;
        }
        public BlobArray<GoToWithFlag> GoToIfFlagSetTasks;
        public BlobArray<GoToWithFlag> GoToIfFlagUnsetTasks;
        public struct PromptOption
        {
            public BlobArray<FixedString128Bytes> strings;
            public BlobArray<FixedString128Bytes> goToTaskIndices;
        }
        public BlobArray<PromptOption> promptOptionTasks;

        public BlobArray<int> showDevDungeonPreviewTasks;  // Index into blobs buffer
        public BlobArray<int> enterDevDungeonTasks;

        public BlobArray<int> addQuestTasks;  // Index into blobs buffer
        public BlobArray<int> finishQuestTasks;

        public BlobArray<BlobArray<byte> > printTextTasks;

        public struct WorldEntityToInstantiate
        {
            public TransformQvvs worldTransform;
            public Entity        entityToInstantiate;
        }
        public BlobArray<WorldEntityToInstantiate> instantiateEntityTasks;
        public struct ChildEntityToInstantiate
        {
            public TransformQvvs localTransform;
            public Entity        entityToInstantiate;
        }
        public BlobArray<ChildEntityToInstantiate> instantiateEntityWithParentTasks;
    }
}

