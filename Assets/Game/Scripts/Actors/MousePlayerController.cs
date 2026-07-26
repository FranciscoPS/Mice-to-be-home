using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiceToBeHome
{
    [RequireComponent(typeof(Rigidbody))]
    public class MousePlayerController : MonoBehaviour
    {
        public event Action Hit;

        public bool IsInvincible => invincibleTimer > 0f;
        public Vector3 Position => body != null ? body.position : transform.position;

        private Rigidbody body;
        private SpriteRenderer visual;
        private Animator animator;
        private BalanceSettings balance;
        private bool active;
        private float invincibleTimer;
        private float knockbackTimer;
        private Vector3 knockbackVelocity;
        private float lastDirectionX = 1f;
        private bool hitReactionActive;
        private float footstepTimer;

        [SerializeField] private GameObject hitAnimationPrefab;
        private GameObject hitAnimationInstance;
        private Animator hitAnimator;
        private float lastHitTick = -10f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            visual = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            ActorPhysics.ApplyTo(GetComponent<Collider>());
        }

        public void Initialize(BalanceSettings settings)
        {
            balance = settings;
        }

        public void SetActive(bool value)
        {
            active = value;
            invincibleTimer = 0f;
            knockbackTimer = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
            SetAlpha(1f);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMouseRunning(false);
            }
        }

        public void Teleport(Vector3 position)
        {
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector3.zero;
            }
            transform.position = position;
        }

        public bool TakeHit(Vector3 attackerPosition)
        {
            if (invincibleTimer > 0f)
            {
                return false;
            }

            invincibleTimer = balance.invincibleSeconds;

            Vector3 away = transform.position - attackerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f)
            {
                away = Vector3.forward;
            }

            knockbackVelocity = away.normalized * balance.mouseKnockback;
            knockbackTimer = 0.25f;
            Hit?.Invoke();
            TriggerHitReaction();

            // Play hit VFX/animation like traps' repair animation
            lastHitTick = Time.time;
            EnsureHitAnimationInstance();

            return true;
        }

        private void TriggerHitReaction()
        {
            if (!active || !isActiveAndEnabled || balance == null)
            {
                return;
            }

            // No hit-stop outside active play (e.g. the killing blow freezes the player and
            // plays its own dramatic defeat slow-mo instead).
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            if (balance.hitSlowDuration <= 0f)
            {
                return;
            }

            StartCoroutine(HitReaction());
        }

        private IEnumerator HitReaction()
        {
            hitReactionActive = true;

            // Freeze the current run frame and slow the world for hit game-feel (never idle).
            if (animator != null)
            {
                animator.SetBool("IsMoving", true);
                animator.speed = 0f;
            }

            Time.timeScale = Mathf.Clamp01(balance.hitSlowScale);
            yield return new WaitForSecondsRealtime(balance.hitSlowDuration);

            if (animator != null)
            {
                animator.speed = 1f;
            }
            if (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing)
            {
                Time.timeScale = 1f;
            }

            hitReactionActive = false;
        }

        private void OnDisable()
        {
            // Safety: never leave the game frozen/slowed if disabled mid-reaction.
            if (animator != null)
            {
                animator.speed = 1f;
            }
            if (hitReactionActive)
            {
                hitReactionActive = false;
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            if (invincibleTimer > 0f)
            {
                invincibleTimer -= Time.deltaTime;
                Blink();
            }
            else
            {
                SetAlpha(1f);
            }

            // If hit animation exists but no recent hit tick, remove it (player moved away / hit finished)
            if (hitAnimationInstance != null && Time.time - lastHitTick > 0.5f)
            {
                DestroyHitAnimationImmediate();
            }
        }

        private void FixedUpdate()
        {
            if (!active || body == null)
            {
                return;
            }

            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                body.linearVelocity = knockbackVelocity;
                // Mantener la animación de correr durante el golpe (no idle).
                if (animator != null)
                {
                    animator.SetBool("IsMoving", true);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMouseRunning(false);
                }
                return;
            }

            Vector3 move = ReadDirection();
            body.linearVelocity = move * balance.mouseSpeed;

            // Actualizar animación según si hay movimiento
            if (animator != null)
            {
                animator.SetBool("IsMoving", move.sqrMagnitude > 0f);
            }

            // Flipear sprite según dirección horizontal
            if (move.x != 0f)
            {
                lastDirectionX = move.x;
                if (visual != null)
                {
                    visual.flipX = lastDirectionX < 0f;
                }
            }

            RepairTrapsUnderfoot();
            UpdateFootsteps(move.sqrMagnitude > 0f);
        }

        private void UpdateFootsteps(bool moving)
        {
            AudioManager audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            audio.SetMouseRunning(moving);
            if (moving)
            {
                footstepTimer -= Time.fixedDeltaTime;
                if (footstepTimer <= 0f)
                {
                    audio.PlayMouseFootstep();
                    footstepTimer = audio.FootstepInterval;
                }
            }
            else
            {
                footstepTimer = 0f;
            }
        }

        private void RepairTrapsUnderfoot()
        {
            var traps = TrapRegistry.Active;
            for (int i = 0; i < traps.Count; i++)
            {
                Trap trap = traps[i];
                if (trap != null && trap.NeedsRepair && trap.IsInZone(body.position))
                {
                    trap.Repair(Time.fixedDeltaTime);
                }
            }
        }

        private Vector3 ReadDirection()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector3.zero;
            }

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

            Vector3 direction = new Vector3(x, 0f, z);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void Blink()
        {
            float alpha = Mathf.PingPong(Time.unscaledTime * 8f, 1f) > 0.5f ? 1f : 0.3f;
            SetAlpha(alpha);
        }

        private void SetAlpha(float alpha)
        {
            if (visual == null)
            {
                return;
            }

            Color color = visual.color;
            color.a = alpha;
            visual.color = color;
        }

        // --- Hit animation helpers (mimic trap repair animation behavior) ---------

        private void EnsureHitAnimationInstance()
        {
            if (hitAnimationPrefab == null || hitAnimationInstance != null)
            {
                return;
            }

            hitAnimationInstance = Instantiate(hitAnimationPrefab, transform);
            // Place the VFX above the player similar to trap repair placement.
            hitAnimationInstance.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            // Rotate to face camera if it's a world-space canvas / billboard.
            if (Camera.main != null)
            {
                hitAnimationInstance.transform.rotation = Camera.main.transform.rotation;
            }

            hitAnimator = hitAnimationInstance.GetComponent<Animator>();
            hitAnimationInstance.SetActive(true);

            // If animator has clips, play the first immediately and destroy after its length.
            if (hitAnimator != null && hitAnimator.runtimeAnimatorController != null)
            {
                var clips = hitAnimator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0)
                {
                    string clipName = clips[0].name;
                    float clipLen = clips[0].length;
                    hitAnimator.Play(clipName, -1, 0f);
                    StartCoroutine(PlayThenDestroy(hitAnimationInstance, clipLen));
                    return;
                }
            }

            // Fallback: destroy after a short default time.
            StartCoroutine(PlayThenDestroy(hitAnimationInstance, 0.6f));
        }

        private void DestroyHitAnimationImmediate()
        {
            if (hitAnimationInstance == null)
            {
                return;
            }

            Destroy(hitAnimationInstance);
            hitAnimationInstance = null;
            hitAnimator = null;
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

            if (animObj == hitAnimationInstance)
            {
                hitAnimationInstance = null;
                hitAnimator = null;
            }
        }
    }
}
