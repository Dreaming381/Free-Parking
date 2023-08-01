using System.Collections;
using System.Collections.Generic;
using Latios;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace FreeParking
{
    public class BasicDevDungeonPauseMenuController : MonoBehaviour, IPauseMenuDevDungeon
    {
        [SerializeField] protected TMP_Text m_displayNameText;
        [SerializeField] protected TMP_Text m_descriptionText;
        [SerializeField] protected TMP_Text m_creatorsText;
        [SerializeField] protected RawImage m_thumbnailObject;

        protected EntityManager                                 m_entityManager;
        protected LatiosWorldUnmanaged                          m_latiosWorldUnmanaged;
        protected BlobAssetReference<DevDungeonDescriptionBlob> m_devDungeonDescriptionBlob;

        protected Texture2D m_thumbnailTexture;

        public virtual unsafe void Init(EntityManager entityManager, LatiosWorldUnmanaged latiosWorld, BlobAssetReference<DevDungeonDescriptionBlob> description)
        {
            m_entityManager             = entityManager;
            m_latiosWorldUnmanaged      = latiosWorld;
            m_devDungeonDescriptionBlob = description;

            ref var blob = ref m_devDungeonDescriptionBlob.Value;

            FixedString4096Bytes stringCache = default;
            if (blob.displayName.Length > 0)
            {
                stringCache.Append((byte*)blob.displayName.GetUnsafePtr(), blob.displayName.Length);
                m_displayNameText.SetText(stringCache.ToString());
            }

            stringCache.Clear();
            if (blob.description.Length > 0)
            {
                stringCache.Append((byte*)blob.description.GetUnsafePtr(), blob.description.Length);
                m_descriptionText.SetText(stringCache.ToString());
            }

            stringCache.Clear();
            for (int i = 0; i < blob.creators.Length; i++)
            {
                stringCache.Append(' ');
                stringCache.Append('-');
                stringCache.Append(' ');
                stringCache.Append((byte*)blob.creators[i].GetUnsafePtr(), blob.creators[i].Length);
                stringCache.Append('\n');
            }
            if (blob.creators.Length > 0)
                m_creatorsText.SetText(stringCache.ToString());

            if (blob.thumbnail.Length > 0)
            {
                m_thumbnailTexture = new Texture2D(blob.thumbnailDimensions.x, blob.thumbnailDimensions.y, TextureFormat.RGBA32, false);
                var pixels         = new NativeArray<Color32>(blob.thumbnail.Length, Allocator.Temp);
                UnsafeUtility.MemCpy(pixels.GetUnsafePtr(), blob.thumbnail.GetUnsafePtr(), pixels.Length * UnsafeUtility.SizeOf<Color32>());
                m_thumbnailTexture.SetPixelData(pixels, 0);
                m_thumbnailTexture.Apply();
                var originalRect  = m_thumbnailObject.rectTransform.rect;
                var originalRatio = originalRect.width / originalRect.height;
                var newRatio      = (float)blob.thumbnailDimensions.x / blob.thumbnailDimensions.y;
                if (newRatio > originalRatio)
                {
                    // The image is too wide. Decrease the height.
                    m_thumbnailObject.rectTransform.sizeDelta *= new Vector2(1f, originalRatio / newRatio);
                }
                else if (originalRatio > newRatio)
                {
                    // The image is too tall. Decrease the width.
                    m_thumbnailObject.rectTransform.sizeDelta *= new Vector2(newRatio / originalRatio, 1f);
                }
                m_thumbnailObject.texture = m_thumbnailTexture;
            }
        }

        public virtual void SetEnabled(bool enabled)
        {
            gameObject.SetActive(enabled);
        }

        protected virtual void OnDestroy()
        {
            Destroy(m_thumbnailTexture);
        }

        public virtual void OnResumePressed()
        {
            m_latiosWorldUnmanaged.syncPoint.CreateEntityCommandBuffer().RemoveComponent<PausedTag>(m_latiosWorldUnmanaged.sceneBlackboardEntity);
            m_latiosWorldUnmanaged.syncPoint.AddMainThreadCompletionForProducer();
        }

        public virtual void OnExitDungeonPressed()
        {
            m_latiosWorldUnmanaged.syncPoint.CreateEntityCommandBuffer().AddComponent(m_latiosWorldUnmanaged.sceneBlackboardEntity, new RequestLoadScene
            {
                newScene = "Main World Scene"
            });
            m_latiosWorldUnmanaged.syncPoint.AddMainThreadCompletionForProducer();
        }
    }
}

