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
        private bool armed;

        public bool IsAvailable => armed;
        public float EffectSeconds => Definition != null ? Definition.effectSeconds : 0f;
        public float DistractionRadius => Definition != null ? Definition.distractionRadius : 0f;

        public void Initialize(TrapDefinition definition, IReadOnlyList<Vector2Int> footprint, Orientation orientation)
        {
            Definition = definition;
            Orientation = orientation;

            cells.Clear();
            for (int i = 0; i < footprint.Count; i++)
            {
                cells.Add(footprint[i]);
            }

            armed = false;
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
