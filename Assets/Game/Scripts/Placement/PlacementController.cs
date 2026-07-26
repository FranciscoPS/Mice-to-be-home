using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiceToBeHome
{
    public class PlacementController : MonoBehaviour
    {
        private GridSystem grid;
        private BalanceSettings balance;
        private Camera viewCamera;
        private Transform trapParent;
        private AudioManager audioManager;
        private PlacementGhost ghost;

        private readonly List<Trap> placedTraps = new List<Trap>();
        private readonly Dictionary<Vector2Int, Trap> cellToTrap = new Dictionary<Vector2Int, Trap>();
        private readonly List<Vector2Int> footprintBuffer = new List<Vector2Int>();
        private readonly List<Trap> inventory = new List<Trap>();
        private readonly Dictionary<Trap, int> stock = new Dictionary<Trap, int>();

        private Trap selected;
        private Sprite selectedSprite;
        private Color selectedColor = Color.white;
        private Vector3 selectedScale = Vector3.one;
        private float selectedLift;
        private int maxTraps = 5;
        private bool carrying;
        private bool active;
        private Vector2Int lastHoverCell;
        private bool hasHoverCell;

        public int MaxTraps => maxTraps;
        public int PlacedCount => placedTraps.Count;
        public Trap SelectedTrap => carrying ? selected : null;
        public int GetStock(Trap prefab) => prefab != null && stock.TryGetValue(prefab, out int amount) ? amount : 0;

        public void Initialize(GridSystem gridSystem, BalanceSettings balanceSettings, Camera camera, Transform placedParent, AudioManager audioManager, IReadOnlyList<Trap> trapPrefabs)
        {
            grid = gridSystem;
            balance = balanceSettings;
            viewCamera = camera;
            trapParent = placedParent;
            this.audioManager = audioManager;
            maxTraps = Mathf.Max(1, balanceSettings.maxTraps);

            inventory.Clear();
            for (int i = 0; i < trapPrefabs.Count; i++)
            {
                if (trapPrefabs[i] != null && !inventory.Contains(trapPrefabs[i]))
                {
                    inventory.Add(trapPrefabs[i]);
                }
            }
            RefillStock();

            var ghostObject = new GameObject("PlacementGhost");
            ghostObject.transform.SetParent(transform, false);
            ghost = ghostObject.AddComponent<PlacementGhost>();
            ghost.Build(balance.cellSize);
        }

        private void RefillStock()
        {
            stock.Clear();
            for (int i = 0; i < inventory.Count; i++)
            {
                Trap prefab = inventory[i];
                stock[prefab] = prefab.Definition != null ? Mathf.Max(0, prefab.Definition.stock) : 0;
            }
        }

        public void SetActive(bool value)
        {
            active = value;
            if (!value)
            {
                CancelCarry();
            }
        }

        public void SelectFromInventory(Trap trapPrefab)
        {
            if (trapPrefab == null || placedTraps.Count >= maxTraps || GetStock(trapPrefab) <= 0)
            {
                return;
            }

            selected = trapPrefab;
            carrying = true;

            ReadTrapVisual(trapPrefab, out selectedSprite, out selectedColor, out selectedScale, out selectedLift);
        }

        public void SetTrapsArmed(bool armed)
        {
            for (int i = 0; i < placedTraps.Count; i++)
            {
                if (placedTraps[i] != null)
                {
                    placedTraps[i].SetArmed(armed);
                }
            }
        }

        public void ResetLevel()
        {
            for (int i = 0; i < placedTraps.Count; i++)
            {
                if (placedTraps[i] != null)
                {
                    Destroy(placedTraps[i].gameObject);
                }
            }

            placedTraps.Clear();
            cellToTrap.Clear();
            grid.ClearOccupancy();
            RefillStock();
            CancelCarry();
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool leftClick = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

            if (rightClick)
            {
                if (carrying)
                {
                    CancelCarry();
                }
                return;
            }

            if (!carrying)
            {
                hasHoverCell = false;
                if (!overUI && TryGetMouseCell(out Vector2Int hovered)
                    && cellToTrap.TryGetValue(hovered, out Trap existing) && existing != null)
                {
                    ReadTrapVisual(existing, out _, out _, out Vector3 exScale, out float exLift);
                    ghost.ShowRemoval(hovered, grid, exScale, exLift);
                    if (leftClick)
                    {
                        RemoveTrap(existing);
                        ghost.Hide();
                    }
                }
                else
                {
                    ghost.Hide();
                }
                return;
            }

            if (overUI || !TryGetMouseCell(out Vector2Int origin))
            {
                ghost.Hide();
                hasHoverCell = false;
                return;
            }

            footprintBuffer.Clear();
            footprintBuffer.Add(origin);
            bool valid = grid.CanPlace(footprintBuffer);
            Vector3 center = grid.FootprintCenter(footprintBuffer);
            ghost.ShowPlacement(footprintBuffer, center, valid, grid, selectedSprite, selectedColor, selectedScale, selectedLift);

            if ((!hasHoverCell || origin != lastHoverCell) && audioManager != null)
            {
                audioManager.PlayHover();
            }
            lastHoverCell = origin;
            hasHoverCell = true;

            if (leftClick && valid)
            {
                PlaceTrap(center);
            }
        }

        private void PlaceTrap(Vector3 center)
        {
            Trap trap = Instantiate(selected, center, Quaternion.identity, trapParent);
            trap.name = "Trap_" + selected.Definition.displayName;
            trap.SourcePrefab = selected;
            trap.Configure(footprintBuffer);

            grid.Occupy(footprintBuffer);
            for (int i = 0; i < footprintBuffer.Count; i++)
            {
                cellToTrap[footprintBuffer[i]] = trap;
            }
            placedTraps.Add(trap);

            if (stock.ContainsKey(selected))
            {
                stock[selected] = Mathf.Max(0, stock[selected] - 1);
            }

            if (audioManager != null)
            {
                audioManager.PlayClick();
            }

            if (GetStock(selected) <= 0 || placedTraps.Count >= maxTraps)
            {
                CancelCarry();
            }
        }

        private void RemoveTrap(Trap trap)
        {
            grid.Release(trap.Cells);
            for (int i = 0; i < trap.Cells.Count; i++)
            {
                cellToTrap.Remove(trap.Cells[i]);
            }
            placedTraps.Remove(trap);

            Trap source = trap.SourcePrefab;
            if (source != null && stock.ContainsKey(source))
            {
                stock[source] += 1;
            }

            Destroy(trap.gameObject);

            if (audioManager != null)
            {
                audioManager.PlayClick();
            }
        }

        private void CancelCarry()
        {
            carrying = false;
            selected = null;
            if (ghost != null)
            {
                ghost.Hide();
            }
        }

        // Reads a trap's on-screen sprite + world scale + height offset straight from its Visual, so the
        // ghost matches whatever size the prefab was authored/tweaked to (not a hardcoded cell size).
        private static void ReadTrapVisual(Trap trap, out Sprite sprite, out Color color, out Vector3 worldScale, out float lift)
        {
            sprite = null;
            color = Color.white;
            worldScale = Vector3.one;
            lift = 0f;
            if (trap == null)
            {
                return;
            }

            SpriteRenderer skin = trap.GetComponentInChildren<SpriteRenderer>();
            if (skin == null)
            {
                color = trap.Definition != null ? trap.Definition.tint : Color.white;
                return;
            }

            sprite = skin.sprite;
            color = skin.color;
            worldScale = skin.transform.lossyScale;
            lift = skin.transform.localPosition.y;
        }

        private bool TryGetMouseCell(out Vector2Int cell)
        {
            cell = default;

            if (Mouse.current == null)
            {
                return false;
            }

            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, new Vector3(0f, grid.Center.y, 0f));
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 point = ray.GetPoint(distance);
            cell = grid.WorldToCell(point);
            return grid.IsInside(cell);
        }
    }
}
