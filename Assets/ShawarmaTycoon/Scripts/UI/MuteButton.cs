using UnityEngine;
using UnityEngine.UI;

namespace ShawarmaTycoon.UI
{
    /// <summary>Small speaker toggle; the choice persists through GameProgress.</summary>
    public sealed class MuteButton : MonoBehaviour
    {
        private Text glyph;

        public static MuteButton Create(RectTransform parent)
        {
            MuteButton mute = null;
            Button button = UIFactory.Button("Mute", parent, "", UITheme.Panel,
                UITheme.Ink, UITheme.FontBody, () => mute.Toggle());
            UIFactory.Anchor(button.GetComponent<RectTransform>(), UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-24f, -124f), new Vector2(88f, 88f));

            mute = button.gameObject.AddComponent<MuteButton>();
            mute.glyph = UIFactory.Label("Glyph", button.transform, "♪",
                UITheme.FontLarge, UITheme.Ink);
            UIFactory.Stretch(mute.glyph.rectTransform);
            return mute;
        }

        private void Start() => Render();

        private void Toggle()
        {
            if (AudioDirector.Instance == null) return;
            AudioDirector.Instance.SetMuted(!AudioDirector.Instance.Muted);
            Render();
            AudioDirector.Play(GameSfx.Pickup);
        }

        private void Render()
        {
            bool muted = AudioDirector.Instance != null && AudioDirector.Instance.Muted;
            glyph.text = muted ? "×" : "♪";
            glyph.color = muted ? UITheme.InkSoft : UITheme.Ink;
        }
    }
}
