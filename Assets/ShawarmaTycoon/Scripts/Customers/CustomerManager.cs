using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CustomerManager : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float spawnInterval = 1.7f;

        /// <summary>
        /// Queue length and patience have to agree, or the back of the line is
        /// unservable by construction. The stations run unattended now, so the
        /// shop can turn a customer round in roughly 5 s rather than 7.5, and a
        /// seven deep queue needs about 35 s at the back. Patience is set well
        /// clear of that; drop one of these two numbers without the other and the
        /// tail of the line times out however well the shop is run.
        /// </summary>
        [SerializeField, Min(1)] private int maxQueueLength = 7;
        [SerializeField, Min(2f)] private float customerPatience = 55f;

        /// <summary>
        /// Opened up from 1.05 so the order bubbles have room. At the old spacing
        /// a three item order sat over the shoulders of the people either side.
        /// </summary>
        [SerializeField, Min(0.5f)] private float queueSpacing = 1.3f;

        /// <summary>
        /// How much each person already waiting sours a new arrival. Softened
        /// along with the longer queue: at the old rate a seven deep line - which
        /// is the line the shop is now built to hold - pinned every arrival at the
        /// floor of this penalty, and their mood then cut the same bill a second
        /// time. Tables were paying out a coin or two.
        /// </summary>
        [SerializeField, Range(0f, 0.3f)] private float queuePressurePenalty = 0.035f;

        private readonly List<CustomerTable> tables = new();
        private readonly List<CustomerAgent> customers = new();

        /// <summary>How far along the pavement a customer starts, either side of the gate.</summary>
        [SerializeField, Min(2f)] private float approachDistance = 13f;

        private ItemStation serviceStation;
        /// <summary>Where each thing on an order is taken from, keyed by what it is.</summary>
        private readonly Dictionary<ItemType, ItemStation> counters = new();
        private CashPile till;
        private Transform entryPoint;
        private Transform exitPoint;
        private Transform gatePoint;
        private Transform approachStart;
        private Transform approachCorner;
        private Transform queueFront;
        private Vector3 queueDirection = Vector3.right;
        private float spawnTimer;
        private int customerIndex;
        private int nextVipCustomer;
        private bool vipCustomersEnabled = true;

        public int ActiveCustomers => customers.Count;
        public int ActiveVipCustomers
        {
            get
            {
                int count = 0;
                for (int i = 0; i < customers.Count; i++)
                    if (customers[i] != null && customers[i].IsVip) count++;
                return count;
            }
        }

        /// <summary>
        /// The walk in: where people appear on the west pavement, and the turn at
        /// the bottom of it they round before heading for the gate. Set separately
        /// from <see cref="Configure"/> because a shop laid out without a pavement
        /// down that side still works - customers just walk straight at the door.
        /// </summary>
        public void SetApproachRoute(Transform start, Transform corner)
        {
            approachStart = start;
            approachCorner = corner;
        }

        public void Configure(
            ItemStation service,
            CashPile counterTill,
            Transform entry,
            Transform exit,
            Transform gate,
            Transform queueStart,
            Vector3 queueLineDirection,
            IEnumerable<CustomerTable> customerTables)
        {
            serviceStation = service;
            counters[ItemType.Wrap] = service;
            till = counterTill;
            entryPoint = entry;
            exitPoint = exit;
            gatePoint = gate;
            queueFront = queueStart;
            queueDirection = queueLineDirection.sqrMagnitude > 0.01f
                ? queueLineDirection.normalized
                : Vector3.right;

            tables.Clear();
            tables.AddRange(customerTables);
            spawnTimer = 0.5f;

            nextVipCustomer = Random.Range(8, 12);
            GameCatalogs.Initialize();
            vipCustomersEnabled = GameCatalogs.Game.Features.VipCustomers;
        }

        public void RegisterTable(CustomerTable table)
        {
            if (table != null && !tables.Contains(table)) tables.Add(table);
        }

        /// <summary>
        /// Adds a counter customers can be served from. Anything registered here
        /// becomes something they may ask for; until the fridge is bought, nobody
        /// orders a drink, because there is nowhere for one to come from.
        /// </summary>
        public void RegisterCounter(ItemType type, ItemStation counter)
        {
            if (type == ItemType.None || counter == null) return;
            counters[type] = counter;
        }

        /// <summary>Whether a counter for this exists and has one to give.</summary>
        public bool IsStocked(ItemType type) =>
            counters.TryGetValue(type, out ItemStation counter) &&
            counter != null && counter.isActiveAndEnabled && counter.OutputCount > 0;

        private bool Sells(ItemType type) =>
            counters.TryGetValue(type, out ItemStation counter) &&
            counter != null && counter.isActiveAndEnabled;

        /// <summary>Whether the shop has these on the menu at all, stocked or not.</summary>
        public bool SellsDrinks => Sells(ItemType.Drink);
        public bool SellsDesserts => Sells(ItemType.Dessert);

        /// <summary>
        /// The table a worker should walk to, rather than only whether the job
        /// could be done from anywhere. Null when there is nothing to go for.
        /// </summary>
        public CustomerTable FindTableWithCash() => FindTable(t => t.HasUncollectedCash);

        public CustomerTable FindDirtyTable() => FindTable(t => t.IsDirty);

        /// <summary>
        /// The next dirty table nobody has already set off for. With two bussers
        /// on the floor, the plain search hands them both the same table and one
        /// arrives to a job that is already done.
        /// </summary>
        public CustomerTable FindDirtyTable(ICollection<CustomerTable> exclude) =>
            FindTable(t => t.IsDirty && (exclude == null || !exclude.Contains(t)));

        private CustomerTable FindTable(System.Func<CustomerTable, bool> wanted)
        {
            for (int i = 0; i < tables.Count; i++)
            {
                CustomerTable table = tables[i];
                if (table != null && table.gameObject.activeInHierarchy && wanted(table))
                    return table;
            }
            return null;
        }

        public bool TryCollectTableCashByWorker()
        {
            for (int i = 0; i < tables.Count; i++)
                if (tables[i] != null && tables[i].TryAutoCollectCash()) return true;
            return false;
        }

        public bool TryCleanTableByWorker()
        {
            for (int i = 0; i < tables.Count; i++)
                if (tables[i] != null && tables[i].TryAutoCleanTrash()) return true;
            return false;
        }

        private void Update()
        {
            UpdateSpawning();
            UpdateQueue();
            TryServeFrontCustomer();
        }

        private void UpdateSpawning()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;

            spawnTimer = spawnInterval * RushHourSystem.SpawnIntervalMultiplier;
            int waitingCount = 0;
            for (int i = 0; i < customers.Count; i++)
                if (customers[i] != null && customers[i].State == CustomerState.Queueing) waitingCount++;

            int activeTableCount = 0;
            for (int i = 0; i < tables.Count; i++)
                if (tables[i] != null && tables[i].gameObject.activeInHierarchy) activeTableCount++;

            if (waitingCount >= maxQueueLength || customers.Count >= activeTableCount + maxQueueLength)
                return;

            SpawnCustomer(false);
        }

        public CustomerAgent SpawnVipNow() => vipCustomersEnabled ? SpawnCustomer(true) : null;

        private CustomerAgent SpawnCustomer(bool forceVip)
        {
            if (entryPoint == null) return null;

            Color[] colors =
            {
                new(0.28f, 0.62f, 0.78f),
                new(0.82f, 0.48f, 0.34f),
                new(0.42f, 0.70f, 0.45f),
                new(0.64f, 0.45f, 0.72f)
            };

            int spawnNumber = ++customerIndex;
            bool vip = vipCustomersEnabled && (forceVip || spawnNumber >= nextVipCustomer);
            if (vip) nextVipCustomer = spawnNumber + Random.Range(9, 14);

            // Everyone comes down the west pavement rather than appearing on
            // whichever side of the door the last one did not. They start up by the
            // shopfronts on the flank, walk south along the paving, then turn east
            // for the gate - the corner is a waypoint, see CustomerAgent, so the
            // walk in reads as coming down a street rather than cutting the corner
            // diagonally across the forecourt.
            Vector3 arrival = approachStart != null
                ? approachStart.position
                : entryPoint.position + Vector3.left * approachDistance;
            arrival.z += Random.Range(-1.2f, 1.2f);
            arrival.x += Random.Range(-0.5f, 0.5f);

            GameObject customer = new($"Musteri {spawnNumber}");
            customer.transform.SetParent(transform, false);
            customer.transform.position = arrival;

            // Rotate through the authored body variants so a queue is not six
            // copies of the same person.
            string bodyId = MeshyVisuals.CustomerVariants[
                spawnNumber % MeshyVisuals.CustomerVariants.Length];
            if (MeshyVisuals.TryAttachAuthored(
                    customer.transform, bodyId, Vector3.zero, Vector3.zero) == null)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Customer Fallback", PrimitiveType.Capsule, customer.transform,
                    new Vector3(0f, 0.82f, 0f), new Vector3(0.48f, 0.82f, 0.48f),
                    colors[customerIndex % colors.Length]);
            }

            // Fitted before the agent, which caches it in Awake and walks the
            // capsule from then on rather than setting the transform outright.
            CharacterBody.Attach(customer);
            CustomerAgent agent = customer.AddComponent<CustomerAgent>();
            // VIPs are worth the most and are the least willing to wait; during a
            // rush nobody has time to spare.
            float patience = (vip ? customerPatience * 0.75f : customerPatience)
                * RushHourSystem.PatienceMultiplier;

            // Walking up to a queue already sours the mood, so a shop that lets
            // the line build pays less on the people joining it as well as on
            // the ones already standing there.
            int waiting = 0;
            for (int i = 0; i < customers.Count; i++)
                if (customers[i] != null && customers[i].State == CustomerState.Queueing) waiting++;
            float arrivalMood = Mathf.Clamp(1f - waiting * queuePressurePenalty, 0.75f, 1f);

            // The bill, before the tip. Set so a meal plus a good shop's tip comes
            // to about what the whole meal used to be worth, which is what the
            // economy was measured and priced against.
            // Stepped through three heights along the queue, which is what keeps
            // full-size bubbles off each other's neighbours.
            agent.SetBubbleLift(spawnNumber % 3 * 0.36f);
            agent.Configure(this, exitPoint, 2.4f, 4.5f, 24, patience, vip, arrivalMood,
                BuildOrder(vip));
            if (approachCorner != null) agent.SetApproachCorner(approachCorner.position);
            if (gatePoint != null) agent.SetGatePoint(gatePoint.position);
            customers.Add(agent);
            AudioDirector.Play(GameSfx.CustomerArrive, vip ? 0.9f : 0.45f, vip ? 1.15f : 1f);
            return agent;
        }

        /// <summary>
        /// Rolls what a customer wants. Only from what the shop actually sells:
        /// asking for a drink before the fridge is bought would be an order that
        /// can never be filled, and the extras are what the fridge and the oven
        /// are bought for in the first place.
        /// </summary>
        private CustomerOrder BuildOrder(bool vip)
        {
            CustomerOrder order = new();
            order.Add(ItemType.Wrap, vip && Random.value < 0.5f ? 2 : 1);
            if (Sells(ItemType.Drink) && Random.value < 0.55f)
                order.Add(ItemType.Drink, 1);
            if (Sells(ItemType.Dessert) && Random.value < 0.35f)
                order.Add(ItemType.Dessert, 1);
            return order;
        }

        private void UpdateQueue()
        {
            if (queueFront == null) return;

            int queueIndex = 0;
            for (int i = 0; i < customers.Count; i++)
            {
                CustomerAgent customer = customers[i];
                if (customer == null || customer.State != CustomerState.Queueing) continue;
                customer.SetQueueTarget(queueFront.position + queueDirection * (queueIndex * queueSpacing));
                queueIndex++;
            }
        }

        private void TryServeFrontCustomer()
        {
            CustomerAgent front = null;
            for (int i = 0; i < customers.Count; i++)
            {
                if (customers[i] != null && customers[i].State == CustomerState.Queueing)
                {
                    front = customers[i];
                    break;
                }
            }
            if (front == null || !front.HasReachedQueue) return;

            // An order that cannot be filled would hold the whole line up until
            // the fridge was restocked. Once the customer has waited past patience,
            // they give up on the extras and take what there is - which still costs
            // the shop, because a smaller order is a smaller bill.
            if (front.HasGivenUpOnExtras && front.Order.TrimUnavailableExtras(IsStocked))
                front.RefreshOrderBubble();

            if (!CanFill(front.Order)) return;

            CustomerTable freeTable = null;
            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] != null && tables[i].IsAvailable)
                {
                    freeTable = tables[i];
                    break;
                }
            }
            if (freeTable == null) return;
            if (!freeTable.TryReserve(front)) return;

            if (!TakeOrder(front.Order))
            {
                freeTable.CancelReservation(front);
                return;
            }

            front.Serve(freeTable);
            // Paid over the counter here and now; the rest is left on the table.
            till?.Add(front.CounterPayment);
        }

        private bool CanFill(CustomerOrder order)
        {
            for (int i = 0; i < CustomerOrder.DisplayOrder.Length; i++)
            {
                ItemType type = CustomerOrder.DisplayOrder[i];
                int wanted = order.CountOf(type);
                if (wanted <= 0) continue;
                if (!counters.TryGetValue(type, out ItemStation counter) ||
                    counter == null || !counter.isActiveAndEnabled ||
                    counter.OutputCount < wanted)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Takes the whole order off the counters. Only called once
        /// <see cref="CanFill"/> has agreed it is all there, so a half-filled
        /// order cannot leave a wrap missing from the counter for nobody.
        /// </summary>
        private bool TakeOrder(CustomerOrder order)
        {
            if (!CanFill(order)) return false;

            for (int i = 0; i < CustomerOrder.DisplayOrder.Length; i++)
            {
                ItemType type = CustomerOrder.DisplayOrder[i];
                int wanted = order.CountOf(type);
                for (int taken = 0; taken < wanted; taken++)
                    counters[type].TryTakeForCustomer();
            }
            return true;
        }

        public void Despawn(CustomerAgent customer)
        {
            if (customer == null) return;
            customers.Remove(customer);
            Destroy(customer.gameObject);
        }
    }
}
