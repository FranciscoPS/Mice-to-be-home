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

        [Min(0f)]
        [Tooltip("Seconds the trap is on cooldown (ghosted) after being triggered.")]
        public float cooldownSeconds = 6f;

        [Tooltip("Placeholder color used when the visual has no sprite.")]
        public Color tint = Color.white;
    }
}
