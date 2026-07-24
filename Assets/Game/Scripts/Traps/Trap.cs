using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        private GameObject repairRoot;
        private Image repairFill;
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

            Transform repair = transform.Find("Repair");
            if (repair != null)
            {
                repairRoot = repair.gameObject;
                Transform fill = repair.Find("Fill");
                if (fill != null)
                {
                    repairFill = fill.GetComponent<Image>();
                }
                repairRoot.SetActive(false);
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
            UpdateRepairVisual();
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
                return;
            }

            UpdateRepairVisual();
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
                UpdateRepairVisual();
            }
        }

        private void UpdateRepairVisual()
        {
            if (repairFill != null && definition != null && definition.effectSeconds > 0f)
            {
                repairFill.fillAmount = Mathf.Clamp01(repairProgress / definition.effectSeconds);
            }

            if (repairRoot == null)
            {
                return;
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                repairRoot.transform.rotation = cameraTransform.rotation;
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
            if (repairRoot != null)
            {
                repairRoot.SetActive(ghost);
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
