using FreeParking.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.Bootstrap.Authoring
{
    [AddComponentMenu("Free Parking/Dev Dungeon Scene Data")]
    public class DevDungeonSceneAuthoring : MonoBehaviour
    {
        public DevDungeonDescription devDungeonDescription;
        public GameObject            pauseMenuPrefab;
    }

    public class DevDungeonSceneAuthoringBaker : Baker<DevDungeonSceneAuthoring>
    {
        public override void Bake(DevDungeonSceneAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            if (authoring.devDungeonDescription != null)
            {
                DependsOn(authoring.devDungeonDescription);
                var builder = new BlobBuilder(Allocator.Temp);
                var blob    = authoring.devDungeonDescription.BakeIntoBlob(ref builder);
                AddBlobAsset(ref blob, out _);
                AddComponent(entity, new CurrentDevDungeonDescription
                {
                    currentDevDungeonDescriptionBlob = blob
                });
            }
            if (authoring.pauseMenuPrefab != null)
            {
                AddSharedComponentManaged(entity, new DevDungeonPauseMenuPrefab
                {
                    pauseMenuPrefab = authoring.pauseMenuPrefab
                });
                AddComponent<DevDungeonPauseMenu.ExistComponent>(entity);
            }
        }
    }
}

