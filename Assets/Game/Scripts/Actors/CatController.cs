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
        private SpriteRenderer visual;
        private Animator animator;
        private MousePlayerController player;
        private BalanceSettings balance;
        private AudioManager audioManager;
        private GridSystem grid;

        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private Vector2Int cachedGoal;
        private bool hasCachedGoal;
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
        private float lastDirectionX = 1f;
        private float footstepTimer;

        [SerializeField] private bool debugLogs = true;
        private float debugTimer;
        private bool dbgHadLos;
        private int dbgPathCount;
        private Vector2Int dbgNextCell;

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
            visual = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
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
            if (!value)
            {
                if (body != null)
                {
                    body.linearVelocity = Vector3.zero;
                }
                if (audioManager != null)
                {
                    audioManager.SetCatPurring(false);
                }
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
            path.Clear();
            hasCachedGoal = false;
            footstepTimer = 0f;
            if (audioManager != null)
            {
                audioManager.SetCatPurring(false);
            }
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
                        if (audioManager != null)
                        {
                            audioManager.SetCatPurring(false);
                        }
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

            // Idle SOLO cuando el gato está stunneado; persiguiendo / atacando / recuperándose
            // se mantiene la animación de correr (no tenemos animación de ataque).
            if (animator != null)
            {
                animator.SetBool("IsMoving", state != CatState.Stunned);
            }

            UpdateFootsteps();
        }

        private void UpdateFootsteps()
        {
            if (audioManager == null)
            {
                return;
            }
            bool moving = state == CatState.Chasing && body.linearVelocity.sqrMagnitude > 0.04f;
            if (moving)
            {
                footstepTimer -= Time.fixedDeltaTime;
                if (footstepTimer <= 0f)
                {
                    audioManager.PlayCatFootstep();
                    footstepTimer = audioManager.FootstepInterval;
                }
            }
            else
            {
                footstepTimer = 0f;
            }
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
                LogSnapshot("UNSTICK");
                TryCatch();
                return;
            }

            Vector3 target = ResolveSteerTarget();
            Vector3 direction = target - body.position;
            direction.y = 0f;

            Vector3 velocity = direction.sqrMagnitude > 0.0004f ? direction.normalized * currentSpeed : Vector3.zero;
            body.linearVelocity = velocity;

            // Flipear sprite según dirección horizontal
            if (direction.x != 0f)
            {
                lastDirectionX = direction.x;
                if (visual != null)
                {
                    visual.flipX = lastDirectionX < 0f;
                }
            }

            if (velocity.sqrMagnitude > 0.01f)
            {
                TrackProgress();
            }
            else
            {
                stuckTimer = 0f;
                lastPosition = body.position;
            }

            LogSnapshot("STEER");
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

            // Slip toward the side the current route heads around the obstacle so the
            // cat keeps circling the same way instead of dithering in place (which let
            // the player camp behind wide furniture). Fall back to a random side.
            float side = 0f;
            if (grid != null && path.Count > 0)
            {
                Vector3 toWaypoint = grid.CellToWorld(path[0]) - body.position;
                toWaypoint.y = 0f;
                side = Vector3.Dot(toWaypoint, perpendicular);
            }
            if (side < 0f || (Mathf.Abs(side) < 0.0001f && Random.value < 0.5f))
            {
                perpendicular = -perpendicular;
            }

            unstickVelocity = (perpendicular * 0.8f + toPlayer * 0.2f).normalized * currentSpeed;

            if (debugLogs)
            {
                Vector2Int cCell = grid != null ? grid.WorldToCell(body.position) : default;
                Vector2Int pCell = grid != null ? grid.WorldToCell(player.Position) : default;
                Debug.Log($"[CatDbg] UNSTICK-begin cat={cCell} plr={pCell} pathCount={path.Count} side={side:0.00} dir=({unstickVelocity.x:0.00},{unstickVelocity.z:0.00})");
            }

            repathTimer = 0f;
            path.Clear();
            hasCachedGoal = false;
        }

        private Vector3 ResolveSteerTarget()
        {
            if (grid == null)
            {
                return player.Position;
            }

            Vector2Int catCell = grid.WorldToCell(body.position);
            Vector2Int playerCell = grid.WorldToCell(player.Position);

            // If the cat itself ended up on a blocked cell (e.g. shoved onto furniture),
            // the pathfinder can't route from here — head to the nearest open cell first.
            if (!grid.IsWalkable(catCell))
            {
                path.Clear();
                dbgHadLos = false;
                dbgPathCount = 0;
                Vector2Int escapeCell = NearestWalkable(catCell);
                dbgNextCell = escapeCell;
                return grid.CellToWorld(escapeCell);
            }

            if (catCell == playerCell || grid.HasLineOfSight(catCell, playerCell))
            {
                path.Clear();
                dbgHadLos = true;
                dbgPathCount = 0;
                dbgNextCell = playerCell;
                return player.Position;
            }

            dbgHadLos = false;
            Vector2Int goalCell = grid.IsWalkable(playerCell) ? playerCell : NearestWalkable(playerCell);

            while (path.Count > 0 && path[0] == catCell)
            {
                path.RemoveAt(0);
            }

            repathTimer -= Time.fixedDeltaTime;

            // Commit to the chosen route so the cat cannot be mirror-juked back and
            // forth around a symmetric obstacle: only repath when there is no committed
            // goal yet, when the route is spent, when the player relocates far, or as an
            // occasional safety refresh.
            bool needsRepath = !hasCachedGoal || path.Count == 0 || repathTimer <= 0f;
            if (!needsRepath)
            {
                int goalDrift = Mathf.Abs(goalCell.x - cachedGoal.x) + Mathf.Abs(goalCell.y - cachedGoal.y);
                needsRepath = goalDrift >= 4;
            }

            if (needsRepath)
            {
                bool found = GridPathfinder.TryFindPath(grid, catCell, goalCell, path);
                if (found)
                {
                    while (path.Count > 0 && path[0] == catCell)
                    {
                        path.RemoveAt(0);
                    }
                }
                cachedGoal = goalCell;
                hasCachedGoal = true;
                repathTimer = 1.5f;
                if (debugLogs)
                {
                    Debug.Log($"[CatDbg] REPATH cat={catCell} goal={goalCell} found={found} len={path.Count}");
                }
            }

            dbgPathCount = path.Count;
            dbgNextCell = path.Count > 0 ? path[0] : playerCell;
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

        private void LogSnapshot(string mode)
        {
            if (!debugLogs)
            {
                return;
            }
            debugTimer -= Time.fixedDeltaTime;
            if (debugTimer > 0f)
            {
                return;
            }
            debugTimer = 0.4f;
            Vector2Int catCell = grid != null ? grid.WorldToCell(body.position) : default;
            Vector2Int plrCell = grid != null && player != null ? grid.WorldToCell(player.Position) : default;
            float dist = player != null ? HorizontalDistance(body.position, player.Position) : -1f;
            Debug.Log($"[CatDbg] {mode} st={state} cat={catCell} plr={plrCell} los={dbgHadLos} path={dbgPathCount} next={dbgNextCell} vel={body.linearVelocity.magnitude:0.00} spd={currentSpeed:0.00} stuck={stuckTimer:0.00} unstick={unstickTimer:0.00} dist={dist:0.00}");
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
                audioManager.PlayCatTrapped();
                audioManager.SetCatPurring(true);
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
                if (audioManager != null)
                {
                    audioManager.PlayCatAttack();
                }
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
