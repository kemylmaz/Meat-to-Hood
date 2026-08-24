using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Stand on it and the money drains out of the wallet into whatever the pad
    /// buys, with a green bar showing how far the payment has got.
    ///
    /// A pad writes only the concrete object name and its price on the ground.
    /// The old pads popped abstract captions the moment you walked past -
    /// "GENİŞLET", "ISCI LV.2", "TAKEAWAY" - so you had to decode a menu label
    /// instead of seeing "Masa" beside a floating table preview.
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
        private float nextPaymentFeedback;

        // A pad remains in the world so it can watch its prerequisite, but its
        // renderers and original colliders stay hidden until that prerequisite
        // is met. This makes the shop reveal its next decision instead of
        // covering the opening floor with every late-game purchase at once.
        private Func<bool> availabilityCondition;
        private bool available = true;
        private bool availabilityInitialized;
        private Renderer[] gatedRenderers = Array.Empty<Renderer>();
        private Collider[] gatedColliders = Array.Empty<Collider>();
        private bool[] gatedColliderStates = Array.Empty<bool>();

        private WorldCashMarker priceMarker;
        private FeedbackPulse gaugePulse;
        private int shownPrice = -1;

        public int Level => level;
        public bool SoldOut => costs == null || level >= costs.Length;
        public int CurrentCost => SoldOut ? 0 : costs[level];
        public int Remaining => Mathf.Max(0, CurrentCost - paid);
        public bool IsAvailable => available;

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
            Action<int, bool> levelBought)
        {
            player = playerTransform;
            saveKey = string.IsNullOrWhiteSpace(progressKey) ? "pad." + name : progressKey;
            costs = levelCosts != null && levelCosts.Length > 0 ? levelCosts : new[] { 100 };
            onLevelBought = levelBought;

            level = Mathf.Clamp(GameProgress.GetInt(saveKey, 0), 0, costs.Length);
            paid = Mathf.Clamp(GameProgress.GetInt(saveKey + ".paid", 0), 0, CurrentCost);
            UpgradeProgress.Register(saveKey, costs.Length, () => level);

            BuildVisuals();
            for (int owned = 1; owned <= level; owned++)
                onLevelBought?.Invoke(owned, true);

            if (SoldOut) gameObject.SetActive(false);
            else Refresh();
        }

        /// <summary>
        /// Keeps this purchase hidden until an earlier shop milestone is owned.
        /// The condition is checked at runtime, so the next pad appears immediately
        /// after the purchase that unlocks it and restored saves rebuild correctly.
        /// </summary>
        public void SetAvailability(Func<bool> condition)
        {
            availabilityCondition = condition;
            gatedRenderers = GetComponentsInChildren<Renderer>(true);
            gatedColliders = GetComponentsInChildren<Collider>(true);
            gatedColliderStates = new bool[gatedColliders.Length];
            for (int i = 0; i < gatedColliders.Length; i++)
                gatedColliderStates[i] = gatedColliders[i] != null && gatedColliders[i].enabled;

            EvaluateAvailability(false);
        }

        private void EvaluateAvailability(bool celebrate)
        {
            bool shouldBeAvailable = availabilityCondition == null || availabilityCondition();
            if (availabilityInitialized && shouldBeAvailable == available) return;

            bool wasAvailable = availabilityInitialized && available;
            availabilityInitialized = true;
            available = shouldBeAvailable;

            for (int i = 0; i < gatedRenderers.Length; i++)
                if (gatedRenderers[i] != null) gatedRenderers[i].enabled = available;
            for (int i = 0; i < gatedColliders.Length; i++)
                if (gatedColliders[i] != null)
                    gatedColliders[i].enabled = available && gatedColliderStates[i];

            if (available && !wasAvailable && celebrate)
            {
                UnlockCelebration.Spawn(transform.position + Vector3.up * 0.25f);
                AudioDirector.Play(GameSfx.Unlock, 0.7f);
            }
        }

        /// <summary>
        /// The pad is negative space framed by four white ground corners. Its
        /// object name, banknote and price lie directly on the floor like the
        /// reference; there is deliberately no circular base or receipt card.
        /// The floating unlock preview is built separately below.
        /// </summary>
        private void BuildVisuals()
        {
            priceMarker = WorldCashMarker.CreatePurchase(transform, FriendlyName(name));
            gaugePulse = priceMarker.gameObject.AddComponent<FeedbackPulse>();
        }

        private static string FriendlyName(string padName)
        {
            if (string.IsNullOrWhiteSpace(padName)) return "Yükseltme";
            string label = padName.EndsWith(" Pedi", StringComparison.Ordinal)
                ? padName[..^5]
                : padName;
            return label == "Masa Ekle" ? "Masa" : label;
        }

        /// <summary>
        /// Floats a pocket-sized version of the unlock above the pad. This is a
        /// showroom piece, not a collider or a second gameplay object.
        /// </summary>
        public void SetPreview(
            string assetId, Vector3 targetSize, float yaw = 0f,
            Vector3? localPosition = null)
        {
            if (SoldOut || string.IsNullOrEmpty(assetId)) return;

            GameObject preview = new("Satın Alma Önizlemesi");
            preview.transform.SetParent(transform, false);
            preview.transform.localPosition = localPosition ?? new Vector3(0f, 1.15f, 0.60f);
            preview.AddComponent<PurchasePreviewBob>();

            GameObject pedestal = PrototypeVisuals.CreatePrimitive(
                "Işık Halkası", PrimitiveType.Cylinder, preview.transform,
                new Vector3(0f, -0.35f, 0f), new Vector3(0.52f, 0.025f, 0.52f),
                new Color(0.46f, 0.87f, 0.57f));
            PaymentFlyer.DisablePhysicsAndShadows(pedestal);

            GameObject model = MeshyVisuals.TryAttach(
                preview.transform, assetId, targetSize, Vector3.zero,
                new Vector3(0f, yaw, 0f));
            if (model == null)
            {
                Destroy(preview);
                return;
            }

            Material mint = PrototypeVisuals.Material(new Color(0.66f, 0.91f, 0.70f));
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = mint;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        private void Update()
        {
            EvaluateAvailability(true);
            if (!available) return;
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
            if (Time.time >= nextPaymentFeedback)
            {
                nextPaymentFeedback = Time.time + 0.17f;
                PaymentFlyer.Send(
                    player.position + Vector3.up * 1.25f,
                    transform.position + Vector3.up * 0.42f);
                AudioDirector.Play(GameSfx.Coin, 0.16f, 0.82f + progressPitch * 0.24f);
                gaugePulse?.Kick();
            }
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
            UnlockCelebration.Spawn(transform.position + Vector3.up * 0.25f);
            AudioDirector.Play(GameSfx.Unlock);
            onLevelBought?.Invoke(level, false);
            RestaurantNavigation.Instance?.Rebuild();

            if (SoldOut) gameObject.SetActive(false);
            else Refresh();
        }

        private void Refresh()
        {
            int outstanding = Remaining;
            if (priceMarker != null && outstanding != shownPrice)
            {
                shownPrice = outstanding;
                // Drain already supplies its own deliberate pulse cadence. Avoid
                // punching the card on every tiny countdown step as well.
                priceMarker.SetAmount(outstanding, false);
            }
        }

        private float progressPitch => CurrentCost <= 0 ? 0f : Mathf.Clamp01(paid / (float)CurrentCost);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = PrototypeVisuals.Green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
