using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class ConveyorLink : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color LockedBelt = new(0.35f, 0.32f, 0.30f);

        [SerializeField, Min(0.1f)] private float transferInterval = 0.75f;
        private ItemStation source;
        private ItemStation destination;
        private bool unlocked;
        private float timer;
        private TextMesh label;
        private int level;

        /// <summary>Renderer + submesh pairs that carry the locked/unlocked tint.</summary>
        private readonly List<KeyValuePair<Renderer, int>> beltSlots = new();

        public bool IsUnlocked => unlocked;
        public int Level => level;

        public void Configure(ItemStation from, ItemStation to)
        {
            source = from;
            destination = to;
            label = PrototypeVisuals.CreateLabel("BANT KİLİTLİ", transform, Vector3.up * 0.95f, 0.085f);
            label.gameObject.SetActive(false);
            CacheBeltSlots();
            UpdateVisual();
        }

        /// <summary>
        /// Finds the belt surface to tint. On the authored model that is the
        /// MAT_BeltDark submesh; tinting whatever renderer happened to be first
        /// would recolour an arbitrary part of the frame instead.
        /// </summary>
        private void CacheBeltSlots()
        {
            beltSlots.Clear();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] != null && slots[i].name.Contains("BeltDark"))
                        beltSlots.Add(new KeyValuePair<Renderer, int>(renderer, i));
            }

            if (beltSlots.Count > 0) return;
            Renderer fallback = GetComponentInChildren<Renderer>(true);
            if (fallback != null)
                beltSlots.Add(new KeyValuePair<Renderer, int>(fallback, 0));
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
            // A property block keeps the shared palette material untouched, so
            // one belt's state cannot bleed into every other object using it.
            Color tint = unlocked ? PrototypeVisuals.Teal : LockedBelt;
            MaterialPropertyBlock block = new();
            block.SetColor(BaseColorId, tint);
            block.SetColor(ColorId, tint);
            for (int i = 0; i < beltSlots.Count; i++)
                if (beltSlots[i].Key != null)
                    beltSlots[i].Key.SetPropertyBlock(block, beltSlots[i].Value);

            if (label != null) label.text = unlocked ? "BANT AÇIK" : "BANT KİLİTLİ";
        }
    }
}
