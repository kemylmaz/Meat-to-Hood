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
            Button button = UIFactory.Button("Mute", parent, "", UITheme.CounterPaper,
                UITheme.Ink, UITheme.FontBody, () => mute.Toggle());
            UIFactory.Anchor(button.GetComponent<RectTransform>(), UIFactory.TopRight, UIFactory.TopRight,
                new Vector2(-28f, -132f), new Vector2(76f, 76f));

            mute = button.gameObject.AddComponent<MuteButton>();
            // The button brings its own label; adding a second one here would
            // stack two glyphs on top of each other.
            mute.glyph = button.GetComponentInChildren<Text>();
            mute.glyph.text = "♪";
            mute.glyph.fontSize = 34;
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
