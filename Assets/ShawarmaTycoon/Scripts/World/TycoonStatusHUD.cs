using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Compact safe-area status for the two time-sensitive tycoon systems.</summary>
    public sealed class TycoonStatusHUD : MonoBehaviour
    {
        private const float CoinHudBottom = 76f;

        private GUIStyle panelStyle;
        private GUIStyle rushStyle;
        private GUIStyle comboStyle;
        private Texture2D panelTexture;
        private int styledFontSize = -1;

        private void OnGUI()
        {
            RushHourSystem rush = RushHourSystem.Instance;
            ComboSystem combo = ComboSystem.Instance;
            if (rush == null && combo == null)
                return;

            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);

            float scale = Mathf.Clamp(safeArea.width / 720f, 0.76f, 1.15f);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(15f * scale), 12, 18);
            EnsureStyles(fontSize);

            float width = Mathf.Clamp(safeArea.width * 0.64f, 220f * scale, 380f * scale);
            width = Mathf.Min(width, safeArea.width - 16f);
            float height = Mathf.Clamp(66f * scale, 54f, 76f);

            // Screen.safeArea uses a bottom-left origin; IMGUI uses a top-left origin.
            float safeTop = Screen.height - safeArea.yMax;
            float safeBottom = Screen.height - safeArea.yMin;
            float x = safeArea.xMin + (safeArea.width - width) * 0.5f;
            float y = Mathf.Max(safeTop + 8f * scale, CoinHudBottom + 8f);
            y = Mathf.Min(y, safeBottom - height - 8f);

            Rect panel = new(x, y, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            float lineHeight = height * 0.5f;
            Rect rushLine = new(panel.x + 8f, panel.y + 2f, panel.width - 16f, lineHeight - 2f);
            Rect comboLine = new(panel.x + 8f, panel.y + lineHeight - 1f, panel.width - 16f, lineHeight - 2f);

            rushStyle.normal.textColor = rush != null && rush.IsActive
                ? new Color(1f, 0.52f, 0.13f)
                : new Color(0.57f, 0.42f, 0.31f);
            comboStyle.normal.textColor = combo != null && combo.IsActive
                ? new Color(0.18f, 0.76f, 0.68f)
                : new Color(0.57f, 0.42f, 0.31f);

            GUI.Label(rushLine, RushText(rush), rushStyle);
            GUI.Label(comboLine, ComboText(combo), comboStyle);
        }

        private void EnsureStyles(int fontSize)
        {
            if (panelTexture == null)
            {
                panelTexture = new Texture2D(1, 1)
                {
                    hideFlags = HideFlags.DontSave,
                    name = "Tycoon Status Background"
                };
                panelTexture.SetPixel(0, 0, new Color(0.16f, 0.11f, 0.09f, 0.90f));
                panelTexture.Apply();
            }

            if (styledFontSize == fontSize && panelStyle != null)
                return;

            styledFontSize = fontSize;
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panelTexture },
                padding = new RectOffset(8, 8, 4, 4)
            };
            rushStyle = CreateLineStyle(fontSize);
            comboStyle = CreateLineStyle(fontSize);
        }

        private static GUIStyle CreateLineStyle(int fontSize)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
        }

        private static string RushText(RushHourSystem rush)
        {
            if (rush == null)
                return "RUSH: --";

            if (rush.IsActive)
                return $"RUSH! {FormatClock(rush.ActiveTimeRemaining)}  x{RushHourSystem.IncomeMultiplier:0.#}";

            return $"RUSH: {FormatClock(rush.TimeUntilNextRush)}";
        }

        private static string ComboText(ComboSystem combo)
        {
            if (combo == null)
                return "KOMBO: --";

            return $"KOMBO {combo.Streak}  x{combo.Multiplier:0.0}  {combo.TimeRemaining:0.0}s";
        }

        private static string FormatClock(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
                Destroy(panelTexture);
        }
    }
}
