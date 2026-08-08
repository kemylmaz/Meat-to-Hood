using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class ManagementRoomUnlockPad : MonoBehaviour
    {
        [SerializeField, Min(1)] private int cost = 120;
        [SerializeField, Min(0.2f)] private float radius = 1.25f;
        private Transform player;
        private GameObject roomRoot;
        private TextMesh label;
        private string saveKey = "office";
        private string displayName = "OFFICE";
        private bool unlocked;

        public void Configure(Transform playerTransform, GameObject targetRoot, int unlockCost)
        {
            Configure(playerTransform, targetRoot, unlockCost, "office", "OFFICE");
        }

        public void Configure(Transform playerTransform, GameObject targetRoot, int unlockCost, string uniqueKey, string title)
        {
            player = playerTransform;
            roomRoot = targetRoot;
            cost = unlockCost;
            saveKey = string.IsNullOrWhiteSpace(uniqueKey) ? "office" : uniqueKey;
            displayName = string.IsNullOrWhiteSpace(title) ? "OFFICE" : title;
            unlocked = GameProgress.GetInt(saveKey, 0) == 1;
            if (roomRoot != null) roomRoot.SetActive(unlocked);
            label = PrototypeVisuals.CreateLabel(displayName + "\n$" + cost, transform, Vector3.up * 0.42f, 0.12f);
            label.gameObject.SetActive(false);
            if (unlocked) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (unlocked || player == null || roomRoot == null) return;
            bool nearby = Vector3.SqrMagnitude(player.position - transform.position) <= radius * radius;
            if (label != null) label.gameObject.SetActive(nearby);
            if (!nearby || GameEconomy.Instance == null || !GameEconomy.Instance.TrySpend(cost)) return;

            unlocked = true;
            roomRoot.SetActive(true);
            GameProgress.SetInt(saveKey, 1);
            GameProgress.RecordUpgrade();
            CoinBurst.Spawn(transform.position + Vector3.up * 0.7f, cost);
            gameObject.SetActive(false);
        }
    }
}
