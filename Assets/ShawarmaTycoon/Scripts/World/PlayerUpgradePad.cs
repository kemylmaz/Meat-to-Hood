using UnityEngine;

namespace ShawarmaTycoon
{
    public enum PlayerUpgradeType { MoveSpeed, CarryCapacity }

    public sealed class PlayerUpgradePad : MonoBehaviour
    {
        [SerializeField] private PlayerUpgradeType upgradeType;
        [SerializeField, Min(1)] private int baseCost = 50;
        [SerializeField, Min(1)] private int maxLevel = 5;
        [SerializeField, Min(0.5f)] private float radius = 1.2f;
        private Transform player;
        private MobilePlayerController motor;
        private CarryInventory inventory;
        private TextMesh label;
        private int level;
        private string saveKey;
        private float cooldown;

        private int CurrentCost => Mathf.RoundToInt(baseCost * Mathf.Pow(1.65f, level));

        public void Configure(Transform playerTransform, MobilePlayerController playerMotor, CarryInventory playerInventory, PlayerUpgradeType type, int cost)
        {
            player = playerTransform;
            motor = playerMotor;
            inventory = playerInventory;
            upgradeType = type;
            baseCost = cost;
            saveKey = "player." + type;
            level = GameProgress.GetInt(saveKey, 0);
            ApplyLevel();
            label = PrototypeVisuals.CreateLabel("", transform, Vector3.up * 0.38f, 0.12f);
            label.gameObject.SetActive(false);
            RefreshLabel();
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (player == null || level >= maxLevel) { if (label != null) label.gameObject.SetActive(false); return; }
            bool nearby = Vector3.SqrMagnitude(player.position - transform.position) <= radius * radius;
            if (label != null) label.gameObject.SetActive(nearby);
            if (!nearby || cooldown > 0f || GameEconomy.Instance == null || !GameEconomy.Instance.TrySpend(CurrentCost)) return;

            int paid = CurrentCost;
            level++;
            GameProgress.SetInt(saveKey, level);
            GameProgress.RecordUpgrade();
            ApplyLevel();
            CoinBurst.Spawn(transform.position + Vector3.up * 0.7f, paid);
            cooldown = 0.8f;
            RefreshLabel();
        }

        private void ApplyLevel()
        {
            if (upgradeType == PlayerUpgradeType.MoveSpeed) motor?.SetUpgradeLevel(level);
            else inventory?.SetCapacityUpgradeLevel(level);
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            string title = upgradeType == PlayerUpgradeType.MoveSpeed ? "HIZ" : "KAPASITE";
            label.text = level >= maxLevel ? $"{title} MAX" : $"{title} LV.{level + 1}\n{CurrentCost}";
        }
    }
}
