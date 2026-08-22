using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CustomerTable : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float trashPickupRadius = 1.65f;
        /// <summary>Reach of the money pad itself, kept to roughly the pad's own footprint.</summary>
        [SerializeField, Min(0.3f)] private float cashPadRadius = 0.95f;

        private Transform player;
        private readonly Transform[] seatPoints = new Transform[2];
        private readonly CustomerAgent[] occupants = new CustomerAgent[2];
        private GameObject cleanVisual;
        private GameObject dirtyVisual;
        private readonly GameObject[] dirtyIndicators = new GameObject[2];
        private GameObject cashPad;
        private GameObject cashStack;
        private readonly GameObject[] makeoverVisuals = new GameObject[6];
        private WorldCashMarker cashMarker;
        private TextMesh statusLabel;
        private readonly bool[] dirtySeats = new bool[2];
        private int pendingCash;
        private int makeoverTier = 1;

        private Collider[] seatClearanceHits = new Collider[32];

        public bool IsAvailable => HasReservableSeat;
        public bool HasReservableSeat
        {
            get
            {
                if (!gameObject.activeInHierarchy) return false;
                for (int i = 0; i < seatPoints.Length; i++)
                    if (seatPoints[i] != null && occupants[i] == null && !dirtySeats[i] &&
                        IsSeatApproachClear(i)) return true;
                return false;
            }
        }
        public bool IsDirty => dirtySeats[0] || dirtySeats[1];
        /// <summary>Money still sitting on the pad, so a cashier has somewhere to go.</summary>
        public bool HasUncollectedCash => pendingCash > 0;
        /// <summary>Where the money actually is, which is not the table centre.</summary>
        public Vector3 CashPoint => cashPad != null ? cashPad.transform.position : transform.position;
        public bool IsReserved => occupants[0] != null || occupants[1] != null;
        public int SeatCapacity => (seatPoints[0] != null ? 1 : 0) + (seatPoints[1] != null ? 1 : 0);
        public int OccupiedSeatCount => (occupants[0] != null ? 1 : 0) + (occupants[1] != null ? 1 : 0);
        public Transform SeatPoint => seatPoints[0];

        /// <summary>
        /// A standing point outside the chair collider. Customers walk here, then
        /// sit down; aiming a CharacterController at the seat itself leaves it
        /// permanently pressed against the chair half a metre short of its goal.
        /// </summary>
        public Vector3 SeatApproachPoint
        {
            get
            {
                return SeatApproachPointFor(0);
            }
        }

        public Transform GetSeatPoint(CustomerAgent customer)
        {
            int index = SeatIndex(customer);
            return index >= 0 ? seatPoints[index] : null;
        }

        public Vector3 GetSeatApproachPoint(CustomerAgent customer)
        {
            int index = SeatIndex(customer);
            return index >= 0 ? SeatApproachPointFor(index) : transform.position;
        }

        private Vector3 SeatApproachPointFor(int index)
        {
            Transform point = index >= 0 && index < seatPoints.Length ? seatPoints[index] : null;
            if (point == null) return transform.position;
            Vector3 away = point.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;
            return point.position + away.normalized * 0.52f;
        }

        /// <summary>Used by build mode to keep a person-sized aisle at the chair.</summary>
        public bool IsSeatApproachClear()
        {
            for (int i = 0; i < seatPoints.Length; i++)
                if (seatPoints[i] != null && !IsSeatApproachClear(i)) return false;
            return true;
        }

        private bool IsSeatApproachClear(int seatIndex)
        {
            if (seatClearanceHits == null) seatClearanceHits = new Collider[32];
            Vector3 approach = SeatApproachPointFor(seatIndex);
            int count = Physics.OverlapCapsuleNonAlloc(
                approach + Vector3.up * 0.28f,
                approach + Vector3.up * 1.42f,
                0.29f,
                seatClearanceHits,
                ~0,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit = seatClearanceHits[i];
                if (hit == null || !hit.enabled || hit.transform.IsChildOf(transform)) continue;
                if (hit.GetComponentInParent<DioramaWalkableSurface>() != null) continue;
                if (hit.GetComponentInParent<MobilePlayerController>() != null) continue;
                if (hit.GetComponentInParent<CustomerAgent>() != null) continue;
                if (hit.GetComponentInParent<WorkerAgent>() != null) continue;
                return false;
            }
            return true;
        }

        public void Configure(
            Transform playerTransform, Transform firstCustomerSeat, Transform secondCustomerSeat = null)
        {
            player = playerTransform;
            seatPoints[0] = firstCustomerSeat;
            seatPoints[1] = secondCustomerSeat;

            for (int i = 0; i < dirtyIndicators.Length; i++)
            {
                dirtyIndicators[i] = PrototypeVisuals.CreatePrimitive(
                    "Dirty Plate " + (i + 1), PrimitiveType.Cylinder, transform,
                    new Vector3(-0.28f, 0.94f, i == 0 ? -0.20f : 0.20f),
                    new Vector3(0.32f, 0.035f, 0.32f), PrototypeVisuals.Red);
                dirtyIndicators[i].SetActive(false);
            }

            statusLabel = PrototypeVisuals.CreateLabel("Table", transform, new Vector3(0f, 1.25f, 0f), 0.12f);
            statusLabel.gameObject.SetActive(false);

            cashPad = new GameObject("Cash Pad");
            cashPad.transform.SetParent(transform, false);
            cashPad.transform.localPosition = new Vector3(1.22f, 0.02f, -0.62f);
            PrototypeVisuals.CreatePrimitive(
                "Cash Pad Surface", PrimitiveType.Cube, cashPad.transform,
                new Vector3(0f, 0.055f, 0f), new Vector3(0.62f, 0.04f, 0.40f),
                new Color(0.12f, 0.66f, 0.27f));
            cashMarker = WorldCashMarker.Create(cashPad.transform);

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

        /// <summary>
        /// Changes only the authored furniture shell. The gameplay root, seat,
        /// money pad and colliders remain exactly where build mode placed them.
        /// </summary>
        public void ApplyMakeoverTier(int tier)
        {
            makeoverTier = Mathf.Clamp(tier, 1, 5);
            if (makeoverTier > 1 && makeoverVisuals[makeoverTier] == null)
                makeoverVisuals[makeoverTier] = BuildMakeoverVisual(makeoverTier);

            for (int i = 2; i < makeoverVisuals.Length; i++)
                if (makeoverVisuals[i] != null)
                    makeoverVisuals[i].SetActive(i == makeoverTier);

            RefreshDirtyVisual();
        }

        private GameObject BuildMakeoverVisual(int tier)
        {
            GameObject root = new($"Masa Teması SV.{tier}");
            root.transform.SetParent(transform, false);

            string tableAsset = tier == 2 || tier == 5
                ? "159_shop_table_round_small"
                : "158_shop_table_round";
            string chairAsset = tier == 3 || tier == 5
                ? "156_shop_chair_b"
                : "155_shop_chair_a";

            GameObject tabletop = MeshyVisuals.TryAttach(root.transform, tableAsset,
                new Vector3(1.32f, 0.82f, 1.04f), Vector3.zero, Vector3.zero, true);
            if (tabletop == null)
            {
                tabletop = PrototypeVisuals.CreatePrimitive("Temalı Masa", PrimitiveType.Cube,
                    root.transform, new Vector3(0f, 0.72f, 0f),
                    new Vector3(1.38f, 0.12f, 1.02f), TableWood(tier));
                PrototypeVisuals.CreatePrimitive("Masa Ayağı", PrimitiveType.Cylinder,
                    root.transform, new Vector3(0f, 0.36f, 0f),
                    new Vector3(0.25f, 0.68f, 0.25f), TableDark(tier));
            }

            AddThemedChair(root.transform, chairAsset, new Vector3(0f, 0f, 1.03f), 180f, tier);
            AddThemedChair(root.transform, chairAsset, new Vector3(0f, 0f, -1.03f), 0f, tier);

            Color accent = TableAccent(tier);
            PrototypeVisuals.CreatePrimitive("Masa Runner", PrimitiveType.Cube, root.transform,
                new Vector3(0f, 0.835f, 0f), new Vector3(0.30f, 0.025f, 0.78f), accent);

            if (tier >= 3)
            {
                PrototypeVisuals.CreatePrimitive("Minik Vazo", PrimitiveType.Cylinder, root.transform,
                    new Vector3(0f, 0.94f, 0f), new Vector3(0.16f, 0.20f, 0.16f),
                    tier == 5 ? new Color(0.97f, 0.74f, 0.25f) : new Color(0.87f, 0.89f, 0.75f));
                PrototypeVisuals.CreatePrimitive("Vazo Dalı", PrimitiveType.Sphere, root.transform,
                    new Vector3(0f, 1.12f, 0f), Vector3.one * (tier == 5 ? 0.25f : 0.20f),
                    tier == 4 ? new Color(0.96f, 0.65f, 0.25f) : new Color(0.32f, 0.66f, 0.40f));
            }

            return root;
        }

        private static void AddThemedChair(
            Transform parent, string asset, Vector3 position, float yaw, int tier)
        {
            if (MeshyVisuals.TryAttach(parent, asset, new Vector3(0.62f, 0.90f, 0.62f),
                    position, new Vector3(0f, yaw, 0f), true) != null) return;

            PrototypeVisuals.CreatePrimitive("Temalı Sandalye", PrimitiveType.Cube, parent,
                position + new Vector3(0f, 0.43f, 0f), new Vector3(0.54f, 0.70f, 0.54f),
                TableAccent(tier));
        }

        private static Color TableWood(int tier)
        {
            return tier switch
            {
                2 => new Color(0.77f, 0.53f, 0.34f),
                3 => new Color(0.62f, 0.48f, 0.31f),
                4 => new Color(0.45f, 0.29f, 0.20f),
                _ => new Color(0.35f, 0.20f, 0.16f)
            };
        }

        private static Color TableDark(int tier) => Color.Lerp(TableWood(tier), Color.black, 0.28f);

        private static Color TableAccent(int tier)
        {
            return tier switch
            {
                2 => new Color(0.88f, 0.42f, 0.28f),
                3 => new Color(0.30f, 0.63f, 0.48f),
                4 => new Color(0.91f, 0.60f, 0.22f),
                _ => new Color(0.73f, 0.20f, 0.17f)
            };
        }

        private void RefreshDirtyVisual()
        {
            bool swaps = cleanVisual != null && dirtyVisual != null;
            bool themed = makeoverTier > 1 && makeoverVisuals[makeoverTier] != null;
            if (swaps)
            {
                bool fullyDirty = dirtySeats[0] && dirtySeats[1];
                cleanVisual.SetActive(!themed && !fullyDirty);
                dirtyVisual.SetActive(!themed && fullyDirty);
            }

            // A themed table keeps its coherent furniture set and uses the clear
            // red plate marker to communicate dirt instead of swapping back to
            // the starter-table model.
            bool authoredDirtyShown = swaps && !themed && dirtySeats[0] && dirtySeats[1];
            for (int i = 0; i < dirtyIndicators.Length; i++)
                if (dirtyIndicators[i] != null)
                    dirtyIndicators[i].SetActive(dirtySeats[i] && !authoredDirtyShown);
        }

        public bool TryReserve(CustomerAgent customer)
        {
            if (customer == null || SeatIndex(customer) >= 0) return false;
            for (int i = 0; i < seatPoints.Length; i++)
            {
                if (seatPoints[i] == null || occupants[i] != null || dirtySeats[i] ||
                    !IsSeatApproachClear(i)) continue;
                occupants[i] = customer;
                UpdateLabel();
                return true;
            }
            return false;
        }

        public void CancelReservation(CustomerAgent customer)
        {
            int index = SeatIndex(customer);
            if (index < 0) return;
            occupants[index] = null;
            UpdateLabel();
        }

        public void FinishMeal(CustomerAgent customer, int payout)
        {
            int index = SeatIndex(customer);
            if (index < 0) return;
            occupants[index] = null;
            dirtySeats[index] = true;
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
            if (Time.timeScale <= 0f) return;
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
            if (!IsDirty || player == null) return;
            if (Vector3.SqrMagnitude(player.position - transform.position) > trashPickupRadius * trashPickupRadius) return;

            CarryInventory inventory = player.GetComponent<CarryInventory>();
            if (inventory == null) return;
            bool pickedUp = false;
            for (int i = 0; i < dirtySeats.Length; i++)
            {
                if (!dirtySeats[i] || !inventory.TryAdd(ItemType.Trash)) continue;
                dirtySeats[i] = false;
                pickedUp = true;
            }
            if (!pickedUp) return;
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
            int dirtyIndex = dirtySeats[0] ? 0 : dirtySeats[1] ? 1 : -1;
            if (dirtyIndex < 0) return false;
            dirtySeats[dirtyIndex] = false;
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
            cashMarker?.SetAmount(pendingCash);
        }

        private void UpdateLabel()
        {
            if (statusLabel == null || !statusLabel.gameObject.activeSelf) return;
            statusLabel.text = string.Empty;
        }

        private int SeatIndex(CustomerAgent customer)
        {
            if (customer == null) return -1;
            for (int i = 0; i < occupants.Length; i++)
                if (occupants[i] == customer) return i;
            return -1;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, trashPickupRadius);
            for (int i = 0; i < seatPoints.Length; i++)
            {
                if (seatPoints[i] == null) continue;
                Gizmos.color = Color.green;
                Vector3 approach = SeatApproachPointFor(i);
                Gizmos.DrawWireSphere(approach + Vector3.up * 0.3f, 0.29f);
                Gizmos.DrawLine(approach, seatPoints[i].position);
            }
        }
    }
}
