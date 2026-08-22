using System.Collections.Generic;
using UnityEngine;

namespace ShawarmaTycoon
{
    public enum GameSfx
    {
        Pickup,
        Drop,
        Serve,
        Cook,
        Coin,
        CashRegister,
        ComboUp,
        Error,
        Unlock,
        Reward,
        Trash,
        CustomerArrive
    }

    /// <summary>
    /// One place that owns every sound cue. Clips are synthesised on first use and
    /// played through a small round-robin AudioSource pool so overlapping events
    /// never cut each other off.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int VoiceCount = 8;
        private const float RepeatGuardSeconds = 0.035f;

        public static AudioDirector Instance { get; private set; }

        private readonly Dictionary<GameSfx, AudioClip> clips = new();
        private readonly Dictionary<GameSfx, float> lastPlayed = new();
        private AudioSource[] voices;
        private AudioSource musicVoice;
        private int nextVoice;
        private float masterVolume = 0.8f;

        public static AudioDirector Ensure(Transform parent)
        {
            if (Instance != null) return Instance;
            GameObject go = new("Audio Director");
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<AudioDirector>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            masterVolume = GameProgress.GetInt("audio.muted", 0) == 1 ? 0f : 0.8f;
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.dopplerLevel = 0f;
                voices[i] = source;
            }

            musicVoice = gameObject.AddComponent<AudioSource>();
            musicVoice.playOnAwake = false;
            musicVoice.loop = true;
            musicVoice.spatialBlend = 0f;
            musicVoice.dopplerLevel = 0f;
            musicVoice.volume = masterVolume * 0.38f;
            musicVoice.clip = Resources.Load<AudioClip>("Audio/Music/meat_and_eat_main_loop");
            if (musicVoice.clip != null && masterVolume > 0f) musicVoice.Play();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool Muted => masterVolume <= 0f;

        public void SetMuted(bool muted)
        {
            masterVolume = muted ? 0f : 0.8f;
            GameProgress.SetInt("audio.muted", muted ? 1 : 0);
            if (musicVoice != null)
            {
                musicVoice.volume = masterVolume * 0.38f;
                if (muted) musicVoice.Pause();
                else if (musicVoice.clip != null) musicVoice.UnPause();
            }
        }

