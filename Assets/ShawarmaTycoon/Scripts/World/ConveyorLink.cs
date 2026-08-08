using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class ConveyorLink : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float transferInterval = 0.75f;
        private ItemStation source;
        private ItemStation destination;
        private bool unlocked;
        private float timer;
        private TextMesh label;
        private Renderer beltRenderer;
        private int level;

        public bool IsUnlocked => unlocked;
        public int Level => level;

        public void Configure(ItemStation from, ItemStation to)
        {
            source = from;
            destination = to;
            label = PrototypeVisuals.CreateLabel("BANT KİLİTLİ", transform, Vector3.up * 0.35f, 0.11f);
            label.gameObject.SetActive(false);
            beltRenderer = GetComponentInChildren<Renderer>();
            UpdateVisual();
        }

        public void Unlock()
        {
            SetLevel(Mathf.Max(1, level));
        }

        public void SetLevel(int newLevel)
        {
            level = Mathf.Clamp(newLevel, 0, 3);
            unlocked = level > 0;
            transferInterval = level switch { 3 => 0.28f, 2 => 0.46f, _ => 0.75f };
            UpdateVisual();
        }

        private void Update()
        {
            if (!unlocked || source == null || destination == null) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = transferInterval;

            if (!source.TryTakeOutputForConveyor(out ItemType item)) return;
            if (!destination.TryReceiveFromConveyor(item))
            {
                source.ReturnOutputFromConveyor(item);
            }
        }

        private void UpdateVisual()
        {
            if (beltRenderer != null)
                beltRenderer.sharedMaterial = PrototypeVisuals.Material(unlocked ? PrototypeVisuals.Teal : new Color(0.35f, 0.32f, 0.30f));
            if (label != null) label.text = unlocked ? "BANT AÇIK" : "BANT KİLİTLİ";
        }
    }
}
