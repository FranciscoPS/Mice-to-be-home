using System;
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
        private SpriteAnimator animator;
        private BalanceSettings balance;
        private bool active;
        private bool transforming;
        private float invincibleTimer;
        private float knockbackTimer;
        private Vector3 knockbackVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            visual = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<SpriteAnimator>();
            ActorPhysics.ApplyTo(GetComponent<Collider>());
        }

        /// <summary>Plays the girl -> mouse transform clip in place, then invokes <paramref name="onDone"/>.</summary>
        public void BeginIntro(Action onDone)
        {
            transforming = true;
            active = false;
            invincibleTimer = 0f;
            knockbackTimer = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
            SetAlpha(1f);

            if (animator != null && animator.Has(CharacterAnim.Transform))
            {
                animator.Play(CharacterAnim.Transform, () => EndIntro(onDone));
            }
            else
            {
                EndIntro(onDone);
            }
        }

        private void EndIntro(Action onDone)
        {
            transforming = false;
            onDone?.Invoke();
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
            return true;
        }

        private void Update()
        {
            if (transforming)
            {
                Keyboard keyboard = Keyboard.current;
                if (animator != null && keyboard != null &&
                    (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                {
                    animator.Complete();
                }
                return;
            }

            if (invincibleTimer > 0f)
            {
                invincibleTimer -= Time.deltaTime;
                Blink();
            }
            else
            {
                SetAlpha(1f);
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
                UpdateLocomotion(knockbackVelocity);
                return;
            }

            Vector3 move = ReadDirection();
            body.linearVelocity = move * balance.mouseSpeed;

            RepairTrapsUnderfoot();
            UpdateLocomotion(body.linearVelocity);
        }

        private void UpdateLocomotion(Vector3 velocity)
        {
            if (animator == null)
            {
                return;
            }
            animator.FaceHorizontal(velocity.x);
            animator.Play(velocity.sqrMagnitude > 0.04f ? CharacterAnim.Run : CharacterAnim.Idle);
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
    }
}
