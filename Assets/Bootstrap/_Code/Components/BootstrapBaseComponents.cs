using System;
using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace FreeParking
{
    /// <summary>
    /// Lives on the SceneBlackboardEntity. If present, the game is paused.
    /// </summary>
    public struct PausedTag : IComponentData { }

    public interface IPauseMenuMainWorld
    {
        void Init(EntityManager entityManager, LatiosWorldUnmanaged latiosWorld);
        void SetEnabled(bool enabled);
    }

    /// <summary>
    /// Lives on the SceneBlackboardEntity.
    /// </summary>
    public struct MainWorldPauseMenuPrefab : ISharedComponentData, IEquatable<MainWorldPauseMenuPrefab>
    {
        public UnityEngine.GameObject pauseMenuPrefab;

        public bool Equals(MainWorldPauseMenuPrefab other)
        {
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
    public partial struct MainWorldPauseMenu : IManagedStructComponent
    {
        public UnityEngine.GameObject pauseMenuGameObject;
        public IPauseMenuMainWorld    entryPoint;

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

