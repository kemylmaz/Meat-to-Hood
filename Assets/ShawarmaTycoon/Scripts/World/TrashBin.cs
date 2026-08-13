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
        private float cooldown;
        private float dwell;

        public void Configure(Transform playerTransform, CarryInventory playerInventory)
        {
            player = playerTransform;
            inventory = playerInventory;
            label = PrototypeVisuals.CreateLabel("", transform, Vector3.up * 1.05f, 0.12f);
            label.gameObject.SetActive(false);
        }

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (player == null || inventory == null) return;

            bool carrying = inventory.TrashCount > 0 || inventory.Count > 0;
            bool nearby = Vector3.SqrMagnitude(player.position - transform.position) <= interactionRadius * interactionRadius;
            if (label != null)
            {
                label.gameObject.SetActive(nearby && carrying);
                if (nearby && carrying) label.text = "AT";
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
