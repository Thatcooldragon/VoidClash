using UnityEngine;

namespace VoidClash
{
    /// <summary>Single source of truth for all balance numbers. The editor setup script bakes these
    /// into .asset ScriptableObjects; at runtime the baked assets are used (with this as fallback).</summary>
    public static class DataDefs
    {
        public static UnitData[] CreateUnits()
        {
            var worker = ScriptableObject.CreateInstance<UnitData>();
            worker.id = "worker"; worker.displayName = "Worker";
            worker.description = "Harvests minerals and constructs buildings. Weak in a fight.";
            worker.hotkey = KeyCode.Q;
            worker.mineralCost = 50; worker.supplyCost = 1; worker.trainTime = 9f;
            worker.maxHP = 60; worker.armor = 0; worker.armorClass = ArmorClass.Light;
            worker.damage = 5f; worker.damageClass = DamageClass.Normal;
            worker.attackRange = 1.4f; worker.attackCooldown = 1.0f; worker.projectileSpeed = 0f;
            worker.moveSpeed = 3.6f; worker.visionRadius = 9f; worker.isWorker = true;
            worker.accentColor = new Color(1f, 0.85f, 0.25f); worker.bodyScale = 0.8f;

            var soldier = ScriptableObject.CreateInstance<UnitData>();
            soldier.id = "soldier"; soldier.displayName = "Soldier";
            soldier.description = "Cheap frontline infantry. Even damage against everything.";
            soldier.hotkey = KeyCode.Q;
            soldier.mineralCost = 50; soldier.supplyCost = 1; soldier.trainTime = 10f;
            soldier.maxHP = 120; soldier.armor = 0; soldier.armorClass = ArmorClass.Light;
            soldier.damage = 8f; soldier.damageClass = DamageClass.Normal;
            soldier.attackRange = 4f; soldier.attackCooldown = 0.8f; soldier.projectileSpeed = 0f;
            soldier.moveSpeed = 3.5f; soldier.visionRadius = 10f;
            soldier.accentColor = new Color(0.3f, 1f, 0.5f); soldier.bodyScale = 1f;

            var ranged = ScriptableObject.CreateInstance<UnitData>();
            ranged.id = "ranged"; ranged.displayName = "Ranger";
            ranged.description = "Long-range piercing DPS. Shreds light units, weak vs armor.";
            ranged.hotkey = KeyCode.W;
            ranged.mineralCost = 75; ranged.supplyCost = 1; ranged.trainTime = 12f;
            ranged.maxHP = 90; ranged.armor = 0; ranged.armorClass = ArmorClass.Light;
            ranged.damage = 12f; ranged.damageClass = DamageClass.Piercing;
            ranged.attackRange = 9f; ranged.attackCooldown = 1.2f; ranged.projectileSpeed = 28f;
            ranged.moveSpeed = 3.3f; ranged.visionRadius = 11f;
            ranged.accentColor = new Color(0.4f, 0.85f, 1f); ranged.bodyScale = 0.95f;

            var heavy = ScriptableObject.CreateInstance<UnitData>();
            heavy.id = "heavy"; heavy.displayName = "Heavy";
            heavy.description = "Slow siege tank. Crushes armor and buildings, poor vs infantry.";
            heavy.hotkey = KeyCode.Q;
            heavy.mineralCost = 150; heavy.supplyCost = 3; heavy.trainTime = 20f;
            heavy.maxHP = 350; heavy.armor = 2; heavy.armorClass = ArmorClass.Armored;
            heavy.damage = 25f; heavy.damageClass = DamageClass.Siege;
            heavy.attackRange = 6.5f; heavy.attackCooldown = 1.8f; heavy.projectileSpeed = 22f;
            heavy.moveSpeed = 2.2f; heavy.visionRadius = 10f;
            heavy.accentColor = new Color(1f, 0.55f, 0.2f); heavy.bodyScale = 1.35f;

            // ---- campaign enemy units (Zerg / Protoss / boss) ----

            var zergling = ScriptableObject.CreateInstance<UnitData>();
            zergling.id = "zergling"; zergling.displayName = "Zergling";
            zergling.description = "Fast, cheap swarm melee. Dies in droves, arrives in more.";
            zergling.mineralCost = 25; zergling.supplyCost = 1; zergling.trainTime = 6f;
            zergling.maxHP = 70; zergling.armor = 0; zergling.armorClass = ArmorClass.Light;
            zergling.damage = 6f; zergling.damageClass = DamageClass.Normal;
            zergling.attackRange = 1.3f; zergling.attackCooldown = 0.7f; zergling.projectileSpeed = 0f;
            zergling.moveSpeed = 4.4f; zergling.visionRadius = 9f;
            zergling.accentColor = new Color(0.9f, 0.3f, 0.6f); zergling.bodyScale = 0.75f;

            var hydra = ScriptableObject.CreateInstance<UnitData>();
            hydra.id = "hydralisk"; hydra.displayName = "Hydralisk";
            hydra.description = "Ranged spine-thrower. Piercing needles shred light targets.";
            hydra.mineralCost = 70; hydra.supplyCost = 1; hydra.trainTime = 11f;
            hydra.maxHP = 95; hydra.armor = 0; hydra.armorClass = ArmorClass.Light;
            hydra.damage = 11f; hydra.damageClass = DamageClass.Piercing;
            hydra.attackRange = 7.5f; hydra.attackCooldown = 1.1f; hydra.projectileSpeed = 26f;
            hydra.moveSpeed = 3.4f; hydra.visionRadius = 10f;
            hydra.accentColor = new Color(0.75f, 0.4f, 0.9f); hydra.bodyScale = 1f;

            var zealot = ScriptableObject.CreateInstance<UnitData>();
            zealot.id = "zealot"; zealot.displayName = "Zealot";
            zealot.description = "Elite psi-blade warrior. Slow to fall, brutal up close.";
            zealot.mineralCost = 100; zealot.supplyCost = 2; zealot.trainTime = 16f;
            zealot.maxHP = 240; zealot.armor = 1; zealot.armorClass = ArmorClass.Light;
            zealot.damage = 16f; zealot.damageClass = DamageClass.Normal;
            zealot.attackRange = 1.5f; zealot.attackCooldown = 1.0f; zealot.projectileSpeed = 0f;
            zealot.moveSpeed = 3.6f; zealot.visionRadius = 10f;
            zealot.accentColor = new Color(0.3f, 0.9f, 1f); zealot.bodyScale = 1.05f;

            var stalker = ScriptableObject.CreateInstance<UnitData>();
            stalker.id = "stalker"; stalker.displayName = "Stalker";
            stalker.description = "Armored walker with a long-range particle disruptor.";
            stalker.mineralCost = 125; stalker.supplyCost = 2; stalker.trainTime = 18f;
            stalker.maxHP = 160; stalker.armor = 1; stalker.armorClass = ArmorClass.Armored;
            stalker.damage = 14f; stalker.damageClass = DamageClass.Normal;
            stalker.attackRange = 8f; stalker.attackCooldown = 1.3f; stalker.projectileSpeed = 30f;
            stalker.moveSpeed = 3.2f; stalker.visionRadius = 11f;
            stalker.accentColor = new Color(0.4f, 0.7f, 1f); stalker.bodyScale = 1.15f;

            var overlord = ScriptableObject.CreateInstance<UnitData>();
            overlord.id = "overlord"; overlord.displayName = "Overlord";
            overlord.description = "The brood mother. Kill it and the swarm dies.";
            overlord.mineralCost = 0; overlord.supplyCost = 0; overlord.trainTime = 1f;
            overlord.maxHP = 4500; overlord.armor = 3; overlord.armorClass = ArmorClass.Armored;
            overlord.damage = 38f; overlord.damageClass = DamageClass.Siege;
            overlord.attackRange = 6.5f; overlord.attackCooldown = 1.6f; overlord.projectileSpeed = 18f;
            overlord.moveSpeed = 1.9f; overlord.visionRadius = 13f;
            overlord.accentColor = new Color(1f, 0.25f, 0.5f); overlord.bodyScale = 3.1f;

            return new[] { worker, soldier, ranged, heavy, zergling, hydra, zealot, stalker, overlord };
        }

