using UnityEngine;

namespace ShawarmaTycoon
{
    public enum GeneralManagerUpgradeType { MovementSpeed, Capacity, IncomeIncrease }

    /// <summary>Player-owned upgrades sold by the General Manager office.</summary>
    public sealed class PlayerUpgradeSystem : MonoBehaviour
    {
        public static PlayerUpgradeSystem Instance { get; private set; }

        [SerializeField, Min(1)] private int maxLevel = ShopPrices.BoardLevels;
        private Transform player;
        private MobilePlayerController motor;
        private CarryInventory inventory;
        private int movementLevel;
        private int capacityLevel;
        private int incomeLevel;

        public static float IncomeMultiplier => Instance == null ? 1f : 1f + Instance.incomeLevel * 0.12f;
        /// <summary>
        /// Capacity is also counter skill: better tray handling lets the player
        /// hand over complete orders faster and earns a small personal-service
        /// bonus. It therefore keeps value after the kitchen is automated.
        /// </summary>
        public static float ManualCheckoutIntervalMultiplier => Instance == null
            ? 1f
            : 1f / (1f + Instance.capacityLevel * 0.18f);

        public static float ManualServiceRewardMultiplier => Instance == null
            ? 1f
            : 1f + Instance.capacityLevel * 0.05f;

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
            foreach (GeneralManagerUpgradeType type in
                     (GeneralManagerUpgradeType[])System.Enum.GetValues(typeof(GeneralManagerUpgradeType)))
            {
                GeneralManagerUpgradeType captured = type;
                UpgradeProgress.Register("gm." + type, maxLevel, () => GetLevel(captured));
            }
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
            // Income last: it compounds with everything else, so it stays the
            // dearest line on the board even after the whole board came down to a
            // sixth of what it used to cost.
            int baseCost = type switch
            {
                GeneralManagerUpgradeType.MovementSpeed => ShopPrices.PlayerSpeed,
                GeneralManagerUpgradeType.Capacity => ShopPrices.PlayerCapacity,
                _ => ShopPrices.PlayerIncome
            };
            return ShopPrices.BoardCost(baseCost, GetLevel(type));
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
            UpgradeProgress.NotifyChanged();
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
