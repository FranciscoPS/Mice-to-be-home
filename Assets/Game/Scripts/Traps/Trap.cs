using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class Trap : MonoBehaviour
    {
        public TrapDefinition Definition { get; private set; }
        public Orientation Orientation { get; private set; }
        public IReadOnlyList<Vector2Int> Cells => cells;

        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private readonly List<Vector3> cellCenters = new List<Vector3>();
        private bool armed;

        public bool IsAvailable => armed;
        public float EffectSeconds => Definition != null ? Definition.effectSeconds : 0f;
        public float DistractionRadius => Definition != null ? Definition.distractionRadius : 0f;

        public void Initialize(TrapDefinition definition, IReadOnlyList<Vector2Int> footprint, Orientation orientation, IReadOnlyList<Vector3> worldCellCenters)
        {
            Definition = definition;
            Orientation = orientation;

            cells.Clear();
            cellCenters.Clear();
            for (int i = 0; i < footprint.Count; i++)
            {
                cells.Add(footprint[i]);
            }
            for (int i = 0; i < worldCellCenters.Count; i++)
            {
                cellCenters.Add(worldCellCenters[i]);
            }

            armed = false;
        }

        public float HorizontalDistanceTo(Vector3 point)
        {
            if (cellCenters.Count == 0)
            {
                return Horizontal(transform.position, point);
            }

            float min = float.MaxValue;
            for (int i = 0; i < cellCenters.Count; i++)
            {
                float distance = Horizontal(cellCenters[i], point);
                if (distance < min)
                {
                    min = distance;
                }
            }
            return min;
        }

        private static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public void SetArmed(bool value)
        {
            armed = value;
        }

        public float Trigger()
        {
            return EffectSeconds;
        }

        private void OnEnable() => TrapRegistry.Register(this);

        private void OnDisable() => TrapRegistry.Unregister(this);
    }
}
