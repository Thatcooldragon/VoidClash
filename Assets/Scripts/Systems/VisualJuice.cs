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
}
