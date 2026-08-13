using UnityEngine;
using System.Collections.Generic;

namespace ShawarmaTycoon
{
    public enum EmployeeUpgradeType { MovementSpeed, Capacity, AdoptUse }

    /// <summary>Shared HR upgrades apply to every hired employee and station worker.</summary>
    public sealed class HumanResourcesSystem : MonoBehaviour
    {
        public static HumanResourcesSystem Instance { get; private set; }

        [SerializeField, Min(1)] private int maxLevel = 5;
        private Transform player;
        private readonly List<ConveyorLink> conveyors = new();
        private int movementLevel;
        private int capacityLevel;
        private int adoptUseLevel;

        public static float WorkerSpeedMultiplier => Instance == null ? 1f : 1f + Instance.movementLevel * 0.12f;
        public static int WorkerCapacityBonus => Instance == null ? 0 : Instance.capacityLevel * 2;
        /// <summary>
        /// Automation rate, applied to both the hired assistants' action interval
        /// and every conveyor belt. Lower is faster.
        /// </summary>
        public static float AssistIntervalMultiplier => Instance == null ? 1f : Mathf.Lerp(1f, 0.55f, Instance.adoptUseLevel / 5f);

        private void Awake()
        {
            Instance = this;
        }

        public void Configure(Transform playerTransform, IEnumerable<ConveyorLink> automationConveyors)
        {
            player = playerTransform;
            conveyors.Clear();
            if (automationConveyors != null) conveyors.AddRange(automationConveyors);
            movementLevel = GameProgress.GetInt("hr.movement", 0);
            capacityLevel = GameProgress.GetInt("hr.capacity", 0);
            adoptUseLevel = GameProgress.GetInt("hr.adopt", 0);
            ApplyAutomationLevel();
        }

        public int GetLevel(EmployeeUpgradeType type) => type switch
        {
            EmployeeUpgradeType.MovementSpeed => movementLevel,
            EmployeeUpgradeType.Capacity => capacityLevel,
            _ => adoptUseLevel
        };

        public int GetCost(EmployeeUpgradeType type)
        {
            // Automation was the cheapest thing on the board at 150, below a
            // single belt, while speeding up every belt and every assistant at
            // once. It is the strongest upgrade here and is priced like it.
            int baseCost = type switch
            {
                EmployeeUpgradeType.MovementSpeed => 500,
                EmployeeUpgradeType.Capacity => 300,
                _ => 450
            };
            return Mathf.RoundToInt(baseCost * Mathf.Pow(1.55f, GetLevel(type)));
        }

        public bool TryUpgrade(EmployeeUpgradeType type, bool free)
        {
            int level = GetLevel(type);
            if (level >= maxLevel) return false;

            int cost = GetCost(type);
            if (!free && (GameEconomy.Instance == null || !GameEconomy.Instance.TrySpend(cost))) return false;

            level++;
            switch (type)
            {
                case EmployeeUpgradeType.MovementSpeed:
                    movementLevel = level;
                    GameProgress.SetInt("hr.movement", level);
                    break;
                case EmployeeUpgradeType.Capacity:
                    capacityLevel = level;
                    GameProgress.SetInt("hr.capacity", level);
                    break;
                default:
                    adoptUseLevel = level;
                    GameProgress.SetInt("hr.adopt", level);
                    ApplyAutomationLevel();
                    break;
            }

            GameProgress.RecordUpgrade();
            if (!free) CoinBurst.Spawn(player != null ? player.position + Vector3.up * 1.2f : transform.position + Vector3.up, cost);
            return true;
        }

        /// <summary>
        /// Pushes the new automation rate onto the belts. It does not touch their
        /// levels: which belts you own is what their own management pads sell, and
        /// assigning levels here used to wipe every belt the player had paid for.
        /// </summary>
        private void ApplyAutomationLevel()
        {
            for (int i = 0; i < conveyors.Count; i++)
                if (conveyors[i] != null) conveyors[i].ApplyInterval();
        }
    }
}
