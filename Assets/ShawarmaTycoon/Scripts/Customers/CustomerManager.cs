using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class CustomerManager : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float spawnInterval = 3f;
        [SerializeField, Min(1)] private int maxQueueLength = 4;
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

        public int ActiveCustomers => customers.Count;

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
        }

        public void RegisterTable(CustomerTable table)
        {
            if (table != null && !tables.Contains(table)) tables.Add(table);
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

            spawnTimer = spawnInterval;
            int waitingCount = 0;
            for (int i = 0; i < customers.Count; i++)
                if (customers[i] != null && customers[i].State == CustomerState.Queueing) waitingCount++;

            int activeTableCount = 0;
            for (int i = 0; i < tables.Count; i++)
                if (tables[i] != null && tables[i].gameObject.activeInHierarchy) activeTableCount++;

            if (waitingCount >= maxQueueLength || customers.Count >= activeTableCount + maxQueueLength)
                return;

            SpawnCustomer();
        }

        private void SpawnCustomer()
        {
            if (entryPoint == null) return;

            Color[] colors =
            {
                new(0.28f, 0.62f, 0.78f),
                new(0.82f, 0.48f, 0.34f),
                new(0.42f, 0.70f, 0.45f),
                new(0.64f, 0.45f, 0.72f)
            };

            GameObject customer = PrototypeVisuals.CreatePrimitive(
                $"Musteri {++customerIndex}",
                PrimitiveType.Capsule,
                transform,
                Vector3.zero,
                new Vector3(0.48f, 0.62f, 0.48f),
                colors[customerIndex % colors.Length]);
            customer.transform.position = entryPoint.position;

            CustomerAgent agent = customer.AddComponent<CustomerAgent>();
            agent.Configure(this, exitPoint, 2.4f, 4.5f, 15);
            customers.Add(agent);
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
