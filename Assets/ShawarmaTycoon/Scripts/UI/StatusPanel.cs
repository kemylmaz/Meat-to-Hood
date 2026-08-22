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
            // A small order ticket under the shop sign: transient information is
            // visually secondary to the brand and the player's money.
            Image panel = UIFactory.Panel("StatusPanel", parent, UITheme.CounterPaper);
            UIFactory.Anchor(panel.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -128f), new Vector2(400f, 58f));
            UIFactory.AddCartoonFinish(panel, 2f, 5f);

            StatusPanel status = panel.gameObject.AddComponent<StatusPanel>();
            status.root = panel.rectTransform;

            status.rushLabel = UIFactory.Label("Rush", panel.transform, "RUSH --",
                18, UITheme.Terracotta, TextAnchor.MiddleLeft);
            UIFactory.Anchor(status.rushLabel.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(-98f, 0f), new Vector2(174f, 42f));

            status.comboLabel = UIFactory.Label("Combo", panel.transform, "KOMBO 0",
                15, UITheme.InkSoft, TextAnchor.MiddleRight);
            UIFactory.Anchor(status.comboLabel.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(91f, -3f), new Vector2(184f, 38f));

            Image divider = UIFactory.Panel("Divider", panel.transform, UITheme.Cream, false);
            UIFactory.Anchor(divider.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 0f), new Vector2(2f, 34f));
            divider.raycastTarget = false;

            Image track = UIFactory.Panel("ComboTrack", panel.transform, UITheme.Cream);
            UIFactory.Anchor(track.rectTransform, UIFactory.BottomRight, UIFactory.BottomRight,
                new Vector2(-17f, 7f), new Vector2(168f, 6f));
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
                ? $"  •  İT {ReputationSystem.Instance.Score:0}"
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
