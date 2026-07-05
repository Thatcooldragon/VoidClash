using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Plays synthesized SFX (pooled sources) + ambient music. Volumes persist in PlayerPrefs.</summary>
    public class AudioManager : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        AudioSource _music;
        float _sfxVolume = 1f;
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();

        public const string PrefMaster = "vc_master_volume";
        public const string PrefMusic = "vc_music_volume";

        public void Init()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(PrefMaster, 0.8f);
            for (int i = 0; i < 10; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                _pool.Add(src);
            }
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.volume = PlayerPrefs.GetFloat(PrefMusic, 0.5f);
            _music.clip = GetClip("music");
            if (!Application.isBatchMode) _music.Play();
        }

        public AudioClip GetClip(string name)
        {
            if (_clips.TryGetValue(name, out var clip) && clip != null) return clip;
            clip = G.DB != null ? G.DB.FindClip(name) : null;
            if (clip == null) clip = SynthLib.CreateClip(name);
            _clips[name] = clip;
            return clip;
        }

        /// <summary>2D UI/feedback sound with throttling so spam doesn't stack.</summary>
        public void Play(string name, float volume = 1f)
        {
            if (_lastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < 0.07f) return;
            _lastPlayed[name] = Time.unscaledTime;
            var src = NextSource();
            if (src == null) return;
            src.spatialBlend = 0f;
            src.pitch = Random.Range(0.96f, 1.05f);
            src.PlayOneShot(GetClip(name), volume * _sfxVolume);
        }

        /// <summary>Positional world sound.</summary>
        public void PlayAt(string name, Vector3 pos, float volume = 0.9f)
        {
            if (_lastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < 0.05f) return;
            _lastPlayed[name] = Time.unscaledTime;
            var src = NextSource();
            if (src == null) return;
            // cheap positional: attenuate by distance to camera focus
            float dist = G.Cam != null ? Vector3.Distance(G.Cam.FocusPoint, pos) : 0f;
            float att = Mathf.Clamp01(1f - dist / 55f);
            if (att <= 0.02f) return;
            src.spatialBlend = 0f;
            src.pitch = Random.Range(0.93f, 1.08f);
            src.PlayOneShot(GetClip(name), volume * att * _sfxVolume);
        }

        AudioSource NextSource()
        {
            foreach (var s in _pool) if (!s.isPlaying) return s;
            return _pool.Count > 0 ? _pool[0] : null;
        }

        public static void SetMasterVolume(float v)
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat(PrefMaster, v);
        }

        public void SetMusicVolume(float v)
        {
            if (_music != null) _music.volume = v;
            PlayerPrefs.SetFloat(PrefMusic, v);
        }
    }
}
