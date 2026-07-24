using UnityEngine;

namespace MiceToBeHome
{
    [RequireComponent(typeof(Rigidbody))]
    public class CatController : MonoBehaviour
    {
        private enum CatState
        {
            Idle,
            Chasing,
            Stunned,
            Recovering
        }

        private Rigidbody body;
        private MousePlayerController player;
        private BalanceSettings balance;
        private AudioManager audioManager;

        private CatState state = CatState.Idle;
        private float currentSpeed;
        private float stateTimer;
        private bool active;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ActorPhysics.ApplyTo(GetComponent<Collider>());
        }

        public void Initialize(MousePlayerController target, BalanceSettings settings, AudioManager audio)
        {
            player = target;
            balance = settings;
            audioManager = audio;
        }

        public void SetActive(bool value)
        {
            active = value;
            if (!value && body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
        }

        public void ResetForChase(Vector3 startPosition)
        {
            if (body != null)
            {
                body.position = startPosition;
                body.linearVelocity = Vector3.zero;
            }
            transform.position = startPosition;
            currentSpeed = balance.catBaseSpeed;
            state = CatState.Chasing;
            stateTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (!active || body == null || player == null)
            {
                return;
            }

            switch (state)
            {
                case CatState.Stunned:
                    body.linearVelocity = Vector3.zero;
                    stateTimer -= Time.fixedDeltaTime;
                    if (stateTimer <= 0f)
                    {
                        Debug.Log("[Cat] No longer stunned - resuming the chase!");
                        state = CatState.Chasing;
                    }
                    break;
                case CatState.Recovering:
                    body.linearVelocity = Vector3.zero;
                    stateTimer -= Time.fixedDeltaTime;
                    if (stateTimer <= 0f)
                    {
                        state = CatState.Chasing;
                    }
                    break;
                case CatState.Chasing:
                    Chase();
                    break;
            }
        }

        private void Chase()
        {
            if (TryStun())
            {
                return;
            }

            currentSpeed = Mathf.Min(balance.catMaxSpeed, currentSpeed + balance.catAcceleration * Time.fixedDeltaTime);

            Vector3 direction = player.Position - body.position;
            direction.y = 0f;

            body.linearVelocity = direction.sqrMagnitude > 0.0004f ? direction.normalized * currentSpeed : Vector3.zero;

            TryCatch();
        }

        private bool TryStun()
        {
            Trap best = null;
            float bestDistance = float.MaxValue;
            var traps = TrapRegistry.Active;

            for (int i = 0; i < traps.Count; i++)
            {
                Trap trap = traps[i];
                if (trap == null || !trap.IsAvailable)
                {
                    continue;
                }

                float distance = HorizontalDistance(body.position, trap.transform.position);
                if (distance <= trap.DistractionRadius && distance < bestDistance)
                {
                    best = trap;
                    bestDistance = distance;
                }
            }

            if (best == null)
            {
                return false;
            }

            float seconds = best.Trigger();
            stateTimer = seconds;
            state = CatState.Stunned;
            currentSpeed = balance.catBaseSpeed;
            body.linearVelocity = Vector3.zero;

            Debug.Log($"[Cat] Stunned by {best.Definition.displayName} for {seconds:0.0}s");

            if (audioManager != null)
            {
                audioManager.PlayDistract();
            }
            return true;
        }

        private void TryCatch()
        {
            if (player.IsInvincible)
            {
                return;
            }

            if (HorizontalDistance(body.position, player.Position) <= balance.catchRadius && player.TakeHit(body.position))
            {
                currentSpeed = balance.catBaseSpeed;
                stateTimer = 0.6f;
                state = CatState.Recovering;
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
