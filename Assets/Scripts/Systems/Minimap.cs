using UnityEngine;

namespace VoidClash
{
    /// <summary>Composites the minimap texture (terrain, fog, unit dots, camera box) ~7x/sec.
    /// The HUD displays it via RawImage and forwards click/drag to MoveCameraTo.</summary>
    public class Minimap : MonoBehaviour
    {
        public const int Res = FogOfWar.Res; // reuse fog resolution (160)
        public Texture2D Texture { get; private set; }

        Color32[] _terrain;
        Color32[] _px;
        float _timer;

        static readonly Color32 GroundCol = new Color32(38, 44, 52, 255);
        static readonly Color32 CliffCol = new Color32(88, 84, 100, 255);
        static readonly Color32 MineralCol = new Color32(60, 200, 235, 255);
        static readonly Color32 PlayerCol = new Color32(70, 255, 100, 255);
        static readonly Color32 EnemyCol = new Color32(255, 60, 50, 255);
        static readonly Color32 CamBoxCol = new Color32(240, 240, 240, 255);

        public void Init()
        {
            Texture = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            Texture.name = "MinimapTexture";
            Texture.filterMode = FilterMode.Bilinear;
            Texture.wrapMode = TextureWrapMode.Clamp;
            _px = new Color32[Res * Res];
            BakeTerrain();
            Redraw();
        }

        void BakeTerrain()
        {
            _terrain = new Color32[Res * Res];
            for (int i = 0; i < _terrain.Length; i++) _terrain[i] = GroundCol;
            if (G.Map != null)
            {
                foreach (var r in G.Map.BlockedRects)
                {
                    int x0 = WorldToCell(r.xMin), x1 = WorldToCell(r.xMax);
                    int y0 = WorldToCell(r.yMin), y1 = WorldToCell(r.yMax);
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                            _terrain[y * Res + x] = CliffCol;
                }
                foreach (var m in G.Map.MineralSpots)
                    Dot(_terrain, WorldToCell(m.x), WorldToCell(m.z), 1, MineralCol);
            }
        }

        void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                _timer = 0.14f;
                Redraw();
            }
        }

        void Redraw()
        {
            // terrain + fog
            for (int y = 0; y < Res; y++)
            {
                for (int x = 0; x < Res; x++)
                {
                    int i = y * Res + x;
                    Color32 c = _terrain[i];
                    if (G.Fog != null)
                    {
                        if (G.Fog.VisibleCell(x, y) == 0)
                        {
                            if (G.Fog.ExploredCell(x, y) == 1)
                                c = Color32.Lerp(c, new Color32(0, 0, 0, 255), 0.55f);
                            else
                                c = new Color32(4, 5, 8, 255);
                        }
                    }
                    _px[i] = c;
                }
            }

            // entity dots
            foreach (var e in Entity.All)
            {
                if (e == null || e.IsDead) continue;
                if (e.Faction == Faction.Player)
                    Dot(_px, WorldToCell(e.Position.x), WorldToCell(e.Position.z), e.IsBuilding ? 2 : 1, PlayerCol);
                else if (e.Faction == Faction.Enemy && e.VisibleToPlayer)
                    Dot(_px, WorldToCell(e.Position.x), WorldToCell(e.Position.z), e.IsBuilding ? 2 : 1, EnemyCol);
            }

            DrawCameraBox();

            Texture.SetPixels32(_px);
            Texture.Apply(false);
        }

        void DrawCameraBox()
        {
            if (G.Cam == null || G.Cam.Cam == null) return;
            Vector3[] corners = new Vector3[4];
            Vector2[] cells = new Vector2[4];
            var vps = new[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, 1), new Vector3(0, 1) };
            for (int i = 0; i < 4; i++)
            {
                var ray = G.Cam.Cam.ViewportPointToRay(vps[i]);
                var plane = new Plane(Vector3.up, Vector3.zero);
                if (!plane.Raycast(ray, out float d)) d = 100f;
                corners[i] = ray.GetPoint(Mathf.Min(d, 200f));
                cells[i] = new Vector2(WorldToCell(corners[i].x), WorldToCell(corners[i].z));
            }
            for (int i = 0; i < 4; i++)
                Line(cells[i], cells[(i + 1) % 4]);
        }

        void Line(Vector2 a, Vector2 b)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b));
            steps = Mathf.Clamp(steps, 1, Res * 2);
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)steps);
                int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
                if (x >= 0 && x < Res && y >= 0 && y < Res)
                    _px[y * Res + x] = CamBoxCol;
            }
        }

        static void Dot(Color32[] buf, int cx, int cy, int r, Color32 c)
        {
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                    if (x >= 0 && x < Res && y >= 0 && y < Res)
                        buf[y * Res + x] = c;
        }

        static int WorldToCell(float v)
            => Mathf.Clamp(Mathf.FloorToInt((v + MapBuilder.Half) / (MapBuilder.Size / Res)), 0, Res - 1);

        /// <summary>uv in [0,1] from the minimap RawImage → move camera there.</summary>
        public void MoveCameraTo(Vector2 uv)
        {
            var world = new Vector3(uv.x * MapBuilder.Size - MapBuilder.Half, 0f, uv.y * MapBuilder.Size - MapBuilder.Half);
            G.Cam.Focus(world);
        }
    }
}
