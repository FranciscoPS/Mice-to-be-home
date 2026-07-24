using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public static class GridPathfinder
    {
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        private static readonly List<Vector2Int> Open = new List<Vector2Int>();
        private static readonly HashSet<Vector2Int> Closed = new HashSet<Vector2Int>();
        private static readonly Dictionary<Vector2Int, Vector2Int> CameFrom = new Dictionary<Vector2Int, Vector2Int>();
        private static readonly Dictionary<Vector2Int, float> GScore = new Dictionary<Vector2Int, float>();
        private static readonly Dictionary<Vector2Int, float> FScore = new Dictionary<Vector2Int, float>();

        public static bool TryFindPath(GridSystem grid, Vector2Int start, Vector2Int goal, List<Vector2Int> result)
        {
            result.Clear();

            if (grid == null || !grid.IsWalkable(start) || !grid.IsWalkable(goal))
            {
                return false;
            }

            if (start == goal)
            {
                result.Add(start);
                return true;
            }

            Open.Clear();
            Closed.Clear();
            CameFrom.Clear();
            GScore.Clear();
            FScore.Clear();

            Open.Add(start);
            GScore[start] = 0f;
            FScore[start] = Heuristic(start, goal);

            while (Open.Count > 0)
            {
                Vector2Int current = PopLowest();

                if (current == goal)
                {
                    Reconstruct(current, result);
                    return true;
                }

                Closed.Add(current);

                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int dir = Directions[i];
                    Vector2Int next = current + dir;

                    if (!grid.IsWalkable(next) || Closed.Contains(next))
                    {
                        continue;
                    }

                    bool diagonal = dir.x != 0 && dir.y != 0;
                    if (diagonal &&
                        (!grid.IsWalkable(new Vector2Int(current.x + dir.x, current.y)) ||
                         !grid.IsWalkable(new Vector2Int(current.x, current.y + dir.y))))
                    {
                        continue;
                    }

                    float tentative = GScore[current] + (diagonal ? 1.41421356f : 1f);
                    if (!GScore.TryGetValue(next, out float known) || tentative < known)
                    {
                        CameFrom[next] = current;
                        GScore[next] = tentative;
                        FScore[next] = tentative + Heuristic(next, goal);
                        if (!Open.Contains(next))
                        {
                            Open.Add(next);
                        }
                    }
                }
            }

            return false;
        }

        private static Vector2Int PopLowest()
        {
            int bestIndex = 0;
            float bestScore = FScore.TryGetValue(Open[0], out float f0) ? f0 : float.MaxValue;

            for (int i = 1; i < Open.Count; i++)
            {
                float score = FScore.TryGetValue(Open[i], out float f) ? f : float.MaxValue;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            Vector2Int best = Open[bestIndex];
            Open.RemoveAt(bestIndex);
            return best;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static void Reconstruct(Vector2Int current, List<Vector2Int> result)
        {
            result.Add(current);
            while (CameFrom.TryGetValue(current, out Vector2Int previous))
            {
                current = previous;
                result.Add(current);
            }
            result.Reverse();
        }
    }
}
