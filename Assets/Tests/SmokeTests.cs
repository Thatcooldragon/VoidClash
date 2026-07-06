using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoidClash;

namespace VoidClash.Tests
{
    /// <summary>Console-error guard shared by the smoke tests.</summary>
    static class LogGuard
    {
        static readonly List<string> Errors = new List<string>();
        static bool _hooked;

        public static void Begin()
        {
            Errors.Clear();
            if (!_hooked)
            {
                Application.logMessageReceived += OnLog;
                _hooked = true;
            }
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add($"[{type}] {condition}");
        }

        public static void AssertClean()
        {
            if (Errors.Count > 0)
                Assert.Fail($"Console errors during playthrough ({Errors.Count}):\n" +
                            string.Join("\n", Errors.GetRange(0, Mathf.Min(Errors.Count, 12))));
        }
    }

    public class SmokeTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            LogGuard.Begin();
            Campaign.Current = null; // default: free play
            SkirmishConfig.Mode = SkirmishMode.Terran;
            SceneManager.LoadScene("Game");
            yield return null; // Awake/Start
            yield return null;
            Assert.IsNotNull(G.Game, "GameBootstrap should have created the GameManager");

            var ground = GameObject.Find("Ground");
            Assert.IsNotNull(ground, "runtime map should create a Ground object");
            Assert.IsNotNull(ground.GetComponent<MeshFilter>()?.sharedMesh, "Ground should have a visible mesh");
            Assert.IsNotNull(ground.GetComponent<MeshRenderer>()?.sharedMaterial, "Ground should render its material");

