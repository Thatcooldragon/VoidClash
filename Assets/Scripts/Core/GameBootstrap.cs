using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VoidClash
{
    /// <summary>The only object saved in the Game scene. Constructs the entire match at runtime:
    /// environment, map, navmesh, factions, systems, UI, AI.</summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            G.ResetAll();
            MaterialLibrary.Clear();
            G.EnsureDatabase();
            Time.timeScale = 1f;

            var mission = Campaign.Current; // null = free play
            VisualFactory.EnemyBodyOverride = mission == null || mission.enemyRace == EnemyRace.Terran ? null
                : (mission.enemyRace == EnemyRace.Zerg ? "zerg_body" : "protoss_body");
            VisualFactory.EnemyAccentOverride = mission == null || mission.enemyRace == EnemyRace.Terran ? null
                : (mission.enemyRace == EnemyRace.Zerg ? "zerg_accent" : "protoss_accent");

            // banks first — buildings register supply on completion
            G.PlayerBank = new ResourceBank(Faction.Player, mission?.playerStartMinerals ?? 50);
            G.EnemyBank = new ResourceBank(Faction.Enemy, mission?.enemyStartMinerals ?? 50);

            // managers
            G.Game = gameObject.AddComponent<GameManager>();
            G.Audio = gameObject.AddComponent<AudioManager>();
            G.Effects = gameObject.AddComponent<EffectsManager>();
            G.Selection = gameObject.AddComponent<SelectionManager>();
            G.Input = gameObject.AddComponent<InputController>();
            G.Placer = gameObject.AddComponent<BuildingPlacer>();
            G.Map = gameObject.AddComponent<MapBuilder>();
            G.Fog = gameObject.AddComponent<FogOfWar>();
            G.Minimap = gameObject.AddComponent<Minimap>();
            G.Cam = gameObject.AddComponent<CameraController>();
            G.AI = gameObject.AddComponent<EnemyAI>();
            G.Hud = gameObject.AddComponent<HUD>();

            G.Audio.Init();
            SetupEnvironment();
            G.Map.Build();               // includes runtime NavMesh bake
            G.Cam.Init(MapBuilder.PlayerBasePos + new Vector3(2f, 0f, 2f));
            SpawnStartingBases();
            G.Fog.Init();
            G.Minimap.Init();
            G.Hud.Build();
            G.AI.Init(MapBuilder.EnemyBasePos);
            G.Game.StartMatch();

            if (mission != null) ApplyMissionExtras(mission);
        }

        void ApplyMissionExtras(MissionDef mission)
        {
            if (!string.IsNullOrEmpty(mission.bossUnitId))
            {
                var bossData = G.DB.Unit(mission.bossUnitId);
                var boss = UnitFactory.Spawn(bossData, Faction.Enemy,
                    MapBuilder.EnemyBasePos + new Vector3(-4f, 0f, -4f));
                if (boss != null)
                {
                    G.Game.RegisterBoss(boss);
                    G.AI.RegisterBoss(boss);
                }
            }
            G.Hud.ShowBriefing(mission.title, mission.briefing);
        }

        void SetupEnvironment()
        {
            // key light
            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

            // cool fill light, no shadows
            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.35f, 0.5f, 0.85f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(40f, 140f, 0f);

            // ambient + gradient sky + ground fog
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.55f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.28f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.18f);

            var sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetFloat("_SunSize", 0.03f);
            sky.SetFloat("_AtmosphereThickness", 0.7f);
            sky.SetColor("_SkyTint", new Color(0.25f, 0.35f, 0.6f));
            sky.SetColor("_GroundColor", new Color(0.08f, 0.09f, 0.13f));
            sky.SetFloat("_Exposure", 1.1f);
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.12f, 0.16f, 0.24f);
            RenderSettings.fogDensity = 0.0065f;

            SetupPostProcessing();
        }

        void SetupPostProcessing()
        {
            var volGo = new GameObject("PostFX");
            var volume = volGo.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>();
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(1.0f);
            bloom.scatter.Override(0.65f);

            var tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.42f);

            var color = profile.Add<ColorAdjustments>();
            color.saturation.Override(12f);
            color.contrast.Override(8f);
            color.postExposure.Override(0.15f);

            var lift = profile.Add<LiftGammaGain>();
            lift.lift.Override(new Vector4(0.98f, 0.99f, 1.05f, 0f));
            lift.gain.Override(new Vector4(1.0f, 1.0f, 1.06f, 0f));

            volume.profile = profile;
        }

        void SpawnStartingBases()
        {
            var ccData = G.DB.Building("cc");
            var workerData = G.DB.Unit("worker");

            foreach (var faction in new[] { Faction.Player, Faction.Enemy })
            {
                Vector3 basePos = faction == Faction.Player ? MapBuilder.PlayerBasePos : MapBuilder.EnemyBasePos;
                BuildingFactory.Place(ccData, faction, basePos, true);

                Vector3 toCenter = (Vector3.zero - basePos).normalized;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = Quaternion.Euler(0f, -30f + i * 20f, 0f) * toCenter * 5.5f;
                    var w = (WorkerUnit)UnitFactory.Spawn(workerData, faction, basePos + offset);
                    G.Bank(faction).TrySpend(0, workerData.supplyCost); // starting units occupy supply
                    var node = MineralNode.FindNearest(w.Position, 30f);
                    if (node != null) w.CommandHarvest(node);
                }
            }
        }
    }
}
