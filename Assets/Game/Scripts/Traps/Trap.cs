using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    [RequireComponent(typeof(BoxCollider))]
    public class Trap : MonoBehaviour
    {
        [SerializeField] private TrapDefinition definition = new TrapDefinition();

        public TrapDefinition Definition => definition;
        public Orientation Orientation { get; private set; }
        public Trap SourcePrefab { get; set; }
        public IReadOnlyList<Vector2Int> Cells => cells;
        public bool IsAvailable => armed;

        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private BoxCollider zone;
        private SpriteRenderer visual;
        private Vector3 baseZoneSize;
        private Vector3 baseVisualScale;
        private bool captured;
        private bool armed;

        private void Awake()
        {
            EnsureRefs();
        }

        private void EnsureRefs()
        {
            if (captured)
            {
                return;
            }

            zone = GetComponent<BoxCollider>();
            if (zone != null)
            {
                zone.isTrigger = true;
                baseZoneSize = zone.size;
            }

            visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null)
            {
                baseVisualScale = visual.transform.localScale;
            }

            captured = true;
        }

        public void Configure(IReadOnlyList<Vector2Int> footprint, Orientation orientation)
        {
            EnsureRefs();
            Orientation = orientation;

            cells.Clear();
            for (int i = 0; i < footprint.Count; i++)
            {
                cells.Add(footprint[i]);
            }

            armed = false;
        }

        public void FitToFootprint(int cellsX, int cellsZ, float cellSize)
        {
            EnsureRefs();

            float extraX = (cellsX - 1) * cellSize;
            float extraZ = (cellsZ - 1) * cellSize;

            if (zone != null)
            {
                zone.size = new Vector3(baseZoneSize.x + extraX, baseZoneSize.y, baseZoneSize.z + extraZ);
            }
            if (visual != null)
            {
                visual.transform.localScale = new Vector3(baseVisualScale.x + extraX, baseVisualScale.y + extraZ, 1f);
            }
        }

        public void SetArmed(bool value)
        {
            armed = value;
        }

        public float Trigger()
        {
            return definition != null ? definition.effectSeconds : 0f;
        }

        public bool IsInZone(Vector3 point)
        {
            EnsureRefs();
            if (zone == null)
            {
                return false;
            }

            Vector3 sample = new Vector3(point.x, zone.bounds.center.y, point.z);
            return (zone.ClosestPoint(sample) - sample).sqrMagnitude < 0.0004f;
        }

        private void OnEnable() => TrapRegistry.Register(this);

        private void OnDisable() => TrapRegistry.Unregister(this);

#if UNITY_EDITOR
        public void EditorAssign(TrapDefinition value)
        {
            definition = value;
        }
#endif
    }
}
