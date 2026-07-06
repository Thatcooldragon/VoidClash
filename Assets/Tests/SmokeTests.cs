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

            // --- build all 5 building types (worker-constructed, like the real flow) ---
            G.PlayerBank.AddMinerals(5000);
            foreach (var id in new[] { "depot", "barracks", "factory", "turret", "cc" })
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
