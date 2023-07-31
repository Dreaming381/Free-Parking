using FreeParking.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.Bootstrap.Authoring
{
    public class DevDungeonSceneAuthoring : MonoBehaviour
    {
        public DevDungeonDescription devDungeonDescription;
        public GameObject            pauseMenuPrefab;
    }

    public class DevDungeonSceneAuthoringBaker : Baker<DevDungeonSceneAuthoring>
    {
        public override void Bake(DevDungeonSceneAuthoring authoring)
        {
            var entity  = GetEntity(TransformUsageFlags.None);
            var builder = new BlobBuilder(Allocator.Temp);
            var blob    = authoring.devDungeonDescription.BakeIntoBlob(ref builder);
            AddBlobAsset(ref blob, out _);
            AddComponent(entity, new CurrentDevDungeonDescription
            {
                currentDevDungeonDescriptionBlob = blob
            });
            AddSharedComponentManaged(entity, new DevDungeonPauseMenuPrefab
            {
                pauseMenuPrefab = authoring.pauseMenuPrefab
            });
            AddComponent<DevDungeonPauseMenu.ExistComponent>(entity);
        }
    }
}

