using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>
    /// Floating joystick: the base snaps to wherever the player first touches an
    /// empty part of the screen, so there is no fixed thumb zone to find. It sits
    /// behind every other widget in the hierarchy, so buttons keep their taps.
    /// </summary>
    public sealed class TouchJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, Min(20f)] private float radius = 130f;
        [SerializeField, Min(0f)] private float deadZone = 0.12f;

        private RectTransform canvasRect;
        private RectTransform baseRect;
        private RectTransform knobRect;
        private CanvasGroup group;
        private Vector2 origin;
        private int activePointerId = int.MinValue;

        public Vector2 Value { get; private set; }
        public bool IsActive => activePointerId != int.MinValue;

        public static TouchJoystick Create(RectTransform parent, RectTransform canvas)
        {
            RectTransform root = UIFactory.Node("Joystick", parent);
            UIFactory.Stretch(root);
            TouchJoystick joystick = root.gameObject.AddComponent<TouchJoystick>();
            joystick.canvasRect = canvas;

            // Transparent full-screen catcher: receives drags but draws nothing.
            Image catcher = root.gameObject.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;

            RectTransform visual = UIFactory.Node("Stick", root);
            visual.anchorMin = visual.anchorMax = UIFactory.BottomLeft;
            visual.pivot = UIFactory.Center;
            visual.sizeDelta = Vector2.one * (joystick.radius * 2f);
            joystick.group = visual.gameObject.AddComponent<CanvasGroup>();
            joystick.group.alpha = 0f;
            joystick.group.blocksRaycasts = false;
            joystick.baseRect = visual;

            Image ring = UIFactory.Icon("Base", visual, UITheme.Ring, new Color(1f, 0.94f, 0.82f, 0.55f));
            UIFactory.Stretch(ring.rectTransform);

            RectTransform knob = UIFactory.Node("Knob", visual);
            knob.anchorMin = knob.anchorMax = UIFactory.Center;
            knob.pivot = UIFactory.Center;
            knob.sizeDelta = Vector2.one * (joystick.radius * 0.82f);
            Image knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = UITheme.Circle;
            knobImage.color = new Color(1f, 0.88f, 0.68f, 0.85f);
            knobImage.raycastTarget = false;
            joystick.knobRect = knob;

            return joystick;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsActive) return;
            activePointerId = eventData.pointerId;
            origin = ToCanvas(eventData);
            baseRect.anchoredPosition = origin;
            knobRect.anchoredPosition = Vector2.zero;
            group.alpha = 1f;
            Value = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            Vector2 delta = ToCanvas(eventData) - origin;
            Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
            knobRect.anchoredPosition = clamped;
            Vector2 value = clamped / radius;
            Value = value.magnitude < deadZone ? Vector2.zero : value;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            activePointerId = int.MinValue;
            Value = Vector2.zero;
            group.alpha = 0f;
        }

        private Vector2 ToCanvas(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 local);
            // Canvas rect is centre pivoted; the stick is anchored bottom-left.
            return local + canvasRect.rect.size * 0.5f;
        }
    }
}
