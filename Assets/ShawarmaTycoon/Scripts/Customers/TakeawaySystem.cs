using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// A compact, single-order takeaway window. Wraps are handed over by the
    /// player or runner; completed-order cash remains on its pad until collected.
    /// </summary>
    public sealed class TakeawaySystem : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float firstOrderDelay = 5f;
        [SerializeField, Min(1f)] private float minimumOrderInterval = 8f;
        [SerializeField, Min(1f)] private float maximumOrderInterval = 12f;
        [SerializeField, Min(1f)] private float lateAfterSeconds = 15f;
        /// <summary>How long a late order waits before the customer gives up on it.</summary>
        [SerializeField, Min(2f)] private float expireAfterSeconds = 26f;
        [SerializeField, Min(0.5f)] private float interactionRadius = 1.55f;
        [SerializeField, Min(1)] private int basePayout = 26;

        private Transform player;
        private CarryInventory inventory;
        private TextMesh orderLabel;
        private TextMesh cashLabel;
        private GameObject bagVisual;
        private GameObject orderLight;
        private GameObject cashPad;
        private GameObject cashStack;
        private float orderTimer;
        private float orderAge;
        private float transferCooldown;
        private bool pendingOrder;
        private int pendingCash;
        private bool visualsBuilt;

        public bool NeedsWrap => pendingOrder;
        public bool PendingOrder => pendingOrder;
        public int PendingCash => pendingCash;
        public bool IsLate => pendingOrder && orderAge >= lateAfterSeconds;

        public void Configure(Transform playerTransform, CarryInventory playerInventory)
        {
            player = playerTransform;
            inventory = playerInventory;
            BuildVisuals();
            orderTimer = firstOrderDelay;
            UpdateVisuals();
        }

        public bool CreateOrderNow()
        {
            if (pendingOrder)
                return false;

            pendingOrder = true;
            orderAge = 0f;
            orderTimer = 0f;
            UpdateVisuals();
            return true;
        }

        public bool TryReceiveWrap(ItemType item, bool byWorker)
        {
            if (!pendingOrder || item != ItemType.Wrap)
                return false;

            FulfillOrder(byWorker);
            return true;
        }

        public bool TryAutoCollectCash()
        {
            return CollectCash(true);
        }

        private void Update()
        {
            transferCooldown -= Time.deltaTime;

            if (pendingOrder)
            {
                bool wasLate = IsLate;
                orderAge += Time.deltaTime;
                if (wasLate != IsLate)
                    UpdateVisuals();

                if (orderAge >= expireAfterSeconds)
                    ExpireOrder();
                else
                    TryPlayerDelivery();
            }
            else
            {
                orderTimer -= Time.deltaTime;
                if (orderTimer <= 0f)
                    CreateOrderNow();
            }

            TryPlayerCashCollection();
        }

        private void TryPlayerDelivery()
        {
            if (player == null || inventory == null || transferCooldown > 0f)
                return;
            if (Vector3.SqrMagnitude(player.position - transform.position) > interactionRadius * interactionRadius)
                return;
            if (inventory.HeldType != ItemType.Wrap || !inventory.TryRemove(ItemType.Wrap))
                return;

            transferCooldown = 0.20f;
            FulfillOrder(false);
        }

        private void FulfillOrder(bool byWorker)
        {
            bool fast = !IsLate;
            if (fast)
                ComboSystem.Instance?.RegisterTakeaway(byWorker);
            else
                ComboSystem.Instance?.BreakCombo();

            float serviceMultiplier = fast ? 1f : 0.60f;
            int payout = RewardCalculator.Calculate(basePayout, serviceMultiplier);
            pendingCash += payout;
            ClearOrder();

            GameProgress.RecordServed(vip: false, takeaway: true);
            UpdateVisuals();
        }

        /// <summary>
        /// The waiting customer gives up. Without this a late order sat on the
        /// counter indefinitely and still paid out at 0.6x, so the window was
        /// free money you could collect whenever it suited you.
        /// </summary>
        private void ExpireOrder()
        {
            ClearOrder();
            ComboSystem.Instance?.BreakCombo();
            GameProgress.RecordLostCustomer();
            AudioDirector.Play(GameSfx.Error, 0.55f);
            UpdateVisuals();
        }

        private void ClearOrder()
        {
            pendingOrder = false;
            orderAge = 0f;
            orderTimer = Random.Range(
                Mathf.Min(minimumOrderInterval, maximumOrderInterval),
                Mathf.Max(minimumOrderInterval, maximumOrderInterval));
        }

        private void TryPlayerCashCollection()
        {
            if (pendingCash <= 0 || player == null || cashPad == null)
                return;
            if (Vector3.SqrMagnitude(player.position - cashPad.transform.position) > 0.95f * 0.95f)
                return;

            CollectCash(false);
        }

        private bool CollectCash(bool byWorker)
        {
            if (pendingCash <= 0 || GameEconomy.Instance == null)
                return false;

            int collected = pendingCash;
            pendingCash = 0;
            GameEconomy.Instance.AddCoins(collected);
            GameProgress.RecordRevenue(collected);
            if (byWorker) ComboSystem.Instance?.RegisterWorkerAction();
            else ComboSystem.Instance?.RegisterManualAction();
            Vector3 feedbackPosition = cashPad != null
                ? cashPad.transform.position + Vector3.up * 0.35f
                : transform.position + Vector3.up;
            CoinBurst.SpawnGain(feedbackPosition, collected);
            UpdateVisuals();
            return true;
        }

        private void BuildVisuals()
        {
            if (visualsBuilt)
                return;
            visualsBuilt = true;

            Color body = new(0.82f, 0.40f, 0.24f);
            Color teal = new(0.20f, 0.62f, 0.58f);
            Color mustard = new(0.95f, 0.70f, 0.24f);

            PrototypeVisuals.CreatePrimitive(
                "Takeaway Counter Body", PrimitiveType.Cube, transform,
                new Vector3(0f, 0.48f, 0f), new Vector3(2.10f, 0.90f, 1.25f), body,
                colliderEnabled: true);
            PrototypeVisuals.CreatePrimitive(
                "Takeaway Counter Top", PrimitiveType.Cube, transform,
                new Vector3(0f, 0.96f, 0f), new Vector3(2.20f, 0.12f, 1.34f), PrototypeVisuals.Cream);

            bagVisual = new GameObject("Takeaway Bag");
            bagVisual.transform.SetParent(transform, false);
            bagVisual.transform.localPosition = new Vector3(-0.43f, 1.12f, -0.05f);
            PrototypeVisuals.CreatePrimitive(
                "Bag Body", PrimitiveType.Cube, bagVisual.transform,
                Vector3.zero, new Vector3(0.46f, 0.50f, 0.30f), mustard);
            PrototypeVisuals.CreatePrimitive(
                "Handle Left", PrimitiveType.Cube, bagVisual.transform,
                new Vector3(-0.13f, 0.30f, 0f), new Vector3(0.055f, 0.22f, 0.055f), body);
            PrototypeVisuals.CreatePrimitive(
                "Handle Right", PrimitiveType.Cube, bagVisual.transform,
                new Vector3(0.13f, 0.30f, 0f), new Vector3(0.055f, 0.22f, 0.055f), body);

            orderLight = PrototypeVisuals.CreatePrimitive(
                "Order Light", PrimitiveType.Sphere, transform,
                new Vector3(0.43f, 1.16f, -0.05f), new Vector3(0.20f, 0.20f, 0.20f), teal);
            orderLabel = PrototypeVisuals.CreateLabel(
                "PAKET", transform, new Vector3(0f, 1.70f, 0f), 0.105f);
            orderLabel.fontStyle = FontStyle.Bold;

            cashPad = new GameObject("Takeaway Cash Pad");
            cashPad.transform.SetParent(transform, false);
            cashPad.transform.localPosition = new Vector3(0.85f, 0.02f, 0.95f);
            PrototypeVisuals.CreatePrimitive(
                "Cash Pad Surface", PrimitiveType.Cube, cashPad.transform,
                new Vector3(0f, 0.055f, 0f), new Vector3(0.70f, 0.04f, 0.46f),
                new Color(0.16f, 0.72f, 0.30f));
            cashLabel = PrototypeVisuals.CreateLabel(
                string.Empty, cashPad.transform, Vector3.up * 0.34f, 0.12f);

            cashStack = new GameObject("Takeaway Cash Stack");
            cashStack.transform.SetParent(cashPad.transform, false);
            cashStack.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            for (int i = 0; i < 4; i++)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Cash Bill", PrimitiveType.Cube, cashStack.transform,
                    new Vector3(i % 2 == 0 ? -0.03f : 0.03f, i * 0.025f, 0f),
                    new Vector3(0.58f, 0.025f, 0.34f),
                    i % 2 == 0 ? new Color(0.24f, 0.88f, 0.34f) : new Color(0.14f, 0.68f, 0.24f));
            }
        }

        private void UpdateVisuals()
        {
            if (!visualsBuilt)
                return;

            if (bagVisual != null)
                bagVisual.SetActive(pendingOrder);

            if (orderLabel != null)
            {
                orderLabel.text = pendingOrder ? (IsLate ? "GEÇ" : "DÜRÜM") : "PAKET";
                orderLabel.color = IsLate ? PrototypeVisuals.Red : new Color(0.24f, 0.15f, 0.11f);
            }

            if (orderLight != null)
            {
                Renderer renderer = orderLight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = PrototypeVisuals.Material(
                        IsLate ? PrototypeVisuals.Red : pendingOrder ? new Color(0.95f, 0.70f, 0.24f) : PrototypeVisuals.Teal);
                }
            }

            bool hasCash = pendingCash > 0;
            if (cashPad != null) cashPad.SetActive(hasCash);
            if (cashStack != null) cashStack.SetActive(hasCash);
            if (cashLabel != null) cashLabel.text = hasCash ? "+$" + pendingCash : string.Empty;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.20f, 0.75f, 0.58f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
