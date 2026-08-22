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
