using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>
    /// Bottom hint that tells the player what to do with what they are carrying.
    /// The old IMGUI HUD computed this text but never drew it.
    /// </summary>
    public sealed class ObjectiveBanner : MonoBehaviour
    {
        private readonly List<CustomerTable> tables = new();
        private CarryInventory inventory;
        private Text label;
        private CanvasGroup group;
        private string current = string.Empty;
        private float overrideTimer;

        public static ObjectiveBanner Create(RectTransform parent)
        {
            Image panel = UIFactory.Panel("Objective", parent, UITheme.Hex(0x3D2A20, 1f));
            UIFactory.Anchor(panel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 42f), new Vector2(820f, 88f));
            panel.raycastTarget = false;

            ObjectiveBanner banner = panel.gameObject.AddComponent<ObjectiveBanner>();
            banner.group = panel.gameObject.AddComponent<CanvasGroup>();
            banner.group.blocksRaycasts = false;
            banner.group.alpha = 0f;
            banner.label = UIFactory.Label("Text", panel.transform, "",
                UITheme.FontBody, UITheme.CreamLight);
            UIFactory.Stretch(banner.label.rectTransform, 24f, 8f);
            UIFactory.AddShadow(banner.label, new Color(0f, 0f, 0f, 0.55f), new Vector2(2f, -2f));
            return banner;
        }

        public void Bind(CarryInventory playerInventory) => inventory = playerInventory;

        /// <summary>
        /// Tables the banner watches so it can call out a full dining room. A
        /// table stays dirty until someone clears it, so an unattended shop
        /// silently stops seating anyone and the income goes to zero with nothing
        /// on screen explaining why.
        /// </summary>
        public void BindTables(IEnumerable<CustomerTable> diningTables)
        {
            tables.Clear();
            if (diningTables != null) tables.AddRange(diningTables);
        }

        /// <summary>Show a one-off message (unlock hints, warnings) for a moment.</summary>
        public void Flash(string message, float seconds = 2.5f)
        {
            current = message;
            label.text = message;
            overrideTimer = seconds;
        }

        private void Update()
        {
            if (overrideTimer > 0f)
            {
                overrideTimer -= Time.deltaTime;
                group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.deltaTime * 6f);
                return;
            }

            string next = ObjectiveText();
            if (next != current)
            {
                current = next;
                label.text = next;
            }

            float targetAlpha = string.IsNullOrEmpty(next) ? 0f : 1f;
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.deltaTime * 5f);
        }

        private string ObjectiveText()
        {
            // A dining room with nowhere to seat anyone outranks the carry hint:
            // however much stock is on the counter, nobody can be served until a
            // table is cleared.
            if (NoSeatsLeft())
                return "Boş masa yok - kirli masaları temizle";

            if (inventory == null || inventory.Count == 0)
                return "Et deposundan çiğ et al";

            return inventory.HeldType switch
            {
                ItemType.RawMeat => "Çiğ etleri ocağa bırak",
                ItemType.CookedMeat => "Pişen etleri kesim tezgâhına götür",
                ItemType.SlicedMeat => "Kesilmiş etleri dürüm tezgâhına götür",
                ItemType.Wrap => "Dürümleri servis tezgâhına bırak",
                ItemType.Trash => "Çöpü çöp kutusuna at",
                _ => string.Empty
            };
        }

        /// <summary>True when at least one table is dirty and none are free.</summary>
        private bool NoSeatsLeft()
        {
            bool anyDirty = false;
            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] == null || !tables[i].gameObject.activeInHierarchy) continue;
                if (tables[i].IsAvailable) return false;
                if (tables[i].IsDirty) anyDirty = true;
            }
            return anyDirty;
        }
    }
}
