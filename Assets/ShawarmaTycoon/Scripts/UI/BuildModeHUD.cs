using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Runtime controls and guidance for the restaurant layout editor.</summary>
    public sealed class BuildModeHUD : MonoBehaviour
    {
        private BuildModeController controller;
        private Button toggleButton;
        private Text toggleLabel;
        private RectTransform modeBanner;
        private Text messageLabel;
        private Text selectionLabel;
        private Button rotateButton;
        private Button resetButton;
        private bool? lastActive;

        public static BuildModeHUD Create(RectTransform parent)
        {
            RectTransform root = UIFactory.Node("Build Mode", parent);
            UIFactory.Stretch(root);
            BuildModeHUD hud = root.gameObject.AddComponent<BuildModeHUD>();

            hud.toggleButton = UIFactory.Button(
                "Build Toggle", root, "İNŞA", UITheme.Teal, UITheme.Ink,
                22, () => hud.controller?.ToggleBuildMode());
            UIFactory.Anchor(hud.toggleButton.GetComponent<RectTransform>(),
                UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-28f, -226f), new Vector2(156f, 78f));
            hud.toggleLabel = hud.toggleButton.GetComponentInChildren<Text>();

            Image banner = UIFactory.Panel("Build Banner", root, UITheme.Mustard);
            UIFactory.Anchor(banner.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -32f), new Vector2(650f, 118f));
            UIFactory.AddCartoonFinish(banner, 4f, 9f);
            banner.rectTransform.localEulerAngles = new Vector3(0f, 0f, 1f);
            hud.modeBanner = banner.rectTransform;

            hud.selectionLabel = UIFactory.Label(
                "Selection", banner.transform, "İNŞA MODU", 32, UITheme.Ink);
            hud.selectionLabel.font = UITheme.DisplayFont;
            hud.selectionLabel.fontStyle = FontStyle.Bold;
            UIFactory.Anchor(hud.selectionLabel.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -8f), new Vector2(580f, 42f));

            hud.messageLabel = UIFactory.Label(
                "Message", banner.transform, "Bir eşyaya dokunup sürükle",
                23, UITheme.InkSoft, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Anchor(hud.messageLabel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 10f), new Vector2(580f, 46f));

            Image toolbar = UIFactory.Panel("Build Toolbar", root, UITheme.CounterPaper);
            UIFactory.Anchor(toolbar.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 40f), new Vector2(690f, 116f));
            UIFactory.AddCartoonFinish(toolbar, 3f, 8f);

            hud.rotateButton = UIFactory.Button(
                "Rotate", toolbar.transform, "90° DÖNDÜR", UITheme.Teal, Color.white,
                23, () => hud.controller?.RotateSelected());
            UIFactory.Anchor(hud.rotateButton.GetComponent<RectTransform>(),
                UIFactory.Center, UIFactory.Center, new Vector2(-165f, 0f), new Vector2(290f, 78f));

            hud.resetButton = UIFactory.Button(
                "Reset", toolbar.transform, "SIFIRLA", UITheme.Terracotta, Color.white,
                23, () => hud.controller?.ResetSelected());
            UIFactory.Anchor(hud.resetButton.GetComponent<RectTransform>(),
                UIFactory.Center, UIFactory.Center, new Vector2(165f, 0f), new Vector2(290f, 78f));

            hud.modeBanner.gameObject.SetActive(false);
            toolbar.gameObject.SetActive(false);
            hud.toolbar = toolbar.rectTransform;
            return hud;
        }

        private RectTransform toolbar;

        public void Bind(BuildModeController buildController)
        {
            controller = buildController;
            RefreshImmediately();
        }

        private void Update()
        {
            if (controller != null && controller.IsActive) RefreshImmediately();
        }

        public void RefreshImmediately()
        {
            bool active = controller != null && controller.IsActive;
            if (lastActive != active)
            {
                lastActive = active;
                GameHUD.Instance?.SetGameplayChromeVisible(!active);
            }
            toggleLabel.text = active ? "BİTİR" : "İNŞA";
            toggleButton.GetComponent<Image>().color = active ? UITheme.Green : UITheme.Teal;
            modeBanner.gameObject.SetActive(active);
            toolbar.gameObject.SetActive(active);
            if (!active) return;

            PlaceableObject selected = controller.Selected;
            selectionLabel.text = selected == null ? "İNŞA MODU" : selected.DisplayName.ToUpperInvariant();
            messageLabel.text = controller.Message;
            // The default "pick something" hint is neutral. Red only means an
            // actually selected object is currently in an invalid position.
            messageLabel.color = selected != null && !controller.PlacementValid
                ? UITheme.WarmRed
                : UITheme.InkSoft;
            bool hasSelection = selected != null && selected.CanMove;
            rotateButton.interactable = hasSelection;
            resetButton.interactable = hasSelection;
        }
    }
}
