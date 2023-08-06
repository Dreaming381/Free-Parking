using System;
using Latios;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace FreeParking
{
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
        public unsafe bool CurrentDevDungeonPathStartsWith(FixedString512Bytes testString)
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
        public BlobArray<byte>                nameWithPath;
        public BlobArray<byte>                displayName;
        public BlobArray<BlobArray<byte> >    creators;
        public BlobArray<byte>                description;
        public BlobArray<UnityEngine.Color32> thumbnail;
        public int2                           thumbnailDimensions;
        public FixedString128Bytes            entrySceneName;
    }

    public interface IPauseMenuDevDungeon
    {
        void Init(EntityManager entityManager, LatiosWorldUnmanaged latiosWorld, BlobAssetReference<DevDungeonDescriptionBlob> description);
        void SetEnabled(bool enabled);
    }

    /// <summary>
    /// Lives on the SceneBlackboardEntity.
    /// </summary>
    public struct DevDungeonPauseMenuPrefab : ISharedComponentData, IEquatable<DevDungeonPauseMenuPrefab>
    {
        public UnityEngine.GameObject pauseMenuPrefab;

        public bool Equals(DevDungeonPauseMenuPrefab other)
        {
            if (pauseMenuPrefab == null)
                return other.pauseMenuPrefab == null;

            return pauseMenuPrefab.Equals(other.pauseMenuPrefab);
        }

        public override int GetHashCode()
        {
            return pauseMenuPrefab.GetHashCode();
        }
    }

    /// <summary>
    /// Lives on the SceneBlackboardEntity.
    /// </summary>
    public partial struct DevDungeonPauseMenu : IManagedStructComponent
    {
        public UnityEngine.GameObject pauseMenuGameObject;
        public IPauseMenuDevDungeon   entryPoint;

        public void Dispose()
        {
            if (pauseMenuGameObject != null)
            {
                UnityEngine.Object.Destroy(pauseMenuGameObject);
                pauseMenuGameObject = null;
                entryPoint          = null;
            }
        }
    }
}

