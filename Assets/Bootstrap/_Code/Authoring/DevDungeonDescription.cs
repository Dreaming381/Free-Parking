using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace FreeParking.Authoring
{
    public class DevDungeonDescription : ScriptableObject
    {
        [Tooltip("The name of the group creating the Dev Dungeon. Additional subgroups for organization can be added with '/' delineation.")]
        public string groupNameOrPath;

        [Tooltip("The name of the dev dungeon in code.")]
        public string devDungeonName;

        [Tooltip("The display name of the dev dungeon that the player sees.")]
        public string devDungeonDisplayName;

        [Tooltip("The name of the initial scene the Dev Dungeon should load into")]
        public string initialScene;

        [Tooltip("The creator(s) of the dev dungeon.")]
        public List<string> creators;

        [Tooltip("A teaser for the dev dungeon. It is recommended you watermark this with icons associated with the creator(s).")]
        public Texture2D thumbnail;

        [Tooltip("Short description of the dev dungeon.")]
        [TextArea]
        public string description;

        public unsafe BlobAssetReference<DevDungeonDescriptionBlob> BakeIntoBlob(ref BlobBuilder builder)
        {
            FixedString4096Bytes stringCache = default;

            ref var root = ref builder.ConstructRoot<DevDungeonDescriptionBlob>();

            if (groupNameOrPath != null)
                stringCache.Append(groupNameOrPath);
            if (!stringCache.EndsWith('/'))
                stringCache.Append("/");
            if (devDungeonName != null)
                stringCache.Append(devDungeonName);
            if (stringCache.Length > 0)
            {
                var path = builder.Allocate(ref root.nameWithPath, stringCache.Length);
                UnsafeUtility.MemCpy(path.GetUnsafePtr(), stringCache.GetUnsafePtr(), stringCache.Length);
            }
            else
            {
                builder.Allocate(ref root.nameWithPath, 0);
            }

            stringCache.Clear();
            if (devDungeonDisplayName != null)
                stringCache.Append(devDungeonDisplayName);
            if (stringCache.Length > 0)
            {
                var displayName = builder.Allocate(ref root.displayName, stringCache.Length);
                UnsafeUtility.MemCpy(displayName.GetUnsafePtr(), stringCache.GetUnsafePtr(), stringCache.Length);
            }
            else
            {
                builder.Allocate(ref root.displayName, 0);
            }

            if (creators == null || creators.Count == 0)
            {
                builder.Allocate(ref root.creators, 0);
            }
            else
            {
                var creatorArray = builder.Allocate(ref root.creators, creators.Count);
                int i            = 0;
                foreach (var creator in creators)
                {
                    stringCache.Clear();
                    if (creator != null)
                        stringCache.Append(creator);
                    var c = builder.Allocate(ref creatorArray[i], stringCache.Length);
                    UnsafeUtility.MemCpy(c.GetUnsafePtr(), stringCache.GetUnsafePtr(), stringCache.Length);
                }
            }

            stringCache.Clear();
            if (description != null)
                stringCache.Append(description);
            if (stringCache.Length > 0)
            {
                var desc = builder.Allocate(ref root.description, stringCache.Length);
                UnsafeUtility.MemCpy(desc.GetUnsafePtr(), stringCache.GetUnsafePtr(), stringCache.Length);
            }
            else
            {
                builder.Allocate(ref root.description, 0);
            }

            if (thumbnail != null)
            {
                root.thumbnailDimensions = new int2(thumbnail.width, thumbnail.height);
                var pixels               = thumbnail.GetPixels32();
                var tn                   = builder.Allocate(ref root.thumbnail, pixels.Length);
                var temp                 = new NativeArray<Color32>(pixels, Allocator.Temp);
                UnsafeUtility.MemCpy(tn.GetUnsafePtr(), temp.GetUnsafeReadOnlyPtr(), temp.Length * UnsafeUtility.SizeOf<Color32>());
            }
            else
            {
                root.thumbnailDimensions = 0;
                builder.Allocate(ref root.thumbnail, 0);
            }

            stringCache.Clear();
            if (initialScene != null)
                root.entrySceneName = initialScene;
            else
                root.entrySceneName = default;

            return builder.CreateBlobAssetReference<DevDungeonDescriptionBlob>(Allocator.Persistent);
        }
    }
}

