using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Drives the Bubble faction: structure-driven economy (springs drain nearby
    /// minerals into income + a stream of soap bubbles), a passive Nexus trickle, Poison Pool
    /// morphing, and poison-gas bursts. Bubbles are free chaff that gather at their spring's
    /// rally point (or idle for the player to command); enemy-owned bubbles still push out.</summary>
    public class BubbleSystem : MonoBehaviour
    {
        readonly Dictionary<Building, float> _spawnTimers = new Dictionary<Building, float>();
        readonly Dictionary<int, float> _mineralFraction = new Dictionary<int, float>();
        const float MineralLinkRange = 9f;
        const float SpawnEvery = 2.6f;
        const float PoolMorphRange = 7f;
        const float MorphEvery = 1.6f;
        const int BubbleSoftCap = 60;
        float _morphTimer;

        void OnEnable() => Entity.AnyDied += OnEntityDied;
        void OnDisable() => Entity.AnyDied -= OnEntityDied;

        void Update()
        {
            if (G.Game == null || G.Game.IsPaused || G.Game.IsOver || G.DB == null) return;
            TickEconomyAndSprings();
            TickPoisonPools();
        }

        // ---- economy + bubble production ----

        void TickEconomyAndSprings()
        {
            float dt = Time.deltaTime;
            var snapshot = new List<Entity>(Entity.All);
            foreach (var e in snapshot)
            {
                if (!(e is Building b) || b.IsDead || !b.IsComplete) continue;
                if (b.Data.passiveMineralsPerSec <= 0f) continue;

                if (b.Data.id == "bubble_spring")
                {
                    var node = NearestLiveNode(b.Position, MineralLinkRange);
                    if (node == null) continue; // a spring only works while linked to minerals
                    Accrue(b.Faction, b.Data.passiveMineralsPerSec * dt, node);

                    _spawnTimers.TryGetValue(b, out float timer);
                    timer -= dt;
                    if (timer <= 0f)
                    {
                        timer = SpawnEvery;
                        if (CountBubbles(b.Faction) < BubbleSoftCap) SpawnBubble(b);
                    }
                    _spawnTimers[b] = timer;
                }
                else
                {
                    // Bubble Nexus and other structures: flat trickle, no node needed
                    Accrue(b.Faction, b.Data.passiveMineralsPerSec * dt, null);
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

        void SpawnBubble(Building spring)
        {
            var data = G.DB.Unit("bubble");
            Vector3 pos = spring.Position + Random.insideUnitSphere * 2.2f;
            pos.y = 0f;
            var bubble = UnitFactory.Spawn(data, spring.Faction, pos);
            if (bubble == null) return;
            SendNewBubble(bubble, spring);
            if (spring.Faction == Faction.Player && G.Audio != null) G.Audio.Play("deposit", 0.2f);
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
        float _life = 4.5f;
        float _tick;
        const float Radius = 4.2f;

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
            _tick = 0.45f;

            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction == _owner || e.Faction == Faction.Neutral) continue;
                if (Vector3.Distance(e.Position, transform.position) <= Radius)
                    e.Health.TakeDamage(4f, DamageClass.Normal, null);
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
