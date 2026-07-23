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

        [Min(0.1f)]
        [Tooltip("Distance at which the cat detects and gets stunned by the trap.")]
        public float distractionRadius = 1.6f;

        [Tooltip("Object sprite. If left empty, a colored square is used.")]
        public Sprite sprite;

        [Tooltip("Placeholder color used when no sprite is assigned.")]
        public Color tint = Color.white;
    }
}
