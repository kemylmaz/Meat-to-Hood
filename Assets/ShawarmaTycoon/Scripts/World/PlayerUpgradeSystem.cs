using UnityEngine;

namespace ShawarmaTycoon
{
    public enum GeneralManagerUpgradeType { MovementSpeed, Capacity, IncomeIncrease }

    /// <summary>Player-owned upgrades sold by the General Manager office.</summary>
    public sealed class PlayerUpgradeSystem : MonoBehaviour
    {
        public static PlayerUpgradeSystem Instance { get; private set; }

        [SerializeField, Min(1)] private int maxLevel = 5;
        private Transform player;
        private MobilePlayerController motor;
        private CarryInventory inventory;
        private int movementLevel;
        private int capacityLevel;
        private int incomeLevel;

        public static float IncomeMultiplier => Instance == null ? 1f : 1f + Instance.incomeLevel * 0.12f;

        private void Awake()
        {
            Instance = this;
        }

        public void Configure(Transform playerTransform, MobilePlayerController playerMotor, CarryInventory playerInventory)
        {
            player = playerTransform;
            motor = playerMotor;
            inventory = playerInventory;
            movementLevel = GameProgress.GetInt("gm.movement", 0);
            capacityLevel = GameProgress.GetInt("gm.capacity", 0);
            incomeLevel = GameProgress.GetInt("gm.income", 0);
            ApplyLevels();
        }

        public int GetLevel(GeneralManagerUpgradeType type) => type switch
        {
            GeneralManagerUpgradeType.MovementSpeed => movementLevel,
            GeneralManagerUpgradeType.Capacity => capacityLevel,
            _ => incomeLevel
        };

        public int GetCost(GeneralManagerUpgradeType type)
        {
            int baseCost = type switch
            {
                GeneralManagerUpgradeType.MovementSpeed => 200,
                GeneralManagerUpgradeType.Capacity => 700,
                _ => 600
            };
            return Mathf.RoundToInt(baseCost * Mathf.Pow(1.55f, GetLevel(type)));
        }

        public bool TryUpgrade(GeneralManagerUpgradeType type, bool free)
        {
            int level = GetLevel(type);
            if (level >= maxLevel) return false;

            int cost = GetCost(type);
            if (!free && (GameEconomy.Instance == null || !GameEconomy.Instance.TrySpend(cost))) return false;

            level++;
            switch (type)
            {
                case GeneralManagerUpgradeType.MovementSpeed:
                    movementLevel = level;
                    GameProgress.SetInt("gm.movement", level);
                    break;
                case GeneralManagerUpgradeType.Capacity:
                    capacityLevel = level;
                    GameProgress.SetInt("gm.capacity", level);
                    break;
                default:
                    incomeLevel = level;
                    GameProgress.SetInt("gm.income", level);
                    break;
            }

            ApplyLevels();
            GameProgress.RecordUpgrade();
            if (!free) CoinBurst.Spawn(player != null ? player.position + Vector3.up * 1.2f : transform.position + Vector3.up, cost);
            return true;
        }

        public static int ApplyIncome(int amount) => Mathf.Max(1, Mathf.RoundToInt(amount * IncomeMultiplier));

        private void ApplyLevels()
        {
            motor?.SetUpgradeLevel(movementLevel);
            inventory?.SetCapacityUpgradeLevel(capacityLevel);
        }
    }
}
