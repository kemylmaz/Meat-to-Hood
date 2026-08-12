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
            if (inventory == null || inventory.Count == 0)
                return "Et deposundan çiğ et al";

            return inventory.HeldType switch
            {
                ItemType.RawMeat => "Çiğ etleri ocağa bırak",
                ItemType.CookedMeat => "Pişen etleri kesim tezgâhına götür",
                ItemType.SlicedMeat => "Kesilmiş etleri dürüm tezgâhına götür",
                ItemType.Wrap => "Dürümleri servis tezgâhına bırak",
                _ => string.Empty
            };
        }
    }
}
