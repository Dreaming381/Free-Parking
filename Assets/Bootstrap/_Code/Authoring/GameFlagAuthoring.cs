using Unity.Entities;
using UnityEngine;

namespace FreeParking.Authoring
{
    [CreateAssetMenu(menuName = "Free Parking/Game Flag", fileName = "NewGameFlag")]
    public class GameFlagAuthoring : ScriptableObject
    {
        [SerializeField]
        Unity.Entities.Hash128 m_guid;

        private void OnValidate()
        {
            if (m_guid == default)
            {
#if UNITY_EDITOR
                m_guid = UnityEditor.GUID.Generate();
#endif
            }
        }

        public static implicit operator GameFlagHandle(GameFlagAuthoring authoring) => new GameFlagHandle
        {
            guid = authoring.m_guid
        };
    }
}

