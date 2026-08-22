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
        private Image catcher;
        private Vector2 origin;
        private int activePointerId = int.MinValue;
        private bool buildMode;

        public Vector2 Value { get; private set; }
        public bool IsActive => activePointerId != int.MinValue;

        public static TouchJoystick Create(RectTransform parent, RectTransform canvas)
        {
            RectTransform root = UIFactory.Node("Joystick", parent);
            UIFactory.Stretch(root);
            TouchJoystick joystick = root.gameObject.AddComponent<TouchJoystick>();
            joystick.canvasRect = canvas;

            // Transparent full-screen catcher: receives drags but draws nothing.
            joystick.catcher = root.gameObject.AddComponent<Image>();
            joystick.catcher.color = new Color(0f, 0f, 0f, 0f);
            joystick.catcher.raycastTarget = true;

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

        public void SetInputEnabled(bool inputEnabled)
        {
            enabled = inputEnabled;
            if (catcher != null) catcher.raycastTarget = inputEnabled;
            if (inputEnabled) return;
            ResetInput();
        }

        /// <summary>
        /// During layout editing only the lower-left touch zone belongs to
        /// movement; the rest of the transparent catcher yields to furniture.
        /// Mouse users keep the whole restaurant selectable and move with WASD.
        /// </summary>
        public void SetBuildMode(bool active)
        {
            buildMode = active;
            ResetInput();
        }

        public bool ClaimsBuildModePointer(Vector2 screenPosition, bool isTouchPointer) =>
            buildMode && isTouchPointer && InBuildMovementZone(screenPosition);

        private static bool InBuildMovementZone(Vector2 screenPosition)
        {
            // Use the shorter screen edge so portrait does not turn almost half
            // the display into a joystick and hide furniture from touch editing.
            float extent = Mathf.Min(Screen.width, Screen.height) * 0.36f;
            return screenPosition.x <= extent && screenPosition.y <= extent;
        }

        private void ResetInput()
        {
            activePointerId = int.MinValue;
            Value = Vector2.zero;
            if (group != null) group.alpha = 0f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsActive) return;
            if (buildMode && (eventData.pointerId < 0 || !InBuildMovementZone(eventData.position))) return;
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
