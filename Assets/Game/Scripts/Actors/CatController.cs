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
        private SpriteAnimator animator;

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private Vector2Int cachedGoal = new Vector2Int(int.MinValue, int.MinValue);
        private float repathTimer;

        private CatState state = CatState.Idle;
        private float currentSpeed;
        private float stateTimer;
        private bool active;
        private bool stunImmune;
        private Vector3 lastPosition;
        private float stuckTimer;
        private float unstickTimer;
        private Vector3 unstickVelocity;

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<SpriteAnimator>();
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
            lastPosition = startPosition;
            stuckTimer = 0f;
            unstickTimer = 0f;
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

            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            if (animator == null)
            {
                return;
            }
            Vector3 velocity = body != null ? body.linearVelocity : Vector3.zero;
            animator.FaceHorizontal(velocity.x);
            animator.Play(velocity.sqrMagnitude > 0.04f ? CharacterAnim.Run : CharacterAnim.Idle);
        }

        private void Chase()
        {
            if (TryStun())
            {
                return;
            }

            currentSpeed = Mathf.Min(balance.catMaxSpeed, currentSpeed + balance.catAcceleration * Time.fixedDeltaTime);

            if (unstickTimer > 0f)
            {
                unstickTimer -= Time.fixedDeltaTime;
                body.linearVelocity = unstickVelocity;
                TrackProgress();
                TryCatch();
                return;
            }

            Vector3 target = ResolveSteerTarget();
            Vector3 direction = target - body.position;
            direction.y = 0f;

            Vector3 velocity = direction.sqrMagnitude > 0.0004f ? direction.normalized * currentSpeed : Vector3.zero;
            body.linearVelocity = velocity;

            if (velocity.sqrMagnitude > 0.01f)
            {
                TrackProgress();
            }
            else
            {
                stuckTimer = 0f;
                lastPosition = body.position;
            }

            TryCatch();
        }

        private void TrackProgress()
        {
            float moved = HorizontalDistance(body.position, lastPosition);
            lastPosition = body.position;

            float expected = currentSpeed * Time.fixedDeltaTime * 0.35f;
            if (moved < expected)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= 0.4f)
                {
                    BeginUnstick();
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        private void BeginUnstick()
        {
            stuckTimer = 0f;
            unstickTimer = 0.45f;

            Vector3 toPlayer = player.Position - body.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                toPlayer = Vector3.forward;
            }
            toPlayer.Normalize();

            Vector3 perpendicular = new Vector3(-toPlayer.z, 0f, toPlayer.x);
            if (Random.value < 0.5f)
            {
                perpendicular = -perpendicular;
            }

            unstickVelocity = (perpendicular * 0.8f + toPlayer * 0.2f).normalized * currentSpeed;

            repathTimer = 0f;
            path.Clear();
            cachedGoal = new Vector2Int(int.MinValue, int.MinValue);
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
                path.Clear();
                return player.Position;
            }

            Vector2Int goalCell = grid.IsWalkable(playerCell) ? playerCell : NearestWalkable(playerCell);

            while (path.Count > 0 && path[0] == catCell)
            {
                path.RemoveAt(0);
            }

            repathTimer -= Time.fixedDeltaTime;
            int goalDrift = Mathf.Abs(goalCell.x - cachedGoal.x) + Mathf.Abs(goalCell.y - cachedGoal.y);

            // Commit to the chosen route so the cat cannot be mirror-juked back and
            // forth around a symmetric obstacle: only repath when the route is spent,
            // when the player relocates far, or as an occasional safety refresh.
            if (path.Count == 0 || goalDrift >= 4 || repathTimer <= 0f)
            {
                if (GridPathfinder.TryFindPath(grid, catCell, goalCell, path))
                {
                    while (path.Count > 0 && path[0] == catCell)
                    {
                        path.RemoveAt(0);
                    }
                }
                cachedGoal = goalCell;
                repathTimer = 1.5f;
            }

            return path.Count > 0 ? grid.CellToWorld(path[0]) : player.Position;
        }

        private Vector2Int NearestWalkable(Vector2Int cell)
        {
            Vector2Int best = cell;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                Vector2Int neighbor = cell + NeighborOffsets[i];
                if (!grid.IsWalkable(neighbor))
                {
                    continue;
                }

                float distance = HorizontalDistance(grid.CellToWorld(neighbor), body.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = neighbor;
                }
            }
            return best;
        }

        private bool TryStun()
        {
            Trap best = null;
            bool insideTrapZone = false;
            var traps = TrapRegistry.Active;

            for (int i = 0; i < traps.Count; i++)
            {
                Trap trap = traps[i];
                if (trap == null || !trap.IsAvailable)
                {
                    continue;
                }

                if (trap.IsInZone(body.position))
                {
                    insideTrapZone = true;
                    if (best == null)
                    {
                        best = trap;
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
