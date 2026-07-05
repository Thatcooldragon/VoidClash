using UnityEngine;

namespace VoidClash
{
    [CreateAssetMenu(menuName = "VoidClash/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public KeyCode hotkey = KeyCode.Q;

        [Header("Cost")]
        public int mineralCost;
        public int supplyCost;
        public float trainTime = 10f;

        [Header("Survivability")]
        public int maxHP = 100;
        public int armor = 0;
        public ArmorClass armorClass = ArmorClass.Light;

        [Header("Offense")]
        public float damage = 8f;
        public DamageClass damageClass = DamageClass.Normal;
        public float attackRange = 4f;      // <= 1.5 is treated as melee
        public float attackCooldown = 1f;
        public float projectileSpeed = 0f;  // 0 = hitscan / melee strike
        public bool canAttack = true;

        [Header("Mobility & Senses")]
        public float moveSpeed = 3.5f;
        public float visionRadius = 10f;

        [Header("Role")]
        public bool isWorker = false;

        [Header("Visuals")]
        public Color accentColor = new Color(0.2f, 0.9f, 1f);
        public float bodyScale = 1f;

        public bool IsMelee => attackRange <= 1.6f;
    }
}
