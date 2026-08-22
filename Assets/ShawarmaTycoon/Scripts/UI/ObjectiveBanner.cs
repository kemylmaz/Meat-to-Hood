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
        private readonly List<ItemStation> stations = new();
        private CarryInventory inventory;
        private Text label;
        private CanvasGroup group;
        private string current = string.Empty;
        private float overrideTimer;

        public static ObjectiveBanner Create(RectTransform parent)
        {
            Image panel = UIFactory.Panel("Objective", parent, UITheme.DeepGreen);
            UIFactory.Anchor(panel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 30f), new Vector2(580f, 60f));
            UIFactory.AddCartoonFinish(panel, 2f, 6f);
            panel.raycastTarget = false;

            Image marker = UIFactory.Icon("Marker", panel.transform, UITheme.Circle, UITheme.Mustard);
            UIFactory.Anchor(marker.rectTransform, new Vector2(0f, 0.5f), UIFactory.Center,
                new Vector2(31f, 0f), new Vector2(26f, 26f));

            Text markerGlyph = UIFactory.DisplayLabel("Marker Glyph", marker.transform, "!", 19, UITheme.Ink);
            UIFactory.Stretch(markerGlyph.rectTransform);

            ObjectiveBanner banner = panel.gameObject.AddComponent<ObjectiveBanner>();
            banner.group = panel.gameObject.AddComponent<CanvasGroup>();
            banner.group.blocksRaycasts = false;
            banner.group.alpha = 0f;
            banner.label = UIFactory.Label("Text", panel.transform, "",
                21, UITheme.CreamLight, TextAnchor.MiddleLeft);
            UIFactory.Anchor(banner.label.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(36f, 0f), new Vector2(476f, 46f));
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

        /// <summary>
        /// Stations the banner watches so it can stop telling the player to do
        /// something that will not work. A backed up station cannot take what you
        /// are holding, and you cannot put it down anywhere except the bin, so
        /// "take it to the wrap counter" was advice that led nowhere.
        /// </summary>
        public void BindStations(IEnumerable<ItemStation> lineStations)
        {
            stations.Clear();
            if (lineStations != null) stations.AddRange(lineStations);
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
            bool holdingStock = inventory != null && inventory.Count > 0;
            ItemStation destination = holdingStock ? DestinationFor(inventory.HeldType) : null;
            bool destinationFull = destination != null && !destination.CanAcceptDelivery;

            // A dining room with nowhere to seat anyone outranks the carry hint:
            // however much stock is on the counter, nobody can be served until a
            // table is cleared. Plates ride in their own slot now, so a full load
            // of stock is no longer in the way - only a full plate slot is.
            if (NoSeatsLeft())
                return inventory != null && !inventory.CanAccept(ItemType.Trash)
                    ? "Elin çöp dolu - çöp kutusuna boşalt"
                    : "Boş masa yok - kirli masaları temizle";

            if (inventory == null || inventory.Count == 0)
                return inventory != null && inventory.TrashCount > 0
                    ? "Çöpü çöp kutusuna at"
                    : "Et deposundan çiğ et al";

            if (destinationFull)
            {
                if (destination.Mode == StationMode.Service)
                    return "Servis tezgâhı dolu - müşteriler alana kadar bekle";

                // Input full but the finished goods still have room: the station
                // keeps working while the player stands at it, so waiting clears
                // it. Both full and it has stopped, and waiting achieves nothing -
                // the pile has to be carried on, which means putting down the load
                // that is in the way first.
                return destination.OutputIsFull
                    ? "Tezgâhın çıkışı dolu - elindekini çöpe at, biten ürünleri taşı"
                    : "Tezgâh dolu - başında durup işlemesini bekle";
            }

            return inventory.HeldType switch
            {
                ItemType.RawMeat => "Çiğ etleri ocağa bırak",
                ItemType.CookedMeat => "Pişen etleri kesim tezgâhına götür",
                ItemType.Wrap => "Dürümleri servis tezgâhına bırak",
                _ => string.Empty
            };
        }

        /// <summary>The station that consumes <paramref name="held"/>, if any.</summary>
        private ItemStation DestinationFor(ItemType held)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                ItemStation station = stations[i];
                if (station != null && station.InputType == held) return station;
            }
            return null;
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
