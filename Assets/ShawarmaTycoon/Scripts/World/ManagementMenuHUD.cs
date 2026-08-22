using ShawarmaTycoon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Three-card upgrade menus for the manager offices and the recruiting desk.
    /// Built as uGUI under the shared HUD canvas and rebuilt lazily on first open.
    /// </summary>
    public sealed class ManagementMenuHUD : MonoBehaviour
    {
        private const int MaxLevel = 5;

        private HumanResourcesSystem hr;
        private PlayerUpgradeSystem gm;
        private RecruitmentSystem recruitment;
        private ManagementMenu? activeMenu;

        private RectTransform root;
        private Text titleLabel;
        private Text feedbackLabel;
        private Card[] cards;
        private float feedbackTimer;

        private sealed class Card
        {
            public RectTransform Root;
            public Text Title;
            public Text Pips;
            public Button Free;
            public Button Paid;
            public Text PaidLabel;
            public Text MaxLabel;
        }

        public void Configure(HumanResourcesSystem humanResources, PlayerUpgradeSystem generalManager, RecruitmentSystem recruitSystem)
        {
            hr = humanResources;
            gm = generalManager;
            recruitment = recruitSystem;
        }

        public void Open(ManagementMenu menu)
        {
            EnsureBuilt();
            if (root == null) return;
            activeMenu = menu;
            root.gameObject.SetActive(true);
            feedbackLabel.text = string.Empty;
            AudioDirector.Play(GameSfx.Pickup);
            Refresh();
        }

        public void Close(ManagementMenu menu)
        {
            if (activeMenu == menu) CloseAll();
        }

        private void CloseAll()
        {
            activeMenu = null;
            if (root != null) root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f && feedbackLabel != null)
                    feedbackLabel.text = string.Empty;
            }

            if (activeMenu.HasValue) Refresh();
        }

        // ---- construction ---------------------------------------------------

        private void EnsureBuilt()
        {
            if (root != null || GameHUD.Instance == null) return;

            Image scrim = UIFactory.Panel("Management", GameHUD.Instance.ModalLayer, UITheme.Scrim, false);
            UIFactory.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;
            root = scrim.rectTransform;

            Image panel = UIFactory.Panel("Panel", root, UITheme.Panel);
            UIFactory.Anchor(panel.rectTransform, UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(960f, 1080f));
            UIFactory.AddShadow(panel, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -6f));

            Image header = UIFactory.Panel("Header", panel.transform, UITheme.DarkBlueGray);
            UIFactory.Anchor(header.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, 0f), new Vector2(960f, 116f));

            titleLabel = UIFactory.Label("Title", header.transform, "", UITheme.FontLarge, UITheme.CreamLight);
            UIFactory.Stretch(titleLabel.rectTransform, 90f, 0f);

            Button close = UIFactory.Button("Close", header.transform, "✕", UITheme.WarmRed,
                Color.white, UITheme.FontLarge, CloseAll);
            UIFactory.Anchor(close.GetComponent<RectTransform>(), UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-14f, -14f), new Vector2(88f, 88f));

            // Nine slots on a three-by-three grid. The HR desk carries both the
            // five hires and the three staff upgrades, and a fixed row of three
            // could only ever show the first three of them.
            cards = new Card[9];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = BuildCard(panel.transform, i);

            feedbackLabel = UIFactory.Label("Feedback", panel.transform, "",
                UITheme.FontSmall, UITheme.Terracotta);
            UIFactory.Anchor(feedbackLabel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 26f), new Vector2(880f, 44f));

            root.gameObject.SetActive(false);
        }

        private const float CardWidth = 280f;
        private const float CardHeight = 270f;
        private const float CardGap = 16f;
        private const int CardColumns = 3;

        private Card BuildCard(Transform parent, int index)
        {
            Image card = UIFactory.Panel($"Card{index}", parent, UITheme.CreamLight);
            UIFactory.Anchor(card.rectTransform, UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(CardWidth, CardHeight));

            Image titleBar = UIFactory.Panel("TitleBar", card.transform, UITheme.Teal);
            UIFactory.Anchor(titleBar.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                Vector2.zero, new Vector2(CardWidth, 84f));

            Card built = new()
            {
                Root = card.rectTransform,
                Title = UIFactory.Label("Title", titleBar.transform, "", UITheme.FontSmall, Color.white)
            };
            UIFactory.Stretch(built.Title.rectTransform, 12f, 4f);

            built.Pips = UIFactory.Label("Pips", card.transform, "", UITheme.FontBody, UITheme.Mustard);
            UIFactory.Anchor(built.Pips.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 26f), new Vector2(CardWidth - 20f, 44f));

            int captured = index;
            built.Free = UIFactory.Button("Free", card.transform, "BEDAVA", UITheme.Green,
                Color.white, UITheme.FontSmall, () => Purchase(captured, true));
            UIFactory.Anchor(built.Free.GetComponent<RectTransform>(),
                UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 70f), new Vector2(CardWidth - 36f, 62f));

            built.Paid = UIFactory.Button("Paid", card.transform, "", UITheme.Mustard,
                UITheme.Ink, UITheme.FontSmall, () => Purchase(captured, false));
            UIFactory.Anchor(built.Paid.GetComponent<RectTransform>(),
                UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 4f), new Vector2(CardWidth - 36f, 62f));
            built.PaidLabel = built.Paid.GetComponentInChildren<Text>();

            built.MaxLabel = UIFactory.Label("Max", card.transform, "MAX", UITheme.FontBody, UITheme.InkSoft);
            UIFactory.Anchor(built.MaxLabel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 40f), new Vector2(CardWidth - 30f, 56f));
            built.MaxLabel.gameObject.SetActive(false);
            return built;
        }

        /// <summary>
        /// Lays out the cards a menu actually uses and hides the rest. Rows are
        /// centred on what is in them, so a menu of five does not leave a hole in
        /// the middle of its bottom row.
        /// </summary>
        private void LayoutCards(int count)
        {
            int rows = Mathf.CeilToInt(count / (float)CardColumns);
            float rowStride = CardHeight + CardGap;
            float firstRowY = (rows - 1) * rowStride * 0.5f - 18f;

            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < count;
                cards[i].Root.gameObject.SetActive(used);
                if (!used) continue;

                int row = i / CardColumns;
                int inRow = Mathf.Min(CardColumns, count - row * CardColumns);
                int column = i - row * CardColumns;
                float x = (column - (inRow - 1) * 0.5f) * (CardWidth + CardGap);
                cards[i].Root.anchoredPosition = new Vector2(x, firstRowY - row * rowStride);
            }
        }

        // ---- content --------------------------------------------------------

        private static readonly string[] EmployeeTitles = { "Hareket\nHızı", "Kapasite", "Otomasyon" };
        private static readonly string[] PlayerTitles = { "Hareket\nHızı", "Tepsi &\nServis", "Gelir\nArtışı" };
        private static readonly string[] RecruitTitles =
        {
            "Kasiyer", "Drive-Thru\nKasiyeri", "Drive-Thru\nKoşucusu",
            "Bulaşıkçı", "2. Bulaşıkçı"
        };

        private static readonly EmployeeUpgradeType[] EmployeeTypes =
        {
            EmployeeUpgradeType.MovementSpeed, EmployeeUpgradeType.Capacity, EmployeeUpgradeType.AdoptUse
        };
        private static readonly GeneralManagerUpgradeType[] PlayerTypes =
        {
            GeneralManagerUpgradeType.MovementSpeed, GeneralManagerUpgradeType.Capacity,
            GeneralManagerUpgradeType.IncomeIncrease
        };
        private static readonly RecruitRole[] Roles = RecruitmentSystem.AllRoles;

        private void Refresh()
        {
            if (!activeMenu.HasValue || cards == null) return;

            switch (activeMenu.Value)
            {
                // Hiring and staff upgrades on one desk: both are what a personnel
                // office is for, and splitting them across two rooms meant walking
                // through a second door to make the people you just hired faster.
                case ManagementMenu.HumanResources:
                    titleLabel.text = "PERSONEL";
                    LayoutCards(Roles.Length + EmployeeTypes.Length);
                    for (int i = 0; i < Roles.Length; i++)
                    {
                        bool hired = recruitment != null && recruitment.IsHired(Roles[i]);
                        ApplyCard(cards[i], RecruitTitles[i], hired ? "ÇALIŞIYOR" : "MÜSAİT", !hired,
                            recruitment != null ? recruitment.GetCost(Roles[i]) : 0);
                    }
                    for (int i = 0; i < EmployeeTypes.Length; i++)
                    {
                        int level = hr != null ? hr.GetLevel(EmployeeTypes[i]) : 0;
                        ApplyCard(cards[Roles.Length + i], EmployeeTitles[i], Pips(level),
                            level < MaxLevel, hr != null ? hr.GetCost(EmployeeTypes[i]) : 0);
                    }
                    break;

                default:
                    titleLabel.text = "KENDİNİ GELİŞTİR";
                    LayoutCards(PlayerTypes.Length);
                    for (int i = 0; i < PlayerTypes.Length; i++)
                    {
                        int level = gm != null ? gm.GetLevel(PlayerTypes[i]) : 0;
                        ApplyCard(cards[i], PlayerTitles[i], Pips(level), level < MaxLevel,
                            gm != null ? gm.GetCost(PlayerTypes[i]) : 0);
                    }
                    break;
            }
        }

        /// <summary>
        /// No rewarded-ad provider is integrated yet. Flip this on with the SDK
        /// that grants the reward, not before.
        /// </summary>
        private const bool RewardedAdsAvailable = false;

        private static void ApplyCard(Card card, string title, string state, bool available, int cost)
        {
            card.Title.text = title;
            card.Pips.text = state;
            // Hidden until there is a rewarded ad behind it. As wired it hands out
            // the upgrade for nothing the moment it is pressed, which is every
            // priced upgrade in the game available free.
            card.Free.gameObject.SetActive(available && RewardedAdsAvailable);
            card.Paid.gameObject.SetActive(available);
            card.MaxLabel.gameObject.SetActive(!available);
            if (available) card.PaidLabel.text = "₺ " + cost;
        }

        private void Purchase(int index, bool free)
        {
            if (!activeMenu.HasValue) return;

            // The HR desk's first cards are the hires and the rest are the staff
            // upgrades, so the index has to be split against the roster length.
            bool success = activeMenu.Value == ManagementMenu.HumanResources
                ? index < Roles.Length
                    ? recruitment != null && recruitment.TryHire(Roles[index], free)
                    : hr != null && hr.TryUpgrade(EmployeeTypes[index - Roles.Length], free)
                : gm != null && gm.TryUpgrade(PlayerTypes[index], free);

            feedbackLabel.text = success ? "Geliştirme uygulandı" : "Yeterli para yok";
            feedbackLabel.color = success ? UITheme.Green : UITheme.WarmRed;
            feedbackTimer = 1.8f;
            AudioDirector.Play(success ? GameSfx.Unlock : GameSfx.Error);
            Refresh();
        }

        private static readonly string[] PipStrings =
        {
            "○ ○ ○ ○ ○",
            "● ○ ○ ○ ○",
            "● ● ○ ○ ○",
            "● ● ● ○ ○",
            "● ● ● ● ○",
            "● ● ● ● ●"
        };

        private static string Pips(int level) => PipStrings[Mathf.Clamp(level, 0, MaxLevel)];
    }
}
