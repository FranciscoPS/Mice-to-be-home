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
        private Vector3 visualBaseLocalPos;
        private Vector3 visualBaseLocalScale = Vector3.one;

        [SerializeField] private GameObject hitAnimationPrefab = null;
        private GameObject hitAnimationInstance;
        private Animator hitAnimator;
        private float lastHitTick = -10f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            visual = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
            if (visual != null)
            {
                visualBaseLocalPos = visual.transform.localPosition;
                visualBaseLocalScale = visual.transform.localScale;
            }
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
            if (value)
            {
                StopAllCoroutines();
                RestoreVisual();
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

        // Paper Mario style death: the flat sprite spins on its own vertical axis while tipping
        // forward until it lies flat on the floor ("bread slice" flop). Runs on unscaled time so it
        // stays crisp through the defeat slow-mo.
        public void PlayDeath()
        {
            active = false;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMouseRunning(false);
            }
            if (visual == null)
            {
                return;
            }
            if (animator != null)
            {
                animator.enabled = false;
            }
            Billboard billboard = visual.GetComponent<Billboard>();
            if (billboard != null)
            {
                billboard.enabled = false;
            }
            StopAllCoroutines();
            StartCoroutine(DeathFlop(visual.transform));
        }

        // Paper Mario style death, cranked way up: the flat sprite is launched into the air,
        // whirls several times on its own axis while stretching tall, then slams down and
        // squashes into a flat "pancake" with an overshoot past flat. Runs on unscaled time so
        // it stays crisp through the defeat slow-mo.
        private IEnumerator DeathFlop(Transform vis)
        {
            Vector3 basePos = visualBaseLocalPos;
            Vector3 baseScale = visualBaseLocalScale;
            Quaternion startRot = vis.rotation;

            const float duration = 1.05f;
            const float spinTurns = 5f;
            const float hopHeight = 1.6f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);

                // Whirl fast at first, then ease out as it settles.
                float spinEase = 1f - (1f - k) * (1f - k);
                float spin = spinTurns * 360f * spinEase;

                // Tip past flat then settle back (back ease-out overshoot).
                float b = k - 1f;
                const float s = 2.4f;
                float overshoot = b * b * ((s + 1f) * b + s) + 1f;
                float fall = 90f * overshoot;

                // Launch up and slam down within the first 80% of the flop.
                float flight = Mathf.Clamp01(k / 0.8f);
                float airborne = Mathf.Sin(flight * Mathf.PI);
                float hop = airborne * hopHeight;

                // Stretch tall while airborne, squash flat on landing.
                float landed = Mathf.Clamp01((k - 0.8f) / 0.2f);
                float sx = baseScale.x * (1f - 0.30f * airborne + 0.25f * landed);
                float sy = baseScale.y * (1f + 0.55f * airborne - 0.35f * landed);

                vis.localPosition = basePos + new Vector3(0f, hop, 0f);
                vis.localScale = new Vector3(sx, sy, baseScale.z);
                vis.rotation = startRot * Quaternion.Euler(fall, spin, 0f);
                yield return null;
            }

            // Rest flat on the floor as a squashed pancake.
            vis.localPosition = basePos;
            vis.localScale = new Vector3(baseScale.x * 1.25f, baseScale.y * 0.65f, baseScale.z);
            vis.rotation = startRot * Quaternion.Euler(90f, spinTurns * 360f, 0f);
        }

        private void RestoreVisual()
        {
            if (visual != null)
            {
                Billboard billboard = visual.GetComponent<Billboard>();
                if (billboard != null)
                {
                    billboard.enabled = true;
                }
                visual.transform.localPosition = visualBaseLocalPos;
                visual.transform.localScale = visualBaseLocalScale;
                visual.transform.localRotation = Quaternion.identity;
            }
            if (animator != null)
            {
                animator.enabled = true;
                animator.speed = 1f;
            }
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

            // Play hit VFX/animation like traps' repair animation (usar tiempo no escalado)
            lastHitTick = Time.unscaledTime;
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
            if (hitAnimationInstance != null && Time.unscaledTime - lastHitTick > 0.5f)
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

            // Asegurar que el Animator avance en tiempo no escalado para que se reproduzca durante slow-mo.
            if (hitAnimator != null)
            {
                hitAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            // If animator has clips, play the first immediately and destroy after its real length.
            if (hitAnimator != null && hitAnimator.runtimeAnimatorController != null)
            {
                var clips = hitAnimator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0)
                {
                    string clipName = clips[0].name;
                    float clipLen = clips[0].length;
                    hitAnimator.Play(clipName, -1, 0f);
                    StartCoroutine(PlayThenDestroyRealtime(hitAnimationInstance, clipLen));
                    return;
                }
            }

            // Fallback: destroy after a short default time in real seconds.
            StartCoroutine(PlayThenDestroyRealtime(hitAnimationInstance, 0.6f));
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

        private IEnumerator PlayThenDestroyRealtime(GameObject animObj, float delayRealSeconds)
        {
            if (delayRealSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delayRealSeconds);
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
