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
            GUI.Box(new Rect(Screen.width - 190f, 22f, 165f, 54f), $"₺ {coins}", coinStyle);

            string carried = inventory == null || inventory.Count == 0
                ? "Elinde ürün yok"
                : $"{ReadableName(inventory.HeldType)} × {inventory.Count}";
            GUI.Box(new Rect(24f, 22f, 230f, 54f), carried, coinStyle);

            string objective = ObjectiveText();
            GUI.Box(
                new Rect(Screen.width * 0.5f - 210f, Screen.height - 92f, 420f, 58f),
                objective,
                objectiveStyle);
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
