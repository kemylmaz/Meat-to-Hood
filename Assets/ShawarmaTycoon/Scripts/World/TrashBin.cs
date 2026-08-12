using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class TrashBin : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float interactionRadius = 1.25f;
        private Transform player;
        private CarryInventory inventory;
        private TextMesh label;
        private float cooldown;

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

            bool nearby = Vector3.SqrMagnitude(player.position - transform.position) <= interactionRadius * interactionRadius;
            if (label != null) label.gameObject.SetActive(nearby && inventory.Count > 0);
            if (label != null && nearby && inventory.Count > 0) label.text = "AT";
            if (!nearby || inventory.Count == 0 || cooldown > 0f) return;

            ItemType discardedType = inventory.HeldType;
            int discarded = inventory.Clear();
            if (discardedType == ItemType.Trash) GameProgress.RecordTrash(discarded);
            AudioDirector.Play(GameSfx.Trash);
            cooldown = 0.4f;
        }
    }
}