            var fog = GameObject.Find("FogOverlay");
            Assert.IsNotNull(fog, "fog of war overlay should exist");
            Assert.Less(fog.transform.position.y, 0.5f, "fog overlay should sit on the battlefield, not above the camera view");

        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Campaign.Current = null;
            SkirmishConfig.Mode = SkirmishMode.Terran;
            Time.timeScale = 1f;
            yield return null;
        }

        static IEnumerator WaitGameSeconds(float seconds)
        {
            float end = G.Game.MatchTime + seconds;
            while (G.Game != null && G.Game.MatchTime < end && !G.Game.IsOver)
                yield return null;
        }

        static IEnumerator WaitUntil(System.Func<bool> cond, float timeoutGameSeconds, string what)
        {
            float end = G.Game.MatchTime + timeoutGameSeconds;
            while (!cond())
            {
                if (G.Game == null || G.Game.MatchTime >= end)
                    Assert.Fail($"Timed out waiting for: {what}");
                yield return null;
            }
        }

        static int Count<T>(Faction f, System.Func<T, bool> pred = null) where T : Entity
        {
            int n = 0;
            foreach (var e in Entity.All)
                if (e is T t && e.Faction == f && !e.IsDead && (pred == null || pred(t))) n++;
            return n;
        }

        // ------------------------------------------------------------------
        // Criterion 1/2/3/5-adjacent: economy, all buildings, all units, fog.
        // ------------------------------------------------------------------
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator PlayerSystems_EconomyBuildingsUnitsFog()
        {
            Time.timeScale = 8f;
            G.AI.enabled = false; // keep the opponent passive for this systems test

            // --- economy: the 4 starting workers auto-harvest ---
            int start = G.PlayerBank.Minerals;
            yield return WaitUntil(() => G.PlayerBank.Minerals > start, 60f, "workers to deposit minerals");

            // --- supply starts at 10 (Command Center) ---
            Assert.AreEqual(10, G.PlayerBank.SupplyCap, "starting supply cap");
            Assert.AreEqual(4, G.PlayerBank.SupplyUsed, "4 starting workers");

            // --- build all player building types (worker-constructed, like the real flow) ---
            G.PlayerBank.AddMinerals(5000);
            foreach (var id in new[] { "depot", "barracks", "factory", "turret", "sensor", "cc" })
            {
                var data = G.DB.Building(id);
                Vector3? spot = FindSpot(data);
                Assert.IsTrue(spot.HasValue, $"valid placement spot for {id}");
                Assert.IsTrue(G.PlayerBank.TrySpend(data.mineralCost), $"afford {id}");
                var site = G.Placer.PlaceAt(data, Faction.Player, spot.Value);
                var worker = FindWorker();
                Assert.IsNotNull(worker, "a worker to build with");
                worker.CommandBuild(site);
                yield return WaitUntil(() => site == null || site.IsComplete, data.buildTime * 4f + 90f, $"{id} construction");
            }
            Assert.AreEqual(18 + 10, G.PlayerBank.SupplyCap, "supply cap 10 (CC) + 8 (depot) + 10 (2nd CC)");
            Assert.Greater(G.DB.Building("sensor").visionRadius, G.DB.Building("turret").visionRadius,
                "Sensor Tower should provide stronger vision than a Turret");

            // --- train all 4 unit types ---
            IEnumerator Train(string buildingId, string unitId)
            {
                Building trainer = null;
                foreach (var e in Entity.All)
                    if (e is Building b && b.Faction == Faction.Player && b.IsComplete && b.Data.id == buildingId)
                    { trainer = b; break; }
                Assert.IsNotNull(trainer, $"completed {buildingId}");
                var ud = G.DB.Unit(unitId);
                int before = Count<Unit>(Faction.Player, u => u.Data.id == unitId);
                Assert.IsTrue(trainer.TryQueue(ud), $"queue {unitId}");
                yield return WaitUntil(() => Count<Unit>(Faction.Player, u => u.Data.id == unitId) > before,
                    ud.trainTime * 4f + 60f, $"train {unitId}");
            }
            yield return Train("cc", "worker");
            yield return Train("barracks", "soldier");
            yield return Train("barracks", "ranged");
            yield return Train("factory", "heavy");

            // --- fog of war: enemy base hidden, scouting reveals ---
            Assert.IsTrue(G.Fog.IsExplored(MapBuilder.PlayerBasePos), "own base explored");
            Assert.IsFalse(G.Fog.IsExplored(MapBuilder.EnemyBasePos), "enemy base unexplored before scouting");
            float exploredBefore = G.Fog.ExploredFraction();

            Unit scout = null;
            foreach (var e in Entity.All)
                if (e is Unit u && u.Faction == Faction.Player && u.Data.id == "soldier") { scout = u; break; }
            Assert.IsNotNull(scout, "soldier scout");
            scout.CommandMove(Vector3.zero);
            yield return WaitUntil(() => G.Fog.ExploredFraction() > exploredBefore + 0.02f, 120f, "fog reveal from scouting");

            // --- pathfinding: scout actually crossed the map (obstacles routed around) ---
            Assert.Less(Vector3.Distance(scout.Position, Vector3.zero), 20f, "scout should approach map center");

            // --- minimap alive ---
            Assert.IsNotNull(G.Minimap.Texture, "minimap texture");

            LogGuard.AssertClean();
        }

        static Vector3? FindSpot(BuildingData data)
        {
            for (int ring = 1; ring <= 5; ring++)
                for (int i = 0; i < 14; i++)
                {
                    var p = MapBuilder.PlayerBasePos +
                            Quaternion.Euler(0f, i * 26f, 0f) * Vector3.forward * (7f + ring * 4.5f);
                    p = new Vector3(Mathf.Round(p.x), 0f, Mathf.Round(p.z));
                    if (G.Placer.IsValidAt(data, p)) return p;
                }
            return null;
        }

        static WorkerUnit FindWorker()
        {
            foreach (var e in Entity.All)
                if (e is WorkerUnit w && e.Faction == Faction.Player && !e.IsDead) return w;
            return null;
        }

        // ------------------------------------------------------------------
        // v0.3 core loop hardening: workers should keep cycling resources,
        // and construction cancel should cleanly refund without a death blast.
        // ------------------------------------------------------------------
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator WorkerReliability_ContinuesHarvestingAndCancelRefunds()
        {
            Time.timeScale = 8f;
            G.AI.enabled = false;

            int start = G.PlayerBank.Minerals;
            yield return WaitUntil(() => G.PlayerBank.Minerals >= start + 40, 120f, "workers to complete repeated harvest cycles");

            G.PlayerBank.AddMinerals(500);
            var data = G.DB.Building("depot");
            Vector3? spot = FindSpot(data);
            Assert.IsTrue(spot.HasValue, "valid placement spot for cancel test");
            Assert.IsTrue(G.PlayerBank.TrySpend(data.mineralCost), "spend depot cost");
            int afterSpend = G.PlayerBank.Minerals;
            var site = G.Placer.PlaceAt(data, Faction.Player, spot.Value);
            Assert.IsFalse(site.IsComplete, "new depot starts as construction site");

            Assert.IsTrue(site.CancelConstruction(), "cancel unfinished construction");
            Assert.AreEqual(afterSpend + Mathf.RoundToInt(data.mineralCost * 0.75f),
                G.PlayerBank.Minerals, "cancel refunds 75 percent of build cost");
            yield return null;
            Assert.IsTrue(site == null || site.IsDead, "canceled construction site is retired");

            LogGuard.AssertClean();
        }

        // ------------------------------------------------------------------
        // Campaign: mission 3 spawns the Zerg boss, lift-off works,
        // and killing the boss wins the mission.
        // ------------------------------------------------------------------
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Campaign_BossMissionAndLiftOff()
        {
            // reload with mission 3 active
            Campaign.Current = Campaign.Missions[2];
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            Time.timeScale = 8f;

            // Zerg restyle applied to the enemy faction
            Assert.AreEqual("zerg_body", VisualFactory.EnemyBodyOverride, "zerg materials active");

            // boss exists
            Unit boss = null;
            foreach (var e in Entity.All)
                if (e is Unit u && u.Faction == Faction.Enemy && u.Data.id == "overlord") { boss = u; break; }
            Assert.IsNotNull(boss, "Overlord boss spawned");

            // --- lift-off: the starting CC can lift, fly and land ---
            Building cc = null;
            foreach (var e in Entity.All)
                if (e is Building b && b.Faction == Faction.Player && b.Data.id == "cc") { cc = b; break; }
            Assert.IsNotNull(cc, "player CC");
            cc.CommandLiftOff();
            yield return WaitUntil(() => cc.Flight == Building.FlightState.Flying, 60f, "CC lift-off");
            Vector3 pad = cc.Position + new Vector3(10f, 0f, 0f);
            pad = new Vector3(Mathf.Round(pad.x), 0f, Mathf.Round(pad.z));
            cc.CommandFlyTo(pad);
            yield return WaitGameSeconds(8f);
            cc.CommandLandAt(pad);
            yield return WaitUntil(() => cc.Flight == Building.FlightState.Grounded, 90f, "CC landing");

            // --- killing the boss ends the mission in Victory ---
            boss.Health.TakeDamage(999999f, DamageClass.Siege, null);
            yield return WaitUntil(() => G.Game.IsOver, 20f, "match end after boss kill");
            Assert.AreEqual(MatchState.Victory, G.Game.State, "boss kill = mission victory");

            LogGuard.AssertClean();
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Campaign_AllMissionsLoadObjectivesAndRaceSetup()
        {
            Time.timeScale = 8f;

            for (int i = 0; i < Campaign.Missions.Length; i++)
            {
                var mission = Campaign.Missions[i];
                Assert.IsFalse(string.IsNullOrEmpty(mission.objective), $"mission {i + 1} objective");
                Assert.IsFalse(string.IsNullOrEmpty(mission.victoryText), $"mission {i + 1} victory text");
                Assert.IsFalse(string.IsNullOrEmpty(mission.defeatText), $"mission {i + 1} defeat text");
                Assert.IsFalse(string.IsNullOrEmpty(mission.storyBeatText), $"mission {i + 1} story beat");
                Assert.AreNotEqual(AIPersonality.Balanced, mission.aiPersonality, $"mission {i + 1} personality");

                Campaign.Current = mission;
                SceneManager.LoadScene("Game");
                yield return null;
                yield return null;

                string expectedBody = mission.enemyRace == EnemyRace.Zerg ? "zerg_body"
                    : (mission.enemyRace == EnemyRace.Protoss ? "protoss_body" : null);
                Assert.AreEqual(expectedBody, VisualFactory.EnemyBodyOverride, $"mission {i + 1} race body override");

                int enemyBuildings = Count<Building>(Faction.Enemy);
                Assert.Greater(enemyBuildings, 0, $"mission {i + 1} enemy base spawned");

                if (!string.IsNullOrEmpty(mission.bossUnitId))
                {
                    bool foundBoss = false;
                    foreach (var e in Entity.All)
                        if (e is Unit u && u.Faction == Faction.Enemy && u.Data.id == mission.bossUnitId)
                        {
                            foundBoss = true;
                            break;
                        }
                    Assert.IsTrue(foundBoss, $"mission {i + 1} boss spawned");
                }
            }

            LogGuard.AssertClean();
        }

        [Test]
        public void V03DeferredHalf_DataAndAudioDefinitionsExist()
        {
            var db = GameDatabase.BuildTransient();
            var sensor = db.Building("sensor");
            Assert.IsNotNull(sensor, "Sensor Tower data exists");
            Assert.AreEqual(KeyCode.Y, sensor.hotkey, "Sensor Tower hotkey");
            Assert.Greater(sensor.visionRadius, 20f, "Sensor Tower is a real scouting building");

            foreach (var clip in new[] { "voice_select", "voice_move", "voice_attack", "voice_build", "voice_warning" })
                Assert.Greater(SynthLib.Generate(clip).Length, 1000, $"{clip} generated audio");

            foreach (var mission in Campaign.Missions)
            {
                Assert.IsFalse(string.IsNullOrEmpty(mission.storyBeatText), $"{mission.title} has story flavor");
                Assert.AreNotEqual(AIPersonality.Balanced, mission.aiPersonality, $"{mission.title} has AI personality");
            }
        }

        [Test]
        public void BubbleCampaign_MissionsExist()
        {
            int bubbleMissions = 0;
            foreach (var mission in Campaign.Missions)
            {
                if (mission.playerRace != PlayerRace.Bubble) continue;
                bubbleMissions++;
                Assert.IsFalse(string.IsNullOrEmpty(mission.briefing), $"{mission.title} has briefing");
                Assert.IsTrue(mission.objective.Contains("Terran") || mission.objective.Contains("poison"),
                    $"{mission.title} explains a Bubble-facing objective");
                Assert.AreEqual(EnemyRace.Terran, mission.enemyRace, $"{mission.title} starts against Terran for balance");
            }
            Assert.GreaterOrEqual(bubbleMissions, 3, "v0.8 Bubble campaign missions");
        }

        [Test]
        public void DotsCampaign_TutorialMissionExists()
        {
            int dotsMissions = 0;
            foreach (var mission in Campaign.Missions)
            {
                if (mission.playerRace != PlayerRace.Dots) continue;
                dotsMissions++;
                Assert.IsTrue(mission.objective.Contains("Giant"), $"{mission.title} teaches the Giant shape");
                Assert.IsTrue(mission.briefing.Contains("Core Dot"), $"{mission.title} explains the Core Dot");
                Assert.AreEqual(EnemyRace.Terran, mission.enemyRace, $"{mission.title} starts against Terran for balance");
            }
            Assert.GreaterOrEqual(dotsMissions, 1, "Dots tutorial campaign mission");
        }

        [Test]
        public void Bubble_DataDefinitionsExist()
        {
            var db = GameDatabase.BuildTransient();
            Assert.IsNotNull(db.Unit("bubble"), "basic bubble unit");
            Assert.IsNotNull(db.Unit("poison_bubble"), "poison bubble unit");
            Assert.IsNotNull(db.Building("bubble_core"), "Bubble Nexus");
            Assert.IsNotNull(db.Building("bubble_spring"), "Bubble Spring");
            Assert.IsNotNull(db.Building("poison_pool"), "Poison Pool");
            Assert.IsNotNull(db.Building("foam_turret"), "Foam Turret");
            Assert.AreEqual(0, db.Unit("bubble").supplyCost, "bubbles stream without supply");
            Assert.AreEqual(1, db.Unit("bubble").maxHP, "basic bubbles should pop in one hit");
            Assert.AreEqual(1, db.Unit("poison_bubble").maxHP, "poison bubbles should pop in one hit");
            Assert.LessOrEqual(db.Unit("bubble").damage, 1f, "basic bubble damage should stay tiny");

            // every bubble structure self-builds and lives in the bubble tech group
            foreach (var id in new[] { "bubble_core", "bubble_spring", "poison_pool", "foam_turret", "aerator" })
            {
                var b = db.Building(id);
                Assert.AreEqual("bubble", b.techGroup, $"{id} in bubble tech group");
                Assert.IsTrue(b.selfBuild, $"{id} self-builds");
            }
            Assert.Greater(db.Building("bubble_spring").passiveMineralsPerSec, 0f, "spring earns minerals");
            Assert.LessOrEqual(db.Building("bubble_spring").passiveMineralsPerSec, 0.75f, "spring income stays slow");
            Assert.Greater(db.Building("bubble_core").supplyProvided, 0, "nexus provides supply");
            Assert.IsNotNull(db.Building("aerator"), "Aerator upgrade building");
        }

        [Test]
        public void DotsPrototype_DataDefinitionsExist()
        {
            var db = GameDatabase.BuildTransient();
            Assert.IsNotNull(db.Unit("dot"), "basic Dot unit");
            Assert.IsNotNull(db.Unit("dot_core"), "mobile Core Dot unit");
            Assert.IsNotNull(db.Unit("dot_giant"), "Dot Giant shape");
            Assert.AreEqual(0, db.Unit("dot").supplyCost, "prototype Dots gather in groups");
            Assert.IsFalse(db.buildings.Exists(b => b.id == "power_core"), "Power Core is a mobile unit, not a building");

            foreach (var id in new[] { "dot_printer", "shape_matrix" })
            {
                var b = db.Building(id);
                Assert.IsNotNull(b, $"{id} building exists");
                Assert.AreEqual("dots", b.techGroup, $"{id} in Dots tech group");
                Assert.IsTrue(b.selfBuild, $"{id} self-builds like shape-droid structures");
                Assert.IsTrue(b.opensBuildMenu, $"{id} opens the Dots build menu");
            }
            Assert.Greater(db.Unit("dot_core").maxHP, db.Unit("dot").maxHP, "Core Dot is larger and tougher");
            Assert.Greater(db.Unit("dot_giant").damage, db.Unit("dot").damage * 10f, "Giant shape is very powerful");
            Assert.Greater(db.Unit("dot_giant").maxHP, db.Unit("dot_core").maxHP, "Giant hides and protects the Core Dot");
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator DotsLab_StartsPoweredAndPrintsDots()
        {
            Campaign.Current = null;
            SkirmishConfig.Mode = SkirmishMode.DotsLab;
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            Time.timeScale = 8f;
            G.AI.enabled = false;

            Assert.AreEqual(0, Count<Building>(Faction.Player, b => b.Data.id == "cc"), "no Terran Command Center");
            Assert.AreEqual(1, Count<Unit>(Faction.Player, u => u.Data.id == "dot_core"), "Core Dot start");
            Assert.AreEqual(1, Count<Building>(Faction.Player, b => b.Data.id == "dot_printer"), "Dot Printer start");
            Assert.GreaterOrEqual(Count<Unit>(Faction.Player, u => u.Data.id == "dot"), 6, "starter dots");

            Building printer = null;
            foreach (var e in Entity.All)
                if (e is Building b && b.Faction == Faction.Player && b.Data.id == "dot_printer")
                {
                    printer = b;
                    break;
                }
            Assert.IsNotNull(printer, "starting Dot Printer");
            Assert.IsTrue(DotsSystem.IsPowered(Faction.Player, printer.Position), "starting Printer is powered");

            int startMinerals = G.PlayerBank.Minerals;
            yield return WaitUntil(() => G.PlayerBank.Minerals > startMinerals,
                45f, "Core Dot passive mineral income");

            int startDots = Count<Unit>(Faction.Player, u => u.Data.id == "dot");
            yield return WaitUntil(() => Count<Unit>(Faction.Player, u => u.Data.id == "dot") > startDots,
                30f, "Dot Printer production");

            var matrixData = G.DB.Building("shape_matrix");
            var spot = FindSpot(matrixData);
            Assert.IsTrue(spot.HasValue, "a spot for the Shape Matrix");
            G.PlayerBank.AddMinerals(matrixData.mineralCost);
            G.PlayerBank.TrySpend(matrixData.mineralCost);
            var matrix = G.Placer.PlaceAt(matrixData, Faction.Player, spot.Value);
            yield return WaitUntil(() => matrix == null || matrix.IsComplete,
                matrixData.buildTime * 3f + 30f, "Shape Matrix self-builds");
            Assert.IsTrue(matrix != null && matrix.IsComplete, "Shape Matrix completed with no worker");

            var formDots = new List<Entity>();
            var dotData = G.DB.Unit("dot");
            Vector3 core = MapBuilder.PlayerBasePos + (Vector3.zero - MapBuilder.PlayerBasePos).normalized * 5.5f;
            for (int i = 0; i < 22; i++)
            {
                Vector3 pos = core + Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward * (1f + (i % 4) * 0.35f);
                var dot = UnitFactory.Spawn(dotData, Faction.Player, pos);
                Assert.IsNotNull(dot, "extra Dot for form tests");
                formDots.Add(dot);
            }

            Assert.IsTrue(G.Dots.TryFormGiant(formDots, out _), "Dots and Core Dot form a Giant");
            Assert.AreEqual(0, Count<Unit>(Faction.Player, u => u.Data.id == "dot_core"), "Core Dot hides inside the Giant");
            Assert.GreaterOrEqual(Count<Unit>(Faction.Player, u => u.Data.id == "dot_giant"), 1, "Dot Giant exists");

            Unit giant = null;
            foreach (var e in Entity.All)
                if (e is Unit u && u.Faction == Faction.Player && u.Data.id == "dot_giant") { giant = u; break; }
            Assert.IsNotNull(giant, "Dot Giant unit");
            giant.Health.TakeDamage(9999f, DamageClass.Siege, null);
            yield return null;
            yield return null;
            Assert.GreaterOrEqual(Count<Unit>(Faction.Player, u => u.Data.id == "dot_core"), 1, "Core Dot escapes when Giant dies");

            LogGuard.AssertClean();
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator BubbleLab_EconomyProduceUpgradeBuildMorph()
        {
            Campaign.Current = null;
            SkirmishConfig.Mode = SkirmishMode.BubbleLab;
            SceneManager.LoadScene("Game");
            yield return null;
            yield return null;
            Time.timeScale = 8f;
            G.AI.enabled = false;

            // starts as the Bubble faction: Nexus + Spring, and NO Poison Pool
            Assert.AreEqual(0, Count<Building>(Faction.Player, b => b.Data.id == "cc"), "no Terran Command Center");
            Assert.AreEqual(1, Count<Building>(Faction.Player, b => b.Data.id == "bubble_core"), "Bubble Nexus start");
            Assert.AreEqual(1, Count<Building>(Faction.Player, b => b.Data.id == "bubble_spring"), "Bubble Spring start");
            Assert.AreEqual(0, Count<Building>(Faction.Player, b => b.Data.id == "poison_pool"), "no Poison Pool at start");

            // economy: the spring mines a slow trickle
            int startMin = G.PlayerBank.Minerals;
            yield return WaitUntil(() => G.PlayerBank.Minerals > startMin, 45f, "spring mineral income");

            // production: the Nexus blows out more bubbles over time
            int startBubbles = Count<Unit>(Faction.Player, u => u.Data.id == "bubble");
            yield return WaitUntil(() => Count<Unit>(Faction.Player, u => u.Data.id == "bubble") > startBubbles,
                30f, "Nexus bubble production");

            // upgrade: an Aerator upgrade shortens the production interval
            float before = G.Bubble.ProductionInterval;
            G.PlayerBank.AddMinerals(500);
            Assert.IsTrue(G.Bubble.TryUpgradeProduction(Faction.Player, out _), "aerator production upgrade");
            Assert.Less(G.Bubble.ProductionInterval, before, "production interval shrinks after upgrade");

            // self-build: a placed Foam Turret finishes with no worker present
            var turretData = G.DB.Building("foam_turret");
            var spot = FindSpot(turretData);
            Assert.IsTrue(spot.HasValue, "a spot for the Foam Turret");
            G.PlayerBank.TrySpend(turretData.mineralCost);
            var turret = G.Placer.PlaceAt(turretData, Faction.Player, spot.Value);
            Assert.AreEqual(0, Count<WorkerUnit>(Faction.Player), "no workers exist in Bubble Lab");
            yield return WaitUntil(() => turret == null || turret.IsComplete,
                turretData.buildTime * 3f + 30f, "Foam Turret self-builds");
            Assert.IsTrue(turret != null && turret.IsComplete, "Foam Turret completed with no worker");

            // morph: a Poison Pool built at the swarm's gather point converts bubbles
            var poolData = G.DB.Building("poison_pool");
            Vector3 gather = MapBuilder.PlayerBasePos + (Vector3.zero - MapBuilder.PlayerBasePos).normalized * 5.5f;
            G.PlayerBank.TrySpend(poolData.mineralCost);
            var pool = G.Placer.PlaceAt(poolData, Faction.Player, BuildingPlacer.SnapToBuildGrid(gather));
            yield return WaitUntil(() => pool == null || pool.IsComplete,
                poolData.buildTime * 3f + 30f, "Poison Pool self-builds");
            yield return WaitUntil(() => Count<Unit>(Faction.Player, u => u.Data.id == "poison_bubble") >= 2,
                60f, "Poison Pool morphing");

            LogGuard.AssertClean();
        }

        // ------------------------------------------------------------------
        // Criterion 4/6: the AI plays a real match that ends in Victory/Defeat
        // with zero console errors. (No human input → AI should win.)
        // ------------------------------------------------------------------
        [UnityTest]
        [Timeout(900000)]
        public IEnumerator FullMatch_AIReachesConclusion_NoConsoleErrors()
        {
            Time.timeScale = 10f;

            float aiArmySeen = 0;
            bool aiBuiltProduction = false;
            float deadline = Time.realtimeSinceStartup + 700f;

            while (G.Game != null && !G.Game.IsOver)
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Match did not conclude in time (game time {G.Game.MatchTime:0}s, " +
                                $"enemy buildings {Building.CountBuildings(Faction.Enemy)}, " +
                                $"player buildings {Building.CountBuildings(Faction.Player)})");
                aiArmySeen = Mathf.Max(aiArmySeen, Count<Unit>(Faction.Enemy, u => !u.Data.isWorker));
                if (!aiBuiltProduction)
                    aiBuiltProduction = Count<Building>(Faction.Enemy, b => b.Data.id == "barracks") > 0;
                yield return null;
            }

            Assert.IsTrue(G.Game.IsOver, "match reached a conclusion");
            Assert.IsTrue(aiBuiltProduction, "AI constructed a Barracks (economy → production)");
            Assert.Greater(aiArmySeen, 2, "AI fielded an army");
            Assert.AreEqual(MatchState.Defeat, G.Game.State, "with no human playing, the AI should win");

            LogGuard.AssertClean();
        }
    }
}
