using UnityEngine;

namespace VoidClash
{
    /// <summary>Flat ring decal under a unit/building: bright when selected, faint on hover.</summary>
    public class SelectionRing : MonoBehaviour
    {
        Renderer _rend;
        Faction _faction;
        bool _selected, _hovered;

        public static SelectionRing Attach(Entity owner, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SelectionRing";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(owner.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * (radius * 2.4f);
            go.layer = LayerMask.NameToLayer("FX");
            var ring = go.AddComponent<SelectionRing>();
            ring._rend = go.GetComponent<Renderer>();
            ring._rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring._rend.receiveShadows = false;
            ring._faction = owner.Faction;
            ring.Apply();
            return ring;
        }

        public void SetSelected(bool v) { _selected = v; Apply(); }
        public void SetHovered(bool v) { _hovered = v; Apply(); }

        void Apply()
        {
            if (_rend == null) return;
            if (_selected)
            {
                _rend.enabled = true;
                _rend.sharedMaterial = MaterialLibrary.Get(_faction == Faction.Enemy ? "ring_enemy" : "ring_player");
            }
            else if (_hovered)
            {
                _rend.enabled = true;
                _rend.sharedMaterial = MaterialLibrary.Get("ring_hover");
            }
            else _rend.enabled = false;
        }
    }
}
