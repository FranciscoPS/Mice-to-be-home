using System.Collections;
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
        [SerializeField] private GameObject repairAnimationPrefab;

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
        private SpriteJuice juice;
        private bool captured;
        private bool armed;
        private bool spent;
        private float repairProgress;

        // New animation instance state
        private GameObject repairAnimationInstance;
        private Animator repairAnimator;
        private float lastRepairTick = -10f;

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
                juice = visual.GetComponent<SpriteJuice>();
                if (juice == null)
                {
                    juice = visual.gameObject.AddComponent<SpriteJuice>();
                }
                juice.EnableIdleHop(true);
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

            if (juice != null)
            {
                juice.Bump(1.2f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTrap(definition.triggerSound);
            }
            return definition.effectSeconds;
        }

        public void Repair(float deltaTime)
        {
            if (!spent || definition == null)
            {
                return;
            }

            // Mark last tick so we know player is actively repairing.
            lastRepairTick = Time.time;
            EnsureAnimationInstance();

            repairProgress += deltaTime;
            if (repairProgress >= definition.effectSeconds)
            {
                // Play completion animation (if animator present) and let it finish, then remove.
                if (repairAnimationInstance != null)
                {
                    if (repairAnimator != null && repairAnimator.runtimeAnimatorController != null)
                    {
                        var clips = repairAnimator.runtimeAnimatorController.animationClips;
                        if (clips != null && clips.Length > 0)
                        {
                            // Play first clip from start and destroy after its length.
                            string clipName = clips[0].name;
                            float clipLen = clips[0].length;
                            repairAnimator.Play(clipName, -1, 0f);
                            StartCoroutine(PlayThenDestroy(repairAnimationInstance, clipLen));
                            // Don't null the instance here; coroutine will cleanup.
                        }
                        else
                        {
                            DestroyAnimationInstanceImmediate();
                        }
                    }
                    else
                    {
                        DestroyAnimationInstanceImmediate();
                    }
                }

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

            // If we have an animation but no recent repair tick, player left — remove the animation.
            if (repairAnimationInstance != null && Time.time - lastRepairTick > 0.25f)
            {
                DestroyAnimationInstanceImmediate();
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
                if (repairAnimationInstance != null)
                {
                    repairAnimationInstance.transform.rotation = cameraTransform.rotation;
                }
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
            if (juice != null)
            {
                juice.EnableIdleHop(!ghost);
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

        // --- Animation helper methods ------------------------------------------------

        private void EnsureAnimationInstance()
        {
            Debug.Log("Intentando crear animación");

            if (repairAnimationPrefab == null)
            {
                Debug.Log("Prefab NULL");
                return;
            }

            if (repairAnimationInstance != null)
            {
                Debug.Log("Ya existe");
                return;
            }

            repairAnimationInstance =
                Instantiate(repairAnimationPrefab, transform);
            Debug.Log("Instanciado: " + repairAnimationInstance.name);

            Debug.Log("Animación creada");
        }

        private void DestroyAnimationInstanceImmediate()
        {
            if (repairAnimationInstance == null)
            {
                return;
            }

            Destroy(repairAnimationInstance);
            repairAnimationInstance = null;
            repairAnimator = null;
        }

        private IEnumerator PlayThenDestroy(GameObject animObj, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (animObj != null)
            {
                Destroy(animObj);
            }

            if (animObj == repairAnimationInstance)
            {
                repairAnimationInstance = null;
                repairAnimator = null;
            }
        }
    }
}
