using UnityEngine;

namespace VoidClash
{
    /// <summary>Grid-based fog of war: unexplored = black, explored = dim, visible = clear.
    /// Rendered as a big transparent quad above the map; enemy entities are hidden per-cell.</summary>
    public class FogOfWar : MonoBehaviour
    {
        public const int Res = 160;                       // cells per side
        const float CellSize = MapBuilder.Size / Res;     // 0.5 world units
        const float UpdateInterval = 0.18f;

        byte[] _explored;       // 0/1
        byte[] _visible;        // 0/1
        Texture2D _tex;
        Color32[] _px;
        float _timer;
        Renderer _quadRend;

        static readonly Color32 Unexplored = new Color32(2, 3, 6, 245);
        static readonly Color32 Explored = new Color32(2, 3, 6, 130);
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public void Init()
        {
            _explored = new byte[Res * Res];
            _visible = new byte[Res * Res];
            _px = new Color32[Res * Res];
            _tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            _tex.name = "FogTexture";
            _tex.wrapMode = TextureWrapMode.Clamp;
            _tex.filterMode = FilterMode.Bilinear;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FogOverlay";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.position = new Vector3(0f, 0.08f, 0f);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(MapBuilder.Size + 10f, MapBuilder.Size + 10f, 1f);
            quad.layer = LayerMask.NameToLayer("FX");
            _quadRend = quad.GetComponent<Renderer>();
            var mat = new Material(MaterialLibrary.Get("fog"));
            mat.mainTexture = _tex;
            mat.SetTexture("_BaseMap", _tex);
            _quadRend.sharedMaterial = mat;
            _quadRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _quadRend.receiveShadows = false;

            ForceUpdate();
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = UpdateInterval;
                ForceUpdate();
            }
        }

        public void ForceUpdate()
        {
            System.Array.Clear(_visible, 0, _visible.Length);

            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction != Faction.Player) continue;
                StampCircle(e.Position, e.VisionRadius);
            }

            for (int i = 0; i < _px.Length; i++)
                _px[i] = _visible[i] == 1 ? Clear : (_explored[i] == 1 ? Explored : Unexplored);
            _tex.SetPixels32(_px);
            _tex.Apply(false);

            // hide / reveal enemy entities
            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead || e.Faction != Faction.Enemy) continue;
                e.SetVisibleToPlayer(IsVisible(e.Position));
            }
        }

        void StampCircle(Vector3 worldPos, float radius)
        {
            int cx = WorldToCell(worldPos.x);
            int cy = WorldToCell(worldPos.z);
            int r = Mathf.CeilToInt(radius / CellSize);
            int r2 = r * r;
            int y0 = Mathf.Max(0, cy - r), y1 = Mathf.Min(Res - 1, cy + r);
            int x0 = Mathf.Max(0, cx - r), x1 = Mathf.Min(Res - 1, cx + r);
            for (int y = y0; y <= y1; y++)
            {
                int dy = y - cy;
                for (int x = x0; x <= x1; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy > r2) continue;
                    int idx = y * Res + x;
                    _visible[idx] = 1;
                    _explored[idx] = 1;
                }
            }
        }

        static int WorldToCell(float v) => Mathf.Clamp(Mathf.FloorToInt((v + MapBuilder.Half) / CellSize), 0, Res - 1);

        public bool IsVisible(Vector3 worldPos)
            => _visible[WorldToCell(worldPos.z) * Res + WorldToCell(worldPos.x)] == 1;

        public bool IsExplored(Vector3 worldPos)
            => _explored[WorldToCell(worldPos.z) * Res + WorldToCell(worldPos.x)] == 1;

        public float ExploredFraction()
        {
            int n = 0;
            for (int i = 0; i < _explored.Length; i++) n += _explored[i];
            return n / (float)_explored.Length;
        }

        /// <summary>Raw cell access for the minimap (y*Res+x, x/y in cell space).</summary>
        public byte VisibleCell(int x, int y) => _visible[y * Res + x];
        public byte ExploredCell(int x, int y) => _explored[y * Res + x];
    }
}
