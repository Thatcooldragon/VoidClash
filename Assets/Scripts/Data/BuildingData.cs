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
