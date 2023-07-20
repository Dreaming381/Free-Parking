using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace FreeParking
{
    public struct PausedTag : IComponentData { }

    public struct CurrentDevDungeonDescription : IComponentData
    {
        public BlobAssetReference<DevDungeonDescriptionBlob> currentDevDungeonDescriptionBlob;
        public bool                                          isFirstFrame;
        public bool                                          isLastFrame;

        public unsafe bool currentDevDungeonPathStartsWith(ref FixedString128Bytes testString)
        {
            if (currentDevDungeonDescriptionBlob == BlobAssetReference<DevDungeonDescriptionBlob>.Null)
                return false;

            if (testString.Length > currentDevDungeonDescriptionBlob.Value.nameWithPath.Length)
                return false;

            return UnsafeUtility.MemCmp(testString.GetUnsafePtr(), currentDevDungeonDescriptionBlob.Value.nameWithPath.GetUnsafePtr(), testString.Length) == 0;
        }

        public bool TestShouldUpdateSystem(ref bool cachedBoolFieldDefaultFalse, ref FixedString128Bytes testString)
        {
            if (!isFirstFrame && !isLastFrame)
                return cachedBoolFieldDefaultFalse;

            if (isFirstFrame)
            {
                cachedBoolFieldDefaultFalse = currentDevDungeonPathStartsWith(ref testString);
                return cachedBoolFieldDefaultFalse;
            }

            cachedBoolFieldDefaultFalse = false;
            return false;
        }
    }

    public struct DevDungeonDescriptionBlob
    {
        public BlobArray<byte>             nameWithPath;
        public BlobArray<byte>             displayName;
        public BlobArray<BlobArray<byte> > creators;
        public BlobArray<byte>             description;
    }
}

