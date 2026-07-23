using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class SpriteLibrary
    {
        [Header("Ratona (jugadora)")]
        public Sprite mouse;
        public Color mouseTint = new Color(0.75f, 0.6f, 0.85f);

        [Header("Gato (Miauricio)")]
        public Sprite cat;
        public Color catTint = new Color(0.95f, 0.55f, 0.3f);

        [Header("Muebles / obstaculos")]
        public Sprite furniture;
        public Color furnitureTint = new Color(0.4f, 0.45f, 0.55f);

        [Header("Piso de la habitacion")]
        public Sprite floor;
        public Color floorTint = new Color(0.86f, 0.82f, 0.74f);

        [Header("Casilla del grid (guia de edicion)")]
        public Sprite cell;
        public Color freeCellTint = new Color(1f, 1f, 1f, 0.15f);
        public Color blockedCellTint = new Color(0f, 0f, 0f, 0.2f);
    }
}
