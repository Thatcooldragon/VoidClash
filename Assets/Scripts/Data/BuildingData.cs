using UnityEngine;

namespace VoidClash
{
    [CreateAssetMenu(menuName = "VoidClash/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public KeyCode hotkey = KeyCode.Q;

        [Header("Cost")]
        public int mineralCost;
        public float buildTime = 20f;

        [Header("Survivability")]
        public int maxHP = 500;
        public int armor = 1;
        public ArmorClass armorClass = ArmorClass.Structure;

        [Header("Footprint (world units)")]
        public float sizeX = 4f;
        public float sizeZ = 4f;

        [Header("Function")]
        public bool canLift = false;
        public int supplyProvided = 0;
        public bool isDropoff = false;
        public string[] trainableUnits = new string[0];

        [Header("Faction / Bubble economy")]
        /// <summary>Which build hotbar this belongs to ("terran" or "bubble"). Filters what
        /// a selected builder can construct so races don't cross-build.</summary>
        public string techGroup = "terran";
        /// <summary>Bubble structures grow themselves — no worker needed to complete them.</summary>
        public bool selfBuild = false;
        /// <summary>Passive minerals per second while complete (Bubble structure-driven economy).
        /// Springs also require a nearby mineral node; see BubbleSystem.</summary>
        public float passiveMineralsPerSec = 0f;
        /// <summary>Selecting a completed structure with this set opens the bubble build menu.</summary>
        public bool opensBuildMenu = false;

        [Header("Turret (0 damage = no attack)")]
        public float damage = 0f;
        public DamageClass damageClass = DamageClass.Piercing;
        public float attackRange = 9f;
        public float attackCooldown = 1f;
        public float projectileSpeed = 30f;

        [Header("Senses & Visuals")]
        public float visionRadius = 11f;
        public Color accentColor = new Color(0.2f, 0.9f, 1f);

        public bool CanAttack => damage > 0f;
        public bool CanTrain => trainableUnits != null && trainableUnits.Length > 0;
    }
}
