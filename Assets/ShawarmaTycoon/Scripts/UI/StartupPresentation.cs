using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>
    /// Owns the two screens that frame a play session: a short branded loading
    /// beat while the runtime-built restaurant is assembled, then the main menu.
    /// The restaurant remains the background, so the promise on the menu is the
    /// exact toy-like world the player enters rather than unrelated key art.
    /// </summary>
    [DefaultExecutionOrder(-1200)]
    public sealed class StartupPresentation : MonoBehaviour
    {
        public enum PresentationStage
        {
            InitialLoading,
            MainMenu,
            EnteringGame,
            Hidden
        }

        private const float BootMinimumSeconds = 0.90f;
        private const float EnterGameSeconds = 1.10f;

        public static StartupPresentation Instance { get; private set; }
#if UNITY_EDITOR
        public static bool BypassGameplayPauseForTests { get; set; }
#endif
        public PresentationStage Stage { get; private set; }
        public bool BlocksGameplay => Stage != PresentationStage.Hidden;
        public bool WorldReady => worldReady;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform safeArea;
        private GameObject loadingRoot;
        private GameObject menuRoot;
        private RectTransform menuPanel;
        private RectTransform menuContent;
        private RectTransform storyPanel;
        private RectTransform loadingLogo;
        private RectTransform loadingSkewer;
        private RectTransform menuSkewer;
        private Vector2 loadingSkewerBase;
        private RectTransform progressFill;
        private Text progressPercent;
        private Text loadingStatus;
        private Text soundLabel;
        private Button primaryButton;

        private bool worldReady;
        private bool released;
        private bool portrait;
        private float stageStartedAt;
        private float previousTimeScale;
        private float displayedProgress;
        private int displayedPercent = -1;
        private string[] percentLabels;
        private Vector2Int appliedResolution;

        public static StartupPresentation Ensure(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject host = new("Startup Presentation");
            if (parent != null) host.transform.SetParent(parent, false);
            return host.AddComponent<StartupPresentation>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            percentLabels = new string[101];
            for (int i = 0; i < percentLabels.Length; i = i - (-1))
                percentLabels[i] = i.ToString("0\\%");
            BuildCanvas();
            BlockGameplay();
            ShowLoading(PresentationStage.InitialLoading, "DÜKKÂN HAZIRLANIYOR");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (!released)
            {
                Time.timeScale = previousTimeScale;
                if (GameHUD.Instance != null) GameHUD.Instance.SetCanvasVisible(true);
            }
        }

        /// <summary>Called after the bootstrap has finished building the live shop.</summary>
        public void NotifyWorldReady()
        {
            worldReady = true;
#if UNITY_EDITOR
            if (BypassGameplayPauseForTests) return;
#endif
            if (GameHUD.Instance != null) GameHUD.Instance.SetCanvasVisible(false);
        }

        private void LateUpdate()
        {
            ApplyResponsiveLayoutIfNeeded();
            AnimateSignature();

            if (Stage == PresentationStage.InitialLoading)
            {
                UpdateProgress(BootMinimumSeconds, worldReady ? 1f : 0.82f);
                if (worldReady && Time.unscaledTime - stageStartedAt >= BootMinimumSeconds)
                    ShowMainMenu();
            }
            else if (Stage == PresentationStage.EnteringGame)
            {
                UpdateProgress(EnterGameSeconds, 1f);
                if (Time.unscaledTime - stageStartedAt >= EnterGameSeconds)
                    ReleaseIntoGame();
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new("Startup Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.layer = LayerMask.NameToLayer("UI");

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            canvasObject.AddComponent<GraphicRaycaster>();

            canvasRect = (RectTransform)canvasObject.transform;
            safeArea = UIFactory.Node("Safe Area", canvasRect);
            UIFactory.Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            BuildLoadingScreen();
            BuildMainMenu();
            ApplyResponsiveLayout(true);
        }

        private void BuildLoadingScreen()
        {
            RectTransform root = UIFactory.Node("Loading Screen", safeArea);
            UIFactory.Stretch(root);
            loadingRoot = root.gameObject;

            Image background = UIFactory.Panel("Charcoal Background", root, UITheme.Ink, rounded: false);
            UIFactory.Stretch(background.rectTransform);
            background.raycastTarget = true;

            Image sun = UIFactory.Icon("Safran Sun", root, UITheme.Circle, UITheme.Mustard);
            UIFactory.Anchor(sun.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 60f), new Vector2(520f, 520f));

            loadingLogo = UIFactory.Node("Loading Brand Lockup", root);
            UIFactory.Anchor(loadingLogo, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 82f), new Vector2(760f, 300f));

            Image logoShadow = UIFactory.Panel("Brand Shadow", loadingLogo, UITheme.Hex(0x17100E, 0.72f));
            UIFactory.Anchor(logoShadow.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(12f, -14f), new Vector2(650f, 188f));
            logoShadow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -2.5f);

            Image logoCard = UIFactory.Panel("Butcher Paper Sign", loadingLogo, UITheme.Terracotta);
            UIFactory.Anchor(logoCard.rectTransform, UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(650f, 188f));
            logoCard.rectTransform.localEulerAngles = new Vector3(0f, 0f, -2.5f);
            UIFactory.AddCartoonFinish(logoCard, 4f, 8f);

            Image stripe = UIFactory.Panel("Safran Cut", logoCard.transform, UITheme.Mustard);
            UIFactory.Anchor(stripe.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -13f), new Vector2(570f, 12f));

            Text brand = UIFactory.DisplayLabel("Brand", logoCard.transform, "MEAT & EAT", 76,
                UITheme.CreamLight);
            UIFactory.Stretch(brand.rectTransform, 30f, 18f);
            UIFactory.AddShadow(brand, UITheme.DropShadow, new Vector2(4f, -5f));

            loadingSkewer = BuildSkewer(loadingLogo, new Vector2(-360f, 0f), 220f);
            loadingSkewerBase = loadingSkewer.anchoredPosition;

            loadingStatus = UIFactory.DisplayLabel("Loading Status", root, "DÜKKÂN HAZIRLANIYOR", 24,
                UITheme.CreamLight);
            UIFactory.Anchor(loadingStatus.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -124f), new Vector2(700f, 42f));

            Image track = UIFactory.Panel("Loading Track", root, UITheme.Hex(0x17100E, 0.72f));
            UIFactory.Anchor(track.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -184f), new Vector2(610f, 22f));
            UIFactory.AddCartoonFinish(track, 2f, 4f);

            Image fill = UIFactory.Panel("Loading Fill", track.transform, UITheme.Teal);
            progressFill = fill.rectTransform;
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.pivot = new Vector2(0f, 0.5f);
            progressFill.offsetMin = new Vector2(5f, 5f);
            progressFill.offsetMax = new Vector2(-5f, -5f);

            progressPercent = UIFactory.Label("Loading Percent", root, "0%", 16,
                UITheme.Mustard, TextAnchor.MiddleRight);
            UIFactory.Anchor(progressPercent.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(366f, -184f), new Vector2(92f, 30f));

            Text tip = UIFactory.Label("Loading Tip", root,
                "İPUCU  •  Hazır ürün, mutlu müşteri ve daha uzun kombo demektir.",
                17, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Anchor(tip.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 46f), new Vector2(900f, 36f));
        }

        private void BuildMainMenu()
        {
            RectTransform root = UIFactory.Node("Main Menu", safeArea);
            UIFactory.Stretch(root);
            menuRoot = root.gameObject;

            Image scrim = UIFactory.Panel("World Scrim", root, UITheme.Hex(0x271B17, 0.64f), rounded: false);
            UIFactory.Stretch(scrim.rectTransform);
            scrim.raycastTarget = true;

            Image storyWash = UIFactory.Panel("World Warm Wash", root, UITheme.Hex(0xF2BF4B, 0.10f), rounded: false);
            storyWash.rectTransform.anchorMin = new Vector2(0.46f, 0f);
            storyWash.rectTransform.anchorMax = Vector2.one;
            storyWash.rectTransform.offsetMin = Vector2.zero;
            storyWash.rectTransform.offsetMax = Vector2.zero;

            Image panel = UIFactory.Panel("Neighbourhood Green", root, UITheme.Hex(0x315C47, 0.97f), rounded: false);
            menuPanel = panel.rectTransform;
            menuContent = UIFactory.Node("Menu Content", menuPanel);

            Text eyebrow = UIFactory.Label("Eyebrow", menuContent,
                "MAHALLE 01  •  AÇILIŞ VARDİYASI", 16, UITheme.Mustard, TextAnchor.MiddleLeft);
            UIFactory.Anchor(eyebrow.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 330f), new Vector2(610f, 32f));

            Image titleShadow = UIFactory.Panel("Title Shadow", menuContent, UITheme.Hex(0x17100E, 0.62f));
            UIFactory.Anchor(titleShadow.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(12f, 181f), new Vector2(610f, 180f));
            titleShadow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -2.7f);

            Image titleCard = UIFactory.Panel("Butcher Paper Title", menuContent, UITheme.Terracotta);
            UIFactory.Anchor(titleCard.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 194f), new Vector2(610f, 180f));
            titleCard.rectTransform.localEulerAngles = new Vector3(0f, 0f, -2.7f);
            UIFactory.AddCartoonFinish(titleCard, 4f, 8f);

            Image titleStripe = UIFactory.Panel("Title Safran Cut", titleCard.transform, UITheme.Mustard);
            UIFactory.Anchor(titleStripe.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -12f), new Vector2(530f, 11f));

            Text brand = UIFactory.DisplayLabel("Brand", titleCard.transform, "MEAT & EAT", 66,
                UITheme.CreamLight);
            UIFactory.Stretch(brand.rectTransform, 25f, 16f);
            UIFactory.AddShadow(brand, UITheme.DropShadow, new Vector2(4f, -5f));

            menuSkewer = BuildSkewer(menuContent, new Vector2(-348f, 194f), 202f);

            Text genre = UIFactory.Label("Genre", menuContent, "SHAWARMA TYCOON", 15,
                UITheme.Teal, TextAnchor.MiddleLeft);
            UIFactory.Anchor(genre.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 82f), new Vector2(610f, 26f));

            Text promise = UIFactory.DisplayLabel("Promise", menuContent,
                "Ocağı yak. Sırayı yönet.\nDükkânı büyüt.", 34, UITheme.CreamLight,
                TextAnchor.MiddleLeft);
            promise.lineSpacing = 0.92f;
            UIFactory.Anchor(promise.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 20f), new Vector2(610f, 88f));

            RectTransform rhythm = UIFactory.Node("Shop Rhythm", menuContent);
            UIFactory.Anchor(rhythm, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -72f), new Vector2(610f, 54f));
            string[] rhythmLabels = { "PİŞİR", "SERVİS ET", "BÜYÜT" };
            string[] rhythmNames = { "Rhythm Pişir", "Rhythm Servis", "Rhythm Büyüt" };
            Color[] rhythmColors = { UITheme.Mustard, UITheme.Teal, UITheme.Terracotta };
            for (int i = 0; i < rhythmLabels.Length; i = i - (-1))
            {
                Image chip = UIFactory.Panel(rhythmNames[i], rhythm, rhythmColors[i]);
                UIFactory.Anchor(chip.rectTransform, UIFactory.Center, UIFactory.Center,
                    new Vector2((i - 1) * 198f, 0f), new Vector2(178f, 46f));
                Text label = UIFactory.Label("Label", chip.transform, rhythmLabels[i], 15,
                    i == 0 ? UITheme.Ink : Color.white);
                UIFactory.Stretch(label.rectTransform, 8f, 4f);
            }

            primaryButton = UIFactory.Button("Open Shop", menuContent, "DÜKKÂNI AÇ",
                UITheme.Mustard, UITheme.Ink, 25, BeginEnteringGame);
            UIFactory.Anchor(primaryButton.GetComponent<RectTransform>(), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -170f), new Vector2(500f, 76f));

            Button soundButton = UIFactory.Button("Sound Toggle", menuContent, "SES  •  AÇIK",
                UITheme.DarkBlueGray, Color.white, 16, ToggleSound);
            UIFactory.Anchor(soundButton.GetComponent<RectTransform>(), UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -255f), new Vector2(236f, 52f));
            soundLabel = soundButton.GetComponentInChildren<Text>();

            Text build = UIFactory.Label("Build", menuContent, "GELİŞTİRME SÜRÜMÜ  •  2026",
                13, UITheme.Hex(0xFFF4D6, 0.68f), TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Anchor(build.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, -322f), new Vector2(520f, 28f));

            Image story = UIFactory.Panel("Street Story", root, UITheme.Hex(0x271B17, 0.72f));
            storyPanel = story.rectTransform;
            UIFactory.AddCartoonFinish(story, 3f, 7f);

            Text storyBadge = UIFactory.Label("Story Badge", story.transform, "GÜN 1",
                15, UITheme.Mustard, TextAnchor.MiddleLeft);
            UIFactory.Anchor(storyBadge.rectTransform, UIFactory.TopLeft, UIFactory.TopLeft,
                new Vector2(30f, -20f), new Vector2(180f, 28f));

            Text storyTitle = UIFactory.DisplayLabel("Story Title", story.transform,
                "SOKAKTA KÜÇÜK,\nHEDEFTE KOCAMAN.", 30, UITheme.CreamLight,
                TextAnchor.MiddleLeft);
            UIFactory.Anchor(storyTitle.rectTransform, UIFactory.Center, UIFactory.Center,
                new Vector2(0f, 10f), new Vector2(450f, 90f));

            Text storyBody = UIFactory.Label("Story Body", story.transform,
                "İlk tezgâhtan mahallenin en yoğun dükkânına.", 16,
                UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Anchor(storyBody.rectTransform, UIFactory.BottomLeft, UIFactory.BottomLeft,
                new Vector2(30f, 20f), new Vector2(450f, 34f));

            menuRoot.SetActive(false);
        }

        private static RectTransform BuildSkewer(Transform parent, Vector2 offset, float height)
        {
            RectTransform skewer = UIFactory.Node("Doner Skewer", parent);
            UIFactory.Anchor(skewer, UIFactory.Center, UIFactory.Center,
                offset, new Vector2(82f, height));

            Image rod = UIFactory.Panel("Metal Rod", skewer, UITheme.CreamLight);
            UIFactory.Anchor(rod.rectTransform, UIFactory.Center, UIFactory.Center,
                Vector2.zero, new Vector2(8f, height));

            string[] sliceNames =
            {
                "Doner Slice 1", "Doner Slice 2", "Doner Slice 3",
                "Doner Slice 4", "Doner Slice 5"
            };
            for (int i = 0; i < sliceNames.Length; i = i - (-1))
            {
                float width = 66f - Mathf.Abs(2 - i) * 8f;
                Image slice = UIFactory.Panel(sliceNames[i], skewer,
                    i % 2 == 0 ? UITheme.Terracotta : UITheme.Mustard);
                UIFactory.Anchor(slice.rectTransform, UIFactory.Center, UIFactory.Center,
                    new Vector2((i % 2 == 0 ? -2f : 2f), (i - 2) * height * 0.17f),
                    new Vector2(width, 27f));
                slice.rectTransform.localEulerAngles = new Vector3(0f, 0f, i % 2 == 0 ? -5f : 4f);
            }

            Image cap = UIFactory.Icon("Skewer Cap", skewer, UITheme.Circle, UITheme.Mustard);
            UIFactory.Anchor(cap.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, 10f), new Vector2(20f, 20f));
            return skewer;
        }

        private void ShowLoading(PresentationStage nextStage, string status)
        {
            Stage = nextStage;
            stageStartedAt = Time.unscaledTime;
            displayedProgress = 0f;
            loadingStatus.text = status;
            loadingRoot.SetActive(true);
            menuRoot.SetActive(false);
            SetProgress(0f);
        }

        [ContextMenu("Preview Main Menu")]
        public void ShowMainMenu()
        {
            if (released)
            {
                released = false;
                canvas.enabled = true;
            }
            BlockGameplay();
            Stage = PresentationStage.MainMenu;
            loadingRoot.SetActive(false);
            menuRoot.SetActive(true);
            ApplyResponsiveLayout(true);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(primaryButton.gameObject);
        }

        [ContextMenu("Preview Loading Screen")]
        public void PreviewLoadingScreen()
        {
            if (released)
            {
                released = false;
                canvas.enabled = true;
            }
            BlockGameplay();
            worldReady = false;
            ShowLoading(PresentationStage.InitialLoading, "DÜKKÂN HAZIRLANIYOR");
        }

        private void BeginEnteringGame()
        {
            if (!worldReady) return;
            ShowLoading(PresentationStage.EnteringGame, "OCAK YAKILIYOR");
        }

        private void ReleaseIntoGame()
        {
            Stage = PresentationStage.Hidden;
            released = true;
            loadingRoot.SetActive(false);
            menuRoot.SetActive(false);
            if (GameHUD.Instance != null) GameHUD.Instance.SetCanvasVisible(true);
            Time.timeScale = previousTimeScale;
            canvas.enabled = false;
        }

        private void BlockGameplay()
        {
#if UNITY_EDITOR
            if (BypassGameplayPauseForTests) return;
#endif
            Time.timeScale = 0f;
            if (GameHUD.Instance != null) GameHUD.Instance.SetCanvasVisible(false);
        }

        private void ToggleSound()
        {
            AudioListener.pause = !AudioListener.pause;
            soundLabel.text = AudioListener.pause ? "SES  •  KAPALI" : "SES  •  AÇIK";
        }

        private void UpdateProgress(float duration, float target)
        {
            float elapsed = Mathf.Max(0f, Time.unscaledTime - stageStartedAt);
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float desired = Mathf.Min(target, normalized);
            displayedProgress = Mathf.MoveTowards(displayedProgress, desired,
                Time.unscaledDeltaTime * 1.8f);
            SetProgress(displayedProgress);
        }

        private void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            progressFill.anchorMax = new Vector2(value, 1f);
            int percent = Mathf.RoundToInt(value * 100f);
            if (percent == displayedPercent) return;
            displayedPercent = percent;
            progressPercent.text = percentLabels[percent];
        }

        private void AnimateSignature()
        {
            float time = Time.unscaledTime;
            if (loadingLogo != null && loadingRoot.activeSelf)
                loadingLogo.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(time * 1.4f) * 0.45f);
            if (loadingSkewer != null)
            {
                Vector2 bobbedPosition = loadingSkewerBase;
                bobbedPosition.y = loadingSkewerBase.y - (-Mathf.Sin(time * 2.1f) * 5f);
                loadingSkewer.anchoredPosition = bobbedPosition;
            }
            if (menuSkewer != null && menuRoot.activeSelf)
                menuSkewer.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(time * 1.2f) * 1.2f);
        }

        private void ApplyResponsiveLayoutIfNeeded()
        {
            Vector2Int resolution = new(Screen.width, Screen.height);
            bool isPortrait = Screen.height > Screen.width;
            if (resolution != appliedResolution || isPortrait != portrait)
                ApplyResponsiveLayout(true);
        }

        private void ApplyResponsiveLayout(bool force)
        {
            bool isPortrait = Screen.height > Screen.width;
            Vector2Int resolution = new(Screen.width, Screen.height);
            if (!force && resolution == appliedResolution && isPortrait == portrait) return;
            portrait = isPortrait;
            appliedResolution = resolution;

            if (portrait)
            {
                menuPanel.anchorMin = new Vector2(0f, 0f);
                menuPanel.anchorMax = new Vector2(1f, 0.72f);
                menuPanel.offsetMin = Vector2.zero;
                menuPanel.offsetMax = Vector2.zero;
                UIFactory.Anchor(menuContent, UIFactory.Center, UIFactory.Center,
                    new Vector2(0f, -8f), new Vector2(700f, 760f));
                UIFactory.Anchor(storyPanel, UIFactory.TopCenter, UIFactory.TopCenter,
                    new Vector2(0f, -82f), new Vector2(620f, 190f));
            }
            else
            {
                menuPanel.anchorMin = Vector2.zero;
                menuPanel.anchorMax = new Vector2(0.47f, 1f);
                menuPanel.offsetMin = Vector2.zero;
                menuPanel.offsetMax = Vector2.zero;
                UIFactory.Anchor(menuContent, UIFactory.Center, UIFactory.Center,
                    Vector2.zero, new Vector2(700f, 760f));
                UIFactory.Anchor(storyPanel, UIFactory.BottomRight, UIFactory.BottomRight,
                    new Vector2(-74f, 68f), new Vector2(510f, 200f));
            }
        }
    }
}
