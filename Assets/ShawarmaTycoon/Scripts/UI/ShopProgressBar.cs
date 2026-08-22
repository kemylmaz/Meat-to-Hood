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
            Image pill = UIFactory.Panel("ShopProgress", parent, UITheme.Terracotta);
            UIFactory.Anchor(pill.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -28f), new Vector2(520f, 126f));
            UIFactory.AddCartoonFinish(pill, 4f, 9f);
            pill.rectTransform.localEulerAngles = new Vector3(0f, 0f, -1.2f);

            ShopProgressBar bar = pill.gameObject.AddComponent<ShopProgressBar>();
            bar.root = pill.rectTransform;

            Text title = UIFactory.DisplayLabel("Shop Name", pill.transform, "MEAT & EAT",
                UITheme.FontLarge, UITheme.CreamLight);
            UIFactory.Anchor(title.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -5f), new Vector2(480f, 58f));

            Text caption = UIFactory.Label("Caption", pill.transform, "DÜKKÂN GELİŞİMİ",
                19, UITheme.CreamLight, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(caption.rectTransform, UIFactory.BottomLeft, UIFactory.BottomLeft,
                new Vector2(26f, 14f), new Vector2(154f, 30f));

            Image track = UIFactory.Panel("Track", pill.transform, UITheme.Ink);
            UIFactory.Anchor(track.rectTransform, UIFactory.BottomLeft, UIFactory.BottomLeft,
                new Vector2(186f, 18f), new Vector2(258f, 22f));
            track.raycastTarget = false;

            bar.fill = UIFactory.Panel("Fill", track.transform, UITheme.Mustard);
            bar.fill.raycastTarget = false;
            RectTransform fillRect = bar.fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            bar.fill.type = Image.Type.Filled;
            bar.fill.fillMethod = Image.FillMethod.Horizontal;
            bar.fill.fillAmount = 0f;

            bar.percentLabel = UIFactory.DisplayLabel("Percent", pill.transform, "0%",
                22, UITheme.CreamLight, TextAnchor.MiddleRight);
            UIFactory.Anchor(bar.percentLabel.rectTransform, UIFactory.BottomRight, UIFactory.BottomRight,
                new Vector2(-20f, 10f), new Vector2(58f, 38f));
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
