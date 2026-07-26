using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class GridSystem : MonoBehaviour
    {
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public float CellSize { get; private set; }

        private bool[] furniture;
        private bool[] occupied;
        private Vector3 cornerOrigin;
        private GameObject cellsRoot;

        public Vector3 Center => transform.position;
        public Vector2 WorldSize => new Vector2(Columns * CellSize, Rows * CellSize);

        public void Initialize(BalanceSettings balance)
        {
            Columns = Mathf.Max(1, balance.gridColumns);
            Rows = Mathf.Max(1, balance.gridRows);
            CellSize = Mathf.Max(0.1f, balance.cellSize);

            furniture = new bool[Columns * Rows];
            occupied = new bool[Columns * Rows];

            cornerOrigin = transform.position - new Vector3(Columns * CellSize * 0.5f, 0f, Rows * CellSize * 0.5f);

            MarkFurnitureFromLayout(balance);
        }

        // Shows/hides the "Cells" edit-guide overlay (a child built by SceneBuilder). Visible
        // while editing to place traps; hidden during play so only the floor shows.
        public void SetGridVisible(bool visible)
        {
            if (cellsRoot == null)
            {
                Transform found = transform.Find("Cells");
                if (found != null)
                {
                    cellsRoot = found.gameObject;
                }
            }
            if (cellsRoot != null)
            {
                cellsRoot.SetActive(visible);
            }
        }

        // Furniture blocks a cell whenever the layout has anything other than empty ('.') there.
        // This covers every furniture type and the '>' continuation of a 1x2 piece, so 2-cell
        // obstacles block BOTH cells. Layout row 0 is the far/top row, so gridRow = Rows - 1 - i.
        private void MarkFurnitureFromLayout(BalanceSettings balance)
        {
            for (int i = 0; i < Rows; i++)
            {
                string line = balance.GetFurnitureRow(i);
                int gridRow = Rows - 1 - i;
                for (int c = 0; c < Columns && c < line.Length; c++)
                {
                    char ch = line[c];
                    if (ch != '.' && ch != ' ')
                    {
                        furniture[Index(c, gridRow)] = true;
                    }
                }
            }
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
        }

        public bool IsFurniture(Vector2Int cell)
        {
            return IsInside(cell) && furniture[Index(cell.x, cell.y)];
        }

        public bool IsFree(Vector2Int cell)
        {
            return IsInside(cell) && !furniture[Index(cell.x, cell.y)] && !occupied[Index(cell.x, cell.y)];
        }

        public bool IsWalkable(Vector2Int cell)
        {
            return IsInside(cell) && !furniture[Index(cell.x, cell.y)];
        }

        public bool HasLineOfSight(Vector2Int from, Vector2Int to)
        {
            int x0 = from.x;
            int y0 = from.y;
            int x1 = to.x;
            int y1 = to.y;
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (!IsWalkable(new Vector2Int(x0, y0)))
                {
                    return false;
                }
                if (x0 == x1 && y0 == y1)
                {
                    return true;
                }

                int doubled = 2 * err;
                bool stepX = doubled > -dy;
                bool stepY = doubled < dx;

                // On a diagonal step, refuse to let the sight line slip through the
                // corner between two obstacles (the same rule the pathfinder uses).
                // Without this the cat "sees" the player past a furniture corner and
                // drives straight into the collider, jamming behind wide pieces like
                // the bed while the player jukes left/right.
                if (stepX && stepY &&
                    (!IsWalkable(new Vector2Int(x0 + sx, y0)) || !IsWalkable(new Vector2Int(x0, y0 + sy))))
                {
                    return false;
                }

                if (stepX)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (stepY)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        public bool CanPlace(IReadOnlyList<Vector2Int> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!IsFree(cells[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public void Occupy(IReadOnlyList<Vector2Int> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (IsInside(cells[i]))
                {
                    occupied[Index(cells[i].x, cells[i].y)] = true;
                }
            }
        }

        public void Release(IReadOnlyList<Vector2Int> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (IsInside(cells[i]))
                {
                    occupied[Index(cells[i].x, cells[i].y)] = false;
                }
            }
        }

        public void ClearOccupancy()
        {
            if (occupied == null)
            {
                return;
            }
            for (int i = 0; i < occupied.Length; i++)
            {
                occupied[i] = false;
            }
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return CellToWorld(transform.position, Columns, Rows, CellSize, cell.x, cell.y);
        }

        public static Vector3 CellToWorld(Vector3 center, int columns, int rows, float cellSize, int col, int row)
        {
            Vector3 corner = center - new Vector3(columns * cellSize * 0.5f, 0f, rows * cellSize * 0.5f);
            float x = corner.x + (col + 0.5f) * cellSize;
            float z = corner.z + (row + 0.5f) * cellSize;
            return new Vector3(x, center.y, z);
        }

        public Vector3 FootprintCenter(IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return transform.position;
            }

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < cells.Count; i++)
            {
                sum += CellToWorld(cells[i]);
            }
            return sum / cells.Count;
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            int col = Mathf.FloorToInt((world.x - cornerOrigin.x) / CellSize);
            int row = Mathf.FloorToInt((world.z - cornerOrigin.z) / CellSize);
            return new Vector2Int(col, row);
        }

        public IEnumerable<Vector2Int> FurnitureCells()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    if (furniture[Index(col, row)])
                    {
                        yield return new Vector2Int(col, row);
                    }
                }
            }
        }

        public IEnumerable<Vector2Int> AllCells()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    yield return new Vector2Int(col, row);
                }
            }
        }

        private int Index(int col, int row) => row * Columns + col;
    }
}
