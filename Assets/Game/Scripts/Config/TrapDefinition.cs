using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class TrapDefinition
    {
        [Tooltip("Nombre que se muestra en el inventario.")]
        public string displayName = "Trampa";

        [TextArea]
        [Tooltip("Descripcion que aparece en el tooltip al pasar el mouse.")]
        public string description = "";

        [Min(0f)]
        [Tooltip("Segundos que el gato se queda distraido con este objeto.")]
        public float effectSeconds = 3f;

        [Range(1, 2)]
        [Tooltip("Cuantas casillas ocupa (1 o 2).")]
        public int gridSize = 1;

        [Min(0.1f)]
        [Tooltip("Distancia a la que el gato detecta y se distrae con la trampa.")]
        public float distractionRadius = 1.6f;

        [Tooltip("Sprite del objeto. Si se deja vacio se usa un cuadro de color.")]
        public Sprite sprite;

        [Tooltip("Color del placeholder cuando no hay sprite asignado.")]
        public Color tint = Color.white;
    }
}
