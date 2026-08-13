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
        private GameObject workerVisual;
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

        private void ApplyLevel(bool animate)
        {
            if (upgradeType == StationUpgradeType.Worker)
            {
                station?.SetWorkerLevel(level);
                if (level > 0)
                {
                    GameProgress.RegisterWorker(gameObject.name);
                    CreateWorker();
                }
            }
            else conveyor?.SetLevel(level);
        }

        private void CreateWorker()
        {
            if (workerVisual != null) return;
            Transform host = station != null ? station.transform.parent : transform.parent;
            Vector3 position = station != null
                ? station.transform.localPosition + new Vector3(0f, 0f, -1.05f)
                : transform.localPosition + new Vector3(0f, 0f, -0.9f);
            workerVisual = new GameObject(station != null ? "Isci " + station.name : "Isci");
            workerVisual.transform.SetParent(host, false);
            workerVisual.transform.localPosition = position;
            workerVisual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (MeshyVisuals.TryAttach(
                    workerVisual.transform, "03_cashier_worker", new Vector3(0.9f, 1.68f, 0.9f),
                    Vector3.zero, Vector3.zero, false) == null)
            {
                PrototypeVisuals.CreatePrimitive(
                    "Worker Fallback", PrimitiveType.Capsule, workerVisual.transform,
                    new Vector3(0f, 0.72f, 0f), new Vector3(0.42f, 0.72f, 0.42f),
                    new Color(0.95f, 0.35f, 0.26f));
            }
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            string title = upgradeType == StationUpgradeType.Worker ? "ISCI" : "BANT";
            label.text = level >= maxLevel ? $"{title} MAX" : $"{title} LV.{level + 1}\n{CurrentCost}";
        }
    }
}
