using UnityEngine;

namespace MiceToBeHome
{
    public class CatController : MonoBehaviour
    {
        private enum CatState
        {
            Idle,
            Chasing,
            Distracted,
            Recovering
        }

        private Rigidbody body;
        private MousePlayerController player;
        private BreadcrumbTrail trail;
        private BalanceSettings balance;
        private AudioManager audioManager;

        private CatState state = CatState.Idle;
        private float currentSpeed;
        private float stateTimer;
        private bool active;

        public void Initialize(Rigidbody rigidbody, MousePlayerController target, BreadcrumbTrail sharedTrail, BalanceSettings settings, AudioManager audioManager)
        {
            body = rigidbody;
            player = target;
            trail = sharedTrail;
            balance = settings;
            this.audioManager = audioManager;
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
                case CatState.Distracted:
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
            if (TryDistract())
            {
                return;
            }

            currentSpeed = Mathf.Min(balance.catMaxSpeed, currentSpeed + balance.catAcceleration * Time.fixedDeltaTime);

            Vector3 destination = trail != null ? trail.GetTarget(body.position, player.Position) : player.Position;
            Vector3 direction = destination - body.position;
            direction.y = 0f;

            body.linearVelocity = direction.sqrMagnitude > 0.0004f ? direction.normalized * currentSpeed : Vector3.zero;

            TryCatch();
        }

        private bool TryDistract()
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

            stateTimer = best.Trigger();
            state = CatState.Distracted;
            currentSpeed = balance.catBaseSpeed;
            body.linearVelocity = Vector3.zero;

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
                if (trail != null)
                {
                    trail.Clear();
                }
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
