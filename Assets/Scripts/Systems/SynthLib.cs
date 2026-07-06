using System;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Pure-code audio synthesis. Every SFX and the ambient music track are generated
    /// from oscillators + noise. The editor setup can bake these to .wav assets.</summary>
    public static class SynthLib
    {
        public const int SampleRate = 44100;

        public static readonly string[] AllClipNames =
        {
            "select", "move", "attack_order", "fire", "fire_heavy", "melee", "explosion",
            "build_place", "build_done", "train_done", "deposit", "error", "click",
            "voice_select", "voice_move", "voice_attack", "voice_build", "voice_warning",
            "victory", "defeat", "music"
        };

        public static float[] Generate(string name)
        {
            switch (name)
            {
                case "select": return Blip(660f, 880f, 0.09f, 0.25f);
                case "move": return Blip(440f, 520f, 0.08f, 0.2f);
                case "attack_order": return Blip(330f, 220f, 0.12f, 0.3f);
                case "fire": return Laser(1400f, 300f, 0.12f, 0.35f);
                case "fire_heavy": return Thump(90f, 0.25f, 0.5f);
                case "melee": return NoiseHit(0.06f, 0.25f, 3000f);
                case "explosion": return Explosion(0.7f, 0.55f);
                case "build_place": return Thump(70f, 0.3f, 0.45f);
                case "build_done": return Chime(new[] { 523.25f, 659.25f, 783.99f }, 0.5f, 0.3f);
                case "train_done": return Chime(new[] { 659.25f, 880f }, 0.35f, 0.25f);
                case "deposit": return Blip(1200f, 1600f, 0.06f, 0.18f);
                case "error": return Buzz(140f, 0.18f, 0.3f);
                case "click": return Blip(900f, 900f, 0.03f, 0.2f);
                case "voice_select": return Voice(new[] { 520f, 690f, 610f }, 0.32f, 0.22f);
                case "voice_move": return Voice(new[] { 430f, 510f, 610f }, 0.34f, 0.24f);
                case "voice_attack": return Voice(new[] { 360f, 310f, 460f, 300f }, 0.42f, 0.28f);
                case "voice_build": return Voice(new[] { 480f, 560f, 520f, 650f }, 0.45f, 0.24f);
                case "voice_warning": return Voice(new[] { 240f, 190f, 240f, 190f }, 0.55f, 0.3f);
                case "victory": return Chime(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 1.6f, 0.35f);
                case "defeat": return Chime(new[] { 392f, 311.13f, 261.63f }, 1.8f, 0.3f);
                case "music": return Music();
                default:
                    Debug.LogError($"VoidClash: unknown clip '{name}'");
                    return new float[100];
            }
        }

        public static AudioClip CreateClip(string name)
        {
            var data = Generate(name);
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------- Building blocks ----------

        static float[] NewBuf(float seconds) => new float[(int)(seconds * SampleRate)];

        static float Env(int i, int total, float attackFrac = 0.02f)
        {
            float t = i / (float)total;
            if (t < attackFrac) return t / attackFrac;
            return Mathf.Pow(1f - (t - attackFrac) / (1f - attackFrac), 2f);
        }

        static float[] Blip(float f0, float f1, float dur, float vol)
        {
            var buf = NewBuf(dur);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(f0, f1, i / (float)buf.Length);
                buf[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Env(i, buf.Length) * vol;
            }
            return buf;
        }

        static float[] Laser(float f0, float f1, float dur, float vol)
        {
            var buf = NewBuf(dur);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)SampleRate;
                float k = i / (float)buf.Length;
                float f = Mathf.Lerp(f0, f1, k * k);
                float saw = 2f * (f * t - Mathf.Floor(f * t + 0.5f));
                buf[i] = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f + saw * 0.3f) * Env(i, buf.Length, 0.01f) * vol;
            }
            return buf;
        }

        static float[] Thump(float f, float dur, float vol)
        {
            var buf = NewBuf(dur);
            var rng = new System.Random(42);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)SampleRate;
                float freq = f * (1f + 2.5f * Mathf.Exp(-t * 30f));
                float body = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.4f;
                buf[i] = (body + noise) * Env(i, buf.Length, 0.005f) * vol;
            }
            return buf;
        }

        static float[] NoiseHit(float dur, float vol, float lpFreq)
        {
            var buf = NewBuf(dur);
            var rng = new System.Random(7);
            float last = 0f;
            float alpha = Mathf.Clamp01(lpFreq / SampleRate * 6f);
            for (int i = 0; i < buf.Length; i++)
            {
                float n = (float)rng.NextDouble() * 2f - 1f;
                last += alpha * (n - last);
                buf[i] = last * Env(i, buf.Length, 0.01f) * vol;
            }
            return buf;
        }

        static float[] Explosion(float dur, float vol)
        {
            var buf = NewBuf(dur);
            var rng = new System.Random(99);
            float last = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)buf.Length;
                float lp = Mathf.Lerp(0.5f, 0.02f, t); // closing filter
                float n = (float)rng.NextDouble() * 2f - 1f;
                last += lp * (n - last);
                float rumble = Mathf.Sin(2f * Mathf.PI * 45f * (i / (float)SampleRate)) * (1f - t) * 0.5f;
                buf[i] = (last + rumble) * Env(i, buf.Length, 0.004f) * vol;
            }
            return buf;
        }

        static float[] Buzz(float f, float dur, float vol)
        {
            var buf = NewBuf(dur);
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)SampleRate;
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));
                buf[i] = sq * Env(i, buf.Length, 0.02f) * vol * 0.6f;
            }
            return buf;
        }

        static float[] Chime(float[] notes, float dur, float vol)
        {
            var buf = NewBuf(dur);
            float noteDur = dur / notes.Length;
            int noteSamples = (int)(noteDur * SampleRate);
            for (int n = 0; n < notes.Length; n++)
            {
                int start = n * noteSamples;
                int len = Mathf.Min(noteSamples * 2, buf.Length - start); // notes ring past their slot
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)SampleRate;
                    float s = Mathf.Sin(2f * Mathf.PI * notes[n] * t)
                            + Mathf.Sin(2f * Mathf.PI * notes[n] * 2f * t) * 0.3f;
                    buf[start + i] += s * Mathf.Pow(1f - i / (float)len, 1.6f) * vol * 0.6f;
                }
            }
            return buf;
        }

        static float[] Voice(float[] tones, float dur, float vol)
        {
            var buf = NewBuf(dur);
            int syllable = Mathf.Max(1, buf.Length / tones.Length);
            var rng = new System.Random(123 + tones.Length);
            for (int i = 0; i < buf.Length; i++)
            {
                int n = Mathf.Min(tones.Length - 1, i / syllable);
                float t = i / (float)SampleRate;
                float local = (i % syllable) / (float)syllable;
                float carrier = tones[n] * (1f + 0.04f * Mathf.Sin(2f * Mathf.PI * 17f * t));
                float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * carrier * t)) * 0.55f
                    + Mathf.Sin(2f * Mathf.PI * carrier * 1.5f * t) * 0.35f;
                float radio = ((float)rng.NextDouble() * 2f - 1f) * 0.08f;
                float gate = local < 0.72f ? Mathf.Sin(local / 0.72f * Mathf.PI) : 0f;
                buf[i] = (buzz + radio) * gate * Env(i, buf.Length, 0.01f) * vol;
            }
            return buf;
        }

        /// <summary>~32 second ambient loop: slow detuned pad over a four-chord progression.</summary>
        static float[] Music()
        {
            float chordDur = 8f;
            float[][] chords =
            {
                new[] { 110.00f, 130.81f, 164.81f, 220.00f }, // Am
                new[] { 87.31f, 130.81f, 174.61f, 220.00f },  // F
                new[] { 130.81f, 164.81f, 196.00f, 261.63f }, // C
                new[] { 98.00f, 123.47f, 146.83f, 196.00f },  // G
            };
            int chordSamples = (int)(chordDur * SampleRate);
            var buf = new float[chordSamples * chords.Length];
            int fade = (int)(1.5f * SampleRate);

            for (int c = 0; c < chords.Length; c++)
            {
                int start = c * chordSamples;
                foreach (float f in chords[c])
                {
                    double phase1 = 0, phase2 = 0;
                    double inc1 = 2.0 * Math.PI * f / SampleRate;
                    double inc2 = 2.0 * Math.PI * (f * 1.004) / SampleRate; // detune
                    for (int i = 0; i < chordSamples; i++)
                    {
                        float envIn = Mathf.Clamp01(i / (float)fade);
                        float envOut = Mathf.Clamp01((chordSamples - i) / (float)fade);
                        float env = Mathf.Min(envIn, envOut);
                        float lfo = 1f + 0.12f * Mathf.Sin(2f * Mathf.PI * 0.15f * (i / (float)SampleRate) + f);
                        buf[start + i] += (float)(Math.Sin(phase1) + 0.7 * Math.Sin(phase2)) * env * lfo * 0.045f;
                        phase1 += inc1; phase2 += inc2;
                    }
                }
                // sparse arp sparkle
                var rng = new System.Random(c * 31 + 5);
                for (int k = 0; k < 6; k++)
                {
                    int at = start + (int)((k * 1.3f + 0.4f) * SampleRate);
                    float f = chords[c][rng.Next(chords[c].Length)] * 4f;
                    int len = (int)(0.5f * SampleRate);
                    for (int i = 0; i < len && at + i < buf.Length; i++)
                    {
                        float t = i / (float)SampleRate;
                        buf[at + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Pow(1f - i / (float)len, 2f) * 0.035f;
                    }
                }
            }
            return buf;
        }

        // ---------- WAV encoding (used by editor setup to bake .wav assets) ----------

        public static byte[] ToWav(float[] samples)
        {
            int byteCount = samples.Length * 2;
            var bytes = new byte[44 + byteCount];
            void WriteStr(int at, string s) { for (int i = 0; i < s.Length; i++) bytes[at + i] = (byte)s[i]; }
            void WriteInt(int at, int v) { bytes[at] = (byte)v; bytes[at + 1] = (byte)(v >> 8); bytes[at + 2] = (byte)(v >> 16); bytes[at + 3] = (byte)(v >> 24); }
            void WriteShort(int at, short v) { bytes[at] = (byte)v; bytes[at + 1] = (byte)(v >> 8); }

            WriteStr(0, "RIFF"); WriteInt(4, 36 + byteCount); WriteStr(8, "WAVE");
            WriteStr(12, "fmt "); WriteInt(16, 16); WriteShort(20, 1); WriteShort(22, 1);
            WriteInt(24, SampleRate); WriteInt(28, SampleRate * 2); WriteShort(32, 2); WriteShort(34, 16);
            WriteStr(36, "data"); WriteInt(40, byteCount);

            for (int i = 0; i < samples.Length; i++)
            {
                short v = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[44 + i * 2] = (byte)v;
                bytes[44 + i * 2 + 1] = (byte)(v >> 8);
            }
            return bytes;
        }
    }
}
