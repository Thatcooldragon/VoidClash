using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Drives the Bubble faction: the Bubble Nexus blows bubbles on an upgradeable
    /// timer (Aerators speed it up), Bubble Springs slowly mine minerals from nearby crystals,
    /// Poison Pools morph bubbles, and poison bubbles burst into gas. Bubbles gather at the
    /// Nexus for the player to command; enemy-owned bubbles push out.</summary>
    public class BubbleSystem : MonoBehaviour
    {
        readonly Dictionary<Building, float> _spawnTimers = new Dictionary<Building, float>();
        readonly Dictionary<int, float> _mineralFraction = new Dictionary<int, float>();
        const float MineralLinkRange = 9f;
        const float PoolMorphRange = 7f;
        const float MorphEvery = 2.2f;
        const int BubbleSoftCap = 24;
        float _morphTimer;

        // ---- upgradeable bubble production (per-faction Aerator tech) ----
        const float BaseSpawnEvery = 7f;   // seconds between bubbles at level 0
        const float MinSpawnEvery = 3.5f;  // fastest possible
        const float StepPerLevel = 0.7f;   // each Aerator upgrade shaves this off
        public const int MaxProductionLevel = 5;

        public int ProductionLevel { get; private set; }
        public float ProductionInterval => Mathf.Max(MinSpawnEvery, BaseSpawnEvery - ProductionLevel * StepPerLevel);
        public int NextUpgradeCost => 60 + ProductionLevel * 45;
        public bool ProductionMaxed => ProductionLevel >= MaxProductionLevel;

        /// <summary>Interval the Nexus would reach after one more upgrade (for UI preview).</summary>
        public float PreviewNextInterval()
        {
            int lvl = Mathf.Min(ProductionLevel + 1, MaxProductionLevel);
            return Mathf.Max(MinSpawnEvery, BaseSpawnEvery - lvl * StepPerLevel);
        }

        /// <summary>Player-facing upgrade from an Aerator: spends minerals, speeds up the Nexus.</summary>
        public bool TryUpgradeProduction(Faction faction, out string message)
        {
            if (ProductionMaxed) { message = "Bubble speed already maxed"; return false; }
            int cost = NextUpgradeCost;
            if (!G.Bank(faction).TrySpend(cost)) { message = $"Need {cost} minerals to upgrade"; return false; }
            ProductionLevel++;
            message = $"Bubble speed upgraded — now every {ProductionInterval:0.0}s";
            return true;
        }

        void OnEnable() => Entity.AnyDied += OnEntityDied;
        void OnDisable() => Entity.AnyDied -= OnEntityDied;

        void Update()
        {
            if (G.Game == null || G.Game.IsPaused || G.Game.IsOver || G.DB == null) return;
            TickEconomyAndProduction();
            TickPoisonPools();
        }

        // ---- economy (springs) + bubble production (nexus) ----

        void TickEconomyAndProduction()
        {
            float dt = Time.deltaTime;
            var snapshot = new List<Entity>(Entity.All);
            foreach (var e in snapshot)
            {
                if (!(e is Building b) || b.IsDead || !b.IsComplete) continue;

                if (b.Data.id == "bubble_core")
                {
                    // the Nexus blows a bubble on the upgradeable timer + a tiny mineral trickle
                    if (b.Data.passiveMineralsPerSec > 0f) Accrue(b.Faction, b.Data.passiveMineralsPerSec * dt, null);

                    _spawnTimers.TryGetValue(b, out float timer);
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        timer = ProductionInterval;
                        if (CountBubbles(b.Faction) < BubbleSoftCap) SpawnBubble(b);
                    }
                    _spawnTimers[b] = timer;
                }
                else if (b.Data.id == "bubble_spring" && b.Data.passiveMineralsPerSec > 0f)
                {
                    // a spring mines minerals only while linked to live crystals
                    var node = NearestLiveNode(b.Position, MineralLinkRange);
                    if (node != null) Accrue(b.Faction, b.Data.passiveMineralsPerSec * dt, node);
                }
            }
        }

        /// <summary>Adds fractional minerals to a faction, draining a node when one is given.</summary>
        void Accrue(Faction f, float amount, MineralNode drainFrom)
        {
            int key = (int)f;
            _mineralFraction.TryGetValue(key, out float frac);
            frac += amount;
            int whole = Mathf.FloorToInt(frac);
            if (whole > 0)
            {
                frac -= whole;
                if (drainFrom != null) whole = drainFrom.Harvest(whole); // deplete the crystals
                if (whole > 0) G.Bank(f).AddMinerals(whole);
            }
            _mineralFraction[key] = frac;
        }

        static MineralNode NearestLiveNode(Vector3 pos, float range)
        {
            MineralNode best = null;
            float bestD = range * range;
            foreach (var node in MineralNode.All)
            {
                if (node == null || node.Depleted) continue;
                float d = (node.transform.position - pos).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = node; }
            }
            return best;
        }

        void SpawnBubble(Building source)
        {
            var data = G.DB.Unit("bubble");
            Vector3 pos = source.Position + Random.insideUnitSphere * 2.4f;
            pos.y = 0f;
            var bubble = UnitFactory.Spawn(data, source.Faction, pos);
            if (bubble == null) return;
            SendNewBubble(bubble, source);
            if (source.Faction == Faction.Player && G.Audio != null) G.Audio.Play("deposit", 0.2f);
        }

        /// <summary>Fresh bubbles follow the structure's rally point if set; an enemy AI pushes
        /// toward the player; otherwise a player bubble rallies home to the Bubble Nexus, where
        /// the swarm collects (and a Poison Pool can morph it) ready for you to command.</summary>
        static void SendNewBubble(Unit bubble, Building source)
        {
            if (source.RallyPoint.HasValue) { bubble.CommandAttackMove(source.RallyPoint.Value); return; }
            if (source.Faction == Faction.Enemy) { bubble.CommandAttackMove(MapBuilder.PlayerBasePos); return; }
            bubble.CommandMove(GatherPoint(source.Faction));
        }

        /// <summary>Where a faction's ownerless bubbles collect: just in front of its Bubble Nexus.</summary>
        static Vector3 GatherPoint(Faction f)
        {
            foreach (var e in Entity.All)
                if (e is Building b && !b.IsDead && b.Faction == f && b.Data.id == "bubble_core")
                    return b.Position + (Vector3.zero - b.Position).normalized * 4f;
            return f == Faction.Player ? MapBuilder.PlayerBasePos : MapBuilder.EnemyBasePos;
        }

        // ---- poison morphing ----

        void TickPoisonPools()
        {
            _morphTimer -= Time.deltaTime;
            if (_morphTimer > 0f) return;
            _morphTimer = MorphEvery;

            var snapshot = new List<Entity>(Entity.All);
            foreach (var e in snapshot)
            {
                if (!(e is Building pool) || pool.IsDead || !pool.IsComplete || pool.Data.id != "poison_pool") continue;
                MorphNearbyBubble(pool);
            }
        }

        void MorphNearbyBubble(Building pool)
        {
            Unit target = null;
            float range2 = PoolMorphRange * PoolMorphRange;
            foreach (var e in Entity.All)
            {
                if (!(e is Unit u) || u.IsDead || u.Faction != pool.Faction || u.Data.id != "bubble") continue;
                if ((u.Position - pool.Position).sqrMagnitude <= range2) { target = u; break; }
            }
            if (target == null) return;

            Vector3 pos = target.Position;
            Destroy(target.gameObject);
            var poison = UnitFactory.Spawn(G.DB.Unit("poison_bubble"), pool.Faction, pos);
            if (poison != null) SendNewBubble(poison, pool);
            if (pool.Faction == Faction.Player && G.Audio != null) G.Audio.Play("deposit", 0.18f);
        }

        int CountBubbles(Faction faction)
        {
            int count = 0;
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.Faction == faction &&
                    (u.Data.id == "bubble" || u.Data.id == "poison_bubble"))
                    count++;
            return count;
        }

        void OnEntityDied(Entity e)
        {
            if (e is Unit u && u.Data != null && u.Data.id == "poison_bubble")
                PoisonGas.Spawn(u.Position, u.Faction);
        }
    }

    public class PoisonGas : MonoBehaviour
    {
        Faction _owner;
        float _life = 3.2f;
        float _tick;
        const float Radius = 3.0f;

        public static void Spawn(Vector3 pos, Faction owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "PoisonGas";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos + Vector3.up * 0.08f;
            go.transform.localScale = new Vector3(Radius, 0.06f, Radius);
            go.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("zerg_accent");
            var gas = go.AddComponent<PoisonGas>();
            gas._owner = owner;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) { Destroy(gameObject); return; }

            float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.08f;
            transform.localScale = new Vector3(Radius * pulse, 0.06f, Radius * pulse);
            transform.Rotate(0f, 45f * Time.deltaTime, 0f);

            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            _tick = 0.75f;

            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction == _owner || e.Faction == Faction.Neutral) continue;
                if (Vector3.Distance(e.Position, transform.position) <= Radius)
                    e.Health.TakeDamage(1.5f, DamageClass.Normal, null);
            }
        }
    }

    public class BubbleWobble : MonoBehaviour
    {
        Vector3 _basePos;
        Vector3 _baseScale;
        float _seed;

        void Start()
        {
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
            _seed = Random.value * 20f;
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * 3.2f + _seed);
            float stretch = Mathf.Sin(Time.time * 4.7f + _seed * 0.7f) * 0.08f;
            transform.localPosition = _basePos + Vector3.up * (0.12f + bob * 0.13f);
            transform.localScale = new Vector3(
                _baseScale.x * (1f + stretch),
                _baseScale.y * (1f - stretch * 0.75f),
                _baseScale.z * (1f + stretch));
            transform.Rotate(0f, 24f * Time.deltaTime, 0f);
        }
    }
}
