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
            text.font = UITheme.BodyFont;
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

        public static Text DisplayLabel(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            Text text = Label(name, parent, content, fontSize, color, anchor);
            text.font = UITheme.DisplayFont;
            text.fontStyle = FontStyle.Bold;
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

            // Always built, even for an empty caption. Callers that fill the text
            // in later reach for it with GetComponentInChildren<Text>, and when it
            // was skipped they cached a null and threw on every frame the panel
            // was open.
            Text label = Label("Label", image.transform, caption, fontSize, textColor);
            label.font = UITheme.DisplayFont;
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform, 10f, 6f);
            AddCartoonFinish(image);
            image.gameObject.AddComponent<CartoonButtonFeedback>();
            return button;
        }

        /// <summary>
        /// Compact mobile tool: one clear icon with a tiny persistent label.
        /// The icon is a drawn sprite rather than a character on purpose — see
        /// the note above the icons in <see cref="UITheme"/>.
        /// </summary>
        public static Button IconButton(
            string name,
            Transform parent,
            Sprite icon,
            string caption,
            Color background,
            Color foreground,
            Action onClick)
        {
            Button button = Button(name, parent, string.Empty, background, foreground, 14, onClick);
            Text captionLabel = button.GetComponentInChildren<Text>();
            captionLabel.name = "Caption";
            captionLabel.text = caption;
            captionLabel.font = UITheme.BodyFont;
            captionLabel.fontSize = 13;
            captionLabel.fontStyle = FontStyle.Bold;
            UIFactory.Anchor(captionLabel.rectTransform, BottomCenter, BottomCenter,
                new Vector2(0f, 5f), new Vector2(76f, 22f));

            Image glyph = Icon("Glyph", button.transform, icon, foreground);
            UIFactory.Anchor(glyph.rectTransform, TopCenter, TopCenter,
                new Vector2(0f, -10f), new Vector2(38f, 38f));
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

        /// <summary>A dark painted rim plus an offset, toy-like physical shadow.</summary>
        public static void AddCartoonFinish(Graphic graphic, float outline = 3f, float drop = 7f)
        {
            Outline rim = graphic.gameObject.AddComponent<Outline>();
            rim.effectColor = UITheme.Ink;
            rim.effectDistance = new Vector2(outline, -outline);

            Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = UITheme.DropShadow;
            shadow.effectDistance = new Vector2(drop, -drop);
        }
    }
}
