using UnityEngine;

namespace VoidClash
{
    public enum EnemyRace { Terran, Zerg, Protoss }
    public enum PlayerRace { Terran, Bubble, Dots }
    public enum AIPersonality { Balanced, Rusher, Turtle, Expander, Tech, Swarm }
    public enum SkirmishMode { Terran, BubbleLab, DotsLab }
    public enum Difficulty { Easy, Normal, Hard }

    /// <summary>Skirmish setup chosen on the New Skirmish menu (or by the legacy lab buttons).</summary>
    public static class SkirmishConfig
    {
        public static SkirmishMode Mode = SkirmishMode.Terran; // decides the PLAYER race + intro hints
        public static PlayerRace EnemyRace = PlayerRace.Terran; // which race the AI plays in skirmish
        public static Difficulty Difficulty = Difficulty.Normal;

        /// <summary>Player race implied by the current lab/skirmish Mode.</summary>
        public static PlayerRace PlayerRaceFromMode =>
            Mode == SkirmishMode.BubbleLab ? PlayerRace.Bubble :
            Mode == SkirmishMode.DotsLab ? PlayerRace.Dots : PlayerRace.Terran;

        /// <summary>Set everything for a custom skirmish and pick the Mode from the player race.</summary>
        public static void SetSkirmish(PlayerRace player, PlayerRace enemy, Difficulty difficulty)
        {
            Mode = player == PlayerRace.Bubble ? SkirmishMode.BubbleLab :
                   player == PlayerRace.Dots ? SkirmishMode.DotsLab : SkirmishMode.Terran;
            EnemyRace = enemy;
            Difficulty = difficulty;
        }
    }

    /// <summary>Everything that makes one campaign mission different from free play.</summary>
    public class MissionDef
    {
        public int index;
        public string title;
        public string menuBlurb;
        public string objective;
        public string victoryText;
        public string defeatText;
        public string storyBeatText;
        public float storyBeatTime = 180f;
        public string briefing;
        public PlayerRace playerRace = PlayerRace.Terran;
        public EnemyRace enemyRace;
        public AIPersonality aiPersonality = AIPersonality.Balanced;
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
        public string bossUnitId;         // non-null means victory = kill the boss
        public float bossAttackTime = 420f;
    }

    /// <summary>Campaign state. Current == null means free play (the default skirmish).</summary>
    public static class Campaign
    {
        public const string PrefUnlocked = "vc_campaign_unlocked";
        public const string PrefUnlockedPrefix = "vc_campaign_unlocked_";

        public static MissionDef Current;

