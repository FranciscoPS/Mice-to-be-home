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

        private Trap selected;
        private bool carrying;
        private bool active;

        public void Initialize(GridSystem gridSystem, BalanceSettings balanceSettings, Camera camera, Transform placedParent, AudioManager audioManager)
        {
            grid = gridSystem;
            balance = balanceSettings;
            viewCamera = camera;
            trapParent = placedParent;
            this.audioManager = audioManager;

            var ghostObject = new GameObject("PlacementGhost");
            ghostObject.transform.SetParent(transform, false);
            ghost = ghostObject.AddComponent<PlacementGhost>();
            ghost.Build(balance.cellSize);
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
                CancelCarry();
                return;
            }

            if (!carrying)
            {
                ghost.Hide();
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
                return;
            }

            footprintBuffer.Clear();
            footprintBuffer.Add(origin);
            bool valid = grid.CanPlace(footprintBuffer);
            Vector3 center = grid.FootprintCenter(footprintBuffer);
            ghost.UpdatePreview(footprintBuffer, center, valid, grid);

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

            if (audioManager != null)
            {
                audioManager.PlayPlace();
            }
        }

        private void PickUp(Trap trap)
        {
            grid.Release(trap.Cells);
            for (int i = 0; i < trap.Cells.Count; i++)
            {
                cellToTrap.Remove(trap.Cells[i]);
            }
            placedTraps.Remove(trap);

            selected = trap.SourcePrefab;
            carrying = selected != null;

            Destroy(trap.gameObject);
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
