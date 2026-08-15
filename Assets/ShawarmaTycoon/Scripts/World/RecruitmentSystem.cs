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
        /// <summary>Where the cleaner takes the plates it collects.</summary>
        private Transform trashPoint;
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
            Transform workerVisualParent,
            Transform binPoint)
        {
            customers = customerManager;
            wrapStation = wrap;
            serviceStation = service;
            takeaway = takeawayCounter;
            floorSpills = spillSystem;
            visualParent = workerVisualParent;
            trashPoint = binPoint;
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

        /// <summary>
        /// Hands each idle worker its next errand. The timers now pace how often
        /// a worker is willing to set off, not how often the job teleports itself
        /// into being done - the work lands when the worker arrives.
        /// </summary>
        private void Update()
        {
            float assist = HumanResourcesSystem.AssistIntervalMultiplier;

            if (cashier)
            {
                cashierTimer -= Time.deltaTime;
                if (cashierTimer <= 0f && DispatchCashier())
                    cashierTimer = cashierInterval * assist;
            }

            if (cleaner)
            {
                cleanerTimer -= Time.deltaTime;
                if (cleanerTimer <= 0f && DispatchCleaner())
                    cleanerTimer = cleanerInterval * assist;
            }

            if (runner)
            {
                runnerTimer -= Time.deltaTime;
                if (runnerTimer <= 0f && DispatchRunner())
                    runnerTimer = runnerInterval * assist;
            }
        }

        private bool DispatchCashier()
        {
            WorkerAgent agent = AgentFor(RecruitRole.Cashier);
            if (agent == null || agent.IsBusy) return false;

            // Alternate so a busy till does not starve the dining room.
            if (cashierTakeawayTurn && takeaway != null && takeaway.PendingCash > 0)
            {
                cashierTakeawayTurn = false;
                return agent.Dispatch(takeaway.transform.position,
                    () => takeaway.TryAutoCollectCash(), ItemType.None, Vector3.zero, null);
            }

            CustomerTable table = customers != null ? customers.FindTableWithCash() : null;
            if (table != null)
            {
                cashierTakeawayTurn = true;
                return agent.Dispatch(table.CashPoint,
                    () => table.TryAutoCollectCash(), ItemType.None, Vector3.zero, null);
            }

            if (takeaway != null && takeaway.PendingCash > 0)
                return agent.Dispatch(takeaway.transform.position,
                    () => takeaway.TryAutoCollectCash(), ItemType.None, Vector3.zero, null);

            return false;
        }

        private bool DispatchCleaner()
        {
            WorkerAgent agent = AgentFor(RecruitRole.Cleaner);
            if (agent == null || agent.IsBusy) return false;

            CustomerTable table = customers != null ? customers.FindDirtyTable() : null;
            if (table != null && trashPoint != null)
            {
                // Plates are carried to the bin rather than vanishing at the table.
                return agent.Dispatch(table.transform.position,
                    () => table.TryAutoCleanTrash(), ItemType.Trash,
                    trashPoint.position, () => true);
            }

            if (floorSpills != null && cleanerSpillTurn)
                return agent.Dispatch(transform.position, () => floorSpills.TryCleanByWorker(),
                    ItemType.None, Vector3.zero, null);

            cleanerSpillTurn = !cleanerSpillTurn;
            return false;
        }

        private bool DispatchRunner()
        {
            WorkerAgent agent = AgentFor(RecruitRole.Runner);
            if (agent == null || agent.IsBusy || wrapStation == null || serviceStation == null)
                return false;
            if (wrapStation.OutputCount <= 0) return false;

            bool toTakeaway = takeaway != null && takeaway.NeedsWrap && serviceStation.OutputCount >= 2;
            Vector3 dropAt = toTakeaway ? takeaway.transform.position : serviceStation.transform.position;

            ItemType carried = ItemType.None;
            return agent.Dispatch(
                wrapStation.transform.position,
                () =>
                {
                    if (!wrapStation.TryTakeOutputForConveyor(out carried)) return false;
                    return true;
                },
                ItemType.Wrap,
                dropAt,
                () =>
                {
                    bool delivered = toTakeaway && takeaway.TryReceiveWrap(carried, true);
                    if (!delivered) delivered = serviceStation.TryReceiveFromConveyor(carried);
                    // Nowhere to put it: give it back rather than losing the wrap.
                    if (!delivered) wrapStation.ReturnOutputFromConveyor(carried);
                    return delivered;
                });
        }

        private WorkerAgent AgentFor(RecruitRole role)
        {
            if (visualParent == null) return null;
            Transform found = visualParent.Find("Recruit " + role);
            return found != null ? found.GetComponent<WorkerAgent>() : null;
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

            // Hands of their own, so the animation driver can tell walking from
            // carrying and the plate or wrap is visible on the way.
            CarryInventory hands = worker.AddComponent<CarryInventory>();
            hands.Configure(4);
            worker.AddComponent<CozyAnimationDriver>();
            worker.AddComponent<WorkerAgent>().Configure(position, hands);
        }
    }
}
