using UnityEngine;

namespace VoidClash
{
    public enum EnemyRace { Terran, Zerg, Protoss }

    /// <summary>Everything that makes one campaign mission different from free play.</summary>
    public class MissionDef
    {
        public int index;
        public string title;
        public string briefing;
        public EnemyRace enemyRace;
        public string[] armyMix;          // weighted list of unit ids the AI trains
        public int playerStartMinerals = 50;
        public int enemyStartMinerals = 50;
        public int aiWorkerCap = 13;
        public bool aiBuildsFactory = true;
        public int aiTurrets = 2;
        public float firstWaveTime = 210f;
        public float waveInterval = 110f;
        public int firstWaveSize = 6;
        public int waveSizeGrowth = 4;
        public string bossUnitId;         // non-null → victory = kill the boss
        public float bossAttackTime = 420f;
    }

    /// <summary>Campaign state. Current == null → free play (the default skirmish).</summary>
    public static class Campaign
    {
        public const string PrefUnlocked = "vc_campaign_unlocked";

        public static MissionDef Current;

        public static readonly MissionDef[] Missions =
        {
            new MissionDef
            {
                index = 0,
                title = "Mission 1 — First Contact",
                briefing = "Commander, a Zerg brood has infested this sector.\n" +
                           "Their swarms are fast, cheap and endless — but fragile.\n" +
                           "Establish your economy, wall up with Turrets, and burn out\n" +
                           "the infestation. Destroy every Zerg structure.\n\n" +
                           "TIP: Rangers shred swarms. Buildings with <L> can lift off and relocate.",
                enemyRace = EnemyRace.Zerg,
                armyMix = new[] { "zergling", "zergling", "zergling", "hydralisk" },
                playerStartMinerals = 100,
                enemyStartMinerals = 50,
                aiWorkerCap = 10,
                aiBuildsFactory = false,
                aiTurrets = 1,
                firstWaveTime = 170f,
                waveInterval = 85f,
                firstWaveSize = 6,
                waveSizeGrowth = 4,
            },
            new MissionDef
            {
                index = 1,
                title = "Mission 2 — The Golden Armada",
                briefing = "Protoss forces have claimed the far plateau.\n" +
                           "Their warriors are few but terrifyingly strong, and their\n" +
                           "Zealots cut through light infantry like plasma through ice.\n" +
                           "Field Heavies to crack their armor. Raze their nexus.\n\n" +
                           "TIP: Their attacks come slower — expand to the center minerals early.",
                enemyRace = EnemyRace.Protoss,
                armyMix = new[] { "zealot", "zealot", "stalker", "stalker" },
                playerStartMinerals = 150,
                enemyStartMinerals = 200,
                aiWorkerCap = 12,
                aiBuildsFactory = true,
                aiTurrets = 3,
                firstWaveTime = 240f,
                waveInterval = 120f,
                firstWaveSize = 8,
                waveSizeGrowth = 6,
            },
            new MissionDef
            {
                index = 2,
                title = "Mission 3 — The Overmind",
                briefing = "This is it, Commander. The brood mother itself — the OVERLORD —\n" +
                           "nests in the enemy base, regenerating behind an endless swarm.\n" +
                           "It WILL come for you when the swarm senses blood.\n" +
                           "Slay the Overlord and the brood dies with it.\n\n" +
                           "VICTORY: kill the Overlord. Its hide shrugs off small arms — bring Heavies.",
                enemyRace = EnemyRace.Zerg,
                armyMix = new[] { "zergling", "zergling", "zergling", "hydralisk", "hydralisk" },
                playerStartMinerals = 200,
                enemyStartMinerals = 100,
                aiWorkerCap = 13,
                aiBuildsFactory = false,
                aiTurrets = 2,
                firstWaveTime = 150f,
                waveInterval = 90f,
                firstWaveSize = 7,
                waveSizeGrowth = 6,
                bossUnitId = "overlord",
                bossAttackTime = 420f,
            },
        };

        public static int UnlockedCount
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(PrefUnlocked, 1), 1, Missions.Length);
            set => PlayerPrefs.SetInt(PrefUnlocked, Mathf.Clamp(value, 1, Missions.Length));
        }

        public static bool IsCampaign => Current != null;
        public static bool HasNextMission => Current != null && Current.index + 1 < Missions.Length;

        public static void NotifyVictory()
        {
            if (Current == null) return;
            if (Current.index + 1 >= UnlockedCount)
                UnlockedCount = Current.index + 2;
        }
    }
}
