#if UNITY_EDITOR
using System;
using UnityEngine;

namespace MiceToBeHome.EditorTools
{
    /// <summary>One placeable obstacle type. Editor-only data that drives the layout window and
    /// the SceneBuilder furniture prefabs. Width is in grid cells (always horizontal, no rotation).</summary>
    public struct FurnitureType
    {
        public char Code;
        public string Name;
        public int Width;
        public Func<SpriteLibrary, Sprite> Sprite;
        public Color Tint;
    }

    /// <summary>
    /// The catalog of furniture/obstacle types the layout can place. 4 single-cell + 3 double-cell
    /// (1x2 horizontal). The layout string[] stores one char per cell: '.' = empty, a type Code =
    /// that obstacle's anchor (leftmost cell), '>' = the continuation cell of a 1x2 to its left.
    /// Legacy 'X'/'x' maps to the first type (Sillon).
    /// </summary>
    public static class FurnitureCatalog
    {
        public const char Empty = '.';
        public const char Continuation = '>';
        public const char Spawn = 'S';

        public static readonly FurnitureType[] Types =
        {
            new FurnitureType { Code = 'C', Name = "Sillon", Width = 1, Sprite = s => s.sillon, Tint = new Color(0.55f, 0.40f, 0.30f) },
            new FurnitureType { Code = 'M', Name = "Maceta", Width = 1, Sprite = s => s.maceta, Tint = new Color(0.35f, 0.60f, 0.35f) },
            new FurnitureType { Code = 'P', Name = "Puff",   Width = 1, Sprite = s => s.puff,   Tint = new Color(0.55f, 0.40f, 0.70f) },
            new FurnitureType { Code = 'T', Name = "Mesita", Width = 1, Sprite = s => s.mesita, Tint = new Color(0.72f, 0.60f, 0.38f) },
            new FurnitureType { Code = 'A', Name = "Cama",   Width = 2, Sprite = s => s.cama,   Tint = new Color(0.40f, 0.52f, 0.78f) },
            new FurnitureType { Code = 'V', Name = "Tele",   Width = 2, Sprite = s => s.tele,   Tint = new Color(0.32f, 0.32f, 0.36f) },
            new FurnitureType { Code = 'K', Name = "Cocina", Width = 2, Sprite = s => s.cocina, Tint = new Color(0.78f, 0.42f, 0.35f) },
        };

        /// <summary>Legacy 'X'/'x' becomes the first single type so old layouts keep working.</summary>
        public static char Normalize(char code)
        {
            return code == 'X' || code == 'x' ? Types[0].Code : code;
        }

        public static bool TryGet(char code, out FurnitureType type)
        {
            char c = Normalize(code);
            for (int i = 0; i < Types.Length; i++)
            {
                if (Types[i].Code == c)
                {
                    type = Types[i];
                    return true;
                }
            }
            type = default;
            return false;
        }

        public static bool IsDouble(char code)
        {
            return TryGet(code, out FurnitureType type) && type.Width >= 2;
        }
    }
}
#endif