        public static void Play(GameSfx sfx, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null) Instance.PlayInternal(sfx, volume, pitch);
        }

        private void PlayInternal(GameSfx sfx, float volume, float pitch)
        {
            if (masterVolume <= 0f || voices == null) return;

            // Guard against the same cue firing many times in one frame (e.g. a
            // stack of items transferring at once).
            if (lastPlayed.TryGetValue(sfx, out float last) &&
                Time.unscaledTime - last < RepeatGuardSeconds)
                return;
            lastPlayed[sfx] = Time.unscaledTime;

            if (!clips.TryGetValue(sfx, out AudioClip clip))
            {
                clip = BuildClip(sfx);
                clips[sfx] = clip;
            }
            if (clip == null) return;

            AudioSource source = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            source.pitch = Mathf.Clamp(pitch * Random.Range(0.97f, 1.03f), 0.4f, 2.6f);
            source.volume = Mathf.Clamp01(volume * masterVolume);
            source.PlayOneShot(clip);
        }

        private static AudioClip BuildClip(GameSfx sfx)
        {
            AudioClip authored = Resources.Load<AudioClip>(
                "Audio/SFX/" + ResourceName(sfx));
            if (authored != null) return authored;

            switch (sfx)
            {
                case GameSfx.Pickup:
                    return SfxSynth.Build("sfx_pickup",
                        new SfxSynth.Tone(Waveform.Sine, 620f, 900f, 0f, 0.085f, 0.42f));

                case GameSfx.Drop:
                    return SfxSynth.Build("sfx_drop",
                        new SfxSynth.Tone(Waveform.Sine, 470f, 300f, 0f, 0.10f, 0.42f));

                case GameSfx.Serve:
                    return SfxSynth.Build("sfx_serve",
                        new SfxSynth.Tone(Waveform.Triangle, 520f, 720f, 0f, 0.12f, 0.32f),
                        new SfxSynth.Tone(Waveform.Sine, 880f, 880f, 0.08f, 0.12f, 0.20f));

                case GameSfx.Cook:
                    return SfxSynth.Build("sfx_cook",
                        new SfxSynth.Tone(Waveform.Triangle, 320f, 560f, 0f, 0.16f, 0.34f),
                        new SfxSynth.Tone(Waveform.Noise, 1f, 1f, 0f, 0.09f, 0.10f));

                case GameSfx.Coin:
                    return SfxSynth.Build("sfx_coin",
                        new SfxSynth.Tone(Waveform.Sine, 988f, 988f, 0f, 0.055f, 0.34f),
                        new SfxSynth.Tone(Waveform.Sine, 1319f, 1319f, 0.05f, 0.13f, 0.30f));

                case GameSfx.CashRegister:
                    return SfxSynth.Build("sfx_register",
                        new SfxSynth.Tone(Waveform.Noise, 1f, 1f, 0f, 0.05f, 0.16f),
                        new SfxSynth.Tone(Waveform.Sine, 1046f, 1046f, 0.02f, 0.26f, 0.34f),
                        new SfxSynth.Tone(Waveform.Sine, 1568f, 1568f, 0.06f, 0.22f, 0.20f));

                case GameSfx.ComboUp:
                    return SfxSynth.Build("sfx_combo",
                        new SfxSynth.Tone(Waveform.Triangle, 660f, 660f, 0f, 0.07f, 0.30f),
                        new SfxSynth.Tone(Waveform.Triangle, 880f, 880f, 0.06f, 0.09f, 0.30f));

                case GameSfx.Error:
                    return SfxSynth.Build("sfx_error",
                        new SfxSynth.Tone(Waveform.Square, 220f, 155f, 0f, 0.15f, 0.22f));

                case GameSfx.Unlock:
                    return SfxSynth.Build("sfx_unlock",
                        new SfxSynth.Tone(Waveform.Triangle, 523f, 523f, 0f, 0.10f, 0.30f),
                        new SfxSynth.Tone(Waveform.Triangle, 784f, 784f, 0.09f, 0.10f, 0.30f),
                        new SfxSynth.Tone(Waveform.Triangle, 1046f, 1046f, 0.18f, 0.24f, 0.32f));

                case GameSfx.Reward:
                    return SfxSynth.Build("sfx_reward",
                        new SfxSynth.Tone(Waveform.Sine, 523f, 523f, 0f, 0.10f, 0.26f),
                        new SfxSynth.Tone(Waveform.Sine, 659f, 659f, 0.09f, 0.10f, 0.26f),
                        new SfxSynth.Tone(Waveform.Sine, 784f, 784f, 0.18f, 0.10f, 0.26f),
                        new SfxSynth.Tone(Waveform.Sine, 1046f, 1046f, 0.27f, 0.32f, 0.30f));

                case GameSfx.Trash:
                    return SfxSynth.Build("sfx_trash",
                        new SfxSynth.Tone(Waveform.Noise, 1f, 1f, 0f, 0.17f, 0.20f),
                        new SfxSynth.Tone(Waveform.Sine, 240f, 150f, 0f, 0.13f, 0.16f));

                default:
                    return SfxSynth.Build("sfx_customer",
                        new SfxSynth.Tone(Waveform.Sine, 784f, 1046f, 0f, 0.12f, 0.24f));
            }
        }

        private static string ResourceName(GameSfx sfx)
        {
            switch (sfx)
            {
                case GameSfx.Pickup: return "pickup";
                case GameSfx.Drop: return "drop";
                case GameSfx.Serve: return "serve";
                case GameSfx.Cook: return "cook";
                case GameSfx.Coin: return "coin";
                case GameSfx.CashRegister: return "cash_register";
                case GameSfx.ComboUp: return "combo_up";
                case GameSfx.Error: return "error";
                case GameSfx.Unlock: return "unlock";
                case GameSfx.Reward: return "reward";
                case GameSfx.Trash: return "trash";
                default: return "customer_arrive";
            }
        }
    }
}