        public static readonly MissionDef[] Missions =
        {
            new MissionDef
            {
                index = 0,
                title = "Mission 1 - First Contact",
                menuBlurb = "Burn out the Zerg infestation.",
                objective = "Destroy every Zerg structure.",
                victoryText = "The first brood is burned out. Terran foothold secured.",
                defeatText = "The infestation overran the landing zone.",
                storyBeatText = "Scans show the brood feeding faster near the north crystal field.",
                storyBeatTime = 135f,
                briefing = "Commander, a Zerg brood has infested this sector.\n" +
                           "Their swarms are fast, cheap and endless - but fragile.\n" +
                           "Establish your economy, wall up with Turrets, and burn out\n" +
                           "the infestation. Destroy every Zerg structure.\n\n" +
                           "TIP: Rangers shred swarms. Buildings with <L> can lift off and relocate.",
                enemyRace = EnemyRace.Zerg,
                aiPersonality = AIPersonality.Swarm,
                armyMix = new[] { "zergling", "zergling", "zergling", "hydralisk" },
                playerStartMinerals = 100,
                enemyStartMinerals = 50,
                aiWorkerCap = 10,
                aiBuildsFactory = false,
                aiTurrets = 1,
                firstWaveTime = 185f,
                waveInterval = 95f,
                firstWaveSize = 5,
                waveSizeGrowth = 3,
            },
            new MissionDef
            {
                index = 1,
                title = "Mission 2 - The Golden Armada",
                menuBlurb = "Break the Protoss armada.",
                objective = "Destroy every Protoss structure.",
                victoryText = "The Protoss plateau is broken. Their armada retreats.",
                defeatText = "The Golden Armada crushed our forward base.",
                storyBeatText = "Protoss shields are strongest near their towers. Siege them before the next pulse.",
                storyBeatTime = 190f,
                briefing = "Protoss forces have claimed the far plateau.\n" +
                           "Their warriors are few but terrifyingly strong, and their\n" +
                           "Zealots cut through light infantry like plasma through ice.\n" +
                           "Field Heavies to crack their armor. Raze their nexus.\n\n" +
                           "TIP: Their attacks come slower - expand to the center minerals early.",
                enemyRace = EnemyRace.Protoss,
                aiPersonality = AIPersonality.Turtle,
                armyMix = new[] { "zealot", "zealot", "stalker", "stalker" },
                playerStartMinerals = 150,
                enemyStartMinerals = 200,
                aiWorkerCap = 12,
                aiBuildsFactory = true,
                aiTurrets = 3,
                firstWaveTime = 235f,
                waveInterval = 120f,
                firstWaveSize = 7,
                waveSizeGrowth = 5,
            },
            new MissionDef
            {
                index = 2,
                title = "Mission 3 - The Overmind",
                menuBlurb = "Slay the Overlord. [BOSS]",
                objective = "Kill the Overlord boss.",
                victoryText = "The Overlord is dead. The swarm collapses without its brood mind.",
                defeatText = "The Overlord reached our base and drowned it in chitin.",
                storyBeatText = "The Overlord is syncing with every hive nerve. Prepare before it moves.",
                storyBeatTime = 260f,
                briefing = "This is it, Commander. The brood mother itself - the OVERLORD -\n" +
                           "nests in the enemy base, regenerating behind an endless swarm.\n" +
                           "It WILL come for you when the swarm senses blood.\n" +
                           "Slay the Overlord and the brood dies with it.\n\n" +
                           "VICTORY: kill the Overlord. Its hide shrugs off small arms - bring Heavies.",
                enemyRace = EnemyRace.Zerg,
                aiPersonality = AIPersonality.Swarm,
                armyMix = new[] { "zergling", "zergling", "zergling", "hydralisk", "hydralisk" },
                playerStartMinerals = 200,
                enemyStartMinerals = 100,
                aiWorkerCap = 13,
                aiBuildsFactory = false,
                aiTurrets = 2,
                firstWaveTime = 165f,
                waveInterval = 95f,
                firstWaveSize = 7,
                waveSizeGrowth = 5,
                bossUnitId = "overlord",
                bossAttackTime = 430f,
            },
            new MissionDef
            {
                index = 3,
                title = "Mission 4 - Steel Mirror",
                menuBlurb = "Outbuild a rival Terran commander.",
                objective = "Destroy the rebel Terran command.",
                victoryText = "The rebel commander is offline. Their factories now answer to you.",
                defeatText = "The rebel siege line broke our command network.",
                storyBeatText = "Enemy comms are coordinating a second front. Deny their expansion if you can.",
                storyBeatTime = 220f,
                briefing = "A breakaway Terran commander has fortified the opposite ridge.\n" +
                           "Expect the same tools you command: workers, Barracks, Factories,\n" +
                           "Turrets, and lifting production buildings.\n" +
                           "Scout early, keep your Supply Depots ahead, and punish exposed\n" +
                           "production before the mirror war turns into a siege.\n\n" +
                           "TIP: Right-click an unfinished friendly building with workers selected\n" +
                           "to send them back onto construction.",
                enemyRace = EnemyRace.Terran,
                aiPersonality = AIPersonality.Expander,
                armyMix = new[] { "soldier", "soldier", "ranged", "ranged", "heavy" },
                playerStartMinerals = 125,
                enemyStartMinerals = 180,
                aiWorkerCap = 14,
                aiBuildsFactory = true,
                aiTurrets = 3,
                firstWaveTime = 195f,
                waveInterval = 100f,
                firstWaveSize = 8,
                waveSizeGrowth = 5,
            },
            new MissionDef
            {
                index = 4,
                title = "Mission 5 - Shattered Gate",
                menuBlurb = "Hold the center against Protoss pressure.",
                objective = "Control the center and destroy every Protoss structure.",
                victoryText = "The shattered gate is sealed. The center crystals are ours.",
                defeatText = "Protoss pressure split the center and cut off our base.",
                storyBeatText = "The center gate is drawing power. Hold vision there before the Stalkers mass.",
                storyBeatTime = 210f,
                briefing = "Protoss raiders have opened a gate near the center crystals.\n" +
                           "Their first attacks arrive late, but every wave grows sharper.\n" +
                           "Claim the middle, anchor it with Turrets, then roll forward with\n" +
                           "Heavies before Stalkers control the open ground.\n\n" +
                           "VICTORY: destroy every Protoss structure.",
                enemyRace = EnemyRace.Protoss,
                aiPersonality = AIPersonality.Tech,
                armyMix = new[] { "zealot", "stalker", "stalker", "stalker" },
                playerStartMinerals = 175,
                enemyStartMinerals = 260,
                aiWorkerCap = 15,
                aiBuildsFactory = true,
                aiTurrets = 4,
                firstWaveTime = 245f,
                waveInterval = 105f,
                firstWaveSize = 9,
                waveSizeGrowth = 6,
            },
            new MissionDef
            {
                index = 5,
                title = "Mission 6 - Brood Eclipse",
                menuBlurb = "End the final Zerg hive. [BOSS]",
                objective = "Kill the final Overlord before the swarm overwhelms you.",
                victoryText = "The eclipse brood is finished. The sector is finally quiet.",
                defeatText = "The final brood swallowed the sector under the eclipse line.",
                storyBeatText = "The final brood is choosing speed over safety. Expect early pressure and weak flanks.",
                storyBeatTime = 165f,
                briefing = "The last brood has tunneled beneath the eclipse line.\n" +
                           "They attack sooner, rebuild faster, and hide their Overlord behind\n" +
                           "layers of cheap bodies. Build clean walls, keep Rangers alive, and\n" +
                           "add Factories once your economy can breathe.\n\n" +
                           "VICTORY: kill the Overlord before the swarm overwhelms the sector.",
                enemyRace = EnemyRace.Zerg,
                aiPersonality = AIPersonality.Rusher,
                armyMix = new[] { "zergling", "zergling", "zergling", "hydralisk", "hydralisk" },
                playerStartMinerals = 225,
                enemyStartMinerals = 180,
                aiWorkerCap = 16,
                aiBuildsFactory = false,
                aiTurrets = 3,
                firstWaveTime = 135f,
                waveInterval = 85f,
                firstWaveSize = 9,
                waveSizeGrowth = 6,
                bossUnitId = "overlord",
                bossAttackTime = 390f,
            },
            new MissionDef
            {
                index = 6,
                title = "Bubble 1 - First Foam",
                menuBlurb = "Learn the Bubble Nexus.",
                objective = "Destroy the Terran test outpost with bubbles.",
                victoryText = "The first foam tide held together. The Nexus is alive.",
                defeatText = "The outpost burst the whole swarm before it could grow.",
                storyBeatText = "The Nexus is breathing steadily. Protect the Spring and let the swarm collect.",
                storyBeatTime = 115f,
                briefing = "This is the Bubble front. You do not train workers.\n" +
                           "Your Bubble Nexus automatically blows fragile bubbles, and\n" +
                           "your Bubble Spring mines slowly when linked to crystals.\n" +
                           "Select the Nexus or Spring to grow more Bubble structures.\n\n" +
                           "VICTORY: destroy the Terran outpost. Bubbles pop in one hit,\n" +
                           "so attack as a wave, not as single units.",
                playerRace = PlayerRace.Bubble,
                enemyRace = EnemyRace.Terran,
                aiPersonality = AIPersonality.Turtle,
                armyMix = new[] { "soldier", "soldier", "ranged" },
                playerStartMinerals = 110,
                enemyStartMinerals = 40,
                aiWorkerCap = 7,
                aiBuildsFactory = false,
                aiTurrets = 0,
                firstWaveTime = 260f,
                waveInterval = 120f,
                firstWaveSize = 4,
                waveSizeGrowth = 2,
            },
            new MissionDef
            {
                index = 7,
                title = "Bubble 2 - Toxic Pop",
                menuBlurb = "Introduce Poison Pools.",
                objective = "Use poison bubbles to crack the Terran infantry line.",
                victoryText = "The gas clouds broke the formation. Bubble tactics are evolving.",
                defeatText = "The Terran line held and the foam collapsed.",
                storyBeatText = "Grouped infantry hate poison clouds. Build a Poison Pool near the gather point.",
                storyBeatTime = 125f,
                briefing = "The Terrans are bunching their infantry into tighter patrols.\n" +
                           "Build a Poison Pool near your bubble gather point to morph\n" +
                           "some basic bubbles into poison bubbles. Their direct damage\n" +
                           "is tiny, but their burst clouds punish clumps.\n\n" +
                           "VICTORY: destroy the Terran base after learning the poison morph.",
                playerRace = PlayerRace.Bubble,
                enemyRace = EnemyRace.Terran,
                aiPersonality = AIPersonality.Rusher,
                armyMix = new[] { "soldier", "soldier", "soldier", "ranged" },
                playerStartMinerals = 150,
                enemyStartMinerals = 80,
                aiWorkerCap = 9,
                aiBuildsFactory = false,
                aiTurrets = 1,
                firstWaveTime = 210f,
                waveInterval = 95f,
                firstWaveSize = 5,
                waveSizeGrowth = 3,
            },
            new MissionDef
            {
                index = 8,
                title = "Bubble 3 - Pressure Dome",
                menuBlurb = "Hold while the foam engine ramps.",
                objective = "Survive the Terran pressure and destroy the forward base.",
                victoryText = "The dome held. The foam tide can survive organized fire.",
                defeatText = "The Terran pressure popped the Nexus before the tide formed.",
                storyBeatText = "Terran patrols are spreading out. Use Foam Turrets and poison clouds to split them.",
                storyBeatTime = 150f,
                briefing = "The Terrans learned not to stand in poison clouds.\n" +
                           "This time they will pressure your Spring and try to thin the swarm\n" +
                           "before the Nexus reaches critical mass. Build a Foam Turret,\n" +
                           "add an Aerator, and counterattack once the bubble wave is thick.\n\n" +
                           "VICTORY: break the forward Terran base after surviving the pressure.",
                playerRace = PlayerRace.Bubble,
                enemyRace = EnemyRace.Terran,
                aiPersonality = AIPersonality.Expander,
                armyMix = new[] { "soldier", "ranged", "ranged", "soldier", "heavy" },
                playerStartMinerals = 170,
                enemyStartMinerals = 120,
                aiWorkerCap = 10,
                aiBuildsFactory = true,
                aiTurrets = 1,
                firstWaveTime = 170f,
                waveInterval = 95f,
                firstWaveSize = 6,
                waveSizeGrowth = 3,
            },
            new MissionDef
            {
                index = 9,
                title = "Dots 1 - Hidden Core",
                menuBlurb = "Learn Printers, Core Dot, and Giant.",
                objective = "Form a Dot Giant and destroy the Terran test base.",
                victoryText = "The Core survived inside the shape. The Dots are awake.",
                defeatText = "The Core Dot was isolated before the swarm could shape itself.",
                storyBeatText = "Printers make loose Dots. Spend them into Core, Kite, Spike, and Giant shapes.",
                storyBeatTime = 115f,
                briefing = "Dots do not mine with workers. Dot Printers make loose Dots,\n" +
                           "and loose Dots are spent into shapes. Use <C> to form a Core Dot,\n" +
                           "<V> for a flying Kite, <B> for a long-range Spike, and <Z>\n" +
                           "for a Giant once a Core Dot exists. The Giant hides the Core\n" +
                           "inside and releases it when destroyed.\n\n" +
                           "VICTORY: form a Giant and destroy the Terran test base.",
                playerRace = PlayerRace.Dots,
                enemyRace = EnemyRace.Terran,
                aiPersonality = AIPersonality.Turtle,
                armyMix = new[] { "soldier", "soldier", "ranged" },
                playerStartMinerals = 210,
                enemyStartMinerals = 60,
                aiWorkerCap = 7,
                aiBuildsFactory = false,
                aiTurrets = 0,
                firstWaveTime = 280f,
                waveInterval = 130f,
                firstWaveSize = 4,
                waveSizeGrowth = 2,
            },
            new MissionDef
            {
                index = 10,
                title = "Bubble 4 - Glass Undertow",
                menuBlurb = "Fight through Protoss armor with poison clouds.",
                objective = "Use poison bubbles and Foam Turrets to break the Protoss holdout.",
                victoryText = "The undertow slipped under the shields. Even armor can drown.",
                defeatText = "Protoss lances burned the foam before the tide could gather.",
                storyBeatText = "Protoss lines are slow and bright. Drag them through poison clouds before committing the swarm.",
                storyBeatTime = 170f,
                briefing = "Protoss scouts are testing the Bubble front with armored patrols.\n" +
                           "Their units are tougher than Terran infantry, but they bunch up\n" +
                           "around their towers. Build a Poison Pool, anchor your Spring with\n" +
                           "Foam Turrets, then flood the lanes once the clouds soften them.\n\n" +
                           "VICTORY: crack the Protoss holdout with poison and massed bubbles.",
                playerRace = PlayerRace.Bubble,
                enemyRace = EnemyRace.Protoss,
                aiPersonality = AIPersonality.Tech,
                armyMix = new[] { "zealot", "zealot", "stalker" },
                playerStartMinerals = 190,
                enemyStartMinerals = 170,
                aiWorkerCap = 11,
                aiBuildsFactory = true,
                aiTurrets = 2,
                firstWaveTime = 205f,
                waveInterval = 105f,
                firstWaveSize = 7,
                waveSizeGrowth = 4,
            },
            new MissionDef
            {
                index = 11,
                title = "Dots 2 - Needle Orbit",
                menuBlurb = "Use Kites and Spikes to fight at range.",
                objective = "Form Dot Kites and Dot Spikes, then destroy the Terran sensor line.",
                victoryText = "The sensor line is blind. The Dots learned to fight from orbit.",
                defeatText = "The sensor net pinned the Core before the shapes could spread.",
                storyBeatText = "Kites can cross pressure safely; Spikes outrange infantry but must stay screened.",
                storyBeatTime = 135f,
                briefing = "The Terrans have built a sensor line to track the Core Dot.\n" +
                           "This is a range lesson: use <V> for flying Dot Kites, and <B>\n" +
                           "for fragile long-range Dot Spikes. Keep loose Dots between\n" +
                           "the Core and the enemy while your shapes pick apart the line.\n\n" +
                           "VICTORY: make Kite and Spike shapes, then destroy the Terran base.",
                playerRace = PlayerRace.Dots,
                enemyRace = EnemyRace.Terran,
                aiPersonality = AIPersonality.Expander,
                armyMix = new[] { "soldier", "ranged", "ranged" },
                playerStartMinerals = 230,
                enemyStartMinerals = 110,
                aiWorkerCap = 9,
                aiBuildsFactory = false,
                aiTurrets = 1,
                firstWaveTime = 230f,
                waveInterval = 105f,
                firstWaveSize = 5,
                waveSizeGrowth = 3,
            },
            new MissionDef
            {
                index = 12,
                title = "Dots 3 - Giant Relay",
                menuBlurb = "Chain Core Dots into a decisive Giant push.",
                objective = "Form a Dot Giant, protect the released Core, and crush the Protoss relay.",
                victoryText = "The relay collapsed. The Core came back humming.",
                defeatText = "The relay split the swarm and stranded the Core.",
                storyBeatText = "Do not spend every Core at once. A released Core can rebuild the whole shape army.",
                storyBeatTime = 165f,
                briefing = "A Protoss relay is broadcasting patterns that scramble loose Dots.\n" +
                           "Build extra Printers, form a spare Core Dot, then commit a Giant\n" +
                           "when your swarm can protect whatever Core escapes from the wreckage.\n\n" +
                           "VICTORY: form a Giant and destroy the Protoss relay base.",
                playerRace = PlayerRace.Dots,
                enemyRace = EnemyRace.Protoss,
                aiPersonality = AIPersonality.Tech,
                armyMix = new[] { "zealot", "stalker", "stalker" },
                playerStartMinerals = 260,
                enemyStartMinerals = 190,
                aiWorkerCap = 11,
                aiBuildsFactory = true,
                aiTurrets = 2,
                firstWaveTime = 215f,
                waveInterval = 105f,
                firstWaveSize = 6,
                waveSizeGrowth = 4,
            },
        };

