using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>
    /// Builds and owns the whole runtime HUD: canvas, safe area, joystick and
    /// widgets. Replaces the old OnGUI screens - IMGUI allocates every frame and
    /// has no safe-area or DPI story on phones.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        public RectTransform SafeArea { get; private set; }
        public RectTransform ModalLayer { get; private set; }
        public RectTransform CanvasRect { get; private set; }
        public TouchJoystick Joystick { get; private set; }
        public ObjectiveBanner Objective { get; private set; }
        public BuildModeHUD BuildMode { get; private set; }
        public Canvas Canvas { get; private set; }

        private readonly System.Collections.Generic.List<GameObject> gameplayChrome = new();

        public static GameHUD Ensure(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject go = new("Game HUD");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<GameHUD>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureEventSystem();
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject go = new("Event System");
            go.AddComponent<EventSystem>();
            // The project runs the Input System package only, so the legacy
            // StandaloneInputModule would never receive input.
            InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private void Build()
        {
            GameObject canvasObject = new("HUD Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.layer = LayerMask.NameToLayer("UI");

            Canvas = canvasObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasRect = (RectTransform)canvasObject.transform;

            SafeArea = UIFactory.Node("Safe Area", CanvasRect);
            UIFactory.Stretch(SafeArea);
            SafeArea.gameObject.AddComponent<SafeAreaFitter>();

            // Order matters: the joystick catcher sits at the bottom of the draw
            // order so every widget above it keeps its own taps.
            Joystick = TouchJoystick.Create(SafeArea, CanvasRect);

            gameplayChrome.Add(CoinCounter.Create(SafeArea).gameObject);
            gameplayChrome.Add(ShopProgressBar.Create(SafeArea).gameObject);
            gameplayChrome.Add(StatusPanel.Create(SafeArea).gameObject);
            gameplayChrome.Add(TasksPanel.Create(SafeArea).gameObject);
            Objective = ObjectiveBanner.Create(SafeArea);
            gameplayChrome.Add(Objective.gameObject);
            gameplayChrome.Add(MuteButton.Create(SafeArea).gameObject);
            BuildMode = BuildModeHUD.Create(SafeArea);

            ModalLayer = UIFactory.Node("Modals", SafeArea);
            UIFactory.Stretch(ModalLayer);
        }

        /// <summary>Build mode gets a calm, distraction-free editing surface.</summary>
        public void SetGameplayChromeVisible(bool visible)
        {
            for (int i = 0; i < gameplayChrome.Count; i++)
            {
                GameObject item = gameplayChrome[i];
                if (item != null) item.SetActive(visible);
            }
        }

        /// <summary>
        /// Startup presentation owns the screen before play begins. Disabling
        /// the canvas keeps tutorial cards and touch controls from flashing
        /// through it while preserving their runtime state for the first frame.
        /// </summary>
        public void SetCanvasVisible(bool visible)
        {
            if (Canvas != null) Canvas.enabled = visible;
        }
    }
}
