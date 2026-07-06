using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    public class BubbleSystem : MonoBehaviour
    {
        readonly Dictionary<Building, float> _spawnTimers = new Dictionary<Building, float>();
        const float MineralLinkRange = 8f;
        const float SpawnEvery = 3.5f;
        const float PoolMorphRange = 6f;
        const int BubbleSoftCap = 55;

        void OnEnable() => Entity.AnyDied += OnEntityDied;
        void OnDisable() => Entity.AnyDied -= OnEntityDied;

        void Update()
        {
            if (G.Game == null || G.Game.IsPaused || G.Game.IsOver || G.DB == null) return;
            TickSprings();
            TickPoisonPools();
        }

        void TickSprings()
        {
            var snapshot = new List<Entity>(Entity.All);
            foreach (var e in snapshot)
            {
                if (!(e is Building b) || b.IsDead || !b.IsComplete || b.Data.id != "bubble_spring") continue;
                if (!LinkedToMinerals(b.Position)) continue;

                _spawnTimers.TryGetValue(b, out float timer);
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = SpawnEvery;
                    if (CountBubbles(b.Faction) < BubbleSoftCap)
                        SpawnBubble(b);
                }
                _spawnTimers[b] = timer;
            }
        }

        bool LinkedToMinerals(Vector3 pos)
        {
            foreach (var node in MineralNode.All)
                if (node != null && !node.Depleted && Vector3.Distance(node.transform.position, pos) <= MineralLinkRange)
                    return true;
            return false;
        }

        void SpawnBubble(Building spring)
        {
            var data = G.DB.Unit("bubble");
            Vector3 pos = spring.Position + Random.insideUnitSphere * 2.2f;
            pos.y = 0f;
            var bubble = UnitFactory.Spawn(data, spring.Faction, pos);
            if (bubble == null) return;
            bubble.CommandAttackMove(spring.Faction == Faction.Player ? MapBuilder.EnemyBasePos : MapBuilder.PlayerBasePos);
            if (spring.Faction == Faction.Player && G.Audio != null) G.Audio.Play("deposit", 0.25f);
        }

        void TickPoisonPools()
        {
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
            foreach (var e in Entity.All)
            {
                if (!(e is Unit u) || u.IsDead || u.Faction != pool.Faction || u.Data.id != "bubble") continue;
                if (Vector3.Distance(u.Position, pool.Position) <= PoolMorphRange)
                {
                    target = u;
                    break;
                }
            }
            if (target == null) return;

            Vector3 pos = target.Position;
            Destroy(target.gameObject);
            var poison = UnitFactory.Spawn(G.DB.Unit("poison_bubble"), pool.Faction, pos);
            if (poison != null)
                poison.CommandAttackMove(pool.Faction == Faction.Player ? MapBuilder.EnemyBasePos : MapBuilder.PlayerBasePos);
            if (pool.Faction == Faction.Player && G.Hud != null) G.Hud.Notify("Bubble filled with poison gas");
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
