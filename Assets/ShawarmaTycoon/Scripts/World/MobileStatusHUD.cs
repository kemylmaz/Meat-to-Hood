using UnityEngine;

namespace ShawarmaTycoon
{
    public sealed class MobileStatusHUD : MonoBehaviour
    {
        private CustomerManager customers;
        private PrototypeHUD prototypeHud;
        private GUIStyle style;

        public void Configure(CustomerManager manager, PrototypeHUD hud)
        {
            customers = manager;
            prototypeHud = hud;
        }

        private void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 16,
                    padding = new RectOffset(12, 12, 8, 8)
                };
                style.normal.textColor = new Color(0.24f, 0.12f, 0.08f);
            }

            int active = customers != null ? customers.ActiveCustomers : 0;
            int coins = GameEconomy.Instance != null ? GameEconomy.Instance.Coins : 0;
            GUI.Box(new Rect(18f, 88f, 208f, 72f), $"Müşteriler: {active}\nKazanç: ₺{coins}", style);

            if (Screen.width < 700f)
                GUI.Box(new Rect(Screen.width - 186f, Screen.height - 78f, 168f, 46f), "İki parmak: kamera", style);
        }
    }
}
