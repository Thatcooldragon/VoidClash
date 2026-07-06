using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VoidClash
{
    /// <summary>The only object saved in the MainMenu scene. Builds the menu UI + backdrop.</summary>
    public class MenuBootstrap : MonoBehaviour
    {
        RectTransform _mainPanel, _optionsPanel;
        Text _resolutionText;
        Text _fullscreenText;
        int _resIndex = -1;

        void Awake()
        {
            G.ResetAll();
            MaterialLibrary.Clear();
            G.EnsureDatabase();
            Time.timeScale = 1f;
            AudioListener.volume = PlayerPrefs.GetFloat(AudioManager.PrefMaster, 0.8f);

            BuildBackdrop();
            BuildUI();
        }

        void BuildBackdrop()
        {
            var camGo = new GameObject("MenuCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 3f, -12f);
            cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            camGo.AddComponent<AudioListener>();
            var extra = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            extra.renderPostProcessing = true;

            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(0.9f, 0.95f, 1f);
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.4f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.18f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.1f);

            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_AtmosphereThickness", 0.5f);
            sky.SetColor("_SkyTint", new Color(0.2f, 0.25f, 0.5f));
            sky.SetFloat("_Exposure", 0.9f);
            RenderSettings.skybox = sky;

            // Slowly spinning crystal cluster as a set piece.
            var stage = new GameObject("Stage");
            stage.AddComponent<SlowSpin>();
            var rng = new System.Random(5);
            for (int i = 0; i < 9; i++)
            {
                float ang = i * 40f;
                float dist = 2.5f + (float)rng.NextDouble() * 2f;
                var pos = Quaternion.Euler(0, ang, 0) * Vector3.forward * dist + Vector3.up * ((float)rng.NextDouble() * 2f - 0.5f);
                float h = 1f + (float)rng.NextDouble() * 2.2f;
                VisualFactory.Part(stage.transform, PrimitiveType.Cube, "crystal",
                    pos, new Vector3(0.5f, h, 0.5f),
                    new Vector3((float)rng.NextDouble() * 30f - 15f, ang, (float)rng.NextDouble() * 30f - 15f));
            }

            var volGo = new GameObject("PostFX");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>();
            bloom.intensity.Override(1.4f);
            bloom.threshold.Override(0.9f);
            var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>();
            tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
            var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>();
            vig.intensity.Override(0.35f);
            volume.profile = profile;
        }

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("MenuUI");

            var title = UIFactory.Label(canvas.transform, "Title", "VOIDCLASH", 110, TextAnchor.MiddleCenter, new Color(0.5f, 0.8f, 1f));
            UIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1200f, 130f));
            var subtitle = UIFactory.Label(canvas.transform, "Subtitle", "a real-time strategy skirmish", 26, TextAnchor.MiddleCenter, new Color(0.6f, 0.68f, 0.8f));
            UIFactory.SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(900f, 40f));

            _mainPanel = UIFactory.Invisible(canvas.transform, "MainPanel");
            UIFactory.SetRect(_mainPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(360f, 400f));

            void MainButton(string label, float y, System.Action act)
            {
                var b = UIFactory.TextButton(_mainPanel, label, label, 26, act);
                UIFactory.SetRect((RectTransform)b.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 64f));
            }
            MainButton("CAMPAIGN", 0f, () => ShowCampaign(true));
            MainButton("FREE PLAY", -80f, () => { Campaign.Current = null; SceneManager.LoadScene("Game"); });
            MainButton("OPTIONS", -160f, () => ShowOptions(true));
            MainButton("QUIT", -240f, GameManager.QuitApplication);

            BuildOptions(canvas);
            BuildCampaignPanel(canvas);

            var hint = UIFactory.Label(canvas.transform, "hint",
                "Left-click select  |  drag box  |  Right-click move/attack  |  A attack-move  |  Ctrl+1-9 control groups",
                17, TextAnchor.MiddleCenter, new Color(0.5f, 0.58f, 0.7f));
            UIFactory.SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1400f, 30f));
        }

        void BuildOptions(Canvas canvas)
        {
            _optionsPanel = UIFactory.Panel(canvas.transform, "OptionsPanel", UIFactory.PanelColor);
            UIFactory.SetRect(_optionsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(520f, 420f));

            var title = UIFactory.Label(_optionsPanel, "title", "OPTIONS", 30, TextAnchor.MiddleCenter);
            UIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(300f, 44f));

            var masterLabel = UIFactory.Label(_optionsPanel, "masterLabel", "Master Volume", 20);
            UIFactory.SetRect(masterLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -90f), new Vector2(220f, 30f));
            var master = UIFactory.CreateSlider(_optionsPanel, "masterSlider",
                PlayerPrefs.GetFloat(AudioManager.PrefMaster, 0.8f), AudioManager.SetMasterVolume);
            UIFactory.SetRect((RectTransform)master.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -90f), new Vector2(230f, 30f));

            var musicLabel = UIFactory.Label(_optionsPanel, "musicLabel", "Music Volume", 20);
            UIFactory.SetRect(musicLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -140f), new Vector2(220f, 30f));
            var music = UIFactory.CreateSlider(_optionsPanel, "musicSlider",
                PlayerPrefs.GetFloat(AudioManager.PrefMusic, 0.5f), v => PlayerPrefs.SetFloat(AudioManager.PrefMusic, v));
            UIFactory.SetRect((RectTransform)music.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -140f), new Vector2(230f, 30f));

            var resLabel = UIFactory.Label(_optionsPanel, "resLabel", "Resolution", 20);
            UIFactory.SetRect(resLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -195f), new Vector2(220f, 30f));
            var prev = UIFactory.TextButton(_optionsPanel, "resPrev", "<", 22, () => CycleResolution(-1));
            UIFactory.SetRect((RectTransform)prev.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -195f), new Vector2(36f, 34f));
            _resolutionText = UIFactory.Label(_optionsPanel, "resText", "", 18, TextAnchor.MiddleCenter);
            UIFactory.SetRect(_resolutionText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, -195f), new Vector2(170f, 30f));
            var next = UIFactory.TextButton(_optionsPanel, "resNext", ">", 22, () => CycleResolution(1));
            UIFactory.SetRect((RectTransform)next.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(444f, -195f), new Vector2(36f, 34f));

            _fullscreenText = UIFactory.TextButton(_optionsPanel, "fullscreen", "", 20, ToggleFullscreen)
                .GetComponentInChildren<Text>();
            UIFactory.SetRect((RectTransform)_fullscreenText.transform.parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(300f, 44f));

            var back = UIFactory.TextButton(_optionsPanel, "back", "Back", 22, () => ShowOptions(false));
            UIFactory.SetRect((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(220f, 50f));

            RefreshOptionLabels();
            _optionsPanel.gameObject.SetActive(false);
        }

        void ShowOptions(bool show)
        {
            _optionsPanel.gameObject.SetActive(show);
            _mainPanel.gameObject.SetActive(!show);
        }

        RectTransform _campaignPanel;

        void BuildCampaignPanel(Canvas canvas)
        {
            _campaignPanel = UIFactory.Panel(canvas.transform, "CampaignPanel", UIFactory.PanelColor);
            float panelHeight = Mathf.Clamp(200f + Campaign.Missions.Length * 74f, 470f, 650f);
            UIFactory.SetRect(_campaignPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(680f, panelHeight));

            var title = UIFactory.Label(_campaignPanel, "title", "CAMPAIGN - Terran Front", 30, TextAnchor.MiddleCenter);
            UIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(600f, 44f));

            int unlocked = Campaign.UnlockedCount;
            int cleared = Mathf.Clamp(unlocked - 1, 0, Campaign.Missions.Length);
            var progress = UIFactory.Label(_campaignPanel, "progress", $"Progress: {cleared}/{Campaign.Missions.Length} missions cleared", 16, TextAnchor.MiddleCenter, new Color(0.62f, 0.72f, 0.86f));
            UIFactory.SetRect(progress.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(540f, 24f));
            for (int i = 0; i < Campaign.Missions.Length; i++)
            {
                var m = Campaign.Missions[i];
                bool isUnlocked = i < unlocked;
                var captured = m;
                string blurb = string.IsNullOrEmpty(m.menuBlurb) ? "Deploy and complete the objective." : m.menuBlurb;
                var btn = UIFactory.TextButton(_campaignPanel, $"m{i}",
                    isUnlocked ? $"{m.title}\n{blurb}" : $"{m.title}\n[ LOCKED - clear the previous mission ]",
                    18,
                    () => { if (isUnlocked) { Campaign.Current = captured; SceneManager.LoadScene("Game"); } },
                    isUnlocked ? UIFactory.PanelLight : new Color(0.07f, 0.09f, 0.12f, 0.95f));
                btn.interactable = isUnlocked;
                UIFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -104f - i * 74f), new Vector2(620f, 62f));
            }

            var back = UIFactory.TextButton(_campaignPanel, "back", "Back", 22, () => ShowCampaign(false));
            UIFactory.SetRect((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(220f, 50f));
            _campaignPanel.gameObject.SetActive(false);
        }

        void ShowCampaign(bool show)
        {
            _campaignPanel.gameObject.SetActive(show);
            _mainPanel.gameObject.SetActive(!show);
        }

        void CycleResolution(int dir)
        {
            var all = Screen.resolutions;
            if (all == null || all.Length == 0) return;
            if (_resIndex < 0)
            {
                _resIndex = all.Length - 1;
                for (int i = 0; i < all.Length; i++)
                    if (all[i].width == Screen.width && all[i].height == Screen.height) { _resIndex = i; break; }
            }
            _resIndex = (_resIndex + dir + all.Length) % all.Length;
            var r = all[_resIndex];
            Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
            RefreshOptionLabels();
        }

        void ToggleFullscreen()
        {
            Screen.fullScreen = !Screen.fullScreen;
            RefreshOptionLabels();
        }

        void RefreshOptionLabels()
        {
            if (_resolutionText != null) _resolutionText.text = $"{Screen.width} x {Screen.height}";
            if (_fullscreenText != null) _fullscreenText.text = Screen.fullScreen ? "Fullscreen: ON" : "Fullscreen: OFF";
        }
    }

    public class SlowSpin : MonoBehaviour
    {
        void Update() => transform.Rotate(0f, 8f * Time.deltaTime, 0f);
    }
}
