using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.Bootstrap.Systems
{
    [BurstCompile]
    public partial struct ShowPauseMenuSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;
        bool                 m_previouslyPaused;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld        = state.GetLatiosWorldUnmanaged();
            m_previouslyPaused = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var paused = latiosWorld.sceneBlackboardEntity.HasComponent<PausedTag>();
            if (paused && !m_previouslyPaused)
                TryInitOrEnable(ref state);
            else if (!paused && m_previouslyPaused)
                TryDisable();
            m_previouslyPaused = paused;
        }

        void TryInitOrEnable(ref SystemState state)
        {
            if (latiosWorld.sceneBlackboardEntity.HasManagedStructComponent<MainWorldPauseMenu>())
            {
                var menu = latiosWorld.sceneBlackboardEntity.GetManagedStructComponent<MainWorldPauseMenu>();
                if (menu.pauseMenuGameObject == null)
                {
                    var prefab = latiosWorld.sceneBlackboardEntity.GetSharedComponentDataManaged<MainWorldPauseMenuPrefab>().pauseMenuPrefab;
                    if (prefab == null)
                    {
                        Debug.LogError("The main world pause menu prefab is null.");
                        return;
                    }
                    menu.pauseMenuGameObject = Object.Instantiate(prefab);
                    menu.entryPoint          = menu.pauseMenuGameObject.GetComponent<IPauseMenuMainWorld>();
                    if (menu.entryPoint == null)
                    {
                        Debug.LogError("The main world pause menu prefab does not contain a component implementing the IPauseMenuMainWorld interface.");
                    }
                    else
                    {
                        menu.entryPoint.Init(state.EntityManager, latiosWorld);
                    }
                    latiosWorld.sceneBlackboardEntity.SetManagedStructComponent(menu);
                }
                if (menu.pauseMenuGameObject != null)
                    menu.entryPoint?.SetEnabled(true);
            }
            else if (latiosWorld.sceneBlackboardEntity.HasManagedStructComponent<DevDungeonPauseMenu>())
            {
                var menu = latiosWorld.sceneBlackboardEntity.GetManagedStructComponent<DevDungeonPauseMenu>();
                if (menu.pauseMenuGameObject == null)
                {
                    if (!latiosWorld.sceneBlackboardEntity.HasComponent<CurrentDevDungeonDescription>())
                    {
                        Debug.LogError("The current scene does not have a Dev Dungeon Description instance.");
                        return;
                    }
                    var blob = latiosWorld.sceneBlackboardEntity.GetComponentData<CurrentDevDungeonDescription>().currentDevDungeonDescriptionBlob;
                    if (blob == BlobAssetReference<DevDungeonDescriptionBlob>.Null)
                    {
                        Debug.LogError("The current scene's Dev Dungeon Description blob is Null");
                        return;
                    }

                    var prefab = latiosWorld.sceneBlackboardEntity.GetSharedComponentDataManaged<DevDungeonPauseMenuPrefab>().pauseMenuPrefab;
                    if (prefab == null)
                    {
                        Debug.LogError("The main world pause menu prefab is null.");
                        return;
                    }
                    menu.pauseMenuGameObject = Object.Instantiate(prefab);
                    menu.entryPoint          = menu.pauseMenuGameObject.GetComponent<IPauseMenuDevDungeon>();
                    if (menu.entryPoint == null)
                    {
                        Debug.LogError("The main world pause menu prefab does not contain a component implementing the IPauseMenuDevDungeon interface.");
                    }
                    else
                    {
                        menu.entryPoint.Init(state.EntityManager, latiosWorld, blob);
                    }
                    latiosWorld.sceneBlackboardEntity.SetManagedStructComponent(menu);
                }
                if (menu.pauseMenuGameObject != null && menu.entryPoint != null)
                    menu.entryPoint.SetEnabled(true);
            }
            else
            {
                Debug.LogError(
                    "The scene does not have a main menu. Make sure to add a Dev Dungeon Scene Authoring or Main World Pause Prefab Authoring to the sceneBlackboardEntity");
            }
        }

        void TryDisable()
        {
            if (latiosWorld.sceneBlackboardEntity.HasManagedStructComponent<MainWorldPauseMenu>())
            {
                var menu = latiosWorld.sceneBlackboardEntity.GetManagedStructComponent<MainWorldPauseMenu>();
                if (menu.pauseMenuGameObject != null && menu.entryPoint != null)
                    menu.entryPoint.SetEnabled(false);
            }
            else if (latiosWorld.sceneBlackboardEntity.HasManagedStructComponent<DevDungeonPauseMenu>())
            {
                var menu = latiosWorld.sceneBlackboardEntity.GetManagedStructComponent<DevDungeonPauseMenu>();
                if (menu.pauseMenuGameObject != null && menu.entryPoint != null)
                    menu.entryPoint.SetEnabled(false);
            }
        }
    }
}

