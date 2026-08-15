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
                Vector2.zero, new Vector2(960f, 900f));
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

            cards = new Card[3];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = BuildCard(panel.transform, i);

            feedbackLabel = UIFactory.Label("Feedback", panel.transform, "",
                UITheme.FontSmall, UITheme.Terracotta);
            UIFactory.Anchor(feedbackLabel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 26f), new Vector2(880f, 44f));

            root.gameObject.SetActive(false);
        }

        private Card BuildCard(Transform parent, int index)
        {
            const float width = 280f;
            const float gap = 22f;
            float x = (index - 1) * (width + gap);

            Image card = UIFactory.Panel($"Card{index}", parent, UITheme.CreamLight);
            UIFactory.Anchor(card.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(x, -22f), new Vector2(width, 600f));

            Image titleBar = UIFactory.Panel("TitleBar", card.transform, UITheme.Teal);
            UIFactory.Anchor(titleBar.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                Vector2.zero, new Vector2(width, 120f));

            Card built = new()
            {
                Title = UIFactory.Label("Title", titleBar.transform, "", UITheme.FontSmall, Color.white)
            };
            UIFactory.Stretch(built.Title.rectTransform, 12f, 6f);

            built.Pips = UIFactory.Label("Pips", card.transform, "", UITheme.FontBody, UITheme.Mustard);
            UIFactory.Anchor(built.Pips.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 40f), new Vector2(width - 20f, 60f));

            int captured = index;
            built.Free = UIFactory.Button("Free", card.transform, "BEDAVA", UITheme.Green,
                Color.white, UITheme.FontSmall, () => Purchase(captured, true));
            UIFactory.Anchor(built.Free.GetComponent<RectTransform>(),
                UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 108f), new Vector2(width - 36f, 78f));

            built.Paid = UIFactory.Button("Paid", card.transform, "", UITheme.Mustard,
                UITheme.Ink, UITheme.FontSmall, () => Purchase(captured, false));
            UIFactory.Anchor(built.Paid.GetComponent<RectTransform>(),
                UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 22f), new Vector2(width - 36f, 78f));
            built.PaidLabel = built.Paid.GetComponentInChildren<Text>();

            built.MaxLabel = UIFactory.Label("Max", card.transform, "MAX", UITheme.FontBody, UITheme.InkSoft);
            UIFactory.Anchor(built.MaxLabel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 64f), new Vector2(width - 30f, 70f));
            built.MaxLabel.gameObject.SetActive(false);
            return built;
        }

        // ---- content --------------------------------------------------------

        private static readonly string[] EmployeeTitles = { "Hareket\nHızı", "Kapasite", "Otomasyon" };
        private static readonly string[] PlayerTitles = { "Hareket\nHızı", "Kapasite", "Gelir\nArtışı" };
        private static readonly string[] RecruitTitles = { "Kasiyer", "Masa\nTemizlikçisi", "Tezgâh\nKoşucusu" };

        private static readonly EmployeeUpgradeType[] EmployeeTypes =
        {
            EmployeeUpgradeType.MovementSpeed, EmployeeUpgradeType.Capacity, EmployeeUpgradeType.AdoptUse
        };
        private static readonly GeneralManagerUpgradeType[] PlayerTypes =
        {
            GeneralManagerUpgradeType.MovementSpeed, GeneralManagerUpgradeType.Capacity,
            GeneralManagerUpgradeType.IncomeIncrease
        };
        private static readonly RecruitRole[] Roles =
        {
            RecruitRole.Cashier, RecruitRole.Cleaner, RecruitRole.Runner
        };

        private void Refresh()
        {
            if (!activeMenu.HasValue || cards == null) return;

            switch (activeMenu.Value)
            {
                case ManagementMenu.HumanResources:
                    titleLabel.text = "ÇALIŞANLARI GELİŞTİR";
                    for (int i = 0; i < cards.Length; i++)
                    {
                        int level = hr != null ? hr.GetLevel(EmployeeTypes[i]) : 0;
                        ApplyCard(cards[i], EmployeeTitles[i], Pips(level), level < MaxLevel,
                            hr != null ? hr.GetCost(EmployeeTypes[i]) : 0);
                    }
                    break;

                case ManagementMenu.GeneralManager:
                    titleLabel.text = "KENDİNİ GELİŞTİR";
                    for (int i = 0; i < cards.Length; i++)
                    {
                        int level = gm != null ? gm.GetLevel(PlayerTypes[i]) : 0;
                        ApplyCard(cards[i], PlayerTitles[i], Pips(level), level < MaxLevel,
                            gm != null ? gm.GetCost(PlayerTypes[i]) : 0);
                    }
                    break;

                default:
                    titleLabel.text = "PERSONEL AL";
                    for (int i = 0; i < cards.Length; i++)
                    {
                        bool hired = recruitment != null && recruitment.IsHired(Roles[i]);
                        ApplyCard(cards[i], RecruitTitles[i], hired ? "ÇALIŞIYOR" : "MÜSAİT", !hired,
                            recruitment != null ? recruitment.GetCost(Roles[i]) : 0);
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

            bool success = activeMenu.Value switch
            {
                ManagementMenu.HumanResources => hr != null && hr.TryUpgrade(EmployeeTypes[index], free),
                ManagementMenu.GeneralManager => gm != null && gm.TryUpgrade(PlayerTypes[index], free),
                _ => recruitment != null && recruitment.TryHire(Roles[index], free)
            };

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
