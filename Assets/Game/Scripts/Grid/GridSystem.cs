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

            for (int row = 0; row < Rows; row++)
            {
                int stringIndex = Rows - 1 - row;
                string line = balance.GetFurnitureRow(stringIndex);
                for (int col = 0; col < Columns; col++)
                {
                    bool blocked = col < line.Length && (line[col] == 'X' || line[col] == 'x');
                    furniture[Index(col, row)] = blocked;
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
            float x = cornerOrigin.x + (cell.x + 0.5f) * CellSize;
            float z = cornerOrigin.z + (cell.y + 0.5f) * CellSize;
            return new Vector3(x, transform.position.y, z);
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
