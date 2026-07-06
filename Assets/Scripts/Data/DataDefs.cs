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

            var bubble = ScriptableObject.CreateInstance<UnitData>();
            bubble.id = "bubble"; bubble.displayName = "Bubble";
            bubble.description = "Fragile soap bubble blown by the Bubble Nexus. Pops in one hit and deals tiny chip damage.";
            bubble.mineralCost = 0; bubble.supplyCost = 0; bubble.trainTime = 1f;
            bubble.maxHP = 1; bubble.armor = 0; bubble.armorClass = ArmorClass.Light;
            bubble.damage = 1f; bubble.damageClass = DamageClass.Normal;
            bubble.attackRange = 1.2f; bubble.attackCooldown = 1.15f; bubble.projectileSpeed = 0f;
            bubble.moveSpeed = 4.1f; bubble.visionRadius = 7f;
            bubble.accentColor = new Color(0.65f, 0.95f, 1f); bubble.bodyScale = 0.72f;

            var poisonBubble = ScriptableObject.CreateInstance<UnitData>();
            poisonBubble.id = "poison_bubble"; poisonBubble.displayName = "Poison Bubble";
            poisonBubble.description = "Pops in one hit and bursts into a small poison cloud. Weak alone, useful in clumps.";
            poisonBubble.mineralCost = 0; poisonBubble.supplyCost = 0; poisonBubble.trainTime = 1f;
            poisonBubble.maxHP = 1; poisonBubble.armor = 0; poisonBubble.armorClass = ArmorClass.Light;
            poisonBubble.damage = 0.5f; poisonBubble.damageClass = DamageClass.Normal;
            poisonBubble.attackRange = 1.2f; poisonBubble.attackCooldown = 1.3f; poisonBubble.projectileSpeed = 0f;
            poisonBubble.moveSpeed = 4.0f; poisonBubble.visionRadius = 7f;
            poisonBubble.accentColor = new Color(0.35f, 1f, 0.35f); poisonBubble.bodyScale = 0.76f;

            var dot = ScriptableObject.CreateInstance<UnitData>();
            dot.id = "dot"; dot.displayName = "Dot";
            dot.description = "A tiny shape-droid. Weak alone, meant to gather into powered forms later.";
            dot.mineralCost = 0; dot.supplyCost = 0; dot.trainTime = 1f;
            dot.maxHP = 8; dot.armor = 0; dot.armorClass = ArmorClass.Light;
            dot.damage = 1.5f; dot.damageClass = DamageClass.Normal;
            dot.attackRange = 1.2f; dot.attackCooldown = 1.1f; dot.projectileSpeed = 0f;
            dot.moveSpeed = 4.3f; dot.visionRadius = 7f;
            dot.accentColor = new Color(1f, 0.65f, 0.25f); dot.bodyScale = 0.55f;

            var coreDot = ScriptableObject.CreateInstance<UnitData>();
            coreDot.id = "dot_core"; coreDot.displayName = "Core Dot";
            coreDot.description = "A larger power droid. It moves with the swarm, powers nearby Dot structures, and hides inside large shapes.";
            coreDot.mineralCost = 0; coreDot.supplyCost = 0; coreDot.trainTime = 1f;
            coreDot.maxHP = 220; coreDot.armor = 2; coreDot.armorClass = ArmorClass.Armored;
            coreDot.damage = 4f; coreDot.damageClass = DamageClass.Normal;
            coreDot.attackRange = 1.4f; coreDot.attackCooldown = 1.25f; coreDot.projectileSpeed = 0f;
            coreDot.moveSpeed = 3.1f; coreDot.visionRadius = 11f;
            coreDot.accentColor = new Color(1f, 0.78f, 0.25f); coreDot.bodyScale = 1.25f;

            var dotGiant = ScriptableObject.CreateInstance<UnitData>();
            dotGiant.id = "dot_giant"; dotGiant.displayName = "Dot Giant";
            dotGiant.description = "A powerful walking shape made from many Dots with the Core Dot hidden inside.";
            dotGiant.mineralCost = 0; dotGiant.supplyCost = 0; dotGiant.trainTime = 1f;
            dotGiant.maxHP = 720; dotGiant.armor = 3; dotGiant.armorClass = ArmorClass.Armored;
            dotGiant.damage = 42f; dotGiant.damageClass = DamageClass.Siege;
            dotGiant.attackRange = 2.0f; dotGiant.attackCooldown = 1.35f; dotGiant.projectileSpeed = 0f;
            dotGiant.moveSpeed = 2.25f; dotGiant.visionRadius = 12f;
            dotGiant.accentColor = new Color(1f, 0.5f, 0.16f); dotGiant.bodyScale = 2.45f;

            var dotKite = ScriptableObject.CreateInstance<UnitData>();
            dotKite.id = "dot_kite"; dotKite.displayName = "Dot Kite";
            dotKite.description = "A flying shape made of Dots. Hovers over the battlefield and pelts targets from range.";
            dotKite.mineralCost = 0; dotKite.supplyCost = 0; dotKite.trainTime = 1f;
            dotKite.maxHP = 55; dotKite.armor = 0; dotKite.armorClass = ArmorClass.Light;
            dotKite.damage = 9f; dotKite.damageClass = DamageClass.Piercing;
            dotKite.attackRange = 8.5f; dotKite.attackCooldown = 1.15f; dotKite.projectileSpeed = 30f;
            dotKite.moveSpeed = 4.6f; dotKite.visionRadius = 12f;
            dotKite.flying = true; dotKite.hoverHeight = 2.8f;
            dotKite.accentColor = new Color(0.6f, 0.9f, 1f); dotKite.bodyScale = 0.9f;

            var dotSpike = ScriptableObject.CreateInstance<UnitData>();
            dotSpike.id = "dot_spike"; dotSpike.displayName = "Dot Spike";
            dotSpike.description = "A ground shape of Dots bristling with needles. Long range, but fragile.";
            dotSpike.mineralCost = 0; dotSpike.supplyCost = 0; dotSpike.trainTime = 1f;
            dotSpike.maxHP = 45; dotSpike.armor = 0; dotSpike.armorClass = ArmorClass.Light;
            dotSpike.damage = 13f; dotSpike.damageClass = DamageClass.Piercing;
            dotSpike.attackRange = 10f; dotSpike.attackCooldown = 1.4f; dotSpike.projectileSpeed = 32f;
            dotSpike.moveSpeed = 3.2f; dotSpike.visionRadius = 12f;
            dotSpike.accentColor = new Color(1f, 0.75f, 0.35f); dotSpike.bodyScale = 0.95f;

            return new[] { worker, soldier, ranged, heavy, zergling, hydra, zealot, stalker, overlord, bubble, poisonBubble,
                dot, coreDot, dotGiant, dotKite, dotSpike };
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

            var sensor = ScriptableObject.CreateInstance<BuildingData>();
            sensor.id = "sensor"; sensor.displayName = "Sensor Tower";
            sensor.description = "Optional scout structure. No weapon, but reveals a wide area through fog.";
            sensor.hotkey = KeyCode.Y;
            sensor.mineralCost = 125; sensor.buildTime = 18f; sensor.maxHP = 450; sensor.armor = 0;
            sensor.sizeX = 3f; sensor.sizeZ = 3f;
            sensor.visionRadius = 24f;
            sensor.accentColor = new Color(0.35f, 0.95f, 1f);

            // ---- Bubble faction (structure-driven economy, no workers) ----

            var bubbleCore = ScriptableObject.CreateInstance<BuildingData>();
            bubbleCore.id = "bubble_core"; bubbleCore.displayName = "Bubble Nexus";
            bubbleCore.description = "Your home base. It automatically blows one Bubble every 7 seconds, gives 10 supply, and opens the bubble build menu.";
            bubbleCore.hotkey = KeyCode.Q;
            bubbleCore.mineralCost = 350; bubbleCore.buildTime = 26f; bubbleCore.maxHP = 1400; bubbleCore.armor = 1;
            bubbleCore.sizeX = 5f; bubbleCore.sizeZ = 5f;
            bubbleCore.supplyProvided = 10;
            bubbleCore.techGroup = "bubble"; bubbleCore.selfBuild = true;
            bubbleCore.passiveMineralsPerSec = 0.15f; bubbleCore.opensBuildMenu = true;
            bubbleCore.visionRadius = 13f;
            bubbleCore.accentColor = new Color(0.55f, 0.95f, 1f);

            var bubbleSpring = ScriptableObject.CreateInstance<BuildingData>();
            bubbleSpring.id = "bubble_spring"; bubbleSpring.displayName = "Bubble Spring";
            bubbleSpring.description = "Build it next to crystals to mine a slow mineral trickle. Grows itself.";
            bubbleSpring.hotkey = KeyCode.W;
            bubbleSpring.mineralCost = 100; bubbleSpring.buildTime = 14f; bubbleSpring.maxHP = 360; bubbleSpring.armor = 0;
            bubbleSpring.sizeX = 3f; bubbleSpring.sizeZ = 3f;
            bubbleSpring.techGroup = "bubble"; bubbleSpring.selfBuild = true;
            bubbleSpring.passiveMineralsPerSec = 0.6f; bubbleSpring.opensBuildMenu = true;
            bubbleSpring.visionRadius = 10f;
            bubbleSpring.accentColor = new Color(0.6f, 0.95f, 1f);

            var poisonPool = ScriptableObject.CreateInstance<BuildingData>();
            poisonPool.id = "poison_pool"; poisonPool.displayName = "Poison Pool";
            poisonPool.description = "Morphs your soap bubbles into poison bubbles that burst into toxic gas. Grows itself.";
            poisonPool.hotkey = KeyCode.E;
            poisonPool.mineralCost = 90; poisonPool.buildTime = 14f; poisonPool.maxHP = 320; poisonPool.armor = 0;
            poisonPool.sizeX = 3f; poisonPool.sizeZ = 3f;
            poisonPool.techGroup = "bubble"; poisonPool.selfBuild = true;
            poisonPool.opensBuildMenu = true;
            poisonPool.visionRadius = 9f;
            poisonPool.accentColor = new Color(0.35f, 1f, 0.35f);

            var foamTurret = ScriptableObject.CreateInstance<BuildingData>();
            foamTurret.id = "foam_turret"; foamTurret.displayName = "Foam Turret";
            foamTurret.description = "Bubble defense. Spits sticky foam globs at attackers. Grows itself.";
            foamTurret.hotkey = KeyCode.R;
            foamTurret.mineralCost = 100; foamTurret.buildTime = 13f; foamTurret.maxHP = 600; foamTurret.armor = 0;
            foamTurret.sizeX = 2f; foamTurret.sizeZ = 2f;
            foamTurret.techGroup = "bubble"; foamTurret.selfBuild = true;
            foamTurret.damage = 12f; foamTurret.damageClass = DamageClass.Normal;
            foamTurret.attackRange = 9f; foamTurret.attackCooldown = 1.0f; foamTurret.projectileSpeed = 26f;
            foamTurret.visionRadius = 11f;
            foamTurret.accentColor = new Color(0.6f, 0.95f, 1f);

            var aerator = ScriptableObject.CreateInstance<BuildingData>();
            aerator.id = "aerator"; aerator.displayName = "Aerator";
            aerator.description = "Select it and press UPGRADE to make your Bubble Nexus blow bubbles faster (7s down to about 3.5s). Grows itself.";
            aerator.hotkey = KeyCode.T;
            aerator.mineralCost = 100; aerator.buildTime = 12f; aerator.maxHP = 400; aerator.armor = 0;
            aerator.sizeX = 2f; aerator.sizeZ = 2f;
            aerator.techGroup = "bubble"; aerator.selfBuild = true;
            aerator.visionRadius = 8f;
            aerator.accentColor = new Color(0.7f, 0.9f, 1f);

            // ---- Dots prototype (shape-droid faction) ----

            var dotPrinter = ScriptableObject.CreateInstance<BuildingData>();
            dotPrinter.id = "dot_printer"; dotPrinter.displayName = "Dot Printer";
            dotPrinter.description = "Prints loose Dots on its own — no power needed. Dots are your raw material: spend them at a Shape Matrix to form Core Dots and Giants.";
            dotPrinter.hotkey = KeyCode.Q;
            dotPrinter.mineralCost = 120; dotPrinter.buildTime = 16f; dotPrinter.maxHP = 520; dotPrinter.armor = 0;
            dotPrinter.sizeX = 3f; dotPrinter.sizeZ = 3f;
            dotPrinter.techGroup = "dots"; dotPrinter.selfBuild = true; dotPrinter.opensBuildMenu = true;
            dotPrinter.visionRadius = 9f;
            dotPrinter.accentColor = new Color(1f, 0.72f, 0.35f);

            var shapeMatrix = ScriptableObject.CreateInstance<BuildingData>();
            shapeMatrix.id = "shape_matrix"; shapeMatrix.displayName = "Shape Matrix";
            shapeMatrix.description = "Dots tech building. Lets you spend Dots to form Core Dots and Dot Giants.";
            shapeMatrix.hotkey = KeyCode.W;
            shapeMatrix.mineralCost = 140; shapeMatrix.buildTime = 18f; shapeMatrix.maxHP = 560; shapeMatrix.armor = 0;
            shapeMatrix.sizeX = 3f; shapeMatrix.sizeZ = 3f;
            shapeMatrix.techGroup = "dots"; shapeMatrix.selfBuild = true; shapeMatrix.opensBuildMenu = true;
            shapeMatrix.visionRadius = 9f;
            shapeMatrix.accentColor = new Color(1f, 0.55f, 0.2f);

            return new[] { cc, depot, rax, fac, turret, sensor, bubbleCore, bubbleSpring, poisonPool, foamTurret, aerator,
                dotPrinter, shapeMatrix };
        }
    }
}
