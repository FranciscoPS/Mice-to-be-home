using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class BalanceSettings
    {
        [Header("Grid")]
        [Min(1)] public int gridColumns = 5;
        [Min(1)] public int gridRows = 5;
        [Min(0.5f)] public float cellSize = 2f;

        [Tooltip("Furniture layout. One letter per cell. 'X' = furniture (blocked), '.' = free.")]
        public string[] furnitureRows =
        {
            "X...X",
            "..X..",
            ".....",
            "..X..",
            "X...X"
        };

        [Header("Timers (seconds)")]
        [Min(1f)] public float editSeconds = 60f;
        [Min(1f)] public float surviveSeconds = 120f;
        [Tooltip("Lead-in before the chase goes live: the chase intro music plays for this long with the mouse and cat frozen, then gameplay starts.")]
        [Min(0f)] public float chaseLeadInSeconds = 2f;

        [Header("Lives")]
        [Min(1)] public int lives = 3;
        [Min(0f)] public float invincibleSeconds = 1.5f;

        [Header("Mouse (player)")]
        [Min(0.1f)] public float mouseSpeed = 5f;
        [Min(0f)] public float mouseKnockback = 2.5f;

        [Header("Cat")]
        [Min(0.1f)] public float catBaseSpeed = 3f;
        [Min(0.1f)] public float catMaxSpeed = 6.5f;
        [Min(0f)] public float catAcceleration = 0.35f;
        [Min(0.1f)] public float catchRadius = 0.6f;
        [Min(0.5f)] public float catStartDistance = 4.5f;
        [Tooltip("Speed multiplier (above max) the cat uses to decisively break out / press through a gap when cornered against a player camping on furniture.")]
        [Min(1f)] public float catBreakoutBoost = 1.5f;
        [Header("Cat leap (anti-camp)")]
        [Tooltip("Seconds the mouse must camp on/behind furniture before the cat leaps over it.")]
        [Min(0.1f)] public float catJumpAfterSeconds = 2f;
        [Tooltip("Seconds the leap takes. Higher = slower = easier for the mouse to dodge.")]
        [Min(0.15f)] public float catJumpDuration = 0.6f;
        [Tooltip("Visual arc height of the leap (units). Set it above the furniture height so the cat looks like it clears the piece.")]
        [Min(0f)] public float catJumpHeight = 1.6f;

        [Header("Hit feedback")]
        [Tooltip("Time scale during a hit (1 = normal, lower = slower / more frozen).")]
        [Range(0.01f, 1f)] public float hitSlowScale = 0.06f;
        [Tooltip("Real-time seconds the slow-motion lasts on each hit. 0 disables it.")]
        [Min(0f)] public float hitSlowDuration = 0.25f;
        [Tooltip("Camera shake strength when the cat hits you (Perlin noise gain). 0 disables it.")]
        [Min(0f)] public float hitShakeAmplitude = 1.5f;
        [Tooltip("Real-time seconds the hit camera shake lasts.")]
        [Min(0f)] public float hitShakeDuration = 0.3f;

        [Header("Defeat feedback")]
        [Tooltip("How slow time gets at the climax of a loss (1 = normal, lower = closer to frozen).")]
        [Range(0.01f, 1f)] public float loseSlowMinScale = 0.05f;
        [Tooltip("Real-time seconds spent easing time down to the minimum on a loss.")]
        [Min(0.1f)] public float loseSlowDuration = 1.4f;
        [Tooltip("Real-time seconds to hold at the slowest point before the defeat screen.")]
        [Min(0f)] public float loseHoldSeconds = 0.35f;

        [Header("Traps")]
        [Min(1)] public int maxTraps = 5;

        public string GetFurnitureRow(int index)
        {
            if (furnitureRows == null || index < 0 || index >= furnitureRows.Length)
            {
                return string.Empty;
            }
            return furnitureRows[index];
        }
    }
}
