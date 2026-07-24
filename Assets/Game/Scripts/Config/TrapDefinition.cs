using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class TrapDefinition
    {
        [Tooltip("Name shown in the inventory.")]
        public string displayName = "Trap";

        [TextArea]
        [Tooltip("Description shown in the tooltip on hover.")]
        public string description = "";

        [Min(0f)]
        [Tooltip("Seconds the cat stays stunned by this object.")]
        public float effectSeconds = 3f;

        [Range(1, 2)]
        [Tooltip("How many cells it takes up (1 or 2).")]
        public int gridSize = 1;

        [Tooltip("Placeholder color used when the visual has no sprite.")]
        public Color tint = Color.white;
    }
}
