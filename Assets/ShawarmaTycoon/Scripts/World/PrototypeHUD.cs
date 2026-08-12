using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        private CarryInventory inventory;
        private GUIStyle coinStyle;
        private GUIStyle objectiveStyle;

        public void Configure(CarryInventory playerInventory)
        {
            inventory = playerInventory;
        }

        private void OnGUI()
        {
            EnsureStyles();

            int coins = GameEconomy.Instance != null ? GameEconomy.Instance.Coins : 0;
            Rect safe = Screen.safeArea.width > 0f ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);
            float safeTop = Screen.height - safe.yMax;
            GUI.Box(new Rect(safe.xMax - 177f, safeTop + 18f, 165f, 54f), $"₺ {coins}", coinStyle);

        }

        private string ObjectiveText()
        {
            if (inventory == null || inventory.Count == 0)
                return "Et deposuna git • Kirli masanın yanında bekleyerek temizle";

            return inventory.HeldType switch
            {
                ItemType.RawMeat => "Çiğ etleri ocağa bırak",
                ItemType.CookedMeat => "Pişen etleri kesme tezgâhına götür",
                ItemType.SlicedMeat => "Kesilmiş etleri dürüm tezgâhına götür",
                ItemType.Wrap => "Dürümleri servis tezgâhına bırak",
                _ => ""
            };
        }

        private static string ReadableName(ItemType type)
        {
            return type switch
            {
                ItemType.RawMeat => "Çiğ et",
                ItemType.CookedMeat => "Pişmiş et",
                ItemType.SlicedMeat => "Kesilmiş et",
                ItemType.Wrap => "Dürüm",
                _ => "Ürün"
            };
        }

        private void EnsureStyles()
        {
            if (coinStyle != null) return;

            coinStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };
            coinStyle.normal.textColor = new Color(0.24f, 0.12f, 0.08f);

            objectiveStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                wordWrap = true
            };
            objectiveStyle.normal.textColor = new Color(0.24f, 0.12f, 0.08f);
        }
    }
}
