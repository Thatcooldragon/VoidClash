using UnityEngine;

namespace VoidClash
{
    public struct Palette
    {
        public Color Primary;
        public Color Accent;
        public Color Threat;
        public string BodyMaterial;
        public string AccentMaterial;
    }

    public static class FactionPalette
    {
        public static Palette ForPlayerRace(PlayerRace race)
        {
            if (race == PlayerRace.Bubble)
                return new Palette { Primary = new Color(0.08f, 0.34f, 0.36f), Accent = new Color(0.35f, 1f, 0.85f), Threat = new Color(0.2f, 0.95f, 0.75f), BodyMaterial = "crystal", AccentMaterial = "rally" };
            if (race == PlayerRace.Dots)
                return new Palette { Primary = new Color(0.34f, 0.18f, 0.06f), Accent = new Color(1f, 0.58f, 0.28f), Threat = new Color(1f, 0.82f, 0.24f), BodyMaterial = "metal_light", AccentMaterial = "rally" };
            return new Palette { Primary = new Color(0.06f, 0.16f, 0.32f), Accent = new Color(0.25f, 0.62f, 1f), Threat = new Color(0.45f, 0.85f, 1f), BodyMaterial = "player_body", AccentMaterial = "player_accent" };
        }

        public static Palette ForEnemyRace(EnemyRace race)
        {
            if (race == EnemyRace.Zerg)
                return new Palette { Primary = new Color(0.26f, 0.04f, 0.12f), Accent = new Color(1f, 0.25f, 0.55f), Threat = new Color(0.78f, 0.08f, 0.18f), BodyMaterial = "zerg_body", AccentMaterial = "zerg_accent" };
            if (race == EnemyRace.Protoss)
                return new Palette { Primary = new Color(0.36f, 0.27f, 0.06f), Accent = new Color(0.35f, 0.95f, 1f), Threat = new Color(1f, 0.78f, 0.25f), BodyMaterial = "protoss_body", AccentMaterial = "protoss_accent" };
            return new Palette { Primary = new Color(0.35f, 0.07f, 0.06f), Accent = new Color(1f, 0.3f, 0.25f), Threat = new Color(1f, 0.35f, 0.2f), BodyMaterial = "enemy_body", AccentMaterial = "enemy_accent" };
        }

        public static Color RaceAccent(PlayerRace race) => ForPlayerRace(race).Accent;

        public static string RaceName(PlayerRace race)
        {
            if (race == PlayerRace.Bubble) return "Bubble";
            if (race == PlayerRace.Dots) return "Dots";
            return "Terran";
        }
    }
}
