using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Builds the handcrafted 80x80 map: ground grid, border cliffs, interior ridges,
    /// rock clusters and 4 mineral fields. Bakes the NavMesh at runtime.</summary>
    public class MapBuilder : MonoBehaviour
    {
        public const float Size = 80f;
        public const float Half = Size * 0.5f;

        public static readonly Vector3 PlayerBasePos = new Vector3(-26f, 0f, -26f);
        public static readonly Vector3 EnemyBasePos = new Vector3(26f, 0f, 26f);

        /// <summary>Cliff/rock footprints (x, z, width, depth in world units) for minimap painting.</summary>
        public readonly List<Rect> BlockedRects = new List<Rect>();
        public readonly List<Vector3> MineralSpots = new List<Vector3>();

        Transform _root;
        NavMeshSurface _surface;

        public void Build()
        {
            _root = new GameObject("Map").transform;

            BuildGround();
            BuildGrid();
            BuildBorder();
            BuildRidges();
            BuildRocks();
            BuildMinerals();
            BakeNavMesh();
        }

        /// <summary>Ground elevation at a world point. The combat map is intentionally flat so
        /// units, structures and obstacles sit cleanly on the tactical grid.</summary>
        public static float GroundHeight(float x, float z)
        {
            return 0f;
        }

        void BuildGround()
        {
            const int res = 100;      // quads per side
            const float span = 100f;  // covers the playfield + apron beyond the border cliffs

            var mesh = new Mesh { name = "GroundMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var verts = new Vector3[(res + 1) * (res + 1)];
            var uvs = new Vector2[verts.Length];
            for (int z = 0; z <= res; z++)
                for (int x = 0; x <= res; x++)
                {
                    float wx = -span * 0.5f + x * (span / res);
                    float wz = -span * 0.5f + z * (span / res);
                    verts[z * (res + 1) + x] = new Vector3(wx, GroundHeight(wx, wz), wz);
                    uvs[z * (res + 1) + x] = new Vector2(x / (float)res, z / (float)res);
                }
            var tris = new int[res * res * 6];
            int t = 0;
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    int i = z * (res + 1) + x;
                    tris[t++] = i; tris[t++] = i + res + 1; tris[t++] = i + 1;
                    tris[t++] = i + 1; tris[t++] = i + res + 1; tris[t++] = i + res + 2;
                }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var ground = new GameObject("Ground");
            ground.transform.SetParent(_root, false);
            ground.layer = LayerMask.NameToLayer("Ground");

            var filter = ground.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = ground.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialLibrary.Get("ground");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;

            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        void BuildGrid()
        {
            const float step = 2f;
            const float y = 0.035f;

            var grid = new GameObject("GroundGrid");
            grid.transform.SetParent(_root, false);

            var lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "mat_ground_grid"
            };
            lineMaterial.SetColor("_BaseColor", new Color(0.34f, 0.48f, 0.56f, 0.45f));

            int index = 0;
            for (float p = -Half; p <= Half + 0.01f; p += step)
            {
                bool major = Mathf.Abs(p) < 0.01f;
                AddGridLine(grid.transform, $"GridX_{index}", new Vector3(p, y, -Half), new Vector3(p, y, Half), lineMaterial, major);
                AddGridLine(grid.transform, $"GridZ_{index}", new Vector3(-Half, y, p), new Vector3(Half, y, p), lineMaterial, major);
                index++;
            }
        }

        static void AddGridLine(Transform parent, string name, Vector3 a, Vector3 b, Material material, bool major)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            var lr = line.AddComponent<LineRenderer>();
            lr.sharedMaterial = material;
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = major ? 0.07f : 0.035f;
            lr.endWidth = lr.startWidth;
            lr.numCapVertices = 0;
            lr.numCornerVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.useWorldSpace = false;
        }

        void Wall(float cx, float cz, float w, float d, float h = 3.5f)
        {
            float baseY = GroundHeight(cx, cz);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Cliff";
            wall.transform.SetParent(_root, false);
            wall.transform.position = new Vector3(cx, baseY + h * 0.5f - 0.3f, cz);
            wall.transform.localScale = new Vector3(w, h, d);
            wall.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("cliff");
            wall.GetComponent<Renderer>().enabled = false;
            var mod = wall.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = 1; // Not Walkable
            BlockedRects.Add(new Rect(cx - w * 0.5f, cz - d * 0.5f, w, d));

            // jumbled boulder crown along the top — breaks up the box silhouette (visual only)
            var rng = new System.Random((int)(cx * 73 + cz * 131) ^ 0x5f17);
            int chunks = 0;
            bool alongX = w >= d;
            float len = alongX ? w : d;
            for (int i = 0; i < chunks; i++)
            {
                float along = (i + 0.5f) / chunks - 0.5f;
                float jitter = ((float)rng.NextDouble() - 0.5f) * 0.2f;
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "CliffChunk";
                // immediate — the navmesh bakes later this same frame, deferred Destroy is too late
                DestroyImmediate(chunk.GetComponent<Collider>());
                chunk.transform.SetParent(_root, false);
                float sx = (alongX ? len / chunks : d) * (0.75f + (float)rng.NextDouble() * 0.5f);
                float sz = (alongX ? d : len / chunks) * (0.75f + (float)rng.NextDouble() * 0.5f);
                float sh = h * (0.35f + (float)rng.NextDouble() * 0.45f);
                chunk.transform.position = new Vector3(
                    cx + (alongX ? (along + jitter) * len : ((float)rng.NextDouble() - 0.5f) * d * 0.4f),
                    baseY + h - 0.3f + sh * 0.25f,
                    cz + (alongX ? ((float)rng.NextDouble() - 0.5f) * d * 0.4f : (along + jitter) * len));
                chunk.transform.rotation = Quaternion.Euler(
                    ((float)rng.NextDouble() - 0.5f) * 16f,
                    (float)rng.NextDouble() * 90f,
                    ((float)rng.NextDouble() - 0.5f) * 16f);
                chunk.transform.localScale = new Vector3(sx, sh, sz);
                chunk.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("rock");
            }
        }

        void BuildBorder()
        {
            float t = 3f; // thickness
            Wall(0f, Half + t * 0.5f - 1f, Size + 10f, t, 4.5f);
            Wall(0f, -Half - t * 0.5f + 1f, Size + 10f, t, 4.5f);
            Wall(Half + t * 0.5f - 1f, 0f, t, Size + 10f, 4.5f);
            Wall(-Half - t * 0.5f + 1f, 0f, t, Size + 10f, 4.5f);
        }

        void BuildRidges()
        {
            // Central diagonal ridge (NW-SE) with two gaps, forcing two attack lanes.
            Wall(-14f, 20f, 16f, 4f);   // upper-left arm
            Wall(-2f, 8f, 12f, 4f);     // center-left
            Wall(2f, -8f, 12f, 4f);     // center-right
            Wall(14f, -20f, 16f, 4f);   // lower-right arm

            // Side pockets protecting the two center mineral fields
            Wall(-26f, 8f, 8f, 3f);
            Wall(26f, -8f, 8f, 3f);

            // Base entrance funnels
            Wall(-8f, -32f, 10f, 3f);
            Wall(8f, 32f, 10f, 3f);
        }

        void BuildRocks()
        {
            var rockSpots = new[]
            {
                new Vector3(-32f, 0f, 18f), new Vector3(32f, 0f, -18f),
                new Vector3(-18f, 0f, -8f), new Vector3(18f, 0f, 8f),
                new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, -30f),
                new Vector3(24f, 0f, 24f) * 0.4f,
            };
            var rng = new System.Random(1337);
            foreach (var spot in rockSpots)
            {
                int count = 3 + rng.Next(3);
                var cluster = new GameObject("Rocks").transform;
                cluster.SetParent(_root, false);
                cluster.position = spot;
                for (int i = 0; i < count; i++)
                {
                    float ang = (float)rng.NextDouble() * 360f;
                    float dist = 0.6f + (float)rng.NextDouble() * 1.6f;
                    var pos = spot + Quaternion.Euler(0, ang, 0) * Vector3.forward * dist;
                    pos.y = GroundHeight(pos.x, pos.z);
                    float sc = 0.8f + (float)rng.NextDouble() * 1.4f;
                    var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    rock.name = "Rock";
                    rock.transform.SetParent(cluster, false);
                    rock.transform.position = pos + Vector3.up * (sc * 0.3f);
                    rock.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 25f, ang, (float)rng.NextDouble() * 25f);
                    rock.transform.localScale = new Vector3(sc, sc * 0.8f, sc);
                    rock.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("rock");
                    rock.GetComponent<Renderer>().enabled = false;
                    var mod = rock.AddComponent<NavMeshModifier>();
                    mod.overrideArea = true;
                    mod.area = 1;
                }
                BlockedRects.Add(new Rect(spot.x - 2.2f, spot.z - 2.2f, 4.4f, 4.4f));
            }
        }

        void BuildMinerals()
        {
            // arcs behind each base CC, facing the map corner
            SpawnCluster(PlayerBasePos + new Vector3(-5.5f, 0f, -5.5f), 225f, 8);
            SpawnCluster(EnemyBasePos + new Vector3(5.5f, 0f, 5.5f), 45f, 8);
            // two contested center fields
            SpawnCluster(new Vector3(-24f, 0f, 16f), 180f, 6);
            SpawnCluster(new Vector3(24f, 0f, -16f), 0f, 6);
        }

        void SpawnCluster(Vector3 center, float facingDeg, int count)
        {
            var parent = new GameObject("MineralCluster").transform;
            parent.SetParent(_root, false);
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (i / (float)(count - 1)) - 0.5f : 0f;
                float arc = t * 110f;
                var offset = Quaternion.Euler(0f, facingDeg + arc, 0f) * Vector3.forward * (4.5f + Mathf.Abs(t) * 1.5f);
                Vector3 pos = center + offset;
                pos.y = GroundHeight(pos.x, pos.z);
                var nodeGo = VisualFactory.BuildMineralVisual(pos, parent);
                var node = nodeGo.AddComponent<MineralNode>();
                node.Init(1200);
                var mod = nodeGo.AddComponent<NavMeshModifier>();
                mod.overrideArea = true;
                mod.area = 1;
                MineralSpots.Add(pos);
            }
        }

        /// <summary>Decorative pebbles + stray emissive shards. No colliders, so they don't
        /// affect the navmesh or placement — pure texture for the eye.</summary>
        void BuildClutter()
        {
            var rng = new System.Random(4242);
            var parent = new GameObject("Clutter").transform;
            parent.SetParent(_root, false);
            for (int i = 0; i < 90; i++)
            {
                float x = ((float)rng.NextDouble() - 0.5f) * (Size - 6f);
                float z = ((float)rng.NextDouble() - 0.5f) * (Size - 6f);
                bool shard = rng.Next(6) == 0;
                float s = shard ? 0.12f + (float)rng.NextDouble() * 0.15f : 0.1f + (float)rng.NextDouble() * 0.22f;
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = shard ? "Shard" : "Pebble";
                DestroyImmediate(p.GetComponent<Collider>());
                p.transform.SetParent(parent, false);
                p.transform.position = new Vector3(x, GroundHeight(x, z) + s * 0.3f, z);
                p.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 40f);
                p.transform.localScale = shard ? new Vector3(s, s * 3.2f, s) : new Vector3(s * 1.4f, s, s * 1.4f);
                p.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(shard ? "crystal" : "rock");
            }
        }

        void BakeNavMesh()
        {
            _surface = _root.gameObject.AddComponent<NavMeshSurface>();
            _surface.collectObjects = CollectObjects.Children;
            _surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            _surface.BuildNavMesh();
        }

        public bool InBounds(Vector3 p, float margin = 2f)
            => Mathf.Abs(p.x) < Half - margin && Mathf.Abs(p.z) < Half - margin;
    }
}
