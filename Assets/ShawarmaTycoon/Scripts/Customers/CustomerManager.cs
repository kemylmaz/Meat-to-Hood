using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CustomerManager : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float spawnInterval = 3f;

        /// <summary>
        /// Queue length and patience have to agree, or the back of the line is
        /// unservable by construction. Measured service rate is roughly 7.5 s per
        /// customer, so at the old 4 deep the last arrival needed 30 s and had
        /// 20 - they timed out every single time, however well the shop was run.
        /// Three deep needs about 22 s, which a well run shop makes and a badly
        /// run one does not.
        /// </summary>
        [SerializeField, Min(1)] private int maxQueueLength = 3;
        [SerializeField, Min(2f)] private float customerPatience = 28f;

        [SerializeField, Min(0.5f)] private float queueSpacing = 1.05f;

        private readonly List<CustomerTable> tables = new();
        private readonly List<CustomerAgent> customers = new();

        private ItemStation serviceStation;
        private Transform entryPoint;
        private Transform exitPoint;
        private Transform queueFront;
        private Vector3 queueDirection = Vector3.right;
        private float spawnTimer;
        private int customerIndex;
        private int nextVipCustomer;

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

        public void Configure(
            ItemStation service,
            Transform entry,
            Transform exit,
            Transform queueStart,
            Vector3 queueLineDirection,
            IEnumerable<CustomerTable> customerTables)
        {
            serviceStation = service;
            entryPoint = entry;
            exitPoint = exit;
            queueFront = queueStart;
            queueDirection = queueLineDirection.sqrMagnitude > 0.01f
                ? queueLineDirection.normalized
                : Vector3.right;

            tables.Clear();
            tables.AddRange(customerTables);
            spawnTimer = 0.5f;
            nextVipCustomer = Random.Range(8, 12);
        }

        public void RegisterTable(CustomerTable table)
        {
            if (table != null && !tables.Contains(table)) tables.Add(table);
        }

        /// <summary>
        /// The table a worker should walk to, rather than only whether the job
        /// could be done from anywhere. Null when there is nothing to go for.
        /// </summary>
        public CustomerTable FindTableWithCash() => FindTable(t => t.HasUncollectedCash);

        public CustomerTable FindDirtyTable() => FindTable(t => t.IsDirty);

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

        public CustomerAgent SpawnVipNow() => SpawnCustomer(true);

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
            bool vip = forceVip || spawnNumber >= nextVipCustomer;
            if (vip) nextVipCustomer = spawnNumber + Random.Range(9, 14);

            GameObject customer = new($"Musteri {spawnNumber}");
            customer.transform.SetParent(transform, false);
            customer.transform.position = entryPoint.position;

            // Rotate through the authored body variants so a queue is not six
            // copies of the same person.
            string bodyId = MeshyVisuals.CustomerVariants[
                spawnNumber % MeshyVisuals.CustomerVariants.Length];
            if (MeshyVisuals.TryAttach(
                    customer.transform, bodyId, new Vector3(0.75f, 1.70f, 0.85f),
                    Vector3.zero, Vector3.zero, false) == null)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Customer Fallback", PrimitiveType.Capsule, customer.transform,
                    new Vector3(0f, 0.82f, 0f), new Vector3(0.48f, 0.82f, 0.48f),
                    colors[customerIndex % colors.Length]);
            }

            CustomerAgent agent = customer.AddComponent<CustomerAgent>();
            // VIPs are worth the most and are the least willing to wait; during a
            // rush nobody has time to spare.
            float patience = (vip ? customerPatience * 0.75f : customerPatience)
                * RushHourSystem.PatienceMultiplier;
            agent.Configure(this, exitPoint, 2.4f, 4.5f, 15, patience, vip);
            customers.Add(agent);
            AudioDirector.Play(GameSfx.CustomerArrive, vip ? 0.9f : 0.45f, vip ? 1.15f : 1f);
            return agent;
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
            if (serviceStation == null || serviceStation.OutputCount <= 0) return;

            CustomerAgent front = null;
            for (int i = 0; i < customers.Count; i++)
            {
                if (customers[i] != null && customers[i].State == CustomerState.Queueing)
                {
                    front = customers[i];
                    break;
                }
            }
            if (front == null) return;

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
            if (!serviceStation.TryTakeServiceItem())
            {
                freeTable.CancelReservation(front);
                return;
            }

            front.Serve(freeTable);
        }

        public void Despawn(CustomerAgent customer)
        {
            if (customer == null) return;
            customers.Remove(customer);
            Destroy(customer.gameObject);
        }
    }
}
