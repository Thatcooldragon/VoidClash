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
            bool bubbleLab = mission == null && SkirmishConfig.Mode == SkirmishMode.BubbleLab;
            bool dotsLab = mission == null && SkirmishConfig.Mode == SkirmishMode.DotsLab;
            bool bubblePlayer = bubbleLab || mission?.playerRace == PlayerRace.Bubble;
            bool dotsPlayer = dotsLab || mission?.playerRace == PlayerRace.Dots;
            VisualFactory.EnemyBodyOverride = mission == null || mission.enemyRace == EnemyRace.Terran ? null
                : (mission.enemyRace == EnemyRace.Zerg ? "zerg_body" : "protoss_body");
            VisualFactory.EnemyAccentOverride = mission == null || mission.enemyRace == EnemyRace.Terran ? null
                : (mission.enemyRace == EnemyRace.Zerg ? "zerg_accent" : "protoss_accent");

            // banks first — buildings register supply on completion
            G.PlayerBank = new ResourceBank(Faction.Player, (bubblePlayer || dotsPlayer) ? 0 : (mission?.playerStartMinerals ?? 50));
            G.EnemyBank = new ResourceBank(Faction.Enemy, mission?.enemyStartMinerals ?? 50);

            // managers
            G.Game = gameObject.AddComponent<GameManager>();
            G.Audio = gameObject.AddComponent<AudioManager>();
            G.Effects = gameObject.AddComponent<EffectsManager>();
            G.Bubble = gameObject.AddComponent<BubbleSystem>();
            G.Dots = gameObject.AddComponent<DotsSystem>();
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
            if (bubblePlayer) ShowBubbleLabIntro();
            if (dotsPlayer) ShowDotsLabIntro();
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
            gameObject.AddComponent<StoryDirector>().Init(mission);
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
            if (Campaign.Current?.playerRace == PlayerRace.Bubble)
            {
                SpawnBubbleLabStart(Campaign.Current.playerStartMinerals);
                SpawnTerranStart(Faction.Enemy, MapBuilder.EnemyBasePos);
                return;
            }

            if (Campaign.Current == null && SkirmishConfig.Mode == SkirmishMode.BubbleLab)
            {
                SpawnBubbleLabStart();
                SpawnTerranStart(Faction.Enemy, MapBuilder.EnemyBasePos);
                return;
            }

            if (Campaign.Current == null && SkirmishConfig.Mode == SkirmishMode.DotsLab)
            {
                SpawnDotsLabStart();
                SpawnTerranStart(Faction.Enemy, MapBuilder.EnemyBasePos);
                return;
            }

            SpawnTerranStart(Faction.Player, MapBuilder.PlayerBasePos);
            SpawnTerranStart(Faction.Enemy, MapBuilder.EnemyBasePos);
        }

        void SpawnTerranStart(Faction faction, Vector3 basePos)
        {
            var ccData = G.DB.Building("cc");
            var workerData = G.DB.Unit("worker");

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

        void SpawnBubbleLabStart(int startMinerals = 75)
        {
            Vector3 basePos = MapBuilder.PlayerBasePos;

            // Bubble Nexus — the foam HQ (supply + passive minerals + build menu).
            BuildingFactory.Place(G.DB.Building("bubble_core"), Faction.Player, basePos, true);

            // A Bubble Spring seated beside the home mineral field so income + bubbles flow at once.
            var node = MineralNode.FindNearest(basePos, 60f);
            Vector3 springPos = node != null
                ? node.transform.position + (basePos - node.transform.position).normalized * 3.6f
                : basePos + new Vector3(-4.5f, 0f, -4.5f);
            BuildingFactory.Place(G.DB.Building("bubble_spring"), Faction.Player,
                BuildingPlacer.SnapToBuildGrid(springPos), true);

            // Seed a small amount of minerals to shape your first extra structures.
            // (Poison Pool, Aerator, Foam Turret are yours to build when you want them.)
            G.PlayerBank.AddMinerals(startMinerals);

            // A small starting cluster of bubbles gathering at the base, ready to command.
            Vector3 toCenter = (Vector3.zero - basePos).normalized;
            Vector3 gather = basePos + toCenter * 4f;
            var bubbleData = G.DB.Unit("bubble");
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = basePos + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 4f;
                var u = UnitFactory.Spawn(bubbleData, Faction.Player, pos);
                if (u != null) u.CommandMove(gather);
            }
        }

        void SpawnDotsLabStart(int startMinerals = 160)
        {
            Vector3 basePos = MapBuilder.PlayerBasePos;

            UnitFactory.Spawn(G.DB.Unit("dot_core"), Faction.Player, basePos);

            Vector3 toCenter = (Vector3.zero - basePos).normalized;
            Vector3 printerPos = BuildingPlacer.SnapToBuildGrid(basePos + toCenter * 4.2f + Vector3.right * 1.5f);
            BuildingFactory.Place(G.DB.Building("dot_printer"), Faction.Player, printerPos, true);

            G.PlayerBank.AddMinerals(startMinerals);

            var dotData = G.DB.Unit("dot");
            Vector3 gather = basePos + toCenter * 4f;
            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = basePos + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 3.4f;
                var u = UnitFactory.Spawn(dotData, Faction.Player, pos);
                if (u != null) u.CommandMove(gather);
            }
        }

        void ShowBubbleLabIntro()
        {
            foreach (var e in Entity.All)
            {
                if (e is Building b && b.Faction == Faction.Player && b.Data.id == "bubble_core")
                {
                    G.Selection.SelectSingle(b, false);
                    if (G.Cam != null) G.Cam.Focus(b.Position);
                    break;
                }
            }
            if (G.Hud != null)
                G.Hud.Notify("Bubble Nexus selected: it auto-makes bubbles every 7s. Build Aerators to speed it up.");
        }

        void ShowDotsLabIntro()
        {
            foreach (var e in Entity.All)
            {
                if (e is Unit u && u.Faction == Faction.Player && u.Data.id == "dot_core")
                {
                    G.Selection.SelectSingle(u, false);
                    if (G.Cam != null) G.Cam.Focus(u.Position);
                    break;
                }
            }
            if (G.Hud != null)
                G.Hud.Notify("Core Dot selected: it moves, builds Dot structures, and hides inside Giant shapes.");
        }
    }
}
