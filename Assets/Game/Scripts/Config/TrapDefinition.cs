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
        [Tooltip("Seconds the cat stays stunned. Also the time the mouse must stand on the trap to repair it.")]
        public float effectSeconds = 3f;

        [Min(0)]
        [Tooltip("How many copies of this trap the player can place (per type).")]
        public int stock = 1;

        [Tooltip("Placeholder color used when the visual has no sprite.")]
        public Color tint = Color.white;
    }
}
