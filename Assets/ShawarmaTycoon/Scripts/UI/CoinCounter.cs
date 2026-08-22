using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Coin readout that rolls up to new values and punches on income.</summary>
    public sealed class CoinCounter : MonoBehaviour
    {
        private Text valueLabel;
        private RectTransform pill;
        private GameEconomy source;
        private float displayed;
        private int target;
        private float punch;

        public static CoinCounter Create(RectTransform parent)
        {
            Image pill = UIFactory.Panel("CoinPill", parent, UITheme.Mustard);
            UIFactory.Anchor(pill.rectTransform, UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-28f, -32f), new Vector2(238f, 86f));
            UIFactory.AddCartoonFinish(pill, 3f, 7f);

            Image coin = UIFactory.Icon("Coin", pill.transform, UITheme.Circle, UITheme.CreamLight);
            UIFactory.Anchor(coin.rectTransform, new Vector2(0f, 0.5f), UIFactory.Center,
                new Vector2(46f, 0f), new Vector2(54f, 54f));
            Text currency = UIFactory.DisplayLabel("Currency", coin.transform, "₺", 34, UITheme.Ink);
            UIFactory.Stretch(currency.rectTransform);

            Text caption = UIFactory.Label("Caption", pill.transform, "KASA",
                17, UITheme.InkSoft, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Anchor(caption.rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(82f, -9f), new Vector2(120f, 25f));

            CoinCounter counter = pill.gameObject.AddComponent<CoinCounter>();
            counter.pill = pill.rectTransform;
            counter.valueLabel = UIFactory.DisplayLabel("Value", pill.transform, "0",
                38, UITheme.Ink, TextAnchor.MiddleRight);
            UIFactory.Anchor(counter.valueLabel.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(24f, 12f), new Vector2(160f, 54f));
            return counter;
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (source == null) return;
            source.CoinsChanged -= OnCoinsChanged;
            source = null;
        }

        private void OnCoinsChanged(int coins)
        {
            if (coins > target) punch = 1f;
            target = coins;
        }

        private void Update()
        {
            if (valueLabel == null) return;

            // The economy singleton is created during the same bootstrap frame as
            // the HUD, so bind to it lazily.
            if (source == null && GameEconomy.Instance != null)
            {
                source = GameEconomy.Instance;
                source.CoinsChanged += OnCoinsChanged;
                target = source.Coins;
                displayed = target;
                Render();
            }

            if (!Mathf.Approximately(displayed, target))
            {
                float step = Mathf.Max(Mathf.Abs(target - displayed) * 6f, 12f) * Time.deltaTime;
                displayed = Mathf.MoveTowards(displayed, target, step);
                Render();
            }

            if (punch > 0f)
            {
                punch = Mathf.Max(0f, punch - Time.deltaTime * 4.5f);
                float scale = 1f + Mathf.Sin(punch * Mathf.PI) * 0.14f;
                pill.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void Render() => valueLabel.text = Mathf.RoundToInt(displayed).ToString();
    }
}
