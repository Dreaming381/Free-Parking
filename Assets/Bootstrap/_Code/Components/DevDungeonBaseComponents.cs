using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace FreeParking
{
    /// <summary>
    /// Lives on the SceneBlackboardEntity. If present, the game is paused.
    /// </summary>
    public struct PausedTag : IComponentData { }

    /// <summary>
    /// Lives on the SceneBlackboardEntity.
    /// </summary>
    public struct CurrentDevDungeonDescription : IComponentData
    {
        public BlobAssetReference<DevDungeonDescriptionBlob> currentDevDungeonDescriptionBlob;

        /// <summary>
        /// Call this in OnNewScene and cache the result. Use the result in OnShouldUpdate()
        /// </summary>
        /// <param name="testString"></param>
        /// <returns></returns>
        public unsafe bool CurrentDevDungeonPathStartsWith(ref FixedString128Bytes testString)
        {
            if (currentDevDungeonDescriptionBlob == BlobAssetReference<DevDungeonDescriptionBlob>.Null)
                return false;

            if (testString.Length > currentDevDungeonDescriptionBlob.Value.nameWithPath.Length)
                return false;

            return UnsafeUtility.MemCmp(testString.GetUnsafePtr(), currentDevDungeonDescriptionBlob.Value.nameWithPath.GetUnsafePtr(), testString.Length) == 0;
        }
    }

    public struct DevDungeonDescriptionBlob
    {
        public BlobArray<byte>             nameWithPath;
        public BlobArray<byte>             displayName;
        public BlobArray<BlobArray<byte> > creators;
        public BlobArray<byte>             description;
        public FixedString128Bytes         entrySceneName;
    }
}

