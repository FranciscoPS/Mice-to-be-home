using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class PlacementGhost : MonoBehaviour
    {
        private static readonly Color ValidColor = new Color(0.35f, 1f, 0.4f, 0.55f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f, 0.55f);

        private SpriteRenderer trapSprite;
        private Transform tileParent;
        private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        private float cellSize;

        public void Build(float cell)
        {
            cellSize = cell;

            var tileRoot = new GameObject("Tiles");
            tileRoot.transform.SetParent(transform, false);
            tileParent = tileRoot.transform;

            trapSprite = VisualFactory.CreateBillboard("Preview", transform, null, Color.white, SpriteShape.Square, cell * 0.85f);
            Hide();
        }

        public void SetDefinition(TrapDefinition definition)
        {
            PlaceholderFactory.Configure(trapSprite, definition.sprite, definition.tint, SpriteShape.Square, new Vector2(0.5f, 0f));
            Color color = trapSprite.color;
            color.a = 0.75f;
            trapSprite.color = color;
        }

        public void UpdatePreview(IReadOnlyList<Vector2Int> footprint, Vector3 center, bool valid, GridSystem grid)
        {
            gameObject.SetActive(true);
            transform.position = center;

            EnsureTiles(footprint.Count);
            Color color = valid ? ValidColor : InvalidColor;

            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i < footprint.Count;
                tiles[i].gameObject.SetActive(used);
                if (used)
                {
                    tiles[i].transform.position = grid.CellToWorld(footprint[i]) + Vector3.up * 0.03f;
                    tiles[i].color = color;
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void EnsureTiles(int count)
        {
            while (tiles.Count < count)
            {
                SpriteRenderer tile = VisualFactory.CreateGround("Tile", tileParent, null, Color.white,
                    new Vector2(cellSize * 0.92f, cellSize * 0.92f), -500);
                tiles.Add(tile);
            }
        }
    }
}
