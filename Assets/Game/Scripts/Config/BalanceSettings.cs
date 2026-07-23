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

        [Tooltip("Distribucion de muebles. Una letra por casilla. 'X' = mueble (bloqueado), '.' = libre.")]
        public string[] furnitureRows =
        {
            "X...X",
            "..X..",
            ".X.X.",
            "..X..",
            "X...X"
        };

        [Header("Tiempos (segundos)")]
        [Min(1f)] public float editSeconds = 60f;
        [Min(1f)] public float surviveSeconds = 120f;

        [Header("Vidas")]
        [Min(1)] public int lives = 3;
        [Min(0f)] public float invincibleSeconds = 1.5f;

        [Header("Ratona")]
        [Min(0.1f)] public float mouseSpeed = 5f;
        [Min(0f)] public float mouseKnockback = 2.5f;

        [Header("Gato")]
        [Min(0.1f)] public float catBaseSpeed = 3f;
        [Min(0.1f)] public float catMaxSpeed = 6.5f;
        [Min(0f)] public float catAcceleration = 0.35f;
        [Min(0.1f)] public float catchRadius = 0.6f;
        [Min(0.5f)] public float catStartDistance = 4.5f;

        [Header("Rastro (persecucion)")]
        [Min(0.1f)] public float breadcrumbSpacing = 0.4f;
        [Min(0.1f)] public float breadcrumbArrive = 0.35f;

        [Header("Trampas")]
        [Min(0f)] public float trapCooldown = 4f;

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
