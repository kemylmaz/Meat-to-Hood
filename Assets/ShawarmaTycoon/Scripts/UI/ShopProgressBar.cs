using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>
    /// Top of the screen: how much of the shop has been built, as one bar that
    /// creeps toward 100% with every upgrade bought.
    ///
    /// The shop had no answer to "how far along am I" anywhere on screen. Coins
    /// say what you can afford next, not how much is left, and the pads only
    /// speak for themselves.
    /// </summary>
    public sealed class ShopProgressBar : MonoBehaviour
    {
        /// <summary>Seconds the fill takes to catch up, so a purchase is felt.</summary>
        private const float FillCatchUpRate = 0.55f;

        private Image fill;
        private Text percentLabel;
        private RectTransform root;
        private float displayed;
        private float target;
        private float punch;

        public static ShopProgressBar Create(RectTransform parent)
        {
            Image pill = UIFactory.Panel("ShopProgress", parent, UITheme.Panel);
            UIFactory.Anchor(pill.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -24f), new Vector2(470f, 74f));
            UIFactory.AddShadow(pill, new Color(0f, 0f, 0f, 0.18f), new Vector2(0f, -4f));

            ShopProgressBar bar = pill.gameObject.AddComponent<ShopProgressBar>();
            bar.root = pill.rectTransform;

            Image badge = UIFactory.Icon("Badge", pill.transform, UITheme.Circle, UITheme.Terracotta);
            UIFactory.Anchor(badge.rectTransform, new Vector2(0f, 0.5f), UIFactory.Center,
                new Vector2(42f, 0f), new Vector2(52f, 52f));

            Image track = UIFactory.Panel("Track", pill.transform, UITheme.Hex(0xE3D3B8));
            UIFactory.Anchor(track.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(24f, 0f), new Vector2(330f, 30f));
            track.raycastTarget = false;

            bar.fill = UIFactory.Panel("Fill", track.transform, UITheme.Green);
            bar.fill.raycastTarget = false;
            RectTransform fillRect = bar.fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            bar.fill.type = Image.Type.Filled;
            bar.fill.fillMethod = Image.FillMethod.Horizontal;
            bar.fill.fillAmount = 0f;

            bar.percentLabel = UIFactory.Label("Percent", track.transform, "0%",
                UITheme.FontSmall, UITheme.Ink);
            UIFactory.Stretch(bar.percentLabel.rectTransform);
            return bar;
        }

        private void OnEnable()
        {
            UpgradeProgress.Changed += OnProgressChanged;
            SnapToCurrent();
        }

        /// <summary>
        /// AddComponent runs OnEnable before Create has finished assigning the
        /// widgets, so the first sync happens again here, once they exist. The bar
        /// starts at whatever the save already owns rather than sweeping up from
        /// zero every time the shop is opened.
        /// </summary>
        private void Start() => SnapToCurrent();

        private void SnapToCurrent()
        {
            if (fill == null || percentLabel == null) return;
            target = UpgradeProgress.Ratio;
            displayed = target;
            Render();
        }

        private void OnDisable() => UpgradeProgress.Changed -= OnProgressChanged;

        private void OnProgressChanged()
        {
            float next = UpgradeProgress.Ratio;
            // Only a step forward punches. Registration during the bootstrap grows
            // the denominator, which walks the ratio backwards, and that is not
            // something to celebrate.
            if (next > target + 0.0001f) punch = 1f;
            target = next;
        }

        private void Update()
        {
            if (fill == null) return;

            if (!Mathf.Approximately(displayed, target))
            {
                displayed = Mathf.MoveTowards(displayed, target, FillCatchUpRate * Time.deltaTime);
                Render();
            }

            if (punch <= 0f) return;
            punch = Mathf.Max(0f, punch - Time.deltaTime * 3f);
            float scale = 1f + Mathf.Sin(punch * Mathf.PI) * 0.05f;
            root.localScale = new Vector3(scale, scale, 1f);
        }

        private void Render()
        {
            fill.fillAmount = displayed;
            // Rounded down, so it only says 100% when the last thing is bought.
            percentLabel.text = Mathf.FloorToInt(displayed * 100f) + "%";
        }
    }
}