        public static int UnlockedCount
        {
            get => UnlockedForRace(PlayerRace.Terran);
            set => SetUnlockedForRace(PlayerRace.Terran, value);
        }

        public static bool IsCampaign => Current != null;
        public static bool HasNextMission => NextMission(Current) != null;

        public static void NotifyVictory()
        {
            if (Current == null) return;
            int rank = MissionRank(Current);
            if (rank + 1 >= UnlockedForRace(Current.playerRace))
                SetUnlockedForRace(Current.playerRace, rank + 2);
        }

        public static int UnlockedForRace(PlayerRace race)
        {
            int count = CountMissionsForRace(race);
            if (count <= 0) return 0;
            int fallback = race == PlayerRace.Terran ? PlayerPrefs.GetInt(PrefUnlocked, 1) : 1;
            return Mathf.Clamp(PlayerPrefs.GetInt(PrefKey(race), fallback), 1, count);
        }

        public static void SetUnlockedForRace(PlayerRace race, int value)
        {
            int count = CountMissionsForRace(race);
            if (count <= 0) return;
            PlayerPrefs.SetInt(PrefKey(race), Mathf.Clamp(value, 1, count));
        }

        public static int CountMissionsForRace(PlayerRace race)
        {
            int count = 0;
            for (int i = 0; i < Missions.Length; i++)
                if (Missions[i].playerRace == race) count++;
            return count;
        }

        public static int MissionRank(MissionDef mission)
        {
            if (mission == null) return -1;
            int rank = 0;
            for (int i = 0; i < Missions.Length; i++)
            {
                if (Missions[i].playerRace != mission.playerRace) continue;
                if (Missions[i] == mission || Missions[i].index == mission.index) return rank;
                rank++;
            }
            return -1;
        }

        public static bool IsUnlocked(MissionDef mission)
        {
            int rank = MissionRank(mission);
            return mission != null && rank >= 0 && rank < UnlockedForRace(mission.playerRace);
        }

        public static MissionDef NextMission(MissionDef mission)
        {
            if (mission == null) return null;
            bool found = false;
            for (int i = 0; i < Missions.Length; i++)
            {
                if (Missions[i].playerRace != mission.playerRace) continue;
                if (found) return Missions[i];
                if (Missions[i] == mission || Missions[i].index == mission.index) found = true;
            }
            return null;
        }

        static string PrefKey(PlayerRace race) => PrefUnlockedPrefix + race.ToString().ToLowerInvariant();
    }
}
