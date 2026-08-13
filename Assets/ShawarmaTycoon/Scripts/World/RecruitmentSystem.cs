using UnityEngine;

namespace ShawarmaTycoon
{
    public enum RecruitRole { Cashier, Cleaner, Runner }

    /// <summary>Recruitable helpers. Each one replaces a specific repeated player action.</summary>
    public sealed class RecruitmentSystem : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float cashierInterval = 0.8f;
        [SerializeField, Min(0.1f)] private float cleanerInterval = 3.2f;
        [SerializeField, Min(0.1f)] private float runnerInterval = 0.72f;

        private CustomerManager customers;
        private ItemStation wrapStation;
        private ItemStation serviceStation;
        private TakeawaySystem takeaway;
        private FloorSpillSystem floorSpills;
        private Transform visualParent;
        private float cashierTimer;
        private float cleanerTimer;
        private float runnerTimer;
        private bool cashier;
        private bool cleaner;
        private bool runner;
        private bool cashierTakeawayTurn;
        private bool cleanerSpillTurn;

        public void Configure(
            CustomerManager customerManager,
            ItemStation wrap,
            ItemStation service,
            TakeawaySystem takeawayCounter,
            FloorSpillSystem spillSystem,
            Transform workerVisualParent)
        {
            customers = customerManager;
            wrapStation = wrap;
            serviceStation = service;
            takeaway = takeawayCounter;
            floorSpills = spillSystem;
            visualParent = workerVisualParent;
            cashier = GameProgress.GetInt("recruit.cashier", 0) == 1;
            cleaner = GameProgress.GetInt("recruit.cleaner", 0) == 1;
            runner = GameProgress.GetInt("recruit.runner", 0) == 1;
            if (cashier) CreateWorkerVisual(RecruitRole.Cashier);
            if (cleaner) CreateWorkerVisual(RecruitRole.Cleaner);
            if (runner) CreateWorkerVisual(RecruitRole.Runner);
        }

        public bool IsHired(RecruitRole role) => role switch
        {
            RecruitRole.Cashier => cashier,
            RecruitRole.Cleaner => cleaner,
            _ => runner
        };

        /// <summary>
        /// Priced above the station board: each of these removes a whole repeated
        /// chore, so they should be something you save up for rather than pick up
        /// in passing on the first day.
        /// </summary>
        public int GetCost(RecruitRole role) => role switch
        {
            RecruitRole.Cashier => 350,
            RecruitRole.Cleaner => 300,
            _ => 400
        };

        public bool TryHire(RecruitRole role, bool free)
        {
            if (IsHired(role)) return false;
            int cost = GetCost(role);
            if (!free && (GameEconomy.Instance == null || !GameEconomy.Instance.TrySpend(cost))) return false;

            switch (role)
            {
                case RecruitRole.Cashier:
                    cashier = true;
                    GameProgress.SetInt("recruit.cashier", 1);
                    break;
                case RecruitRole.Cleaner:
                    cleaner = true;
                    GameProgress.SetInt("recruit.cleaner", 1);
                    break;
                default:
                    runner = true;
                    GameProgress.SetInt("recruit.runner", 1);
                    break;
            }

            GameProgress.RegisterWorker("recruit." + role);
            GameProgress.RecordUpgrade();
            CreateWorkerVisual(role);
            if (!free) CoinBurst.Spawn(transform.position + Vector3.up, cost);
            return true;
        }

        private void Update()
        {
            float assist = HumanResourcesSystem.AssistIntervalMultiplier;
            if (cashier && customers != null)
            {
                cashierTimer -= Time.deltaTime;
                if (cashierTimer <= 0f)
                {
                    bool handled = cashierTakeawayTurn
                        ? takeaway != null && takeaway.TryAutoCollectCash()
                        : customers.TryCollectTableCashByWorker();
                    if (!handled)
                    {
                        if (cashierTakeawayTurn) customers.TryCollectTableCashByWorker();
                        else if (takeaway != null) takeaway.TryAutoCollectCash();
                    }
                    cashierTakeawayTurn = !cashierTakeawayTurn;
                    cashierTimer = cashierInterval * assist;
                }
            }

            if (cleaner && customers != null)
            {
                cleanerTimer -= Time.deltaTime;
                if (cleanerTimer <= 0f)
                {
                    bool handled = cleanerSpillTurn
                        ? floorSpills != null && floorSpills.TryCleanByWorker()
                        : customers.TryCleanTableByWorker();
                    if (!handled)
                    {
                        if (cleanerSpillTurn) customers.TryCleanTableByWorker();
                        else if (floorSpills != null) floorSpills.TryCleanByWorker();
                    }
                    cleanerSpillTurn = !cleanerSpillTurn;
                    cleanerTimer = cleanerInterval * assist;
                }
            }

            if (runner && wrapStation != null && serviceStation != null)
            {
                runnerTimer -= Time.deltaTime;
                if (runnerTimer <= 0f)
                {
                    bool sendToTakeaway = takeaway != null && takeaway.NeedsWrap && serviceStation.OutputCount >= 2;
                    if (wrapStation.TryTakeOutputForConveyor(out ItemType item))
                    {
                        bool delivered = sendToTakeaway && takeaway.TryReceiveWrap(item, true);
                        if (!delivered) delivered = serviceStation.TryReceiveFromConveyor(item);
                        if (!delivered) wrapStation.ReturnOutputFromConveyor(item);
                    }
                    runnerTimer = runnerInterval * assist;
                }
            }
        }

        private void CreateWorkerVisual(RecruitRole role)
        {
            if (visualParent == null || visualParent.Find("Recruit " + role) != null) return;

            Vector3 position = role switch
            {
                RecruitRole.Cashier => new Vector3(8f, 0.25f, 9.05f),
                RecruitRole.Cleaner => new Vector3(-1.4f, 0.25f, 0f),
                _ => new Vector3(6f, 0.25f, 6.7f)
            };
            Color color = role switch
            {
                RecruitRole.Cashier => new Color(0.95f, 0.42f, 0.24f),
                RecruitRole.Cleaner => new Color(0.38f, 0.70f, 0.64f),
                _ => new Color(0.47f, 0.54f, 0.90f)
            };

            GameObject worker = new("Recruit " + role);
            worker.transform.SetParent(visualParent, false);
            worker.transform.localPosition = position;
            worker.transform.localRotation = role == RecruitRole.Runner
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
            if (MeshyVisuals.TryAttach(
                    worker.transform, "03_cashier_worker", new Vector3(0.9f, 1.68f, 0.9f),
                    Vector3.zero, Vector3.zero, false) == null)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Worker Fallback", PrimitiveType.Capsule, worker.transform,
                    new Vector3(0f, 0.72f, 0f), new Vector3(0.42f, 0.72f, 0.42f), color);
            }
            PrototypeVisuals.CreatePrimitive(
                "Role Badge", PrimitiveType.Sphere, worker.transform,
                new Vector3(0f, 1.48f, -0.30f), new Vector3(0.13f, 0.13f, 0.06f), color);
        }
    }
}
