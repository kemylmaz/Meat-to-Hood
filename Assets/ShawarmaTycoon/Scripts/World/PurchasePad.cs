using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Stand on it and the money drains out of the wallet into whatever the pad
    /// buys, with a green bar showing how far the payment has got.
    ///
    /// The price is the only thing written on a pad. The old pads popped a
    /// caption the moment you walked past - "GENİŞLET", "ISCI LV.2", "TAKEAWAY" -
    /// so you had to read a word to find out what the pad beside an empty table
    /// slot was for. Standing it next to the thing it builds says that already.
    /// </summary>
    public sealed class PurchasePad : MonoBehaviour
    {
        /// <summary>Seconds of standing to pay off one level, whatever it costs.</summary>
        private const float DrainSeconds = 1.45f;

        /// <summary>Slowest drain, so a cheap pad still reads as a payment.</summary>
        private const float MinimumDrainRate = 25f;

        /// <summary>How often coins actually leave the wallet; this is the ticking.</summary>
        private const float TickInterval = 0.085f;

        /// <summary>
        /// Kept tight. Pads stand a couple of metres apart beside what they build,
        /// and a wide catch meant standing between two of them drained coins into
        /// both at once.
        /// </summary>
        [SerializeField, Min(0.2f)] private float radius = 0.85f;

        /// <summary>
        /// How long the player has to have stopped before a pad starts taking
        /// money, and how far they may drift and still count as stopped.
        ///
        /// Pads sit beside the counters, which is the route the player walks all
        /// day. Charging on proximity alone meant every trip across the kitchen
        /// paid something into whatever it passed, and coins could never be saved
        /// for the thing the player was actually heading to.
        /// </summary>
        private const float SettleSeconds = 0.3f;
        private const float SettleTolerance = 0.06f;

        private Transform player;
        private int[] costs;
        private Action<int, bool> onLevelBought;
        private string saveKey;
        private int level;
        private int paid;
        private float tickTimer;
        private float carry;
        private float stillTimer;
        private Vector3 lastPlayerPosition;

        private Transform barFill;
        private TextMesh priceLabel;
        private int shownPrice = -1;
        private float shownProgress = -1f;

        public int Level => level;
        public bool SoldOut => costs == null || level >= costs.Length;
        public int CurrentCost => SoldOut ? 0 : costs[level];
        public int Remaining => Mathf.Max(0, CurrentCost - paid);

        /// <summary>
        /// Wires the pad to what it builds. <paramref name="onLevelBought"/> runs
        /// once per owned level: on a fresh purchase with restored = false, and
        /// once per already-owned level at load with restored = true, so callers
        /// can rebuild saved state without replaying the coin burst.
        /// </summary>
        public void Configure(
            Transform playerTransform,
            string progressKey,
            int[] levelCosts,
            Action<int, bool> levelBought,
            string padAsset = "19_upgrade_pad")
        {
            player = playerTransform;
            saveKey = string.IsNullOrWhiteSpace(progressKey) ? "pad." + name : progressKey;
            costs = levelCosts != null && levelCosts.Length > 0 ? levelCosts : new[] { 100 };
            onLevelBought = levelBought;

            level = Mathf.Clamp(GameProgress.GetInt(saveKey, 0), 0, costs.Length);
            paid = Mathf.Clamp(GameProgress.GetInt(saveKey + ".paid", 0), 0, CurrentCost);
            UpgradeProgress.Register(saveKey, costs.Length, () => level);

            BuildVisuals(padAsset);
            for (int owned = 1; owned <= level; owned++)
                onLevelBought?.Invoke(owned, true);

            if (SoldOut) gameObject.SetActive(false);
            else Refresh();
        }

        /// <summary>
        /// The pad, and a small sign standing over it carrying the price and the
        /// progress bar. Both are on one plate tilted to face the isometric rig:
        /// left flat they read as a smear across the floor, and at the size world
        /// labels use elsewhere the number was wider than the pad it belonged to.
        /// </summary>
        private void BuildVisuals(string padAsset)
        {
            GameObject placeholder = PrototypeVisuals.CreatePrimitive(
                "Pad Surface", PrimitiveType.Cylinder, transform,
                Vector3.zero, new Vector3(0.76f, 0.05f, 0.76f), new Color(0.95f, 0.80f, 0.32f));
            if (!string.IsNullOrEmpty(padAsset))
            {
                MeshyVisuals.TryReplaceDirect(
                    transform, padAsset, new Vector3(1.02f, 0.30f, 1.02f),
                    Vector3.down * 0.03f, Vector3.zero, false, placeholder.name);
            }

            GameObject gauge = new("Gösterge");
            gauge.transform.SetParent(transform, false);
            gauge.transform.localPosition = Vector3.up * 0.76f;
            gauge.transform.localEulerAngles = new Vector3(55f, 0f, 0f);

            PrototypeVisuals.CreatePrimitive(
                "Plaka", PrimitiveType.Cube, gauge.transform,
                Vector3.zero, new Vector3(0.86f, 0.42f, 0.04f), PrototypeVisuals.Cream);

            PrototypeVisuals.CreatePrimitive(
                "Bar Track", PrimitiveType.Cube, gauge.transform,
                new Vector3(0f, -0.13f, -0.03f), new Vector3(0.74f, 0.10f, 0.03f),
                new Color(0.30f, 0.25f, 0.22f));

            // Pivoted at the left end of the track, so scaling the pivot along X
            // grows the bar rightwards instead of out of both ends at once.
            GameObject fillPivot = new("Bar Fill");
            fillPivot.transform.SetParent(gauge.transform, false);
            fillPivot.transform.localPosition = new Vector3(-0.35f, -0.13f, -0.045f);
            barFill = fillPivot.transform;
            PrototypeVisuals.CreatePrimitive(
                "Bar Fill Body", PrimitiveType.Cube, fillPivot.transform,
                new Vector3(0.35f, 0f, 0f), new Vector3(0.70f, 0.075f, 0.03f),
                PrototypeVisuals.Green);

            // Built here rather than through PrototypeVisuals.CreateLabel: that
            // helper applies the camera tilt itself, and on a plate that is already
            // tilted it would be applied twice.
            GameObject labelObject = new("Fiyat");
            labelObject.transform.SetParent(gauge.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.06f, -0.05f);
            priceLabel = labelObject.AddComponent<TextMesh>();
            priceLabel.anchor = TextAnchor.MiddleCenter;
            priceLabel.alignment = TextAlignment.Center;
            priceLabel.characterSize = 0.045f;
            priceLabel.fontSize = 64;
            priceLabel.color = new Color(0.14f, 0.40f, 0.19f);
            priceLabel.fontStyle = FontStyle.Bold;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (SoldOut || player == null) return;

            bool onPad =
                Vector3.SqrMagnitude(player.position - transform.position) <= radius * radius;
            Vector3 position = player.position;
            bool moved = (position - lastPlayerPosition).sqrMagnitude >
                SettleTolerance * SettleTolerance;
            lastPlayerPosition = position;

            if (!onPad || moved)
            {
                stillTimer = 0f;
                tickTimer = 0f;
                carry = 0f;
                return;
            }

            stillTimer += Time.deltaTime;
            if (stillTimer < SettleSeconds) return;

            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = TickInterval;
            Drain();
        }

        /// <summary>
        /// Moves one tick's worth of coins onto the pad. Payment is kept rather
        /// than refunded when the player steps off, so an expensive pad can be
        /// paid across several trips instead of demanding the full price at once.
        /// </summary>
        private void Drain()
        {
            GameEconomy economy = GameEconomy.Instance;
            if (economy == null || economy.Coins <= 0) return;

            float rate = Mathf.Max(MinimumDrainRate, CurrentCost / DrainSeconds);
            carry += rate * TickInterval;
            int step = Mathf.FloorToInt(carry);
            if (step <= 0) return;
            carry -= step;

            step = Mathf.Min(step, Remaining);
            step = Mathf.Min(step, economy.Coins);
            if (step <= 0 || !economy.TrySpend(step)) return;

            paid += step;
            if (paid < CurrentCost)
            {
                GameProgress.SetInt(saveKey + ".paid", paid);
                Refresh();
                return;
            }

            CompleteLevel();
        }

        private void CompleteLevel()
        {
            int price = CurrentCost;
            level++;
            paid = 0;
            carry = 0f;
            GameProgress.SetInt(saveKey, level);
            GameProgress.SetInt(saveKey + ".paid", 0);
            GameProgress.RecordUpgrade();
            UpgradeProgress.NotifyChanged();
            CoinBurst.Spawn(transform.position + Vector3.up * 0.7f, price);
            AudioDirector.Play(GameSfx.Unlock);
            onLevelBought?.Invoke(level, false);
            RestaurantNavigation.Instance?.Rebuild();

            if (SoldOut) gameObject.SetActive(false);
            else Refresh();
        }

        private void Refresh()
        {
            float progress = CurrentCost <= 0 ? 0f : Mathf.Clamp01(paid / (float)CurrentCost);
            if (barFill != null && !Mathf.Approximately(progress, shownProgress))
            {
                shownProgress = progress;
                barFill.localScale = new Vector3(Mathf.Max(0.0001f, progress), 1f, 1f);
            }

            int outstanding = Remaining;
            if (priceLabel != null && outstanding != shownPrice)
            {
                shownPrice = outstanding;
                priceLabel.text = "$" + outstanding;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = PrototypeVisuals.Green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
