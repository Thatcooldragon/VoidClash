using System.Collections.Generic;
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
            cam.transform.position = new Vector3(0f, 4.2f, -14f);
            cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            cam.fieldOfView = 48f;
            camGo.AddComponent<AudioListener>();
            var extra = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            extra.renderPostProcessing = true;

            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.35f;
            sun.color = new Color(0.72f, 0.92f, 1f);
            lightGo.transform.rotation = Quaternion.Euler(45f, -18f, 0f);

            var rimGo = new GameObject("RimLight");
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.intensity = 0.75f;
            rim.color = new Color(0.3f, 0.75f, 1f);
            rimGo.transform.rotation = Quaternion.Euler(20f, 155f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.4f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.15f, 0.18f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.1f);

            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_AtmosphereThickness", 0.5f);
            sky.SetColor("_SkyTint", new Color(0.2f, 0.25f, 0.5f));
            sky.SetFloat("_Exposure", 0.9f);
            RenderSettings.skybox = sky;

            BuildSpaceBackdrop();
            BuildMenuStage();

            var volGo = new GameObject("PostFX");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>();
            bloom.intensity.Override(1.7f);
            bloom.threshold.Override(0.85f);
            var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>();
            tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
            var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>();
            vig.intensity.Override(0.42f);
            volume.profile = profile;
        }

        void BuildSpaceBackdrop()
        {
            var planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "BackgroundPlanet";
            planet.transform.position = new Vector3(-6.5f, 3.0f, 12f);
            planet.transform.localScale = Vector3.one * 8.5f;
            planet.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("cliff");

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "PlanetGlow";
            DestroyImmediate(glow.GetComponent<Collider>());
            glow.transform.position = new Vector3(2.5f, 5.5f, 10.5f);
            glow.transform.localScale = Vector3.one * 1.1f;
            glow.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("crystal");

            var stars = new GameObject("Starfield").transform;
            var rng = new System.Random(77);
            for (int i = 0; i < 95; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "Star";
                DestroyImmediate(star.GetComponent<Collider>());
                star.transform.SetParent(stars, false);
                star.transform.position = new Vector3(
                    -12f + (float)rng.NextDouble() * 24f,
                    0f + (float)rng.NextDouble() * 9f,
                    4f + (float)rng.NextDouble() * 12f);
                float s = 0.025f + (float)rng.NextDouble() * 0.055f;
                star.transform.localScale = Vector3.one * s;
                star.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("metal_light");
            }
        }

        void BuildMenuStage()
        {
            var stage = new GameObject("CampaignStage").transform;
            BuildPlatform(stage, new Vector3(-4.8f, 0f, 0f), "player_accent");
            BuildPlatform(stage, new Vector3(0f, 0f, 0.35f), "crystal");
            BuildPlatform(stage, new Vector3(4.8f, 0f, 0f), "enemy_accent");

            var terran = new GameObject("TerranChampion").transform;
            terran.SetParent(stage, false);
            terran.localPosition = new Vector3(-4.8f, 0.25f, -0.45f);
            terran.localRotation = Quaternion.Euler(0f, 20f, 0f);
            VisualFactory.BuildUnitVisual(terran, "soldier", Faction.Player, 1.85f);

            var bubble = new GameObject("BubbleChampion").transform;
            bubble.SetParent(stage, false);
            bubble.localPosition = new Vector3(0f, 0.35f, -0.15f);
            VisualFactory.BuildUnitVisual(bubble, "poison_bubble", Faction.Player, 2.35f);
            bubble.gameObject.AddComponent<SlowFloat>();

            var core = new GameObject("DotsChampion").transform;
            core.SetParent(stage, false);
            core.localPosition = new Vector3(4.8f, 0.1f, -0.35f);
            core.localRotation = Quaternion.Euler(0f, -20f, 0f);
            VisualFactory.BuildUnitVisual(core, "dot_core", Faction.Player, 1.35f);
            for (int i = 0; i < 6; i++)
            {
                var dot = new GameObject($"MenuDot{i}").transform;
                dot.SetParent(stage, false);
                dot.localPosition = new Vector3(4.8f, 0.55f, -0.35f) +
                    Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 1.25f;
                VisualFactory.BuildUnitVisual(dot, "dot", Faction.Player, 0.75f);
                dot.gameObject.AddComponent<SlowFloat>();
            }
        }

        void BuildPlatform(Transform parent, Vector3 pos, string accent)
        {
            var root = new GameObject("EpisodePlatform").transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            VisualFactory.Part(root, PrimitiveType.Cylinder, "metal_dark", Vector3.zero, new Vector3(2.0f, 0.22f, 2.0f), null, "Base");
            VisualFactory.Part(root, PrimitiveType.Cylinder, "metal_light", new Vector3(0f, 0.18f, 0f), new Vector3(1.7f, 0.08f, 1.7f), null, "Ring");
            VisualFactory.Part(root, PrimitiveType.Cylinder, accent, new Vector3(0f, 0.27f, 0f), new Vector3(1.15f, 0.035f, 1.15f), null, "Glow");
        }

        void BuildUI()
        {
            var canvas = UIFactory.CreateCanvas("MenuUI");

            BuildTopNav(canvas);

            var brand = UIFactory.Label(canvas.transform, "Brand", "VOIDCLASH", 36, TextAnchor.MiddleLeft, new Color(0.55f, 0.86f, 1f));
            UIFactory.SetRect(brand.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -78f), new Vector2(360f, 44f));

            _mainPanel = UIFactory.Invisible(canvas.transform, "MainPanel");
            UIFactory.SetRect(_mainPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(1500f, 720f));

            BuildEpisodeCard(_mainPanel, -500f, "EPISODE I", "TERRAN FRONT", CampaignSummary(PlayerRace.Terran), "PLAY TERRAN",
                new Color(0.25f, 0.62f, 1f), () => ShowCampaign(true, PlayerRace.Terran), true);
            BuildEpisodeCard(_mainPanel, 0f, "EPISODE II", "BUBBLE TIDE", CampaignSummary(PlayerRace.Bubble), "PLAY BUBBLE",
                new Color(0.35f, 1f, 0.85f), () => ShowCampaign(true, PlayerRace.Bubble), true);
            BuildEpisodeCard(_mainPanel, 500f, "EPISODE III", "DOTS AWAKENING", CampaignSummary(PlayerRace.Dots), "PLAY DOTS",
                new Color(1f, 0.58f, 0.28f), () => ShowCampaign(true, PlayerRace.Dots), true);

            BuildOptions(canvas);
            BuildCampaignPanel(canvas);
            BuildSkirmishPanel(canvas);

            var status = UIFactory.Panel(canvas.transform, "BottomStatus", new Color(0.02f, 0.04f, 0.08f, 0.82f));
            UIFactory.SetRect(status, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(980f, 54f));
            var hint = UIFactory.Label(status, "hint",
                "v0.14.0 pre-alpha  |  FREE PLAY: choose your race, AI race, and difficulty.",
                18, TextAnchor.MiddleCenter, new Color(0.6f, 0.75f, 0.9f));
            UIFactory.Stretch(hint.rectTransform, 8f);
        }

        void BuildTopNav(Canvas canvas)
        {
            var top = UIFactory.Panel(canvas.transform, "TopNav", new Color(0.02f, 0.05f, 0.09f, 0.88f));
            UIFactory.SetRect(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920f, 64f));

            var sigil = UIFactory.Label(top, "Sigil", "VC", 24, TextAnchor.MiddleCenter, new Color(0.45f, 0.9f, 1f));
            UIFactory.SetRect(sigil.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(72f, 48f));

            TopNavButton(top, "CAMPAIGN", 120f, 200f, true, () => ShowCampaign(true, PlayerRace.Terran));
            TopNavButton(top, "FREE PLAY", 330f, 200f, false, () => ShowSkirmish(true));
            TopNavButton(top, "OPTIONS", 540f, 180f, false, () => ShowOptions(true));
            TopNavButton(top, "QUIT", 730f, 150f, false, GameManager.QuitApplication);
        }

        void TopNavButton(RectTransform parent, string label, float x, float w, bool active, System.Action action)
        {
            var color = active ? new Color(0.08f, 0.26f, 0.52f, 0.95f) : new Color(0.04f, 0.08f, 0.13f, 0.92f);
            var btn = UIFactory.TextButton(parent, label, label, 20, action, color);
            UIFactory.SetRect((RectTransform)btn.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(w, 54f));
        }

        void BuildEpisodeCard(RectTransform parent, float x, string episode, string title, string progress, string buttonText, Color accent, System.Action action, bool enabled)
        {
            var card = UIFactory.Panel(parent, title, new Color(0.02f, 0.05f, 0.09f, 0.36f));
            UIFactory.SetRect(card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -36f), new Vector2(430f, 185f));

            var ep = UIFactory.Label(card, "episode", episode, 18, TextAnchor.MiddleCenter, new Color(0.62f, 0.8f, 1f));
            UIFactory.SetRect(ep.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(350f, 30f));
            var ttl = UIFactory.Label(card, "title", title, 25, TextAnchor.MiddleCenter, Color.white);
            UIFactory.SetRect(ttl.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(390f, 34f));
            var pr = UIFactory.Label(card, "progress", progress, 16, TextAnchor.MiddleCenter, accent);
            UIFactory.SetRect(pr.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(390f, 26f));

            var line = UIFactory.Panel(card, "line", accent);
            UIFactory.SetRect(line, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -123f), new Vector2(300f, 2f));

            var btn = UIFactory.TextButton(card, "button", buttonText, 18, action, enabled ? new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.32f, 0.95f) : new Color(0.08f, 0.09f, 0.11f, 0.92f));
            btn.interactable = enabled;
            UIFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(245f, 42f));
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
            if (_campaignPanel != null) _campaignPanel.gameObject.SetActive(false);
            if (_skirmishPanel != null) _skirmishPanel.gameObject.SetActive(false);
        }

        RectTransform _campaignPanel;
        PlayerRace _campaignRace = PlayerRace.Terran;

        void BuildCampaignPanel(Canvas canvas)
        {
            _campaignPanel = UIFactory.Panel(canvas.transform, "CampaignPanel", UIFactory.PanelColor);
            UIFactory.SetRect(_campaignPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(680f, 620f));
            _campaignPanel.gameObject.SetActive(false);
        }

        void PopulateCampaignPanel(PlayerRace race)
        {
            if (_campaignPanel == null) return;
            foreach (Transform c in _campaignPanel) Destroy(c.gameObject);

            var missions = MissionsForRace(race);
            float rowStep = missions.Count > 6 ? 52f : 74f;
            float rowHeight = missions.Count > 6 ? 46f : 62f;
            float panelHeight = Mathf.Clamp(220f + missions.Count * rowStep, 470f, 700f);
            UIFactory.SetRect(_campaignPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(680f, panelHeight));

            var title = UIFactory.Label(_campaignPanel, "title", $"CAMPAIGN - {RaceTitle(race)}", 30, TextAnchor.MiddleCenter);
            UIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(600f, 44f));

            int cleared = ClearedForRace(race);
            var progress = UIFactory.Label(_campaignPanel, "progress", $"{RaceTitle(race)}: {missions.Count} missions  |  Progress: {cleared}/{missions.Count} cleared",
                16, TextAnchor.MiddleCenter, new Color(0.62f, 0.72f, 0.86f));
            UIFactory.SetRect(progress.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(540f, 24f));

            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                bool isUnlocked = Campaign.IsUnlocked(m);
                var captured = m;
                string blurb = string.IsNullOrEmpty(m.menuBlurb) ? "Deploy and complete the objective." : m.menuBlurb;
                var btn = UIFactory.TextButton(_campaignPanel, $"m{i}",
                    isUnlocked ? $"{m.title}\n{blurb}" : $"{m.title}\n[ LOCKED - clear the previous {RaceTitle(race)} mission ]",
                    18,
                    () =>
                    {
                        if (isUnlocked)
                        {
                            Campaign.Current = captured;
                            SkirmishConfig.SetSkirmish(captured.playerRace, PlayerRace.Terran, Difficulty.Normal);
                            SceneManager.LoadScene("Game");
                        }
                    },
                    isUnlocked ? UIFactory.PanelLight : new Color(0.07f, 0.09f, 0.12f, 0.95f));
                btn.interactable = isUnlocked;
                UIFactory.SetRect((RectTransform)btn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -104f - i * rowStep), new Vector2(620f, rowHeight));
            }

            var back = UIFactory.TextButton(_campaignPanel, "back", "Back", 22, () => ShowCampaign(false));
            UIFactory.SetRect((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(220f, 50f));
        }

        void ShowCampaign(bool show, PlayerRace race)
        {
            _campaignRace = race;
            if (show) PopulateCampaignPanel(_campaignRace);
            ShowCampaign(show);
        }

        void ShowCampaign(bool show)
        {
            _campaignPanel.gameObject.SetActive(show);
            _mainPanel.gameObject.SetActive(!show);
            if (_optionsPanel != null) _optionsPanel.gameObject.SetActive(false);
            if (_skirmishPanel != null) _skirmishPanel.gameObject.SetActive(false);
        }

        // ---------- Free Play (any race vs any race + difficulty) ----------

        RectTransform _skirmishPanel;
        PlayerRace _skPlayer = PlayerRace.Terran;
        int _skEnemy = 3;                 // 0 Terran, 1 Bubble, 2 Dots, 3 Random
        Difficulty _skDifficulty = Difficulty.Normal;

        void BuildSkirmishPanel(Canvas canvas)
        {
            _skirmishPanel = UIFactory.Panel(canvas.transform, "SkirmishPanel", UIFactory.PanelColor);
            UIFactory.SetRect(_skirmishPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(760f, 520f));
            _skirmishPanel.gameObject.SetActive(false);
            RebuildSkirmishPanel();
        }

        void RebuildSkirmishPanel()
        {
            if (_skirmishPanel == null) return;
            foreach (Transform c in _skirmishPanel) Destroy(c.gameObject);

            var title = UIFactory.Label(_skirmishPanel, "title", "FREE PLAY SETUP", 32, TextAnchor.MiddleCenter);
            UIFactory.SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(500f, 44f));

            ChoiceRow("Your race", -110f, new[] { "Terran", "Bubble", "Dots" }, (int)_skPlayer,
                i => { _skPlayer = (PlayerRace)i; RebuildSkirmishPanel(); });
            ChoiceRow("Enemy race", -185f, new[] { "Terran", "Bubble", "Dots", "Random" }, _skEnemy,
                i => { _skEnemy = i; RebuildSkirmishPanel(); });
            ChoiceRow("Difficulty", -260f, new[] { "Easy", "Normal", "Hard" }, (int)_skDifficulty,
                i => { _skDifficulty = (Difficulty)i; RebuildSkirmishPanel(); });

            var start = UIFactory.TextButton(_skirmishPanel, "start", "START FREE PLAY", 24, StartSkirmish,
                new Color(0.16f, 0.5f, 0.28f, 0.95f));
            UIFactory.SetRect((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(320f, 58f));

            var back = UIFactory.TextButton(_skirmishPanel, "back", "Back", 20, () => ShowSkirmish(false));
            UIFactory.SetRect((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(200f, 48f));
        }

        void ChoiceRow(string label, float y, string[] options, int selected, System.Action<int> onPick)
        {
            var lab = UIFactory.Label(_skirmishPanel, label + "_lbl", label, 21, TextAnchor.MiddleLeft, new Color(0.8f, 0.9f, 1f));
            UIFactory.SetRect(lab.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, y), new Vector2(180f, 34f));

            float bx = 230f;
            for (int i = 0; i < options.Length; i++)
            {
                bool sel = i == selected;
                var color = sel ? new Color(0.2f, 0.55f, 0.95f, 0.98f) : new Color(0.06f, 0.1f, 0.16f, 0.95f);
                int captured = i;
                var btn = UIFactory.TextButton(_skirmishPanel, label + captured, options[i], 17, () => onPick(captured), color);
                UIFactory.SetRect((RectTransform)btn.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(bx, y), new Vector2(118f, 34f));
                bx += 124f;
            }
        }

        void StartSkirmish()
        {
            PlayerRace enemy = _skEnemy >= 3 ? (PlayerRace)Random.Range(0, 3) : (PlayerRace)_skEnemy;
            Campaign.Current = null;
            SkirmishConfig.SetSkirmish(_skPlayer, enemy, _skDifficulty);
            SceneManager.LoadScene("Game");
        }

        void ShowSkirmish(bool show)
        {
            if (show) RebuildSkirmishPanel();
            _skirmishPanel.gameObject.SetActive(show);
            _mainPanel.gameObject.SetActive(!show);
            if (_optionsPanel != null) _optionsPanel.gameObject.SetActive(false);
            if (_campaignPanel != null) _campaignPanel.gameObject.SetActive(false);
        }

        static List<MissionDef> MissionsForRace(PlayerRace race)
        {
            var list = new List<MissionDef>();
            for (int i = 0; i < Campaign.Missions.Length; i++)
                if (Campaign.Missions[i].playerRace == race) list.Add(Campaign.Missions[i]);
            return list;
        }

        static int ClearedForRace(PlayerRace race)
        {
            int cleared = 0;
            for (int i = 0; i < Campaign.Missions.Length; i++)
                if (Campaign.Missions[i].playerRace == race && Campaign.MissionRank(Campaign.Missions[i]) < Campaign.UnlockedForRace(race) - 1)
                    cleared++;
            return cleared;
        }

        static string CampaignSummary(PlayerRace race)
        {
            var missions = MissionsForRace(race);
            return $"{RaceTitle(race)} campaign: {missions.Count} missions";
        }

        static string RaceTitle(PlayerRace race)
        {
            if (race == PlayerRace.Bubble) return "Bubble Tide";
            if (race == PlayerRace.Dots) return "Dots Awakening";
            return "Terran Front";
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

    public class SlowFloat : MonoBehaviour
    {
        Vector3 _base;
        void Start() => _base = transform.localPosition;
        void Update()
        {
            transform.localPosition = _base + Vector3.up * (Mathf.Sin(Time.time * 1.4f) * 0.12f);
            transform.Rotate(0f, 14f * Time.deltaTime, 0f);
        }
    }
}
