using ShawarmaTycoon.UI;
using UnityEngine;

namespace ShawarmaTycoon
{
    /// <summary>
    /// Runs exactly one honest kitchen cycle on a fresh save, then gets out of
    /// the way permanently. Progress is inferred from real inventory/station
    /// state, so the cards cannot advance ahead of what the player actually did.
    /// </summary>
    public sealed class FirstShiftTutorial : MonoBehaviour
    {
        public const string CompletionKey = "tutorial.first_shift_complete";
        private const int TotalSteps = 5;

        private enum Stage { Intro, Source, Oven, Cutting, Service, Checkout, Finished }

        private GameHUD hud;
        private MobilePlayerController player;
        private CarryInventory inventory;
        private ItemStation source;
        private ItemStation oven;
        private ItemStation cutting;
        private ItemStation service;
        private CashPile till;
        private CustomerManager customers;
        private TutorialArrow arrow;
        private FirstShiftTutorialHUD tutorialHud;
        private Stage stage;
        private float resumeTimeScale = 1f;
        private bool ownsPause;

        public void Configure(
            GameHUD gameHud,
            MobilePlayerController playerController,
            CarryInventory playerInventory,
            ItemStation sourceStation,
            ItemStation ovenStation,
            ItemStation cuttingStation,
            ItemStation serviceStation,
            CashPile cashTill,
            CustomerManager customerManager,
            TutorialArrow tutorialArrow)
        {
            hud = gameHud;
            player = playerController;
            inventory = playerInventory;
            source = sourceStation;
            oven = ovenStation;
            cutting = cuttingStation;
            service = serviceStation;
            till = cashTill;
            customers = customerManager;
            arrow = tutorialArrow;

            if (GameProgress.GetInt(CompletionKey) == 1)
            {
                arrow?.SetTarget(null);
                enabled = false;
                return;
            }

            tutorialHud = FirstShiftTutorialHUD.Create(
                hud.ModalLayer, StartShift, EnterFreePlay);
            customers.CustomerCheckedOut += OnCustomerCheckedOut;
            ShowOpening();
        }

        private void Update()
        {
            if (inventory == null || tutorialHud == null) return;

            switch (stage)
            {
                case Stage.Source:
                    if (inventory.HeldType == ItemType.RawMeat)
                        SetStage(Stage.Oven);
                    break;

                case Stage.Oven:
                    if (inventory.HeldType == ItemType.CookedMeat)
                        SetStage(Stage.Cutting);
                    else
                        RefreshOvenHint();
                    break;

                case Stage.Cutting:
                    if (inventory.HeldType == ItemType.Wrap)
                        SetStage(Stage.Service);
                    else
                        RefreshCuttingHint();
                    break;

                case Stage.Service:
                    if (inventory.HeldType == ItemType.None && service.OutputCount > 0)
                        SetStage(Stage.Checkout);
                    break;
            }
        }

        private void ShowOpening()
        {
            stage = Stage.Intro;
            resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            PauseSimulation();
            hud.SetGameplayChromeVisible(false);
            hud.Joystick.gameObject.SetActive(false);
            hud.BuildMode.gameObject.SetActive(false);
            arrow?.SetTarget(null);
            tutorialHud.ShowIntro();
        }

        private void StartShift()
        {
            ResumeSimulation();
            hud.SetGameplayChromeVisible(true);
            // The dedicated step card owns guidance until the first sale, avoiding
            // two banners telling the player different things at once.
            hud.Objective.gameObject.SetActive(false);
            hud.Joystick.gameObject.SetActive(true);
            SetStage(Stage.Source);
        }

        private void SetStage(Stage next)
        {
            if (stage == next) return;
            stage = next;

            switch (stage)
            {
                case Stage.Source:
                    arrow?.SetTarget(source.transform);
                    tutorialHud.ShowStep(1, TotalSteps, "ÇİĞ ETİ AL",
                        "Et dolabına yaklaş; hazır porsiyonları otomatik alırsın.");
                    break;
                case Stage.Oven:
                    arrow?.SetTarget(oven.transform);
                    RefreshOvenHint();
                    break;
                case Stage.Cutting:
                    arrow?.SetTarget(cutting.transform);
                    RefreshCuttingHint();
                    break;
                case Stage.Service:
                    arrow?.SetTarget(service.transform);
                    tutorialHud.ShowStep(4, TotalSteps, "DÜRÜMÜ SERVİSE BIRAK",
                        "Hazır dürümü servis tezgâhına götür ve standa yaklaş.");
                    break;
                case Stage.Checkout:
                    arrow?.SetTarget(till.transform);
                    tutorialHud.ShowStep(5, TotalSteps, "İLK MÜŞTERİYİ KARŞILA",
                        "Sipariş balonu hazır olduğunda kasanın yanında dur.");
                    break;
            }
        }

        private void RefreshOvenHint()
        {
            if (stage != Stage.Oven) return;
            if (inventory.HeldType == ItemType.RawMeat)
                tutorialHud.ShowStep(2, TotalSteps, "ETİ OCAĞA BIRAK",
                    "Sarı okun gösterdiği ocağa yaklaş ve çiğ eti teslim et.");
            else if (oven.OutputCount > 0)
                tutorialHud.ShowStep(2, TotalSteps, "PİŞEN ETİ AL",
                    "Et hazır! Ocağın yanında durup pişmiş eti al.");
            else
                tutorialHud.ShowStep(2, TotalSteps, "PİŞMESİNİ BEKLE",
                    "Ocak çalışıyor; et hazır olduğunda aynı yerden alacaksın.");
        }

        private void RefreshCuttingHint()
        {
            if (stage != Stage.Cutting) return;
            if (inventory.HeldType == ItemType.CookedMeat)
                tutorialHud.ShowStep(3, TotalSteps, "ETİ HAZIRLA",
                    "Pişmiş eti kesim tezgâhına götür ve teslim et.");
            else if (cutting.OutputCount > 0)
                tutorialHud.ShowStep(3, TotalSteps, "DÜRÜMÜ AL",
                    "Dürüm hazır! Kesim tezgâhına yaklaş ve eline al.");
            else
                tutorialHud.ShowStep(3, TotalSteps, "DÜRÜM HAZIRLANIYOR",
                    "Kısa bir an bekle; hazır olduğunda tezgahtan al.");
        }

        private void OnCustomerCheckedOut(bool byCashier)
        {
            if (stage != Stage.Checkout) return;
            FinishTutorial();
        }

        private void FinishTutorial()
        {
            stage = Stage.Finished;
            arrow?.SetTarget(null);
            GameProgress.SetInt(CompletionKey, 1);
            GameProgress.FlushNow();
            PauseSimulation();
            tutorialHud.ShowFinish();
        }

        private void EnterFreePlay()
        {
            ResumeSimulation();
            tutorialHud.HideAll();
            hud.Objective.gameObject.SetActive(true);
            hud.BuildMode.gameObject.SetActive(true);
            enabled = false;
        }

        private void PauseSimulation()
        {
            if (!ownsPause)
            {
                resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : resumeTimeScale;
                ownsPause = true;
            }
            Time.timeScale = 0f;
            if (player != null) player.enabled = false;
        }

        private void ResumeSimulation()
        {
            if (ownsPause) Time.timeScale = Mathf.Max(0.01f, resumeTimeScale);
            ownsPause = false;
            if (player != null) player.enabled = true;
        }

        private void OnDestroy()
        {
            if (customers != null) customers.CustomerCheckedOut -= OnCustomerCheckedOut;
            if (ownsPause) ResumeSimulation();
        }
    }
}
