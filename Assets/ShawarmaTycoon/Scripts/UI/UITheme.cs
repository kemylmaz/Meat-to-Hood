using System;
using UnityEngine;

namespace ShawarmaTycoon.UI
{
    /// <summary>
    /// Shared "neighbourhood doner shop" palette, type and procedural shapes.
    /// The two fonts live in Resources and are OFL licensed; the builtin font is
    /// kept as a defensive fallback for stripped/test players.
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Cream = Hex(0xF3DCAC);
        public static readonly Color CreamLight = Hex(0xFFF4D6);
        public static readonly Color Terracotta = Hex(0xE15D42);
        public static readonly Color WarmRed = Hex(0xD94436);
        public static readonly Color Teal = Hex(0x69B6C5);
        public static readonly Color Mustard = Hex(0xF2BF4B);
        public static readonly Color DarkBlueGray = Hex(0x365B61);
        public static readonly Color Ink = Hex(0x38261F);
        public static readonly Color InkSoft = Hex(0x76584A);
        public static readonly Color Panel = Hex(0xFFF0C9);
        public static readonly Color Scrim = new(0.08f, 0.05f, 0.04f, 0.55f);
        public static readonly Color Green = Hex(0x5B8D4F);
        public static readonly Color DeepGreen = Hex(0x3F6940);
        public static readonly Color CounterPaper = Hex(0xFFF8E7);
        public static readonly Color DropShadow = Hex(0x6D3F2F, 0.55f);

        public const int FontHuge = 54;
        public const int FontLarge = 40;
        public const int FontBody = 30;
        public const int FontSmall = 25;

        private static Font bodyFont;
        private static Font displayFont;
        private static Sprite roundedSprite;
        private static Sprite circleSprite;
        private static Sprite ringSprite;
        private static Sprite checkSprite;
        private static Sprite crossSprite;
        private static Sprite starSprite;
        private static Sprite noteSprite;
        private static Sprite gridSprite;

        public static Color Hex(uint rgb, float alpha = 1f) => new(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            alpha);

        public static Font Font => BodyFont;

        public static Font BodyFont
        {
            get
            {
                if (bodyFont != null) return bodyFont;
                bodyFont = Resources.Load<Font>("Fonts/Nunito-Variable");
                if (bodyFont == null) bodyFont = BuiltinFont();
                return bodyFont;
            }
        }

        public static Font DisplayFont
        {
            get
            {
                if (displayFont != null) return displayFont;
                displayFont = Resources.Load<Font>("Fonts/Baloo2-Variable");
                if (displayFont == null) displayFont = BodyFont;
                return displayFont;
            }
        }

