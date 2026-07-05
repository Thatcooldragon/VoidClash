namespace VoidClash
{
    public enum ArmorClass { Light = 0, Armored = 1, Structure = 2 }

    /// <summary>Damage classes make unit types counter each other:
    /// Piercing shreds Light, Siege cracks Armored/Structures, Normal is even.</summary>
    public enum DamageClass { Normal = 0, Piercing = 1, Siege = 2 }

    public static class DamageTable
    {
        // [damageClass, armorClass]
        static readonly float[,] Mult =
        {
            //           Light  Armored Structure
            /*Normal*/  { 1.00f, 0.85f, 0.90f },
            /*Piercing*/{ 1.30f, 0.70f, 0.60f },
            /*Siege*/   { 0.70f, 1.40f, 1.50f },
        };

        public static float Multiplier(DamageClass d, ArmorClass a) => Mult[(int)d, (int)a];

        /// <summary>Final damage after class multiplier and flat armor, never below 1.</summary>
        public static float Compute(float baseDamage, DamageClass d, ArmorClass a, int armor)
        {
            float v = baseDamage * Multiplier(d, a) - armor;
            return v < 1f ? 1f : v;
        }
    }
}
