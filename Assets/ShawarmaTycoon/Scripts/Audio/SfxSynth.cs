using System;
using UnityEngine;

namespace ShawarmaTycoon
{
    public enum Waveform
    {
        Sine,
        Triangle,
        Square,
        Noise
    }

    /// <summary>
    /// Builds short sound effects as raw AudioClips at runtime. The project ships
    /// no audio files, so every cue is synthesised from a tiny recipe: a waveform,
    /// a pitch sweep and an exponential decay envelope.
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 44100;

        public readonly struct Tone
        {
            public readonly Waveform Wave;
            public readonly float StartHz;
            public readonly float EndHz;
            public readonly float Start;      // seconds into the clip
            public readonly float Duration;
            public readonly float Gain;
            public readonly float Attack;

            public Tone(Waveform wave, float startHz, float endHz, float start,
                        float duration, float gain = 0.5f, float attack = 0.004f)
            {
                Wave = wave;
                StartHz = startHz;
                EndHz = endHz;
                Start = start;
                Duration = duration;
                Gain = gain;
                Attack = attack;
            }
        }

        public static AudioClip Build(string name, params Tone[] tones)
        {
            float length = 0.05f;
            foreach (Tone tone in tones)
                length = Mathf.Max(length, tone.Start + tone.Duration);

            int sampleCount = Mathf.CeilToInt(length * SampleRate);
            float[] data = new float[sampleCount];
            System.Random random = new(name.GetHashCode());

            foreach (Tone tone in tones)
            {
                int begin = Mathf.Clamp(Mathf.RoundToInt(tone.Start * SampleRate), 0, sampleCount);
                int count = Mathf.Clamp(Mathf.RoundToInt(tone.Duration * SampleRate), 0, sampleCount - begin);
                if (count <= 0) continue;

                double phase = 0.0;
                float attackSamples = Mathf.Max(1f, tone.Attack * SampleRate);
                for (int i = 0; i < count; i++)
                {
                    float t = i / (float)count;
                    float frequency = Mathf.Lerp(tone.StartHz, tone.EndHz, t * t);
                    phase += frequency / SampleRate;
                    float value = Sample(tone.Wave, (float)(phase % 1.0), random);

                    float attack = Mathf.Min(1f, i / attackSamples);
                    float decay = Mathf.Exp(-4.2f * t);
                    data[begin + i] += value * tone.Gain * attack * decay;
                }
            }

            // Soft clip so stacked tones never crackle.
            for (int i = 0; i < sampleCount; i++)
                data[i] = (float)Math.Tanh(data[i] * 1.25f) * 0.92f;

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sample(Waveform wave, float phase, System.Random random)
        {
            switch (wave)
            {
                case Waveform.Sine:
                    return Mathf.Sin(phase * Mathf.PI * 2f);
                case Waveform.Triangle:
                    return 4f * Mathf.Abs(phase - 0.5f) - 1f;
                case Waveform.Square:
                    return phase < 0.5f ? 0.55f : -0.55f;
                default:
                    return (float)(random.NextDouble() * 2.0 - 1.0) * 0.7f;
            }
        }
    }
}