        public static BuildingData[] CreateBuildings()
        {
            var cc = ScriptableObject.CreateInstance<BuildingData>();
            cc.id = "cc"; cc.displayName = "Command Center";
            cc.description = "Trains Workers, accepts mineral deliveries, provides 10 supply.";
            cc.hotkey = KeyCode.Q;
            cc.mineralCost = 400; cc.buildTime = 30f; cc.maxHP = 1500; cc.armor = 2;
            cc.sizeX = 6f; cc.sizeZ = 6f;
            cc.supplyProvided = 10; cc.isDropoff = true;
            cc.canLift = true;
            cc.trainableUnits = new[] { "worker" };
            cc.visionRadius = 13f;
            cc.accentColor = new Color(0.3f, 0.9f, 1f);

            var depot = ScriptableObject.CreateInstance<BuildingData>();
            depot.id = "depot"; depot.displayName = "Supply Depot";
            depot.description = "Provides +8 supply.";
            depot.hotkey = KeyCode.W;
            depot.mineralCost = 100; depot.buildTime = 15f; depot.maxHP = 500; depot.armor = 1;
            depot.sizeX = 3f; depot.sizeZ = 3f;
            depot.supplyProvided = 8;
            depot.visionRadius = 9f;
            depot.accentColor = new Color(0.5f, 1f, 0.6f);

            var rax = ScriptableObject.CreateInstance<BuildingData>();
            rax.id = "barracks"; rax.displayName = "Barracks";
            rax.description = "Trains Soldiers and Rangers.";
            rax.hotkey = KeyCode.E;
            rax.mineralCost = 150; rax.buildTime = 20f; rax.maxHP = 900; rax.armor = 1;
            rax.sizeX = 5f; rax.sizeZ = 5f;
            rax.canLift = true;
            rax.trainableUnits = new[] { "soldier", "ranged" };
            rax.visionRadius = 10f;
            rax.accentColor = new Color(0.4f, 1f, 0.5f);

            var fac = ScriptableObject.CreateInstance<BuildingData>();
            fac.id = "factory"; fac.displayName = "Factory";
            fac.description = "Trains Heavies.";
            fac.hotkey = KeyCode.R;
            fac.mineralCost = 200; fac.buildTime = 25f; fac.maxHP = 1100; fac.armor = 1;
            fac.sizeX = 6f; fac.sizeZ = 6f;
            fac.canLift = true;
            fac.trainableUnits = new[] { "heavy" };
            fac.visionRadius = 10f;
            fac.accentColor = new Color(1f, 0.6f, 0.25f);

            var turret = ScriptableObject.CreateInstance<BuildingData>();
            turret.id = "turret"; turret.displayName = "Turret";
            turret.description = "Static defense. Piercing shots, long range.";
            turret.hotkey = KeyCode.T;
            turret.mineralCost = 125; turret.buildTime = 15f; turret.maxHP = 750; turret.armor = 1;
            turret.sizeX = 2f; turret.sizeZ = 2f;
            turret.damage = 14f; turret.damageClass = DamageClass.Piercing;
            turret.attackRange = 10f; turret.attackCooldown = 1.0f; turret.projectileSpeed = 34f;
            turret.visionRadius = 12f;
            turret.accentColor = new Color(1f, 0.35f, 0.35f);

            return new[] { cc, depot, rax, fac, turret };
        }
    }
}
