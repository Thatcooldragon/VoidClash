using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>A harvestable crystal formation.</summary>
    public class MineralNode : MonoBehaviour
    {
        public static readonly List<MineralNode> All = new List<MineralNode>();

        public int Amount { get; private set; }
        public bool Depleted => Amount <= 0;
        public float Radius => 1.1f;

        Vector3 _fullScale;

        public static void ClearRegistry() => All.Clear();

        public void Init(int amount)
        {
            Amount = amount;
            _fullScale = transform.localScale;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public int Harvest(int want)
        {
            int got = Mathf.Min(want, Amount);
            Amount -= got;
            if (Amount <= 0) OnDepleted();
            else
            {
                float f = Mathf.Lerp(0.55f, 1f, Amount / 1200f);
                transform.localScale = _fullScale * Mathf.Clamp(f, 0.55f, 1f);
            }
            return got;
        }

        void OnDepleted()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.sharedMaterial = MaterialLibrary.Get("crystal_dim");
            transform.localScale = _fullScale * 0.4f;
            All.Remove(this);
        }

        public static MineralNode FindNearest(Vector3 pos, float maxDist = float.MaxValue)
        {
            MineralNode best = null;
            float bestD = maxDist * maxDist;
            foreach (var n in All)
            {
                if (n == null || n.Depleted) continue;
                float d = (n.transform.position - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = n; }
            }
            return best;
        }
    }
}
