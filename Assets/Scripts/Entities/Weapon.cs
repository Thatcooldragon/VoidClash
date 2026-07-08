using UnityEngine;

namespace VoidClash
{
    /// <summary>Shared firing logic for units and turrets: cooldown, projectile or instant hit,
    /// muzzle flash + sound.</summary>
    public class Weapon : MonoBehaviour
    {
        Entity _owner;
        float _damage;
        DamageClass _dc;
        float _cooldown;
        float _projectileSpeed;
        Transform _muzzle;
        bool _melee;
        float _nextFire;
        float _cooldownScale = 1f; // Overdrive shortens this

        public float Range { get; private set; }

        /// <summary>Overdrive/stim scales the fire cooldown (1 = normal, &lt;1 = faster).</summary>
        public void SetCooldownScale(float scale) => _cooldownScale = Mathf.Clamp(scale, 0.1f, 3f);

        public void Init(Entity owner, float damage, DamageClass dc, float range, float cooldown,
            float projectileSpeed, Transform muzzle, bool melee)
        {
            _owner = owner;
            _damage = damage;
            _dc = dc;
            Range = range;
            _cooldown = cooldown;
            _projectileSpeed = projectileSpeed;
            _muzzle = muzzle != null ? muzzle : owner.transform;
            _melee = melee;
        }

        public bool Ready => Time.time >= _nextFire;

        public bool InRange(Entity target)
        {
            if (target == null || target.IsDead) return false;
            float d = Vector3.Distance(_owner.Position, target.Position);
            return d <= Range + target.Radius + 0.2f;
        }

        /// <summary>Fires if ready and in range. Returns true if a shot happened.</summary>
        public bool TryFire(Entity target)
        {
            if (!Ready || !InRange(target)) return false;
            _nextFire = Time.time + _cooldown * _cooldownScale;

            Vector3 muzzlePos = _muzzle.position;
            bool seen = _owner.Faction == Faction.Player || _owner.VisibleToPlayer;

            if (_melee)
            {
                target.Health.TakeDamage(_damage, _dc, _owner);
                if (G.Effects != null && seen)
                {
                    G.Effects.SpawnMeleeArc(muzzlePos, target.Position + Vector3.up * 0.5f, _owner.Faction);
                    G.Effects.SpawnDamageImpact(target.Position + Vector3.up * 0.5f, _owner.Faction, _dc);
                }
                if (G.Audio != null && seen) G.Audio.PlayAt("melee", target.Position);
            }
            else if (_projectileSpeed > 0.5f)
            {
                Projectile.Spawn(_owner, target, muzzlePos, _damage, _dc, _projectileSpeed);
                if (G.Effects != null && seen) G.Effects.SpawnMuzzleFlash(muzzlePos, _owner.Faction);
                if (G.Audio != null && seen) G.Audio.PlayAt("fire_heavy", muzzlePos);
            }
            else
            {
                // hitscan
                target.Health.TakeDamage(_damage, _dc, _owner);
                if (G.Effects != null && seen)
                {
                    G.Effects.SpawnMuzzleFlash(muzzlePos, _owner.Faction);
                    G.Effects.SpawnTracer(muzzlePos, target.Position + Vector3.up * 0.5f, _owner.Faction);
                    G.Effects.SpawnDamageImpact(target.Position + Vector3.up * 0.5f, _owner.Faction, _dc);
                }
                if (G.Audio != null && seen) G.Audio.PlayAt("fire", muzzlePos);
            }
            return true;
        }
    }
}
