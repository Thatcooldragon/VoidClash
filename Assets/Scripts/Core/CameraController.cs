using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace VoidClash
{
    /// <summary>Isometric RTS camera: WASD/arrow/edge-scroll pan, scroll zoom, clamped to map.
    /// When units are selected, A/S/H belong to unit commands, so panning uses arrows/edges.</summary>
    public class CameraController : MonoBehaviour
    {
        public Camera Cam { get; private set; }

        const float Pitch = 55f;
        const float Yaw = 45f;
        const float PanSpeed = 26f;
        const float EdgeSize = 8f;
        const float ZoomMin = 11f, ZoomMax = 34f;

        float _zoom = 21f;
        Vector3 _target; // point on ground the camera looks at
        float _shake;

        public void Init(Vector3 startFocus)
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            Cam = camGo.AddComponent<Camera>();
            Cam.fieldOfView = 42f;
            Cam.nearClipPlane = 0.5f;
            Cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();

            var extra = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            extra.renderPostProcessing = true;
            extra.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;

            _target = startFocus;
            Apply();
        }

        public void Focus(Vector3 worldPos)
        {
            _target = Clamp(worldPos);
            Apply();
        }

        public void PlayIntroPan(Vector3 from, Vector3 to, float seconds)
        {
            StopAllCoroutines();
            StartCoroutine(IntroPanRoutine(from, to, seconds));
        }

        IEnumerator IntroPanRoutine(Vector3 from, Vector3 to, float seconds)
        {
            float t = 0f;
            from = Clamp(from);
            to = Clamp(to);
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(0.01f, seconds)));
                _target = Vector3.Lerp(from, to, u);
                Apply();
                yield return null;
            }
            _target = to;
            Apply();
        }

        public void AddShake(float amount) => _shake = Mathf.Min(0.6f, _shake + amount);

        void Update()
        {
            if (Cam == null) return;
            if (G.Game != null && G.Game.IsPaused) return;

            Vector2 pan = Vector2.zero;

            bool unitsSelected = G.Selection != null && G.Selection.HasUnitsSelected;
            // WASD (when not bound to unit commands), arrows always
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) pan.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) pan.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) pan.x += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) pan.x -= 1f;
            if (!unitsSelected)
            {
                if (Input.GetKey(KeyCode.S)) pan.y -= 1f;
                if (Input.GetKey(KeyCode.A)) pan.x -= 1f;
            }

            // edge scroll (skip in batch/test mode where the mouse sits at 0,0)
            if (!Application.isBatchMode && Application.isFocused)
            {
                Vector3 mp = Input.mousePosition;
                if (mp.x >= 0f && mp.x <= Screen.width && mp.y >= 0f && mp.y <= Screen.height)
                {
                    if (mp.x < EdgeSize) pan.x -= 1f;
                    else if (mp.x > Screen.width - EdgeSize) pan.x += 1f;
                    if (mp.y < EdgeSize) pan.y -= 1f;
                    else if (mp.y > Screen.height - EdgeSize) pan.y += 1f;
                }
            }

            if (pan.sqrMagnitude > 0.01f)
            {
                pan = pan.normalized;
                var fwd = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
                var right = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
                float speed = PanSpeed * (_zoom / 26f);
                _target += (fwd * pan.y + right * pan.x) * speed * Time.unscaledDeltaTime;
                _target = Clamp(_target);
            }

            float scroll = Input.mouseScrollDelta.y;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Mathf.Abs(scroll) > 0.01f && !overUI)
                _zoom = Mathf.Clamp(_zoom - scroll * 2.2f, ZoomMin, ZoomMax);

            if (_shake > 0f) _shake = Mathf.Max(0f, _shake - Time.deltaTime * 1.5f);

            Apply();
        }

        Vector3 Clamp(Vector3 p)
        {
            float lim = MapBuilder.Half - 2f;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.z = Mathf.Clamp(p.z, -lim, lim);
            p.y = 0f;
            return p;
        }

        void Apply()
        {
            if (Cam == null) return;
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 offset = rot * Vector3.back * _zoom;
            Vector3 shakeOff = _shake > 0f
                ? new Vector3(Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f, 0f) * _shake
                : Vector3.zero;
            Cam.transform.position = _target + offset + shakeOff;
            Cam.transform.rotation = rot;
        }

        /// <summary>Ground-plane point currently at screen center (for the minimap view box).</summary>
        public Vector3 FocusPoint => _target;

        public bool ScreenToGround(Vector3 screenPos, out Vector3 world)
        {
            world = Vector3.zero;
            if (Cam == null) return false;
            var ray = Cam.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                world = ray.GetPoint(dist);
                return true;
            }
            return false;
        }
    }
}
