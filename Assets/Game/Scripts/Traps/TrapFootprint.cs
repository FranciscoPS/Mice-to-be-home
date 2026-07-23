using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public static class TrapFootprint
    {
        public static void Compute(Vector2Int origin, int size, Orientation orientation, List<Vector2Int> results)
        {
            results.Clear();
            results.Add(origin);

            if (size >= 2)
            {
                Vector2Int offset = orientation == Orientation.Horizontal
                    ? new Vector2Int(1, 0)
                    : new Vector2Int(0, 1);
                results.Add(origin + offset);
            }
        }

        public static Orientation Toggle(Orientation orientation)
        {
            return orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        }
    }
}
