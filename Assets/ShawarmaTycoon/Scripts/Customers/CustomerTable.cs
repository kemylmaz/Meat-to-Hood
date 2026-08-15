using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CustomerTable : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float trashPickupRadius = 1.65f;
        /// <summary>Reach of the money pad itself, kept to roughly the pad's own footprint.</summary>
        [SerializeField, Min(0.3f)] private float cashPadRadius = 0.95f;

        private Transform player;
        private Transform seatPoint;
        private CustomerAgent occupant;
        private GameObject cleanVisual;
        private GameObject dirtyVisual;
        private GameObject dirtyIndicator;
        private GameObject cashPad;
        private GameObject cashStack;
        private TextMesh cashLabel;
        private TextMesh statusLabel;
        private bool reserved;
        private bool dirty;
        private int pendingCash;

        public bool IsAvailable => gameObject.activeInHierarchy && !reserved && !dirty;
        public bool IsDirty => dirty;
        /// <summary>Money still sitting on the pad, so a cashier has somewhere to go.</summary>
        public bool HasUncollectedCash => pendingCash > 0;
        /// <summary>Where the money actually is, which is not the table centre.</summary>
        public Vector3 CashPoint => cashPad != null ? cashPad.transform.position : transform.position;
        public bool IsReserved => reserved;
        public Transform SeatPoint => seatPoint;

        public void Configure(Transform playerTransform, Transform customerSeat)
        {
            player = playerTransform;
            seatPoint = customerSeat;

            dirtyIndicator = PrototypeVisuals.CreatePrimitive("Dirty Plate", PrimitiveType.Cylinder, transform,
                new Vector3(-0.384f, 0.94f, 0f), new Vector3(0.36f, 0.035f, 0.36f), PrototypeVisuals.Red);
            dirtyIndicator.SetActive(false);

            statusLabel = PrototypeVisuals.CreateLabel("Table", transform, new Vector3(0f, 1.25f, 0f), 0.12f);
            statusLabel.gameObject.SetActive(false);

            cashPad = new GameObject("Cash Pad");
            cashPad.transform.SetParent(transform, false);
            cashPad.transform.localPosition = new Vector3(1.22f, 0.02f, -0.62f);
            PrototypeVisuals.CreatePrimitive(
                "Cash Pad Surface", PrimitiveType.Cube, cashPad.transform,
                new Vector3(0f, 0.055f, 0f), new Vector3(0.62f, 0.04f, 0.40f),
                new Color(0.12f, 0.66f, 0.27f));
            cashLabel = PrototypeVisuals.CreateLabel("", cashPad.transform, Vector3.up * 0.36f, 0.13f);

            cashStack = new GameObject("Cash Stack");
            cashStack.transform.SetParent(transform, false);
            cashStack.transform.localPosition = new Vector3(1.22f, 0.13f, -0.62f);
            for (int i = 0; i < 4; i++)
            {
                PrototypeVisuals.CreatePrimitive("Cash Bill", PrimitiveType.Cube, cashStack.transform,
                    new Vector3(i % 2 == 0 ? -0.03f : 0.03f, i * 0.025f, 0f), new Vector3(0.56f, 0.025f, 0.32f),
                    i % 2 == 0 ? new Color(0.25f, 0.88f, 0.33f) : new Color(0.14f, 0.68f, 0.24f));
            }
            // The authored pad already includes its banded cash, so it replaces
            // both the pad surface and the loose bills.
            MeshyVisuals.TryReplaceDirect(cashPad.transform, "18_money_collection_pad",
                new Vector3(0.86f, 0.42f, 0.86f), Vector3.zero, Vector3.zero, false,
                "Cash Pad Surface");
            foreach (Transform bill in cashStack.transform)
                bill.gameObject.SetActive(!MeshyVisuals.IsAvailable("18_money_collection_pad"));
            cashStack.SetActive(false);
            cashPad.SetActive(false);
        }

        /// <summary>
        /// Hands over the two authored table states. When both exist the table
        /// swaps between them instead of dropping a red plate on the clean one.
        /// </summary>
        public void SetTableVariants(GameObject clean, GameObject dirty)
        {
            cleanVisual = clean;
            dirtyVisual = dirty;
            RefreshDirtyVisual();
        }

        private void RefreshDirtyVisual()
        {
            bool swaps = cleanVisual != null && dirtyVisual != null;
            if (swaps)
            {
                cleanVisual.SetActive(!dirty);
                dirtyVisual.SetActive(dirty);
            }

            // The loose plate marker is only needed when there is no dirty model.
            if (dirtyIndicator != null)
                dirtyIndicator.SetActive(dirty && !swaps);
        }

        public bool TryReserve(CustomerAgent customer)
        {
            if (!IsAvailable || customer == null) return false;
            occupant = customer;
            reserved = true;
            UpdateLabel();
            return true;
        }

        public void CancelReservation(CustomerAgent customer)
        {
            if (occupant != customer) return;
            occupant = null;
            reserved = false;
            UpdateLabel();
        }

        public void FinishMeal(CustomerAgent customer, int payout)
        {
            if (occupant != customer) return;
            occupant = null;
            reserved = false;
            dirty = true;
            pendingCash += Mathf.Max(0, payout);
            GameProgress.RecordServed(customer.IsVip, false);
            RefreshDirtyVisual();
            if (cashPad != null) cashPad.SetActive(pendingCash > 0);
            if (cashStack != null) cashStack.SetActive(pendingCash > 0);
            UpdateCashLabel();
            UpdateLabel();
            FloorSpillSystem.Instance?.TrySpawnForTable(this);
        }

        private void Update()
        {
            if (player == null) return;
            CollectCashIfNearby();
            TryPickUpTrash();
        }

        /// <summary>
        /// Cash is taken by standing on the pad it is sitting on, and only there.
        /// Collecting it from anywhere near the table made the pad a decoration:
        /// money arrived without the player ever going to where it was drawn, and
        /// clearing the plates quietly banked it as a side effect.
        /// </summary>
        private void CollectCashIfNearby()
        {
            if (pendingCash <= 0 || cashPad == null || GameEconomy.Instance == null) return;
            if (Vector3.SqrMagnitude(player.position - cashPad.transform.position) >
                cashPadRadius * cashPadRadius) return;
            CollectCash(false);
        }

        private void TryPickUpTrash()
        {
            if (!dirty || player == null) return;
            if (Vector3.SqrMagnitude(player.position - transform.position) > trashPickupRadius * trashPickupRadius) return;

            CarryInventory inventory = player.GetComponent<CarryInventory>();
            if (inventory == null || !inventory.TryAdd(ItemType.Trash)) return;

            dirty = false;
            RefreshDirtyVisual();
            UpdateLabel();
            AudioDirector.Play(GameSfx.Pickup);
            ComboSystem.Instance?.RegisterManualAction();
        }

        public bool TryAutoCollectCash()
        {
            if (pendingCash <= 0 || GameEconomy.Instance == null) return false;
            CollectCash(true);
            return true;
        }

        public bool TryAutoCleanTrash()
        {
            if (!dirty) return false;
            dirty = false;
            GameProgress.RecordTrash(1);
            ComboSystem.Instance?.RegisterWorkerAction();
            RefreshDirtyVisual();
            UpdateLabel();
            return true;
        }

        private void CollectCash(bool byWorker)
        {
            if (pendingCash <= 0 || GameEconomy.Instance == null) return;
            int collected = pendingCash;
            pendingCash = 0;
            GameEconomy.Instance.AddCoins(collected);
            GameProgress.RecordRevenue(collected);
            if (byWorker) ComboSystem.Instance?.RegisterWorkerAction();
            else ComboSystem.Instance?.RegisterManualAction();
            Vector3 feedbackPosition = cashPad != null ? cashPad.transform.position + Vector3.up * 0.35f : transform.position + Vector3.up;
            CoinBurst.SpawnGain(feedbackPosition, collected);
            AudioDirector.Play(GameSfx.CashRegister);
            if (cashPad != null) cashPad.SetActive(false);
            if (cashStack != null) cashStack.SetActive(false);
            UpdateCashLabel();
        }

        private void UpdateCashLabel()
        {
            if (cashLabel != null) cashLabel.text = pendingCash > 0 ? "+$" + pendingCash : string.Empty;
        }

        private void UpdateLabel()
        {
            if (statusLabel == null || !statusLabel.gameObject.activeSelf) return;
            statusLabel.text = string.Empty;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, trashPickupRadius);
        }
    }
}
