using System.Collections.Generic;

namespace MiceToBeHome
{
    public static class TrapRegistry
    {
        private static readonly List<Trap> traps = new List<Trap>();

        public static IReadOnlyList<Trap> Active => traps;

        public static void Register(Trap trap)
        {
            if (trap != null && !traps.Contains(trap))
            {
                traps.Add(trap);
            }
        }

        public static void Unregister(Trap trap)
        {
            traps.Remove(trap);
        }

        public static void Clear()
        {
            traps.Clear();
        }
    }
}
