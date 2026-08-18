using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// The courier bay. Unlike the drive-through, which is the counter queue with
    /// a car in front of it, this asks for a whole bag at once - a wrap, a dessert
    /// and a drink in one order - and pays for the set when the last item lands.
    ///
    /// The player fills it by walking up holding each item. A part-filled bag is
    /// kept, so an order can be assembled across several trips.
    /// </summary>
    public sealed class CourierStation : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float firstOrderDelay = 12f;
        [SerializeField, Min(1f)] private float minimumOrderInterval = 14f;
        [SerializeField, Min(1f)] private float maximumOrderInterval = 22f;
        /// <summary>A bag is a bigger job than a counter sale, so it gets longer.</summary>
        [SerializeField, Min(2f)] private float expireAfterSeconds = 55f;
        [SerializeField, Min(0.5f)] private float interactionRadius = 1.6f;
        [SerializeField, Min(1)] private int basePayoutPerItem = 26;

        private Transform player;
        private CarryInventory inventory;
        private CustomerManager shop;
        private CashPile cash;
        private Transform orderBoard;
        private GameObject scooter;
        private readonly CustomerOrder order = new();
        private readonly Dictionary<ItemType, int> delivered = new();
        private readonly List<GameObject> boardVisuals = new();
        private float orderTimer;
        private float orderAge;
        private float handoverCooldown;
        private bool hasOrder;

        public bool HasOrder => hasOrder;
        public CustomerOrder Order => order;
        public int PendingCash => cash != null ? cash.Pending : 0;

        public void Configure(
            Transform playerTransform, CarryInventory playerInventory,
            CustomerManager customerShop, CashPile cashPile)
        {
            player = playerTransform;
            inventory = playerInventory;
            shop = customerShop;
            cash = cashPile;

            GameObject board = new("Sipariş Panosu");
            board.transform.SetParent(transform, false);
            board.transform.localPosition = new Vector3(0f, 1.55f, -0.1f);
            board.transform.localEulerAngles = new Vector3(55f, 0f, 0f);
            orderBoard = board.transform;

            orderTimer = firstOrderDelay;
            RefreshBoard();
        }

        public void SetScooter(GameObject courierScooter) => scooter = courierScooter;

        /// <summary>How much of the order is still outstanding.</summary>
        public int Outstanding(ItemType type) =>
            Mathf.Max(0, order.CountOf(type) - (delivered.TryGetValue(type, out int d) ? d : 0));

        private void Update()
        {
            handoverCooldown -= Time.deltaTime;

            if (!hasOrder)
            {
                orderTimer -= Time.deltaTime;
                if (orderTimer <= 0f) CreateOrder();
                return;
            }

            orderAge += Time.deltaTime;
            if (orderAge >= expireAfterSeconds)
            {
                ExpireOrder();
                return;
            }

            TryHandover();
        }

        /// <summary>
        /// Builds a bag out of what the shop actually sells. Before the fridge and
        /// the oven are bought that is a wrap or two, which is a thin order but a
        /// real one; once they exist the bags get properly mixed.
        /// </summary>
        private void CreateOrder()
        {
            order.Clear();
            delivered.Clear();

            order.Add(ItemType.Wrap, Random.value < 0.35f ? 2 : 1);
            if (shop != null)
            {
                if (shop.IsStocked(ItemType.Drink) || shop.SellsDrinks)
                    order.Add(ItemType.Drink, Random.value < 0.4f ? 2 : 1);
                if (shop.SellsDesserts && Random.value < 0.75f)
                    order.Add(ItemType.Dessert, 1);
            }

            hasOrder = true;
            orderAge = 0f;
            RefreshBoard();
        }

        private void TryHandover()
        {
            if (player == null || inventory == null || handoverCooldown > 0f) return;
            if (Vector3.SqrMagnitude(player.position - transform.position) >
                interactionRadius * interactionRadius) return;

            ItemType held = inventory.HeldType;
            if (held == ItemType.None || Outstanding(held) <= 0) return;
            if (!inventory.TryRemove(held)) return;

            delivered[held] = (delivered.TryGetValue(held, out int had) ? had : 0) + 1;
            handoverCooldown = 0.18f;
            AudioDirector.Play(GameSfx.Drop, 0.7f);
            RefreshBoard();

            if (IsComplete()) Dispatch();
        }

        private bool IsComplete()
        {
            for (int i = 0; i < CustomerOrder.DisplayOrder.Length; i++)
                if (Outstanding(CustomerOrder.DisplayOrder[i]) > 0) return false;
            return true;
        }

        /// <summary>The bag is full: the courier is paid and rides off.</summary>
        private void Dispatch()
        {
            int payout = RewardCalculator.Calculate(
                basePayoutPerItem * Mathf.Max(1, order.TotalItems), 1f);
            cash?.Add(payout);
            ComboSystem.Instance?.RegisterTakeaway(false);
            GameProgress.RecordServed(vip: false, takeaway: true);
            if (scooter != null) scooter.SendMessage("Depart", SendMessageOptions.DontRequireReceiver);

            ClearOrder();
            RefreshBoard();
        }

        /// <summary>
        /// The courier will not wait forever. Without this a bag nobody filled sat
        /// on the board blocking every later order, and the bay quietly stopped
        /// being part of the shop.
        /// </summary>
        private void ExpireOrder()
        {
            ClearOrder();
            ComboSystem.Instance?.BreakCombo();
            GameProgress.RecordLostCustomer();
            AudioDirector.Play(GameSfx.Error, 0.55f);
            RefreshBoard();
        }

        private void ClearOrder()
        {
            hasOrder = false;
            orderAge = 0f;
            order.Clear();
            delivered.Clear();
            orderTimer = Random.Range(
                Mathf.Min(minimumOrderInterval, maximumOrderInterval),
                Mathf.Max(minimumOrderInterval, maximumOrderInterval));
        }

        /// <summary>
        /// The board shows the whole bag, with what has already been handed over
        /// greyed out, so a half-filled order reads as half filled.
        /// </summary>
        private void RefreshBoard()
        {
            for (int i = 0; i < boardVisuals.Count; i++)
                if (boardVisuals[i] != null) Destroy(boardVisuals[i]);
            boardVisuals.Clear();
            if (orderBoard == null) return;

            foreach (Transform child in orderBoard) Destroy(child.gameObject);
            if (!hasOrder) return;

            int lines = order.LineCount;
            float width = 0.6f * lines + 0.18f;
            boardVisuals.Add(PrototypeVisuals.CreatePrimitive("Pano", PrimitiveType.Cube,
                orderBoard, Vector3.zero, new Vector3(width, 0.54f, 0.05f),
                new Color(0.99f, 0.97f, 0.92f)));

            int slot = 0;
            for (int i = 0; i < CustomerOrder.DisplayOrder.Length; i++)
            {
                ItemType type = CustomerOrder.DisplayOrder[i];
                int wanted = order.CountOf(type);
                if (wanted <= 0) continue;

                float x = (slot - (lines - 1) * 0.5f) * 0.6f;
                slot++;

                GameObject icon = PrototypeVisuals.CreateItemVisual(
                    type, orderBoard, new Vector3(x - 0.1f, 0f, -0.06f), 0.78f);
                if (Outstanding(type) <= 0)
                {
                    Renderer renderer = icon.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                        renderer.sharedMaterial = PrototypeVisuals.Material(
                            new Color(0.62f, 0.66f, 0.63f));
                }
                boardVisuals.Add(icon);

                TextMesh label = new GameObject("Adet").AddComponent<TextMesh>();
                label.transform.SetParent(orderBoard, false);
                label.transform.localPosition = new Vector3(x + 0.18f, -0.02f, -0.08f);
                label.text = (wanted - Outstanding(type)) + "/" + wanted;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.characterSize = 0.032f;
                label.fontSize = 64;
                label.fontStyle = FontStyle.Bold;
                label.color = Outstanding(type) <= 0
                    ? new Color(0.20f, 0.55f, 0.28f)
                    : new Color(0.24f, 0.16f, 0.12f);
                boardVisuals.Add(label.gameObject);
            }

            foreach (Collider collider in orderBoard.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.62f, 0.20f, 0.75f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
