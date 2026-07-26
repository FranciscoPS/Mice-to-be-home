using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class PlacementGhost : MonoBehaviour
    {
        private static readonly Color ValidColor = new Color(0.35f, 1f, 0.4f, 0.55f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f, 0.55f);
        private static readonly Color RemoveTileColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        private static readonly Color RemoveMarkColor = new Color(1f, 0.2f, 0.2f, 0.95f);

        private Transform tileParent;
        private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
        private float cellSize;

        private SpriteRenderer previewSprite;
        private SpriteRenderer removeMark;

        public void Build(float cell)
        {
            cellSize = cell;

            var tileRoot = new GameObject("Tiles");
            tileRoot.transform.SetParent(transform, false);
            tileParent = tileRoot.transform;

            // Faded billboard of the trap that is about to be placed (matches the real trap visual size).
            previewSprite = VisualFactory.CreateBillboard("PreviewSprite", transform, null, Color.white, SpriteShape.Square, cell * 0.9f);

            // Red "X" shown when hovering a placed trap to remove it.
            Sprite cross = PlaceholderFactory.GetSprite(SpriteShape.Cross, new Vector2(0.5f, 0.5f));
            removeMark = VisualFactory.CreateBillboard("RemoveMark", transform, cross, RemoveMarkColor, SpriteShape.Cross, cell * 0.7f);
            removeMark.color = RemoveMarkColor;

            Hide();
        }

        // Preview while carrying: floor tile tinted valid/invalid + a faded billboard of the selected trap.
        public void ShowPlacement(IReadOnlyList<Vector2Int> footprint, Vector3 center, bool valid, GridSystem grid, Sprite trapSprite, Color trapColor, Vector3 spriteScale, float spriteLift)
        {
            gameObject.SetActive(true);
            transform.position = center;

            EnsureTiles(footprint.Count);
            Color tileColor = valid ? ValidColor : InvalidColor;
            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i < footprint.Count;
                tiles[i].gameObject.SetActive(used);
                if (used)
                {
                    tiles[i].transform.position = grid.CellToWorld(footprint[i]) + Vector3.up * 0.03f;
                    tiles[i].color = tileColor;
                }
            }

            if (removeMark != null)
            {
                removeMark.gameObject.SetActive(false);
            }
            if (previewSprite != null)
            {
                previewSprite.gameObject.SetActive(true);
                previewSprite.transform.localScale = spriteScale;
                previewSprite.transform.position = center + Vector3.up * spriteLift;
                if (trapSprite != null)
                {
                    previewSprite.sprite = trapSprite;
                }
                Color c = trapColor;
                c.a = valid ? 0.6f : 0.35f;
                previewSprite.color = c;
            }
        }

        // Preview while NOT carrying and hovering a placed trap: red floor tile + a red X to remove it.
        public void ShowRemoval(Vector2Int cell, GridSystem grid, Vector3 trapScale, float trapLift)
        {
            gameObject.SetActive(true);
            Vector3 center = grid.CellToWorld(cell);
            transform.position = center;

            EnsureTiles(1);
            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i == 0;
                tiles[i].gameObject.SetActive(used);
                if (used)
                {
                    tiles[i].transform.position = center + Vector3.up * 0.03f;
                    tiles[i].color = RemoveTileColor;
                }
            }

            if (previewSprite != null)
            {
                previewSprite.gameObject.SetActive(false);
            }
            if (removeMark != null)
            {
                removeMark.gameObject.SetActive(true);
                removeMark.transform.localScale = trapScale;
                removeMark.transform.position = center + Vector3.up * trapLift;
                removeMark.color = RemoveMarkColor;
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
