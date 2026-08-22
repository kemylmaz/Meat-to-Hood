using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// The five jobs that can be handed to someone else. Drive-through work is
    /// split from the dining room on purpose: one till cannot be in two places,
    /// and a shop with a drive-through has two counters taking money.
    /// </summary>
    public enum RecruitRole
    {
        Cashier,
        DriveThruCashier,
        DriveThruRunner,
        Busser,
        SecondBusser
    }

    /// <summary>Recruitable helpers. Each one replaces a specific repeated player action.</summary>
    public sealed class RecruitmentSystem : MonoBehaviour
    {
        public static readonly RecruitRole[] AllRoles =
        {
            RecruitRole.Cashier, RecruitRole.DriveThruCashier, RecruitRole.DriveThruRunner,
            RecruitRole.Busser, RecruitRole.SecondBusser
        };

        [SerializeField, Min(0.1f)] private float cashierInterval = 0.8f;
        [SerializeField, Min(0.1f)] private float busserInterval = 3.2f;
        [SerializeField, Min(0.1f)] private float runnerInterval = 0.72f;

        private CustomerManager customers;
        private ItemStation wrapSource;
        private ItemStation serviceStation;
        private TakeawaySystem driveThru;
        private CashPile till;
        private FloorSpillSystem floorSpills;
        private Transform visualParent;
        /// <summary>Where a busser takes the plates it collects.</summary>
        private Transform trashPoint;

        private readonly Dictionary<RecruitRole, bool> hired = new();
        private readonly Dictionary<RecruitRole, float> timers = new();
        private readonly Dictionary<RecruitRole, Vector3> homes = new();
        /// <summary>Tables a busser is already on its way to, so two do not race for one.</summary>
        private readonly HashSet<CustomerTable> claimedTables = new();
        private bool spillTurn;

        public void Configure(
            CustomerManager customerManager,
            ItemStation wrapProducer,
            ItemStation service,
            TakeawaySystem driveThruWindow,
            CashPile counterTill,
            FloorSpillSystem spillSystem,
            Transform workerVisualParent,
            Transform binPoint,
            IReadOnlyList<Vector3> homePositions)
        {
            customers = customerManager;
            wrapSource = wrapProducer;
            serviceStation = service;
            driveThru = driveThruWindow;
            till = counterTill;
            floorSpills = spillSystem;
            visualParent = workerVisualParent;
            trashPoint = binPoint;

            for (int i = 0; i < AllRoles.Length; i++)
            {
                RecruitRole role = AllRoles[i];
                homes[role] = homePositions != null && i < homePositions.Count
                    ? homePositions[i]
                    : Vector3.zero;
                timers[role] = 0f;
                hired[role] = GameProgress.GetInt(SaveKey(role), 0) == 1;
                RecruitRole captured = role;
                UpgradeProgress.Register(SaveKey(role), 1, () => IsHired(captured) ? 1 : 0);
                if (hired[role]) CreateWorkerVisual(role);
            }
        }

        /// <summary>
        /// Save keys outlive the enum names. The cleaner became a busser and the
        /// runner picked up the drive-through, but they are the same two jobs and
        /// a rename must not repossess a hire the player paid for.
        /// </summary>
        private static string SaveKey(RecruitRole role) => role switch
        {
            RecruitRole.Cashier => "recruit.cashier",
            RecruitRole.DriveThruCashier => "recruit.drivethru.cashier",
            RecruitRole.DriveThruRunner => "recruit.runner",
            RecruitRole.Busser => "recruit.cleaner",
            _ => "recruit.cleaner2"
        };

        public bool IsHired(RecruitRole role) => hired.TryGetValue(role, out bool value) && value;

        /// <summary>
        /// Priced above the station board: each of these removes a whole repeated
        /// chore, so they should be something you save up for rather than pick up
        /// in passing on the first day. The second busser is cheaper than the
        /// first because it only halves a job the first one already covers.
        /// </summary>
        public int GetCost(RecruitRole role) => role switch
        {
            RecruitRole.Cashier => ShopPrices.HireCashier,
            RecruitRole.DriveThruCashier => ShopPrices.HireDriveThruCashier,
            RecruitRole.DriveThruRunner => ShopPrices.HireRunner,
            RecruitRole.Busser => ShopPrices.HireBusser,
            _ => ShopPrices.HireSecondBusser
        };

        public bool TryHire(RecruitRole role, bool free)
        {
            if (IsHired(role)) return false;
            int cost = GetCost(role);
            if (!free && (GameEconomy.Instance == null || !GameEconomy.Instance.TrySpend(cost))) return false;

            hired[role] = true;
            GameProgress.SetInt(SaveKey(role), 1);
            GameProgress.RegisterWorker("recruit." + role);
            GameProgress.RecordUpgrade();
            UpgradeProgress.NotifyChanged();
            CreateWorkerVisual(role);
            if (!free) CoinBurst.Spawn(transform.position + Vector3.up, cost);
            return true;
        }

        /// <summary>
        /// Hands each idle worker its next errand. The timers pace how often a
        /// worker is willing to set off, not how often the job teleports itself
        /// into being done - the work lands when the worker arrives.
        /// </summary>
        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float assist = HumanResourcesSystem.AssistIntervalMultiplier;

            Step(RecruitRole.Cashier, cashierInterval, assist, DispatchCashier);
            Step(RecruitRole.DriveThruCashier, cashierInterval, assist, DispatchDriveThruCashier);
            Step(RecruitRole.DriveThruRunner, runnerInterval, assist, DispatchRunner);
            Step(RecruitRole.Busser, busserInterval, assist, () => DispatchBusser(RecruitRole.Busser));
            Step(RecruitRole.SecondBusser, busserInterval, assist,
                () => DispatchBusser(RecruitRole.SecondBusser));
        }

        private void Step(RecruitRole role, float interval, float assist, System.Func<bool> dispatch)
        {
            if (!IsHired(role)) return;
            float remaining = timers[role] - Time.deltaTime;
            if (remaining <= 0f && dispatch()) remaining = interval * assist;
            timers[role] = remaining;
        }

        /// <summary>
        /// Checkout first, then the till and tables. A cashier is the automation
        /// for serving the queue; customers never pull wraps off the counter on
        /// their own. Cash collection remains their secondary duty.
        /// </summary>
        private bool DispatchCashier()
        {
            WorkerAgent agent = AgentFor(RecruitRole.Cashier);
            if (agent == null || agent.IsBusy) return false;

            if (customers != null && customers.HasCustomerWaitingAtRegister && till != null)
                return agent.Dispatch(till.CollectPoint,
                    () => customers.TryServeNextCustomer(true),
                    ItemType.None, Vector3.zero, null);

            if (till != null && till.HasCash)
                return agent.Dispatch(till.CollectPoint,
                    () => till.TryCollect(true), ItemType.None, Vector3.zero, null);

            CustomerTable table = customers != null ? customers.FindTableWithCash() : null;
            if (table == null) return false;
            return agent.Dispatch(table.CashPoint,
                () => table.TryAutoCollectCash(), ItemType.None, Vector3.zero, null);
        }

        private bool DispatchDriveThruCashier()
        {
            WorkerAgent agent = AgentFor(RecruitRole.DriveThruCashier);
            if (agent == null || agent.IsBusy || driveThru == null || driveThru.PendingCash <= 0)
                return false;

            return agent.Dispatch(driveThru.transform.position,
                () => driveThru.TryAutoCollectCash(), ItemType.None, Vector3.zero, null);
        }

        /// <summary>
        /// Carries wraps out to the drive-through window, and falls back to
        /// stocking the front counter when there is no car waiting - an idle
        /// runner standing next to a full oven is a runner doing nothing.
        /// </summary>
        private bool DispatchRunner()
        {
            WorkerAgent agent = AgentFor(RecruitRole.DriveThruRunner);
            if (agent == null || agent.IsBusy || wrapSource == null) return false;
            if (wrapSource.OutputCount <= 0) return false;

            bool toDriveThru = driveThru != null && driveThru.NeedsWrap;
            Transform destination = toDriveThru
                ? driveThru.transform
                : serviceStation != null ? serviceStation.transform : null;
            if (destination == null) return false;

            ItemType carried = ItemType.None;
            return agent.Dispatch(
                wrapSource.transform.position,
                () => wrapSource.TryTakeOutputForConveyor(out carried),
                ItemType.Wrap,
                destination.position,
                () =>
                {
                    bool delivered = toDriveThru && driveThru.TryReceiveWrap(carried, true);
                    if (!delivered && serviceStation != null)
                        delivered = serviceStation.TryReceiveFromConveyor(carried);
                    // Nowhere to put it: give it back rather than losing the wrap.
                    if (!delivered) wrapSource.ReturnOutputFromConveyor(carried);
                    return delivered;
                });
        }

        private bool DispatchBusser(RecruitRole role)
        {
            WorkerAgent agent = AgentFor(role);
            if (agent == null || agent.IsBusy) return false;

            CustomerTable table = FindUnclaimedDirtyTable();
            if (table != null && trashPoint != null)
            {
                claimedTables.Add(table);
                // Plates are carried to the bin rather than vanishing at the table.
                return agent.Dispatch(table.transform.position,
                    () => table.TryAutoCleanTrash(), ItemType.Trash,
                    trashPoint.position,
                    () =>
                    {
                        claimedTables.Remove(table);
                        return true;
                    });
            }

            if (floorSpills != null && spillTurn)
                return agent.Dispatch(transform.position, () => floorSpills.TryCleanByWorker(),
                    ItemType.None, Vector3.zero, null);

            spillTurn = !spillTurn;
            return false;
        }

        /// <summary>
        /// A dirty table nobody is already walking to. Without the claim both
        /// bussers set off for the same table and one of them arrived to a job
        /// that had already been done.
        /// </summary>
        private CustomerTable FindUnclaimedDirtyTable()
        {
            if (customers == null) return null;
            claimedTables.RemoveWhere(table => table == null || !table.IsDirty);
            return customers.FindDirtyTable(claimedTables);
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

            Vector3 position = homes.TryGetValue(role, out Vector3 home) ? home : Vector3.zero;
            Color color = role switch
            {
                RecruitRole.Cashier => new Color(0.95f, 0.42f, 0.24f),
                RecruitRole.DriveThruCashier => new Color(0.96f, 0.72f, 0.26f),
                RecruitRole.DriveThruRunner => new Color(0.47f, 0.54f, 0.90f),
                _ => new Color(0.38f, 0.70f, 0.64f)
            };

            GameObject worker = new("Recruit " + role);
            worker.transform.SetParent(visualParent, false);
            worker.transform.localPosition = position;
            // Models look along +Z, so a half turn faces the shop floor rather
            // than the street. Only the first appearance uses this - once a job
            // comes in the agent turns them by where they are walking.
            worker.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (MeshyVisuals.TryAttachAuthored(
                    worker.transform, "03_cashier_worker", Vector3.zero, Vector3.zero) == null)
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
            // Fitted before the agent is configured: the agent picks the capsule up
            // there and walks through it from then on.
            CharacterBody.Attach(worker);
            worker.AddComponent<WorkerAgent>().Configure(worker.transform.position, hands);
        }
    }
}
