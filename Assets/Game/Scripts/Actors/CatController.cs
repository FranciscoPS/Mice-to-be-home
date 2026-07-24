using System.Collections.Generic;
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
        private GridSystem grid;

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private Vector2Int cachedGoal = new Vector2Int(int.MinValue, int.MinValue);
        private float repathTimer;

        private CatState state = CatState.Idle;
        private float currentSpeed;
        private float stateTimer;
        private bool active;
        private bool stunImmune;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ActorPhysics.ApplyTo(GetComponent<Collider>());
        }

        public void Initialize(MousePlayerController target, BalanceSettings settings, AudioManager audio, GridSystem gridSystem)
        {
            player = target;
            balance = settings;
            audioManager = audio;
            grid = gridSystem;
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
            stunImmune = false;
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

            Vector3 target = ResolveSteerTarget();
            Vector3 direction = target - body.position;
            direction.y = 0f;

            body.linearVelocity = direction.sqrMagnitude > 0.0004f ? direction.normalized * currentSpeed : Vector3.zero;

            TryCatch();
        }

        private Vector3 ResolveSteerTarget()
        {
            if (grid == null)
            {
                return player.Position;
            }

            Vector2Int catCell = grid.WorldToCell(body.position);
            Vector2Int playerCell = grid.WorldToCell(player.Position);

            if (catCell == playerCell || grid.HasLineOfSight(catCell, playerCell))
            {
                return player.Position;
            }

            repathTimer -= Time.fixedDeltaTime;
            bool stale = path.Count == 0 || path[0] != catCell || playerCell != cachedGoal || repathTimer <= 0f;
            if (stale)
            {
                GridPathfinder.TryFindPath(grid, catCell, playerCell, path);
                cachedGoal = playerCell;
                repathTimer = 0.2f;
            }

            return path.Count > 1 ? grid.CellToWorld(path[1]) : player.Position;
        }

        private bool TryStun()
        {
            Trap best = null;
            float bestDistance = float.MaxValue;
            bool insideTrapZone = false;
            var traps = TrapRegistry.Active;

            for (int i = 0; i < traps.Count; i++)
            {
                Trap trap = traps[i];
                if (trap == null || !trap.IsAvailable)
                {
                    continue;
                }

                float distance = HorizontalDistance(body.position, trap.transform.position);
                if (distance <= trap.DistractionRadius)
                {
                    insideTrapZone = true;
                    if (distance < bestDistance)
                    {
                        best = trap;
                        bestDistance = distance;
                    }
                }
            }

            if (!insideTrapZone)
            {
                stunImmune = false;
            }

            if (stunImmune || best == null)
            {
                return false;
            }

            float seconds = best.Trigger();
            stateTimer = seconds;
            state = CatState.Stunned;
            currentSpeed = balance.catBaseSpeed;
            body.linearVelocity = Vector3.zero;
            stunImmune = true;

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
