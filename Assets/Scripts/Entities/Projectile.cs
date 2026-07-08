using UnityEngine;

namespace VoidClash
{
    /// <summary>Homing energy bolt with a trail. Applies damage on arrival.</summary>
    public class Projectile : MonoBehaviour
    {
        Entity _owner;
        Entity _target;
        Vector3 _lastTargetPos;
        float _damage;
        DamageClass _dc;
        float _speed;
        Faction _faction;
        float _life;

        public static void Spawn(Entity owner, Entity target, Vector3 from, float damage, DamageClass dc, float speed)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.22f;
            go.layer = LayerMask.NameToLayer("FX");
            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = MaterialLibrary.Get(owner.Faction == Faction.Player ? "projectile_player" : "projectile_enemy");
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.16f;
            trail.endWidth = 0.02f;
            trail.material = MaterialLibrary.Get(owner.Faction == Faction.Player ? "projectile_player" : "projectile_enemy");
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var p = go.AddComponent<Projectile>();
            p._owner = owner;
            p._target = target;
            p._lastTargetPos = target.Position + Vector3.up * 0.5f;
            p._damage = damage;
            p._dc = dc;
            p._speed = speed;
            p._faction = owner.Faction;
        }

        void Update()
        {
            _life += Time.deltaTime;
            if (_life > 6f) { Destroy(gameObject); return; }

            if (_target != null && !_target.IsDead)
                _lastTargetPos = _target.Position + Vector3.up * 0.5f;

            Vector3 to = _lastTargetPos - transform.position;
            float step = _speed * Time.deltaTime;
            if (to.magnitude <= step + 0.25f)
            {
                if (_target != null && !_target.IsDead && _target.Health != null)
                    _target.Health.TakeDamage(_damage, _dc, _owner != null && !_owner.IsDead ? _owner : null);
                if (G.Effects != null) G.Effects.SpawnDamageImpact(_lastTargetPos, _faction, _dc);
                Destroy(gameObject);
                return;
            }
            transform.position += to.normalized * step;
        }
    }
}
