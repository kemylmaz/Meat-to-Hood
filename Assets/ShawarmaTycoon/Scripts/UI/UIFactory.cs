using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Small helpers for assembling uGUI hierarchies from code.</summary>
    public static class UIFactory
    {
        public static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        public static Image Panel(string name, Transform parent, Color color, bool rounded = true)
        {
            RectTransform rect = Node(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = UITheme.Rounded;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 2.4f;
            }
            return image;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rect = Node(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Bold)
        {
            RectTransform rect = Node(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = UITheme.Font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            return text;
        }

        public static Button Button(
            string name,
            Transform parent,
            string caption,
            Color background,
            Color textColor,
            int fontSize,
            Action onClick)
        {
            Image image = Panel(name, parent, background);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(0.72f, 0.72f, 0.72f, 0.65f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            if (!string.IsNullOrEmpty(caption))
            {
                Text label = Label("Label", image.transform, caption, fontSize, textColor);
                Stretch(label.rectTransform, 10f, 6f);
            }
            return button;
        }

        // ---- layout helpers -------------------------------------------------

        public static RectTransform Stretch(RectTransform rect, float paddingX = 0f, float paddingY = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(paddingX, paddingY);
            rect.offsetMax = new Vector2(-paddingX, -paddingY);
            return rect;
        }

        /// <summary>Anchors a fixed-size box to a corner/edge of its parent.</summary>
        public static RectTransform Anchor(
            RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect;
        }

        public static readonly Vector2 TopLeft = new(0f, 1f);
        public static readonly Vector2 TopRight = new(1f, 1f);
        public static readonly Vector2 TopCenter = new(0.5f, 1f);
        public static readonly Vector2 Center = new(0.5f, 0.5f);
        public static readonly Vector2 BottomLeft = new(0f, 0f);
        public static readonly Vector2 BottomRight = new(1f, 0f);
        public static readonly Vector2 BottomCenter = new(0.5f, 0f);

        public static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }
}
