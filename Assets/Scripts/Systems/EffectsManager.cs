using UnityEngine;

namespace VoidClash
{
    /// <summary>Spawns all particle effects. Systems are built in code from the URP particle material.</summary>
    public class EffectsManager : MonoBehaviour
    {
        ParticleSystem MakeSystem(string name, Color color, float size, float speed, float life, bool gravity = false)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("FX");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startSize = size;
            main.startSpeed = speed;
            main.startLifetime = life;
            main.gravityModifier = gravity ? 0.6f : 0f;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colOverLife = ps.colorOverLifetime;
            colOverLife.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.4f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colOverLife.color = grad;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = MaterialLibrary.Get("particle_add");
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return ps;
        }

        void Burst(string name, Vector3 pos, Color color, int count, float size, float speed, float life, bool gravity = false, float lightIntensity = 0f)
        {
            var ps = MakeSystem(name, color, size, speed, life, gravity);
            ps.transform.position = pos;
            ps.Emit(count);
            if (lightIntensity > 0f)
            {
                var lightGo = new GameObject("flash");
                lightGo.transform.position = pos + Vector3.up * 0.5f;
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.intensity = lightIntensity;
                light.range = 6f + lightIntensity;
                lightGo.AddComponent<FadeAndDie>().Init(0.25f);
                lightGo.transform.SetParent(ps.transform);
            }
            Destroy(ps.gameObject, life + 0.6f);
        }

        static Color FactionGlow(Faction f) => f == Faction.Player ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.5f, 0.3f);

        public void SpawnMuzzleFlash(Vector3 pos, Faction f)
            => Burst("fx_muzzle", pos, FactionGlow(f), 6, 0.16f, 2.5f, 0.15f, false, 1.2f);

        public void SpawnImpact(Vector3 pos, Faction f)
            => Burst("fx_impact", pos, FactionGlow(f), 10, 0.14f, 3.5f, 0.3f);

        public void SpawnTracer(Vector3 from, Vector3 to, Faction f)
        {
            var go = new GameObject("fx_tracer");
            go.layer = LayerMask.NameToLayer("FX");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.06f;
            lr.endWidth = 0.02f;
            lr.material = MaterialLibrary.Get(f == Faction.Player ? "projectile_player" : "projectile_enemy");
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<FadeAndDie>().Init(0.09f);
        }

        public void SpawnExplosion(Vector3 pos, float scale, Faction f)
        {
            Burst("fx_explosion_fire", pos, new Color(1f, 0.55f, 0.15f), (int)(24 * scale), 0.5f * scale, 5f * scale, 0.6f, false, 3f * scale);
            Burst("fx_explosion_smoke", pos, new Color(0.25f, 0.24f, 0.26f), (int)(14 * scale), 0.9f * scale, 2f, 1.2f);
            Burst("fx_explosion_sparks", pos, new Color(1f, 0.85f, 0.4f), (int)(16 * scale), 0.12f, 9f * scale, 0.5f, true);
            if (G.Cam != null && Vector3.Distance(G.Cam.FocusPoint, pos) < 30f)
                G.Cam.AddShake(0.12f * scale);
        }

        public void SpawnHarvestSparkle(Vector3 pos)
            => Burst("fx_harvest", pos, new Color(0.4f, 0.95f, 1f), 3, 0.1f, 1.4f, 0.4f);

        public void SpawnHealBurst(Vector3 pos)
            => Burst("fx_heal", pos, new Color(0.4f, 1f, 0.5f), 10, 0.18f, 2.2f, 0.7f, false, 1.2f);

        public void SpawnFrostBurst(Vector3 pos)
            => Burst("fx_frost", pos + Vector3.up * 0.4f, new Color(0.6f, 0.9f, 1f), 24, 0.22f, 4f, 0.9f, false, 1.6f);

        public ParticleSystem AttachConstructionDust(Transform parent, float size)
        {
            var ps = MakeSystem("fx_construction", new Color(0.55f, 0.5f, 0.45f), 0.55f, 1.2f, 1.1f);
            var main = ps.main;
            main.loop = true;
            var emission = ps.emission;
            emission.rateOverTime = 8f * size * 0.5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size, 0.3f, size);
            ps.transform.SetParent(parent, false);
            ps.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            ps.Play();
            return ps;
        }

        public void SpawnMoveMarker(Vector3 pos, bool attack)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = attack ? "fx_attackmarker" : "fx_movemarker";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos + Vector3.up * 0.08f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * 1.4f;
            go.layer = LayerMask.NameToLayer("FX");
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialLibrary.Get(attack ? "marker_attack" : "marker_move");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<ShrinkAndDie>().Init(0.6f);
        }

        public void SpawnPowerMarker(Vector3 pos, float radius, bool hostile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = hostile ? "fx_power_hostile" : "fx_power_support";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos + Vector3.up * 0.09f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * (radius * 2f);
            go.layer = LayerMask.NameToLayer("FX");
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = MaterialLibrary.Get(hostile ? "marker_attack" : "marker_move");
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<PulseAndDie>().Init(1.35f, 0.82f, 1.08f);

            var lightGo = new GameObject("PowerMarkerGlow");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.5f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = hostile ? new Color(1f, 0.28f, 0.2f) : new Color(0.35f, 1f, 0.55f);
            light.intensity = hostile ? 1.6f : 1.1f;
            light.range = radius * 1.6f;
            lightGo.AddComponent<FadeAndDie>().Init(1.35f);
        }
    }

    public class FadeAndDie : MonoBehaviour
    {
        float _life, _total;
        Light _light;
        float _startIntensity;

        public void Init(float life)
        {
            _life = _total = life;
            _light = GetComponent<Light>();
            if (_light != null) _startIntensity = _light.intensity;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            if (_light != null) _light.intensity = _startIntensity * Mathf.Clamp01(_life / _total);
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    public class ShrinkAndDie : MonoBehaviour
    {
        float _life, _total;
        Vector3 _startScale;

        public void Init(float life) { _life = _total = life; _startScale = transform.localScale; }

        void Update()
        {
            _life -= Time.deltaTime;
            float t = Mathf.Clamp01(_life / _total);
            transform.localScale = _startScale * (0.4f + 0.6f * t);
            if (_life <= 0f) Destroy(gameObject);
        }
    }

    public class PulseAndDie : MonoBehaviour
    {
        float _life, _total, _from, _to;
        Vector3 _startScale;

        public void Init(float life, float from, float to)
        {
            _life = _total = life;
            _from = from;
            _to = to;
            _startScale = transform.localScale;
            transform.localScale = _startScale * _from;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_life / _total);
            float pulse = Mathf.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = _startScale * pulse;
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
