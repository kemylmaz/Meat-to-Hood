using UnityEngine;
using UnityEngine.EventSystems;

namespace ShawarmaTycoon.UI
{
    /// <summary>Gives runtime-built buttons a short, physical press instead of a tint only.</summary>
    public sealed class CartoonButtonFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private RectTransform rect;
        private Vector3 restingScale;
        private Vector2 restingPosition;
        private bool pressed;

        private void Awake()
        {
            rect = (RectTransform)transform;
        }

        private void OnDisable() => Restore();

        public void OnPointerDown(PointerEventData eventData)
        {
            restingScale = rect.localScale;
            restingPosition = rect.anchoredPosition;
            pressed = true;
            rect.localScale = restingScale * 0.96f;
            rect.anchoredPosition = restingPosition + new Vector2(2f, -4f);
        }

        public void OnPointerUp(PointerEventData eventData) => Restore();
        public void OnPointerExit(PointerEventData eventData) => Restore();

        private void Restore()
        {
            if (rect == null || !pressed) return;
            pressed = false;
            rect.localScale = restingScale;
            rect.anchoredPosition = restingPosition;
        }
    }
}
