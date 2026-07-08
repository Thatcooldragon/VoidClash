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
        public static readonly Vector3 PlayerExpansionPos = new Vector3(-21f, 0f, 12f);
        public static readonly Vector3 EnemyExpansionPos = new Vector3(21f, 0f, -12f);

        /// <summary>Cliff/rock footprints (x, z, width, depth in world units) for minimap painting.</summary>
        public readonly List<Rect> BlockedRects = new List<Rect>();
        public readonly List<Vector3> MineralSpots = new List<Vector3>();
        public readonly List<Vector3> ExpansionSites = new List<Vector3>();

        Transform _root;
        NavMeshSurface _surface;

        public void Build()
        {
            _root = new GameObject("Map").transform;

            BuildGround();
            BuildGrid();
            BuildBasePads();
            BuildBorder();
            BuildRidges();
            BuildLaneGuides();
            BuildRocks();
            BuildMinerals();
            BuildExpansionMarkers();
            BuildLandmarks();
            BuildClutter();
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
            AddBarrierEdge(cx, cz, w, d);

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

        void AddBarrierEdge(float cx, float cz, float w, float d)
        {
            bool alongX = w >= d;
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "CliffEdge";
            DestroyImmediate(edge.GetComponent<Collider>());
            edge.transform.SetParent(_root, false);
            edge.transform.position = new Vector3(cx, GroundHeight(cx, cz) + 0.09f, cz);
            edge.transform.localScale = alongX
                ? new Vector3(w * 0.94f, 0.08f, Mathf.Min(0.28f, d * 0.55f))
                : new Vector3(Mathf.Min(0.28f, w * 0.55f), 0.08f, d * 0.94f);
            edge.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("lane_edge");
            edge.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void BuildBasePads()
        {
            var parent = new GameObject("BaseIdentity").transform;
            parent.SetParent(_root, false);
            BuildFactionPad(parent, "PlayerBasePad", PlayerBasePos, "base_player", "player_accent", 45f);
            BuildFactionPad(parent, "EnemyBasePad", EnemyBasePos, "base_enemy", "enemy_accent", 225f);
        }

        void BuildFactionPad(Transform parent, string name, Vector3 pos, string padMat, string accentMat, float yaw)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = pos + Vector3.up * 0.04f;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deck.name = "CommandDeck";
            DestroyImmediate(deck.GetComponent<Collider>());
            deck.transform.SetParent(root, false);
            deck.transform.localScale = new Vector3(7.5f, 0.045f, 7.5f);
            deck.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("base_neutral");
            deck.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AddRingSegments(root, 4.35f, 20, padMat, 0.1f, 0.42f);
            AddRingSegments(root, 6.15f, 28, accentMat, 0.12f, 0.25f);

            for (int i = -1; i <= 1; i++)
            {
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = "LaunchStripe";
                DestroyImmediate(strip.GetComponent<Collider>());
                strip.transform.SetParent(root, false);
                strip.transform.localPosition = new Vector3(i * 1.15f, 0.08f, 3.2f);
                strip.transform.localScale = new Vector3(0.32f, 0.06f, 3.6f);
                strip.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(i == 0 ? accentMat : padMat);
                strip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "BaseBeacon";
            DestroyImmediate(beacon.GetComponent<Collider>());
            beacon.transform.SetParent(root, false);
            beacon.transform.localPosition = new Vector3(0f, 0.35f, -3.85f);
            beacon.transform.localScale = Vector3.one * 0.42f;
            beacon.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(accentMat);
            beacon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beacon.AddComponent<WorldPulse>().Amount = 0.12f;
        }

        void AddRingSegments(Transform parent, float radius, int segments, string mat, float height, float depth)
        {
            float circumference = 2f * Mathf.PI * radius;
            for (int i = 0; i < segments; i++)
            {
                if (i % 4 == 3) continue;
                float angle = i * (360f / segments);
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = "PadRingSegment";
                DestroyImmediate(seg.GetComponent<Collider>());
                seg.transform.SetParent(parent, false);
                seg.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius + Vector3.up * height;
                seg.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                seg.transform.localScale = new Vector3(circumference / segments * 0.7f, 0.06f, depth);
                seg.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(mat);
                seg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void BuildLaneGuides()
        {
            var parent = new GameObject("BattlefieldGuides").transform;
            parent.SetParent(_root, false);
            AddLaneLine(parent, "MainAttackLane", PlayerBasePos + new Vector3(6f, 0f, 10f), EnemyBasePos + new Vector3(-6f, 0f, -10f), 0.11f);
            AddLaneLine(parent, "NorthExpansionLane", PlayerBasePos + new Vector3(2f, 0f, 24f), EnemyExpansionPos + new Vector3(-4f, 0f, 3f), 0.065f);
            AddLaneLine(parent, "SouthExpansionLane", PlayerExpansionPos + new Vector3(4f, 0f, -3f), EnemyBasePos + new Vector3(-2f, 0f, -24f), 0.065f);
        }

        void AddLaneLine(Transform parent, string name, Vector3 a, Vector3 b, float width)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            var lr = line.AddComponent<LineRenderer>();
            lr.sharedMaterial = MaterialLibrary.Get("lane_edge");
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(a.x, GroundHeight(a.x, a.z) + 0.075f, a.z));
            lr.SetPosition(1, new Vector3(b.x, GroundHeight(b.x, b.z) + 0.075f, b.z));
            lr.startWidth = width;
            lr.endWidth = width * 0.7f;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
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
            ExpansionSites.Add(PlayerExpansionPos);
            ExpansionSites.Add(EnemyExpansionPos);
        }

        void BuildExpansionMarkers()
        {
            foreach (var site in new[] { PlayerExpansionPos, EnemyExpansionPos })
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pad.name = "ExpansionMarker";
                DestroyImmediate(pad.GetComponent<Collider>());
                pad.transform.SetParent(_root, false);
                pad.transform.position = site + Vector3.up * 0.045f;
                pad.transform.localScale = new Vector3(4.8f, 0.035f, 4.8f);
                pad.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("rally");
                pad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                core.name = "ExpansionCore";
                DestroyImmediate(core.GetComponent<Collider>());
                core.transform.SetParent(_root, false);
                core.transform.position = site + Vector3.up * 0.06f;
                core.transform.localScale = new Vector3(1.1f, 0.04f, 1.1f);
                core.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("metal_light");
                core.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                for (int i = 0; i < 4; i++)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marker.name = "ExpansionPylon";
                    DestroyImmediate(marker.GetComponent<Collider>());
                    marker.transform.SetParent(_root, false);
                    Vector3 offset = Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward * 3.8f;
                    marker.transform.position = site + offset + Vector3.up * 0.55f;
                    marker.transform.rotation = Quaternion.Euler(0f, i * 90f + 45f, 12f);
                    marker.transform.localScale = new Vector3(0.28f, 1.1f, 0.28f);
                    marker.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("crystal");
                    marker.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        void BuildLandmarks()
        {
            BuildCenterBeacon();
            BuildSignalArray(new Vector3(-8f, 0f, 28f), -28f, "player_accent");
            BuildSignalArray(new Vector3(8f, 0f, -28f), 152f, "enemy_accent");
        }

        public void BuildFactionAtmosphere(PlayerRace playerRace, PlayerRace skirmishEnemyRace, EnemyRace campaignEnemyRace)
        {
            var parent = new GameObject("FactionAtmosphere").transform;
            parent.SetParent(_root, false);
            AddBaseAtmosphere(parent, PlayerBasePos, AtmosphereMaterial(playerRace, false), 8.8f, "PlayerFrontAtmosphere");
            string enemyMat = campaignEnemyRace == EnemyRace.Zerg ? "zerg_creep"
                : (campaignEnemyRace == EnemyRace.Protoss ? "protoss_field" : AtmosphereMaterial(skirmishEnemyRace, true));
            AddBaseAtmosphere(parent, EnemyBasePos, enemyMat, 9.2f, "EnemyFrontAtmosphere");
        }

        static string AtmosphereMaterial(PlayerRace race, bool enemy)
        {
            if (race == PlayerRace.Bubble) return "bubble_foam";
            if (race == PlayerRace.Dots) return "dots_orbit";
            return enemy ? "base_enemy" : "base_player";
        }

        void AddBaseAtmosphere(Transform parent, Vector3 center, string mat, float radius, string name)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            DestroyImmediate(pad.GetComponent<Collider>());
            pad.transform.SetParent(parent, false);
            pad.transform.position = center + Vector3.up * 0.032f;
            pad.transform.localScale = new Vector3(radius, 0.02f, radius);
            pad.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(mat);
            pad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pad.AddComponent<WorldPulse>().Amount = 0.025f;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f + 18f;
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"{name}Shard";
                DestroyImmediate(shard.GetComponent<Collider>());
                shard.transform.SetParent(parent, false);
                shard.transform.position = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (radius * 0.55f) + Vector3.up * 0.12f;
                shard.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                shard.transform.localScale = new Vector3(0.12f, 0.08f, 1.45f);
                shard.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(mat);
                shard.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void BuildCenterBeacon()
        {
            var root = new GameObject("CenterBeacon").transform;
            root.SetParent(_root, false);
            root.position = Vector3.up * 0.05f;

            var baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseRing.name = "CenterControlPad";
            DestroyImmediate(baseRing.GetComponent<Collider>());
            baseRing.transform.SetParent(root, false);
            baseRing.transform.localScale = new Vector3(5.4f, 0.045f, 5.4f);
            baseRing.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("base_neutral");
            baseRing.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AddRingSegments(root, 3.35f, 24, "center_beacon", 0.1f, 0.28f);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 45f;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "CenterPillar";
                DestroyImmediate(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(root, false);
                pillar.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 2.25f + Vector3.up * 0.75f;
                pillar.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                pillar.transform.localScale = new Vector3(0.28f, 1.5f, 0.28f);
                pillar.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("center_beacon");
                pillar.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pillar.AddComponent<WorldPulse>().Amount = 0.05f;
            }
        }

        void BuildSignalArray(Vector3 center, float yaw, string accentMat)
        {
            var parent = new GameObject("SignalArray").transform;
            parent.SetParent(_root, false);
            parent.position = center;
            parent.rotation = Quaternion.Euler(0f, yaw, 0f);

            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.2f;
                var mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mast.name = "SignalMast";
                DestroyImmediate(mast.GetComponent<Collider>());
                mast.transform.SetParent(parent, false);
                mast.transform.localPosition = new Vector3(x, 1.0f + i * 0.18f, 0f);
                mast.transform.localScale = new Vector3(0.08f, 1.0f + i * 0.18f, 0.08f);
                mast.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("metal_light");

                var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lamp.name = "SignalLamp";
                DestroyImmediate(lamp.GetComponent<Collider>());
                lamp.transform.SetParent(parent, false);
                lamp.transform.localPosition = new Vector3(x, 2.1f + i * 0.35f, 0f);
                lamp.transform.localScale = Vector3.one * 0.22f;
                lamp.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(accentMat);
                lamp.AddComponent<WorldPulse>().Amount = 0.18f;
            }

            var dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "SignalDish";
            DestroyImmediate(dish.GetComponent<Collider>());
            dish.transform.SetParent(parent, false);
            dish.transform.localPosition = new Vector3(0f, 1.1f, 0.55f);
            dish.transform.localRotation = Quaternion.Euler(82f, 0f, 0f);
            dish.transform.localScale = new Vector3(1.4f, 0.07f, 1.4f);
            dish.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get("metal_dark");
            var spin = dish.AddComponent<WorldSpin>();
            spin.Axis = Vector3.up;
            spin.DegreesPerSecond = 18f;
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
