using UnityEngine;

namespace MiceToBeHome
{
    public static class VisualFactory
    {
        private static readonly Vector2 BottomPivot = new Vector2(0.5f, 0f);
        private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);

        public static SpriteRenderer CreateBillboard(string name, Transform parent, Sprite artwork, Color tint, SpriteShape shape, float worldSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            PlaceholderFactory.Configure(renderer, artwork, tint, shape, BottomPivot);
            go.AddComponent<Billboard>();

            go.transform.localScale = Vector3.one * worldSize;
            return renderer;
        }

        public static SpriteRenderer CreateGround(string name, Transform parent, Sprite artwork, Color tint, Vector2 worldSize, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            PlaceholderFactory.Configure(renderer, artwork, tint, SpriteShape.Square, CenterPivot);
            renderer.sortingOrder = sortingOrder;

            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
            return renderer;
        }
    }
}
