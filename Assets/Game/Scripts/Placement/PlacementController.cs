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
        private int maxTraps = 5;
        private bool carrying;
        private bool active;
        private Vector2Int lastHoverCell;
        private bool hasHoverCell;

        public int MaxTraps => maxTraps;
        public int PlacedCount => placedTraps.Count;
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
                else if (!overUI && TryGetMouseCell(out Vector2Int removeCell)
                    && cellToTrap.TryGetValue(removeCell, out Trap toRemove) && toRemove != null)
                {
                    RemoveTrap(toRemove);
                }
                return;
            }

            if (!carrying)
            {
                ghost.Hide();
                hasHoverCell = false;
                if (!overUI && leftClick && TryGetMouseCell(out Vector2Int hovered))
                {
                    if (cellToTrap.TryGetValue(hovered, out Trap existing) && existing != null)
                    {
                        PickUp(existing);
                    }
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
            ghost.UpdatePreview(footprintBuffer, center, valid, grid);

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

        private Trap DetachTrap(Trap trap)
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
            return source;
        }

        private void PickUp(Trap trap)
        {
            Trap source = DetachTrap(trap);
            selected = source;
            carrying = source != null;
        }

        private void RemoveTrap(Trap trap)
        {
            DetachTrap(trap);
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
