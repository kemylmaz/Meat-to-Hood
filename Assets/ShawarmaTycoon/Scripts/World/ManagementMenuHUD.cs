using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>Reference-style three-card upgrade menus for the two manager offices and recruiting desk.</summary>
    public sealed class ManagementMenuHUD : MonoBehaviour
    {
        private HumanResourcesSystem hr;
        private PlayerUpgradeSystem gm;
        private RecruitmentSystem recruitment;
        private ManagementMenu? activeMenu;
        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle pipStyle;
        private GUIStyle freeButtonStyle;
        private GUIStyle cashButtonStyle;
        private GUIStyle closeStyle;
        private string feedback = string.Empty;
        private float feedbackTimer;

        public void Configure(HumanResourcesSystem humanResources, PlayerUpgradeSystem generalManager, RecruitmentSystem recruitSystem)
        {
            hr = humanResources;
            gm = generalManager;
            recruitment = recruitSystem;
        }

        public void Open(ManagementMenu menu)
        {
            activeMenu = menu;
            feedback = string.Empty;
        }

        public void Close(ManagementMenu menu)
        {
            if (activeMenu == menu) activeMenu = null;
        }

        private void Update()
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f) feedback = string.Empty;
        }

        private void OnGUI()
        {
            if (!activeMenu.HasValue) return;
            EnsureStyles();

            float width = Mathf.Min(560f, Screen.width - 24f);
            float height = 356f;
            float x = (Screen.width - width) * 0.5f;
            float y = Mathf.Max(26f, (Screen.height - height) * 0.5f - 30f);
            Rect panel = new(x, y, width, height);

            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Box(new Rect(x, y, width, 58f), TitleFor(activeMenu.Value), headerStyle);
            if (GUI.Button(new Rect(x + width - 52f, y - 14f, 48f, 48f), "X", closeStyle))
                activeMenu = null;

            if (activeMenu == ManagementMenu.HumanResources)
            {
                DrawUpgradeCards(panel, new[] { "Movement\nSpeed", "Capacity", "Adopt / Use" },
                    new[] { "FAST", "BOX", "PLUS" },
                    new[] { EmployeeUpgradeType.MovementSpeed, EmployeeUpgradeType.Capacity, EmployeeUpgradeType.AdoptUse });
            }
            else if (activeMenu == ManagementMenu.GeneralManager)
            {
                DrawPlayerCards(panel, new[] { "Movement\nSpeed", "Capacity", "Income\nIncrease" },
                    new[] { "FAST", "BOX", "CASH" },
                    new[] { GeneralManagerUpgradeType.MovementSpeed, GeneralManagerUpgradeType.Capacity, GeneralManagerUpgradeType.IncomeIncrease });
            }
            else
            {
                DrawRecruitCards(panel);
            }

            if (!string.IsNullOrEmpty(feedback))
                GUI.Label(new Rect(x + 20f, y + height - 37f, width - 40f, 24f), feedback, bodyStyle);
        }

        private void DrawUpgradeCards(Rect panel, string[] titles, string[] icons, EmployeeUpgradeType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                Rect card = CardRect(panel, i);
                GUI.Box(card, GUIContent.none, cardStyle);
                GUI.Box(new Rect(card.x + 4f, card.y + 4f, card.width - 8f, 43f), titles[i], cardTitleStyle);
                GUI.Label(new Rect(card.x, card.y + 57f, card.width, 34f), icons[i], bodyStyle);
                int level = hr != null ? hr.GetLevel(types[i]) : 0;
                GUI.Label(new Rect(card.x + 6f, card.y + 91f, card.width - 12f, 24f), Pips(level), pipStyle);
                DrawPurchaseButtons(card, hr != null && level < 5, hr != null ? hr.GetCost(types[i]) : 0,
                    () => hr != null && hr.TryUpgrade(types[i], true),
                    () => hr != null && hr.TryUpgrade(types[i], false));
            }
        }

        private void DrawPlayerCards(Rect panel, string[] titles, string[] icons, GeneralManagerUpgradeType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                Rect card = CardRect(panel, i);
                GUI.Box(card, GUIContent.none, cardStyle);
                GUI.Box(new Rect(card.x + 4f, card.y + 4f, card.width - 8f, 43f), titles[i], cardTitleStyle);
                GUI.Label(new Rect(card.x, card.y + 57f, card.width, 34f), icons[i], bodyStyle);
                int level = gm != null ? gm.GetLevel(types[i]) : 0;
                GUI.Label(new Rect(card.x + 6f, card.y + 91f, card.width - 12f, 24f), Pips(level), pipStyle);
                DrawPurchaseButtons(card, gm != null && level < 5, gm != null ? gm.GetCost(types[i]) : 0,
                    () => gm != null && gm.TryUpgrade(types[i], true),
                    () => gm != null && gm.TryUpgrade(types[i], false));
            }
        }

        private void DrawRecruitCards(Rect panel)
        {
            RecruitRole[] roles = { RecruitRole.Cashier, RecruitRole.Cleaner, RecruitRole.Runner };
            string[] titles = { "Cashier", "Table\nCleaner", "Counter\nRunner" };
            string[] icons = { "CASH", "CLEAN", "RUN" };
            for (int i = 0; i < roles.Length; i++)
            {
                Rect card = CardRect(panel, i);
                GUI.Box(card, GUIContent.none, cardStyle);
                GUI.Box(new Rect(card.x + 4f, card.y + 4f, card.width - 8f, 43f), titles[i], cardTitleStyle);
                GUI.Label(new Rect(card.x, card.y + 57f, card.width, 34f), icons[i], bodyStyle);
                bool hired = recruitment != null && recruitment.IsHired(roles[i]);
                GUI.Label(new Rect(card.x + 6f, card.y + 91f, card.width - 12f, 24f), hired ? "HIRED" : "AVAILABLE", pipStyle);
                DrawPurchaseButtons(card, !hired, recruitment != null ? recruitment.GetCost(roles[i]) : 0,
                    () => recruitment != null && recruitment.TryHire(roles[i], true),
                    () => recruitment != null && recruitment.TryHire(roles[i], false));
            }
        }

        private void DrawPurchaseButtons(Rect card, bool available, int cost, System.Func<bool> freeAction, System.Func<bool> paidAction)
        {
            Rect freeButton = new(card.x + 9f, card.y + 124f, card.width - 18f, 35f);
            Rect cashButton = new(card.x + 9f, card.y + 166f, card.width - 18f, 38f);
            if (!available)
            {
                GUI.Label(new Rect(card.x + 5f, card.y + 140f, card.width - 10f, 42f), "MAX", bodyStyle);
                return;
            }

            if (GUI.Button(freeButton, "FREE", freeButtonStyle)) Report(freeAction());
            if (GUI.Button(cashButton, "$ " + cost, cashButtonStyle)) Report(paidAction());
        }

        private void Report(bool success)
        {
            feedback = success ? "Upgrade applied" : "Not enough cash";
            feedbackTimer = 1.8f;
        }

        private static Rect CardRect(Rect panel, int index)
        {
            float gap = 10f;
            float width = (panel.width - 40f - gap * 2f) / 3f;
            return new Rect(panel.x + 20f + index * (width + gap), panel.y + 73f, width, 228f);
        }

        private static string Pips(int level)
        {
            string value = string.Empty;
            for (int i = 0; i < 5; i++) value += i < level ? "● " : "○ ";
            return value;
        }

        private static string TitleFor(ManagementMenu menu) => menu switch
        {
            ManagementMenu.HumanResources => "Upgrade Employees",
            ManagementMenu.GeneralManager => "Upgrade Yourself",
            _ => "Recruit Staff"
        };

        private void EnsureStyles()
        {
            if (panelStyle != null) return;
            panelStyle = CreateStyle(new Color(1f, 0.95f, 0.78f), 0, TextAnchor.MiddleCenter, Color.black);
            headerStyle = CreateStyle(new Color(0.14f, 0.43f, 0.77f), 23, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            cardStyle = CreateStyle(Color.white, 0, TextAnchor.MiddleCenter, Color.black);
            cardTitleStyle = CreateStyle(new Color(0.12f, 0.48f, 0.83f), 14, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            bodyStyle = CreateStyle(Color.clear, 13, TextAnchor.MiddleCenter, new Color(0.24f, 0.13f, 0.08f), FontStyle.Bold);
            pipStyle = CreateStyle(Color.clear, 15, TextAnchor.MiddleCenter, new Color(0.52f, 0.38f, 0.26f));
            freeButtonStyle = CreateStyle(new Color(0.25f, 0.70f, 0.23f), 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            cashButtonStyle = CreateStyle(new Color(0.95f, 0.72f, 0.16f), 15, TextAnchor.MiddleCenter, new Color(0.25f, 0.15f, 0.06f), FontStyle.Bold);
            closeStyle = CreateStyle(new Color(0.91f, 0.22f, 0.27f), 20, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        }

        private static GUIStyle CreateStyle(Color color, int fontSize, TextAnchor alignment, Color textColor, FontStyle fontStyle = FontStyle.Normal)
        {
            Texture2D texture = new(1, 1) { hideFlags = HideFlags.DontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            GUIStyle style = new(GUI.skin.box)
            {
                normal = { background = texture, textColor = textColor },
                alignment = alignment,
                fontSize = fontSize,
                fontStyle = fontStyle,
                wordWrap = true,
                padding = new RectOffset(4, 4, 3, 3)
            };
            return style;
        }
    }
}