        private static Font BuiltinFont()
        {
            Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallback == null) fallback = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return fallback;
        }

        /// <summary>9-sliced rounded rectangle, so one sprite fits every panel size.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (roundedSprite != null) return roundedSprite;
                const int size = 64;
                const float radius = 20f;
                Texture2D texture = NewTexture(size, "UI Rounded");
                Color[] pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - 0.5f, 0f, x + 0.5f - (size - radius));
                    float dy = Mathf.Max(radius - y - 0.5f, 0f, y + 0.5f - (size - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                texture.SetPixels(pixels);
                texture.Apply();
                roundedSprite = Sprite.Create(
                    texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                roundedSprite.name = "UI Rounded";
                return roundedSprite;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circleSprite != null) return circleSprite;
                circleSprite = BuildCircle("UI Circle", 128, 0f);
                return circleSprite;
            }
        }

        /// <summary>Hollow circle used for the joystick base.</summary>
        public static Sprite Ring
        {
            get
            {
                if (ringSprite != null) return ringSprite;
                ringSprite = BuildCircle("UI Ring", 128, 0.80f);
                return ringSprite;
            }
        }

        // ---- button icons ---------------------------------------------------
        //
        // These used to be text: "✓", "★", "♪", "▦". The two fonts the game
        // ships are Latin text faces and carry none of those codepoints, so in
        // the player the buttons came up bare. The editor hid it, because there
        // Unity falls back to a Windows font for glyphs a font is missing; the
        // web build has nothing to fall back to. Drawn here instead, they cost
        // no download and cannot depend on what a platform happens to install.

        /// <summary>Tick, for the daily tasks tab.</summary>
        public static Sprite Check
        {
            get
            {
                if (checkSprite != null) return checkSprite;
                checkSprite = BuildIcon("UI Check", p =>
                    OnSegment(p, new Vector2(0.18f, 0.55f), new Vector2(0.41f, 0.28f), 0.085f) ||
                    OnSegment(p, new Vector2(0.41f, 0.28f), new Vector2(0.82f, 0.75f), 0.085f));
                return checkSprite;
            }
        }

        /// <summary>Diagonal cross: closes a panel, and marks the sound as off.</summary>
        public static Sprite Cross
        {
            get
            {
                if (crossSprite != null) return crossSprite;
                crossSprite = BuildIcon("UI Cross", p =>
                    OnSegment(p, new Vector2(0.24f, 0.24f), new Vector2(0.76f, 0.76f), 0.085f) ||
                    OnSegment(p, new Vector2(0.24f, 0.76f), new Vector2(0.76f, 0.24f), 0.085f));
                return crossSprite;
            }
        }

        /// <summary>Five-pointed star, for the records tab.</summary>
        public static Sprite Star
        {
            get
            {
                if (starSprite != null) return starSprite;
                starSprite = BuildIcon("UI Star", p => InPolygon(p, StarPoints));
                return starSprite;
            }
        }

        /// <summary>Quaver, for the sound toggle.</summary>
        public static Sprite Note
        {
            get
            {
                if (noteSprite != null) return noteSprite;
                noteSprite = BuildIcon("UI Note", p =>
                    InEllipse(p, new Vector2(0.35f, 0.27f), 0.19f, 0.15f) ||
                    OnSegment(p, new Vector2(0.53f, 0.25f), new Vector2(0.53f, 0.80f), 0.045f) ||
                    OnSegment(p, new Vector2(0.53f, 0.80f), new Vector2(0.80f, 0.63f), 0.058f));
                return noteSprite;
            }
        }

        /// <summary>Two-by-two grid: the floor plan behind build mode.</summary>
        public static Sprite Grid
        {
            get
            {
                if (gridSprite != null) return gridSprite;
                const float lo = 0.19f;
                const float hi = 0.81f;
                const float mid = 0.5f;
                const float half = 0.045f;
                gridSprite = BuildIcon("UI Grid", p =>
                    OnSegment(p, new Vector2(lo, lo), new Vector2(hi, lo), half) ||
                    OnSegment(p, new Vector2(lo, hi), new Vector2(hi, hi), half) ||
                    OnSegment(p, new Vector2(lo, lo), new Vector2(lo, hi), half) ||
                    OnSegment(p, new Vector2(hi, lo), new Vector2(hi, hi), half) ||
                    OnSegment(p, new Vector2(mid, lo), new Vector2(mid, hi), half) ||
                    OnSegment(p, new Vector2(lo, mid), new Vector2(hi, mid), half));
                return gridSprite;
            }
        }

        private static readonly Vector2[] StarPoints = BuildStar(new Vector2(0.5f, 0.52f), 0.46f, 0.20f);

        private static Vector2[] BuildStar(Vector2 center, float outer, float inner)
        {
            Vector2[] points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                // Start at the top so the star sits upright.
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        /// <summary>
        /// Rasterises a shape from a coverage test, three by three per pixel.
        /// The circle above can solve its own edge from a distance, but strokes
        /// and polygons cannot, and supersampling lets every icon be written as
        /// one plain description of where the ink goes.
        /// </summary>
        private static Sprite BuildIcon(string name, Func<Vector2, bool> inside, int size = 128)
        {
            const int samples = 3;
            Texture2D texture = NewTexture(size, name);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    Vector2 point = new(
                        (x + (sx + 0.5f) / samples) / size,
                        (y + (sy + 0.5f) / samples) / size);
                    if (inside(point)) hits++;
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, hits / (float)(samples * samples));
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static bool OnSegment(Vector2 point, Vector2 a, Vector2 b, float halfWidth)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(point, a + ab * t) <= halfWidth;
        }

        private static bool InEllipse(Vector2 point, Vector2 center, float radiusX, float radiusY)
        {
            float dx = (point.x - center.x) / radiusX;
            float dy = (point.y - center.y) / radiusY;
            return dx * dx + dy * dy <= 1f;
        }

        private static bool InPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].y > point.y == polygon[j].y > point.y) continue;
                float x = (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                    / (polygon[j].y - polygon[i].y) + polygon[i].x;
                if (point.x < x) inside = !inside;
            }
            return inside;
        }

        private static Sprite BuildCircle(string name, int size, float innerRatio)
        {
            Texture2D texture = NewTexture(size, name);
            Color[] pixels = new Color[size * size];
            float outer = size * 0.5f - 1f;
            float inner = outer * innerRatio;
            Vector2 center = new(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(outer - distance + 0.5f);
                if (innerRatio > 0f)
                    alpha = Mathf.Min(alpha, Mathf.Clamp01(distance - inner + 0.5f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}
