using UnityEngine;

namespace VoidClash
{
    /// <summary>Cheap billboarded quad health bar (no canvas). Shows when damaged or selected.
    /// Also doubles as the construction/training progress bar (blue fill).</summary>
    public class HealthBar : MonoBehaviour
    {
        Transform _fill;
        Transform _progFill;
        GameObject _progRoot;
        Renderer _fillRend;
        Health _health;
        Faction _faction;
        float _width;
        bool _selected;
        bool _suppressed;
        float _progress = -1f; // <0 = hidden

        public static HealthBar Attach(Entity owner, Health health, float width, float heightOffset)
        {
            var root = new GameObject("HealthBar");
            root.transform.SetParent(owner.transform, false);
            root.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            var bar = root.AddComponent<HealthBar>();
            root.AddComponent<Billboard>();
            bar._health = health;
            bar._faction = owner.Faction;
            bar._width = width;
            bar.BuildVisual();
            health.Changed += bar.Refresh;
            owner.GetType(); // keep signature simple
            bar.Refresh();
            return bar;
        }

        static GameObject MakeQuad(Transform parent, string name, Material mat, float w, float h, float z)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.transform.localScale = new Vector3(w, h, 1f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.layer = LayerMask.NameToLayer("FX");
            return go;
        }

        void BuildVisual()
        {
            float h = 0.14f;
            MakeQuad(transform, "back", MaterialLibrary.Get("hp_back"), _width, h, 0.01f);
            var fillGo = MakeQuad(transform, "fill", MaterialLibrary.Get("hp_green"), _width, h * 0.75f, 0f);
            _fill = fillGo.transform;
            _fillRend = fillGo.GetComponent<Renderer>();

            _progRoot = new GameObject("progress");
            _progRoot.transform.SetParent(transform, false);
            _progRoot.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            MakeQuad(_progRoot.transform, "pback", MaterialLibrary.Get("hp_back"), _width, h, 0.01f);
            var pf = MakeQuad(_progRoot.transform, "pfill", MaterialLibrary.Get("hp_build"), _width, h * 0.75f, 0f);
            _progFill = pf.transform;
            _progRoot.SetActive(false);
        }

        void SetFill(Transform fill, float frac)
        {
            frac = Mathf.Clamp01(frac);
            fill.localScale = new Vector3(_width * frac, fill.localScale.y, 1f);
            fill.localPosition = new Vector3(-_width * 0.5f * (1f - frac), fill.localPosition.y, fill.localPosition.z);
        }

        public void Refresh()
        {
            if (_health == null || _fill == null) return;
            float f = _health.Fraction;
            SetFill(_fill, f);
            string matName = _faction == Faction.Enemy ? "hp_red" : (f > 0.6f ? "hp_green" : (f > 0.3f ? "hp_yellow" : "hp_red"));
            _fillRend.sharedMaterial = MaterialLibrary.Get(matName);
            UpdateVisibility();
        }

        public void SetProgress(float p)
        {
            _progress = p;
            if (_progRoot == null) return;
            bool show = p >= 0f && p < 1f && !_suppressed;
            if (_progRoot.activeSelf != show) _progRoot.SetActive(show);
            if (show) SetFill(_progFill, p);
            UpdateVisibility();
        }

        public void SetSelected(bool sel) { _selected = sel; UpdateVisibility(); }
        public void SetSuppressed(bool sup) { _suppressed = sup; UpdateVisibility(); }

        void UpdateVisibility()
        {
            bool damaged = _health != null && _health.Fraction < 0.999f;
            bool progressing = _progress >= 0f && _progress < 1f;
            bool show = !_suppressed && (_selected || damaged || progressing);
            for (int i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i).gameObject;
                if (c == _progRoot) c.SetActive(show && progressing);
                else if (c.activeSelf != show) c.SetActive(show);
            }
        }
    }
}
