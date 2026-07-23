using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public enum SpriteShape
    {
        Square,
        Circle
    }

    public static class PlaceholderFactory
    {
        private const int TextureSize = 64;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite GetSprite(SpriteShape shape, Vector2 pivot)
        {
            string key = $"{shape}_{pivot.x:0.00}_{pivot.y:0.00}";
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite created = Build(shape, pivot);
            Cache[key] = created;
            return created;
        }

        public static void Configure(SpriteRenderer renderer, Sprite artwork, Color fallbackTint, SpriteShape shape, Vector2 pivot)
        {
            if (renderer == null)
            {
                return;
            }

            if (artwork != null)
            {
                renderer.sprite = artwork;
                renderer.color = Color.white;
            }
            else
            {
                renderer.sprite = GetSprite(shape, pivot);
                renderer.color = fallbackTint;
            }
        }

        private static Sprite Build(SpriteShape shape, Vector2 pivot)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = (TextureSize - 1) * 0.5f;
            float radius = TextureSize * 0.5f - 1f;
            Color fill = Color.white;
            Color border = new Color(0.72f, 0.72f, 0.72f, 1f);
            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    bool inside;
                    bool edge;

                    if (shape == SpriteShape.Circle)
                    {
                        float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                        inside = dist <= radius;
                        edge = dist > radius - 3f && dist <= radius;
                    }
                    else
                    {
                        inside = true;
                        edge = x < 3 || y < 3 || x >= TextureSize - 3 || y >= TextureSize - 3;
                    }

                    Color pixel = inside ? (edge ? border : fill) : clear;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), pivot, TextureSize);
        }
    }
}
