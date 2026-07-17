using UnityEngine;

namespace VoidClash
{
    /// <summary>Central service locator. Populated by GameBootstrap each time the Game scene loads.</summary>
    public static class G
    {
        public static GameManager Game;
        public static ResourceBank PlayerBank;
        public static ResourceBank EnemyBank;
        public static FogOfWar Fog;
        public static SelectionManager Selection;
        public static InputController Input;
        public static BuildingPlacer Placer;
        public static HUD Hud;
        public static AudioManager Audio;
        public static EffectsManager Effects;
        public static MapBuilder Map;
        public static CameraController Cam;
        public static Minimap Minimap;
        public static EnemyAI AI;
        public static BubbleSystem Bubble;
        public static CommanderPowers Powers;
        public static DotsSystem Dots;
        public static OnboardingDirector Guidance;
        public static GameDatabase DB;

        public static ResourceBank Bank(Faction f) => f == Faction.Player ? PlayerBank : EnemyBank;

        public static void ResetAll()
        {
            Game = null; PlayerBank = null; EnemyBank = null; Fog = null; Selection = null;
            Input = null; Placer = null; Hud = null; Audio = null; Effects = null;
            Map = null; Cam = null; Minimap = null; AI = null; Bubble = null; Powers = null; Dots = null;
            Guidance = null;
            Entity.ClearRegistry();
            MineralNode.ClearRegistry();
        }

        public static void EnsureDatabase()
        {
            if (DB != null) return;
            DB = Resources.Load<GameDatabase>("GameDatabase");
            if (DB == null)
            {
                Debug.LogWarning("VoidClash: GameDatabase asset not found, building transient database from code.");
                DB = GameDatabase.BuildTransient();
            }
            DB.EnsureDefinitions();
        }
    }

    public enum Faction { Player = 0, Enemy = 1, Neutral = 2 }

    public static class FactionUtil
    {
        public static Color Tint(Faction f) => f == Faction.Player
            ? new Color(0.25f, 0.62f, 1f)
            : (f == Faction.Enemy ? new Color(1f, 0.30f, 0.25f) : new Color(0.8f, 0.8f, 0.4f));

        public static Faction Opponent(Faction f) => f == Faction.Player ? Faction.Enemy : Faction.Player;
    }
}
