using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiceToBeHome
{
    [RequireComponent(typeof(BoxCollider))]
    public class Trap : MonoBehaviour
    {
        private const float GhostAlpha = 0.3f;

        [SerializeField] private TrapDefinition definition = new TrapDefinition();

        public TrapDefinition Definition => definition;
        public Trap SourcePrefab { get; set; }
        public IReadOnlyList<Vector2Int> Cells => cells;
        public bool IsAvailable => armed && !spent;
        public bool NeedsRepair => spent;

        private readonly List<Vector2Int> cells = new List<Vector2Int>();
        private BoxCollider zone;
        private SpriteRenderer visual;
        private TextMeshPro countdown;
        private Transform cameraTransform;
        private Color baseVisualColor = Color.white;
        private bool captured;
        private bool armed;
        private bool spent;
        private float repairProgress;

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
            }

            visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null)
            {
                baseVisualColor = visual.color;
            }

            countdown = GetComponentInChildren<TextMeshPro>(true);
            if (countdown != null)
            {
                countdown.gameObject.SetActive(false);
            }

            captured = true;
        }

        public void Configure(IReadOnlyList<Vector2Int> footprint)
        {
            EnsureRefs();

            cells.Clear();
            for (int i = 0; i < footprint.Count; i++)
            {
                cells.Add(footprint[i]);
            }

            armed = false;
            spent = false;
            repairProgress = 0f;
            SetGhost(false);
        }

        public void SetArmed(bool value)
        {
            armed = value;
        }

        public float Trigger()
        {
            if (definition == null)
            {
                return 0f;
            }

            spent = true;
            repairProgress = 0f;
            SetGhost(true);
            RefreshCountdown();
            return definition.effectSeconds;
        }

        public void Repair(float deltaTime)
        {
            if (!spent || definition == null)
            {
                return;
            }

            repairProgress += deltaTime;
            if (repairProgress >= definition.effectSeconds)
            {
                spent = false;
                repairProgress = 0f;
                SetGhost(false);
            }
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

        private void Update()
        {
            if (spent)
            {
                RefreshCountdown();
            }
        }

        private void RefreshCountdown()
        {
            if (countdown == null)
            {
                return;
            }

            float remaining = definition != null ? definition.effectSeconds - repairProgress : 0f;
            countdown.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                countdown.transform.rotation = cameraTransform.rotation;
            }
        }

        private void SetGhost(bool ghost)
        {
            if (visual != null)
            {
                Color c = baseVisualColor;
                c.a = ghost ? GhostAlpha : baseVisualColor.a;
                visual.color = c;
            }
            if (countdown != null)
            {
                countdown.gameObject.SetActive(ghost);
            }
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
