using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Top strip: rush hour countdown and the live combo streak.</summary>
    public sealed class StatusPanel : MonoBehaviour
    {
        private Text rushLabel;
        private Text comboLabel;
        private Image comboFill;
        private RectTransform root;
        private float pulse;

        public static StatusPanel Create(RectTransform parent)
        {
            // Sits under the shop progress bar, which owns the very top strip.
            Image panel = UIFactory.Panel("StatusPanel", parent, UITheme.Panel);
            UIFactory.Anchor(panel.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -106f), new Vector2(470f, 118f));
            UIFactory.AddShadow(panel, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));

            StatusPanel status = panel.gameObject.AddComponent<StatusPanel>();
            status.root = panel.rectTransform;

            status.rushLabel = UIFactory.Label("Rush", panel.transform, "RUSH --",
                UITheme.FontBody, UITheme.Terracotta);
            UIFactory.Anchor(status.rushLabel.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -10f), new Vector2(440f, 42f));

            status.comboLabel = UIFactory.Label("Combo", panel.transform, "KOMBO 0",
                UITheme.FontSmall, UITheme.InkSoft);
            UIFactory.Anchor(status.comboLabel.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -52f), new Vector2(440f, 34f));

            Image track = UIFactory.Panel("ComboTrack", panel.transform, UITheme.Hex(0xE3D3B8));
            UIFactory.Anchor(track.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 14f), new Vector2(410f, 12f));
            track.raycastTarget = false;

            status.comboFill = UIFactory.Panel("ComboFill", track.transform, UITheme.Teal);
            status.comboFill.raycastTarget = false;
            RectTransform fill = status.comboFill.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            status.comboFill.type = Image.Type.Filled;
            status.comboFill.fillMethod = Image.FillMethod.Horizontal;
            status.comboFill.fillAmount = 0f;
            return status;
        }

        private void Update()
        {
            RushHourSystem rush = RushHourSystem.Instance;
            ComboSystem combo = ComboSystem.Instance;

            if (rush == null) rushLabel.text = "RUSH --";
            else if (rush.IsActive)
            {
                rushLabel.text = $"RUSH!  {Clock(rush.ActiveTimeRemaining)}   x{RushHourSystem.IncomeMultiplier:0.#}";
                rushLabel.color = UITheme.WarmRed;
                pulse += Time.deltaTime * 5f;
                float scale = 1f + Mathf.Sin(pulse) * 0.02f;
                root.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                rushLabel.text = $"RUSH  {Clock(rush.TimeUntilNextRush)}";
                rushLabel.color = UITheme.Terracotta;
                pulse = 0f;
                root.localScale = Vector3.one;
            }

            if (combo == null)
            {
                comboLabel.text = "KOMBO --";
                comboFill.fillAmount = 0f;
                return;
            }

            bool active = combo.IsActive;
            string reputation = ReputationSystem.Instance != null
                ? $"   İTİBAR {ReputationSystem.Instance.Score:0}"
                : string.Empty;
            comboLabel.text = (active
                ? $"KOMBO {combo.Streak}   x{combo.Multiplier:0.0}"
                : "KOMBO hazır") + reputation;
            comboLabel.color = active ? UITheme.Teal : UITheme.InkSoft;
            comboFill.fillAmount = active
                ? Mathf.Clamp01(combo.TimeRemaining / Mathf.Max(0.01f, combo.TimeoutSeconds))
                : 0f;
        }

        private static string Clock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
