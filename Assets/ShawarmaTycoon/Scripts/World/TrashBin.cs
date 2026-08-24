using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class TrashBin : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float interactionRadius = 1.25f;
        /// <summary>How long the player must linger before stock, not plates, is thrown out.</summary>
        [SerializeField, Min(0.1f)] private float stockDiscardDelay = 1.2f;
        private Transform player;
        private CarryInventory inventory;
        private TextMesh label;
        private Collider interactionSurface;
        private Transform workerApproach;
        private float cooldown;
        private float dwell;

        /// <summary>
        /// A clear point on the dining-room side of the large container. Workers
        /// target this instead of its solid centre, which they cannot enter.
        /// </summary>
        public Transform WorkerApproach => workerApproach != null ? workerApproach : transform;

        public void Configure(Transform playerTransform, CarryInventory playerInventory)
        {
            player = playerTransform;
            inventory = playerInventory;
            interactionSurface = transform.Find("Konteyner Çarpışma")?.GetComponent<Collider>();
            workerApproach = transform.Find("Çalışan Yaklaşma Noktası");
            label = PrototypeVisuals.CreateCozyBadge(
                "AT", transform, Vector3.up * 1.52f, 0.74f,
                UI.UITheme.CreamLight, UI.UITheme.Ink);
            label.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            cooldown -= Time.deltaTime;
            if (player == null || inventory == null) return;

            bool carrying = inventory.TrashCount > 0 || inventory.Count > 0;
            Vector3 nearest = interactionSurface != null
                ? interactionSurface.ClosestPoint(player.position)
                : transform.position;
            bool nearby = Vector3.SqrMagnitude(player.position - nearest) <= interactionRadius * interactionRadius;
            if (label != null)
            {
                label.gameObject.SetActive(nearby && carrying);
            }
            dwell = nearby ? dwell + Time.deltaTime : 0f;
            if (!nearby || !carrying || cooldown > 0f) return;

            // Plates go first and go instantly. Binning stock is the deliberate
            // way out of a jammed line, so it wants a moment of standing still:
            // the bin used to empty whatever was in hand the instant you touched
            // it, and the route to the tables runs right past it.
            if (inventory.TrashCount > 0) GameProgress.RecordTrash(inventory.ClearTrash());
            else if (dwell >= stockDiscardDelay) inventory.Clear();
            else return;

            AudioDirector.Play(GameSfx.Trash);
            cooldown = 0.4f;
        }
    }
}
