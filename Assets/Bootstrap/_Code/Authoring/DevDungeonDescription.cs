using System.Collections.Generic;
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

        [Tooltip("The creator(s) of the dev dungeon.")]
        public List<string> creators;

        [Tooltip("An icon for the creator or group of creators.")]
        public Texture2D creatorIcon;

        [Tooltip("A teaser for the dev dungeon.")]
        public Texture2D dungeonIcon;

        [Tooltip("Short description of the dev dungeon.")]
        [TextArea]
        public string description;
    }
}

