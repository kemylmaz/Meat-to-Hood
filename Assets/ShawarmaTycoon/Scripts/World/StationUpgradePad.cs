using UnityEngine;

namespace ShawarmaTycoon
{
    public enum StationUpgradeType { Worker, Conveyor }

    public sealed class StationUpgradePad : MonoBehaviour
    {
        [SerializeField] private StationUpgradeType upgradeType;
        [SerializeField, Min(1)] private int baseCost = 40;
        [SerializeField, Min(1)] private int maxLevel = 3;
        [SerializeField, Min(0.2f)] private float radius = 1.2f;
        private Transform player;
        private ItemStation station;
        private ConveyorLink conveyor;
        private TextMesh label;
        private int level;
        private string saveKey;
        private float cooldown;

        private int CurrentCost => Mathf.RoundToInt(baseCost * Mathf.Pow(1.7f, level));

        public void Configure(Transform playerTransform, StationUpgradeType type, int price, ItemStation targetStation, ConveyorLink targetConveyor)
        {
            player = playerTransform;
            upgradeType = type;
            baseCost = price;
            station = targetStation;
            conveyor = targetConveyor;
            saveKey = "station." + gameObject.name;
            level = GameProgress.GetInt(saveKey, 0);
            ApplyLevel(false);
            // Small and lifted clear: these pads sit in rows, and at the old size the
            // captions of neighbouring pads overlapped into an unreadable pile.
            label = PrototypeVisuals.CreateLabel("", transform, Vector3.up * 0.62f, 0.072f);
            RefreshLabel();
            label.gameObject.SetActive(false);
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (player == null || level >= maxLevel) { if (label != null) label.gameObject.SetActive(false); return; }
            bool nearby = Vector3.SqrMagnitude(player.position - transform.position) <= radius * radius;
            if (label != null) label.gameObject.SetActive(nearby);
            if (!nearby || cooldown > 0f || GameEconomy.Instance == null) return;

            int price = CurrentCost;
            if (!GameEconomy.Instance.TrySpend(price)) return;
            level++;
            GameProgress.SetInt(saveKey, level);
            GameProgress.RecordUpgrade();
            ApplyLevel(true);
            CoinBurst.Spawn(transform.position + Vector3.up * 0.7f, price);
            cooldown = 0.9f;
            RefreshLabel();
        }

        /// <summary>
        /// The station owns the worker figure, keyed to its own worker level. The
        /// pad used to spawn a second one of its own, a metre in front of the
        /// counter on the customer side, standing in the conveyor.
        /// </summary>
        private void ApplyLevel(bool animate)
        {
            if (upgradeType == StationUpgradeType.Worker)
            {
                station?.SetWorkerLevel(level);
                if (level > 0) GameProgress.RegisterWorker(gameObject.name);
            }
            else conveyor?.SetLevel(level);
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            string title = upgradeType == StationUpgradeType.Worker ? "ISCI" : "BANT";
            label.text = level >= maxLevel ? $"{title} MAX" : $"{title} LV.{level + 1}\n{CurrentCost}";
        }
    }
}
