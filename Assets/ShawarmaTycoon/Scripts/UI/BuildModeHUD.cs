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

        public static BuildModeHUD Create(RectTransform parent)
        {
            RectTransform root = UIFactory.Node("Build Mode", parent);
            UIFactory.Stretch(root);
            BuildModeHUD hud = root.gameObject.AddComponent<BuildModeHUD>();

            hud.toggleButton = UIFactory.Button(
                "Build Toggle", root, "İNŞA", UITheme.Mustard, UITheme.Ink,
                UITheme.FontSmall, () => hud.controller?.ToggleBuildMode());
            UIFactory.Anchor(hud.toggleButton.GetComponent<RectTransform>(),
                UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-24f, -224f), new Vector2(176f, 84f));
            hud.toggleLabel = hud.toggleButton.GetComponentInChildren<Text>();

            Image banner = UIFactory.Panel("Build Banner", root, UITheme.Panel);
            UIFactory.Anchor(banner.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -238f), new Vector2(620f, 112f));
            UIFactory.AddShadow(banner, new Color(0f, 0f, 0f, 0.20f), new Vector2(0f, -4f));
            hud.modeBanner = banner.rectTransform;

            hud.selectionLabel = UIFactory.Label(
                "Selection", banner.transform, "İNŞA MODU", UITheme.FontBody, UITheme.Terracotta);
            UIFactory.Anchor(hud.selectionLabel.rectTransform, UIFactory.TopCenter, UIFactory.TopCenter,
                new Vector2(0f, -8f), new Vector2(580f, 42f));

            hud.messageLabel = UIFactory.Label(
                "Message", banner.transform, "Bir eşyaya dokunup sürükle",
                UITheme.FontSmall, UITheme.InkSoft, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Anchor(hud.messageLabel.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 10f), new Vector2(580f, 46f));

            Image toolbar = UIFactory.Panel("Build Toolbar", root, UITheme.Panel);
            UIFactory.Anchor(toolbar.rectTransform, UIFactory.BottomCenter, UIFactory.BottomCenter,
                new Vector2(0f, 42f), new Vector2(680f, 118f));
            UIFactory.AddShadow(toolbar, new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, -5f));

            hud.rotateButton = UIFactory.Button(
                "Rotate", toolbar.transform, "90° DÖNDÜR", UITheme.Teal, Color.white,
                UITheme.FontSmall, () => hud.controller?.RotateSelected());
            UIFactory.Anchor(hud.rotateButton.GetComponent<RectTransform>(),
                UIFactory.Center, UIFactory.Center, new Vector2(-165f, 0f), new Vector2(290f, 78f));

            hud.resetButton = UIFactory.Button(
                "Reset", toolbar.transform, "SIFIRLA", UITheme.Terracotta, Color.white,
                UITheme.FontSmall, () => hud.controller?.ResetSelected());
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
            toggleLabel.text = active ? "BİTİR" : "İNŞA";
            toggleButton.GetComponent<Image>().color = active ? UITheme.Green : UITheme.Mustard;
            modeBanner.gameObject.SetActive(active);
            toolbar.gameObject.SetActive(active);
            if (!active) return;

            PlaceableObject selected = controller.Selected;
            selectionLabel.text = selected == null ? "İNŞA MODU" : selected.DisplayName.ToUpperInvariant();
            messageLabel.text = controller.Message;
            messageLabel.color = controller.PlacementValid ? UITheme.InkSoft : UITheme.WarmRed;
            bool hasSelection = selected != null && selected.CanMove;
            rotateButton.interactable = hasSelection;
            resetButton.interactable = hasSelection;
        }
    }
}
