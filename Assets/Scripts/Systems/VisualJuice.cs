using UnityEngine;

namespace VoidClash
{
    /// <summary>Small presentation-only motion components for hand-built primitive art.</summary>
    public class UnitIdleMotion : MonoBehaviour
    {
        public float BobHeight = 0.045f;
        public float SwayDegrees = 1.8f;
        public float Rate = 2.15f;

        Vector3 _basePos;
        Quaternion _baseRot;
        float _seed;

        void Start()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _seed = Random.value * 40f;
        }

        void Update()
        {
            float wave = Mathf.Sin(Time.time * Rate + _seed);
            transform.localPosition = _basePos + Vector3.up * (wave * BobHeight);
            transform.localRotation = _baseRot * Quaternion.Euler(0f, wave * SwayDegrees, Mathf.Cos(Time.time * Rate * 0.7f + _seed) * SwayDegrees);
        }
    }

    public class WorldSpin : MonoBehaviour
    {
        public Vector3 Axis = Vector3.up;
        public float DegreesPerSecond = 32f;

        void Update()
        {
            transform.Rotate(Axis.normalized, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }

    public class WorldPulse : MonoBehaviour
    {
        public float Amount = 0.08f;
        public float Rate = 2.8f;

        Vector3 _baseScale;
        float _seed;

        void Start()
        {
            _baseScale = transform.localScale;
            _seed = Random.value * 30f;
        }

        void Update()
        {
            float t = 1f + Mathf.Sin(Time.time * Rate + _seed) * Amount;
            transform.localScale = _baseScale * t;
        }
    }

    public class AttackRecoil : MonoBehaviour
    {
        public float Distance = 0.18f;
        public float ReturnSpeed = 14f;

        Vector3 _basePos;
        Vector3 _kick;

        void Start()
        {
            _basePos = transform.localPosition;
        }

        public void Punch(Vector3 worldDir)
        {
            Vector3 local = transform.parent != null ? transform.parent.InverseTransformDirection(worldDir.normalized) : worldDir.normalized;
            _kick = -local * Distance;
        }

        void Update()
        {
            _kick = Vector3.Lerp(_kick, Vector3.zero, Time.deltaTime * ReturnSpeed);
            transform.localPosition = _basePos + _kick;
        }
    }

    public class OrbitingDots : MonoBehaviour
    {
        public float Radius = 0.85f;
        public float Rate = 70f;
        public string MaterialName = "rally";

        Transform _root;

        void Start()
        {
            _root = new GameObject("OrbitingDots").transform;
            _root.SetParent(transform, false);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "OrbitDot";
                Destroy(dot.GetComponent<Collider>());
                dot.transform.SetParent(_root, false);
                dot.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Radius + Vector3.up * 0.12f;
                dot.transform.localScale = Vector3.one * 0.15f;
                dot.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(MaterialName);
            }
        }

        void Update()
        {
            if (_root != null) _root.Rotate(0f, Rate * Time.deltaTime, 0f, Space.Self);
        }
    }

    public class TimedFadeScale : MonoBehaviour
    {
        float _life;
        float _total;
        Vector3 _from;
        Vector3 _to;

        public void Init(float life, Vector3 from, Vector3 to)
        {
            _life = _total = life;
            _from = from;
            _to = to;
            transform.localScale = _from;
        }

        void Update()
        {
            _life -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_life / _total);
            transform.localScale = Vector3.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, t));
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
