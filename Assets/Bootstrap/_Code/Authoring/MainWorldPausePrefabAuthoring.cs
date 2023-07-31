using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.Bootstrap.Authoring
{
    public class MainWorldPausePrefabAuthoring : MonoBehaviour
    {
        public GameObject pauseMenuPrefab;
    }

    public class MainWorldPausePrefabAuthoringBaker : Baker<MainWorldPausePrefabAuthoring>
    {
        public override void Bake(MainWorldPausePrefabAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddSharedComponentManaged(entity, new MainWorldPauseMenuPrefab
            {
                pauseMenuPrefab = authoring.pauseMenuPrefab
            });
            AddComponent<MainWorldPauseMenu.ExistComponent>(entity);
        }
    }
}

