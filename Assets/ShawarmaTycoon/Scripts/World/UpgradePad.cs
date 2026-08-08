using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class UpgradePad : MonoBehaviour
    {
        [SerializeField, Min(1)] private int baseCost = 60;
        [SerializeField, Min(1f)] private float costMultiplier = 1.8f;
        [SerializeField, Min(0.1f)] private float holdDuration = 0.85f;
        [SerializeField, Min(0.5f)] private float interactionRadius = 1.25f;

        private Transform player;
        private DioramaExpansion expansion;
        private TextMesh label;
        private Renderer padRenderer;
        private float holdProgress;
        private int purchaseCount;
        private int lastDisplayedCost = -1;
        private int lastDisplayedPercent = -1;

        public int CurrentCost => Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, purchaseCount));
        public float Progress => holdDuration <= 0f ? 0f : holdProgress / holdDuration;

        public void Configure(Transform playerTransform, DioramaExpansion targetExpansion, int initialCost)
        {
            player = playerTransform;
            expansion = targetExpansion;
            baseCost = Mathf.Max(1, initialCost);
            purchaseCount = expansion != null ? expansion.UnlockedCount : 0;

            padRenderer = GetComponentInChildren<Renderer>();
            label = PrototypeVisuals.CreateLabel("", transform, new Vector3(0f, 0.35f, 0f), 0.14f);
            UpdateLabel();
            label.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (expansion == null || expansion.Remaining <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            bool nearby = player != null &&
                Vector3.SqrMagnitude(player.position - transform.position) <= interactionRadius * interactionRadius;
            if (label != null) label.gameObject.SetActive(nearby);

            if (!nearby)
            {
                holdProgress = Mathf.Max(0f, holdProgress - Time.deltaTime * 2f);
                UpdateVisual();
                return;
            }

            if (GameEconomy.Instance == null || GameEconomy.Instance.Coins < CurrentCost)
            {
                holdProgress = 0f;
                UpdateVisual();
                return;
            }

            holdProgress += Time.deltaTime;
            if (holdProgress >= holdDuration)
            {
                int price = CurrentCost;
                if (GameEconomy.Instance.TrySpend(price) && expansion.UnlockNext())
                    purchaseCount++;
                holdProgress = 0f;
            }

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (padRenderer != null)
            {
                Color target = GameEconomy.Instance != null && GameEconomy.Instance.Coins >= CurrentCost
                    ? Color.Lerp(PrototypeVisuals.Green, Color.white, Progress * 0.45f)
                    : new Color(0.55f, 0.44f, 0.39f);
                padRenderer.sharedMaterial = PrototypeVisuals.Material(target);
            }

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (label == null) return;

            int cost = CurrentCost;
            int percent = Mathf.FloorToInt(Mathf.Clamp01(Progress) * 10f) * 10;
            if (cost == lastDisplayedCost && percent == lastDisplayedPercent)
                return;

            lastDisplayedCost = cost;
            lastDisplayedPercent = percent;
            string progressText = percent > 0 ? $"\n%{percent}" : "";
            label.text = $"GENİŞLET\n₺{cost}{progressText}";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
