using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class TutorialArrow : MonoBehaviour
    {
        private CarryInventory inventory;
        private Transform source;
        private Transform oven;
        private Transform cutting;
        private Transform service;
        private int completedCycles;
        private ItemType previousHeldType;

        public void Configure(
            CarryInventory playerInventory,
            Transform sourceTarget,
            Transform ovenTarget,
            Transform cuttingTarget,
            Transform serviceTarget)
        {
            inventory = playerInventory;
            source = sourceTarget;
            oven = ovenTarget;
            cutting = cuttingTarget;
            service = serviceTarget;
        }

        private void Update()
        {
            if (completedCycles > 0 || inventory == null) { gameObject.SetActive(false); return; }
            if (previousHeldType == ItemType.Wrap && inventory.HeldType == ItemType.None)
            {
                completedCycles++;
                gameObject.SetActive(false);
                return;
            }

            Transform target = inventory.Count == 0 ? source : inventory.HeldType switch
            {
                ItemType.RawMeat => oven,
                ItemType.CookedMeat => cutting,
                ItemType.Wrap => service,
                _ => source
            };

            if (target == null) return;
            transform.position = target.position + Vector3.up * (2.65f + Mathf.Sin(Time.time * 5f) * 0.18f);
            transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            previousHeldType = inventory.HeldType;
        }
    }
}
