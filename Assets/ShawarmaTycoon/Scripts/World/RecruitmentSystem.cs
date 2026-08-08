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
        private Transform visualParent;
        private float cashierTimer;
        private float cleanerTimer;
        private float runnerTimer;
        private bool cashier;
        private bool cleaner;
        private bool runner;

        public void Configure(CustomerManager customerManager, ItemStation wrap, ItemStation service, Transform workerVisualParent)
        {
            customers = customerManager;
            wrapStation = wrap;
            serviceStation = service;
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

        public int GetCost(RecruitRole role) => role switch
        {
            RecruitRole.Cashier => 160,
            RecruitRole.Cleaner => 130,
            _ => 180
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
                    customers.TryCollectTableCashByWorker();
                    cashierTimer = cashierInterval * assist;
                }
            }

            if (cleaner && customers != null)
            {
                cleanerTimer -= Time.deltaTime;
                if (cleanerTimer <= 0f)
                {
                    customers.TryCleanTableByWorker();
                    cleanerTimer = cleanerInterval * assist;
                }
            }

            if (runner && wrapStation != null && serviceStation != null)
            {
                runnerTimer -= Time.deltaTime;
                if (runnerTimer <= 0f)
                {
                    if (wrapStation.TryTakeOutputForConveyor(out ItemType item) && !serviceStation.TryReceiveFromConveyor(item))
                        wrapStation.ReturnOutputFromConveyor(item);
                    runnerTimer = runnerInterval * assist;
                }
            }
        }

        private void CreateWorkerVisual(RecruitRole role)
        {
            if (visualParent == null || visualParent.Find("Recruit " + role) != null) return;

            Vector3 position = role switch
            {
                RecruitRole.Cashier => new Vector3(7.55f, 0.64f, 3.95f),
                RecruitRole.Cleaner => new Vector3(2.15f, 0.64f, -2.2f),
                _ => new Vector3(5.2f, 0.64f, 3.95f)
            };
            Color color = role switch
            {
                RecruitRole.Cashier => new Color(0.95f, 0.42f, 0.24f),
                RecruitRole.Cleaner => new Color(0.38f, 0.70f, 0.64f),
                _ => new Color(0.47f, 0.54f, 0.90f)
            };

            GameObject worker = PrototypeVisuals.CreatePrimitive("Recruit " + role, PrimitiveType.Capsule, visualParent, position,
                new Vector3(0.42f, 0.58f, 0.42f), color);
            PrototypeVisuals.CreatePrimitive("Hat", PrimitiveType.Sphere, worker.transform, new Vector3(0f, 0.68f, 0f),
                new Vector3(0.50f, 0.16f, 0.50f), PrototypeVisuals.Cream);
        }
    }
}
