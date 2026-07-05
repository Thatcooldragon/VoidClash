using UnityEngine;

namespace VoidClash
{
    /// <summary>Builds all unit/building visuals from Unity primitives with sci-fi emissive accents.
    /// Used at runtime and by the editor setup when baking prefabs.</summary>
    public static class VisualFactory
    {
        public static GameObject Part(Transform parent, PrimitiveType type, string matName,
            Vector3 localPos, Vector3 localScale, Vector3? euler = null, string name = null)
        {
            var go = GameObject.CreatePrimitive(type);
            if (name != null) go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(matName);
            return go;
        }

        /// <summary>Campaign missions restyle the enemy faction (Zerg/Protoss).
        /// Set by GameBootstrap before any enemy visuals are built; null = default red Terran.</summary>
        public static string EnemyBodyOverride;
        public static string EnemyAccentOverride;

        static string Body(Faction f) => f == Faction.Player ? "player_body"
            : (EnemyBodyOverride ?? "enemy_body");
        static string Accent(Faction f) => f == Faction.Player ? "player_accent"
            : (EnemyAccentOverride ?? "enemy_accent");

        /// <summary>Instantiates the baked visual prefab when available (play mode only —
        /// the editor setup builds fresh so prefabs never nest themselves).</summary>
        static Transform TrySpawnBaked(Transform root, string key)
        {
            if (!Application.isPlaying || G.DB == null) return null;
            // restyled campaign enemies are built fresh with race materials
            if (EnemyBodyOverride != null && key.EndsWith("_enemy")) return null;
            var prefab = G.DB.FindVisualPrefab(key);
            if (prefab == null) return null;
            var inst = Object.Instantiate(prefab, root, false);
            inst.transform.localPosition = Vector3.zero;
            var visual = inst.transform.Find("Visual");
            if (visual == null) return null;
            visual.SetParent(root, true);
            Object.Destroy(inst);
            return visual;
        }

        /// <summary>Creates the "Visual" child holding all renderers. Muzzle transform is at Visual/Muzzle.</summary>
        public static Transform BuildUnitVisual(Transform root, string unitId, Faction faction, float s)
        {
            var baked = TrySpawnBaked(root, $"unit_{unitId}_{faction}".ToLower());
            if (baked != null) return baked;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);
            string body = Body(faction), accent = Accent(faction);

            switch (unitId)
            {
                case "worker":
                {
                    Part(visual, PrimitiveType.Capsule, body, new Vector3(0, 0.7f * s, 0), new Vector3(0.7f * s, 0.6f * s, 0.7f * s));
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0, 0.85f * s, -0.28f * s), new Vector3(0.5f * s, 0.5f * s, 0.25f * s));
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 1.1f * s, 0.25f * s), Vector3.one * 0.22f * s);
                    Part(visual, PrimitiveType.Cube, "metal_light", new Vector3(0.32f * s, 0.55f * s, 0.2f * s), new Vector3(0.16f * s, 0.5f * s, 0.16f * s), new Vector3(20, 0, 0));
                    var muzzleW = new GameObject("Muzzle").transform;
                    muzzleW.SetParent(visual, false);
                    muzzleW.localPosition = new Vector3(0.32f * s, 0.8f * s, 0.4f * s);
                    break;
                }
                case "soldier":
                {
                    Part(visual, PrimitiveType.Capsule, body, new Vector3(0, 0.8f * s, 0), new Vector3(0.75f * s, 0.7f * s, 0.75f * s));
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.5f * s, 0), Vector3.one * 0.4f * s);
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 1.52f * s, 0.17f * s), new Vector3(0.3f * s, 0.1f * s, 0.1f * s));
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0.3f * s, 0.9f * s, 0.3f * s), new Vector3(0.14f * s, 0.14f * s, 0.8f * s), null, "Gun");
                    var muzzle = new GameObject("Muzzle").transform;
                    muzzle.SetParent(visual, false);
                    muzzle.localPosition = new Vector3(0.3f * s, 0.9f * s, 0.75f * s);
                    break;
                }
                case "ranged":
                {
                    Part(visual, PrimitiveType.Capsule, body, new Vector3(0, 0.78f * s, 0), new Vector3(0.6f * s, 0.72f * s, 0.6f * s));
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 1.45f * s, 0.1f * s), Vector3.one * 0.28f * s);
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0.22f * s, 1.0f * s, 0.35f * s), new Vector3(0.1f * s, 0.1f * s, 1.2f * s), null, "Rifle");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0.22f * s, 1.0f * s, 0.95f * s), Vector3.one * 0.14f * s);
                    var muzzle2 = new GameObject("Muzzle").transform;
                    muzzle2.SetParent(visual, false);
                    muzzle2.localPosition = new Vector3(0.22f * s, 1.0f * s, 1.0f * s);
                    break;
                }
                case "zergling":
                {
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 0.45f * s, 0), new Vector3(0.85f * s, 0.6f * s, 1.1f * s));
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 0.6f * s, 0.5f * s), Vector3.one * 0.5f * s, null, "Head");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0.12f * s, 0.68f * s, 0.7f * s), Vector3.one * 0.12f * s);
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(-0.12f * s, 0.68f * s, 0.7f * s), Vector3.one * 0.12f * s);
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0.35f * s, 0.75f * s, 0.1f * s), new Vector3(0.1f * s, 0.6f * s, 0.1f * s), new Vector3(0, 0, -35f), "ClawR");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(-0.35f * s, 0.75f * s, 0.1f * s), new Vector3(0.1f * s, 0.6f * s, 0.1f * s), new Vector3(0, 0, 35f), "ClawL");
                    var muzzleZ = new GameObject("Muzzle").transform;
                    muzzleZ.SetParent(visual, false);
                    muzzleZ.localPosition = new Vector3(0, 0.6f * s, 0.7f * s);
                    break;
                }
                case "hydralisk":
                {
                    Part(visual, PrimitiveType.Capsule, body, new Vector3(0, 0.75f * s, 0), new Vector3(0.6f * s, 0.75f * s, 0.6f * s), new Vector3(18f, 0, 0));
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 1.45f * s, 0.25f * s), new Vector3(0.5f * s, 0.6f * s, 0.6f * s), null, "Hood");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 1.5f * s, 0.5f * s), Vector3.one * 0.2f * s);
                    for (int i = 0; i < 3; i++)
                        Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3((i - 1) * 0.18f * s, 1.7f * s, -0.05f * s),
                            new Vector3(0.07f * s, 0.5f * s, 0.07f * s), new Vector3(-20f + i * 8f, 0, (i - 1) * 18f), "Spine");
                    var muzzleH = new GameObject("Muzzle").transform;
                    muzzleH.SetParent(visual, false);
                    muzzleH.localPosition = new Vector3(0, 1.5f * s, 0.55f * s);
                    break;
                }
                case "zealot":
                {
                    Part(visual, PrimitiveType.Capsule, body, new Vector3(0, 0.85f * s, 0), new Vector3(0.8f * s, 0.75f * s, 0.8f * s));
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.6f * s, 0), Vector3.one * 0.42f * s);
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 1.62f * s, 0.18f * s), new Vector3(0.32f * s, 0.08f * s, 0.1f * s), null, "Visor");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0.42f * s, 0.95f * s, 0.35f * s), new Vector3(0.09f * s, 0.09f * s, 0.9f * s), null, "BladeR");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(-0.42f * s, 0.95f * s, 0.35f * s), new Vector3(0.09f * s, 0.09f * s, 0.9f * s), null, "BladeL");
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 1.35f * s, -0.3f * s), new Vector3(0.9f * s, 0.15f * s, 0.15f * s), new Vector3(0, 0, 12f), "PauldronBar");
                    var muzzleZe = new GameObject("Muzzle").transform;
                    muzzleZe.SetParent(visual, false);
                    muzzleZe.localPosition = new Vector3(0.42f * s, 0.95f * s, 0.8f * s);
                    break;
                }
                case "stalker":
                {
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 1.15f * s, 0), new Vector3(0.75f * s, 0.55f * s, 0.95f * s), null, "Hull");
                    for (int i = 0; i < 4; i++)
                    {
                        float sx = (i % 2 == 0) ? 1f : -1f;
                        float sz = (i < 2) ? 1f : -1f;
                        Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(sx * 0.45f * s, 0.6f * s, sz * 0.4f * s),
                            new Vector3(0.1f * s, 1.15f * s, 0.1f * s), new Vector3(sz * 22f, 0, sx * -28f), "Leg");
                    }
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 1.35f * s, 0.3f * s), Vector3.one * 0.25f * s, null, "Eye");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 1.2f * s, 0.65f * s), new Vector3(0.1f * s, 0.1f * s, 0.5f * s), null, "Cannon");
                    var muzzleSt = new GameObject("Muzzle").transform;
                    muzzleSt.SetParent(visual, false);
                    muzzleSt.localPosition = new Vector3(0, 1.2f * s, 0.95f * s);
                    break;
                }
                case "overlord":
                {
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 0.65f * s, 0), new Vector3(1.5f * s, 1.0f * s, 1.7f * s), null, "Bulk");
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 1.0f * s, 0.7f * s), Vector3.one * 0.75f * s, null, "Head");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0.2f * s, 1.1f * s, 1.0f * s), Vector3.one * 0.18f * s);
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(-0.2f * s, 1.1f * s, 1.0f * s), Vector3.one * 0.18f * s);
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 1.35f * s, 0.85f * s), Vector3.one * 0.16f * s);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = -50f + i * 25f;
                        Part(visual, PrimitiveType.Cube, "metal_dark",
                            Quaternion.Euler(0, a, 0) * new Vector3(0, 0, -0.9f * s) + new Vector3(0, 1.25f * s, 0),
                            new Vector3(0.16f * s, 0.9f * s, 0.16f * s), new Vector3(-25f, a, 0), "SpineBack");
                    }
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 0.75f * s, -0.4f * s), Vector3.one * 0.5f * s, null, "Heart");
                    var muzzleOv = new GameObject("Muzzle").transform;
                    muzzleOv.SetParent(visual, false);
                    muzzleOv.localPosition = new Vector3(0, 1.0f * s, 1.2f * s);
                    break;
                }
                case "heavy":
                {
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 0.5f * s, 0), new Vector3(1.3f * s, 0.55f * s, 1.6f * s), null, "Chassis");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(-0.62f * s, 0.35f * s, 0), new Vector3(0.3f * s, 0.5f * s, 1.7f * s), null, "TrackL");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0.62f * s, 0.35f * s, 0), new Vector3(0.3f * s, 0.5f * s, 1.7f * s), null, "TrackR");
                    Part(visual, PrimitiveType.Cylinder, "metal_light", new Vector3(0, 0.95f * s, 0), new Vector3(0.8f * s, 0.22f * s, 0.8f * s), null, "TurretBase");
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 1.12f * s, 0.5f * s), new Vector3(0.24f * s, 0.24f * s, 1.1f * s), null, "Barrel");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 1.15f * s, -0.25f * s), Vector3.one * 0.3f * s);
                    var muzzle3 = new GameObject("Muzzle").transform;
                    muzzle3.SetParent(visual, false);
                    muzzle3.localPosition = new Vector3(0, 1.12f * s, 1.1f * s);
                    break;
                }
            }
            return visual;
        }

        public static Transform BuildBuildingVisual(Transform root, string buildingId, Faction faction)
        {
            var baked = TrySpawnBaked(root, $"building_{buildingId}_{faction}".ToLower());
            if (baked != null) return baked;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);
            string body = Body(faction), accent = Accent(faction);

            switch (buildingId)
            {
                case "cc":
                {
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 1.1f, 0), new Vector3(5.4f, 2.2f, 5.4f));
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0, 2.5f, 0), new Vector3(4.2f, 0.7f, 4.2f));
                    Part(visual, PrimitiveType.Cylinder, "metal_light", new Vector3(0, 3.1f, 0), new Vector3(2.2f, 0.5f, 2.2f));
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 3.9f, 0), Vector3.one * 1.1f, null, "Core");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 0.9f, 2.75f), new Vector3(2.2f, 1.2f, 0.15f), null, "Door");
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * 90f + 45f;
                        var p = Quaternion.Euler(0, a, 0) * new Vector3(0, 0, 3.1f);
                        Part(visual, PrimitiveType.Cube, "metal_light", p + new Vector3(0, 0.6f, 0), new Vector3(0.7f, 1.2f, 0.7f), new Vector3(0, a, 0));
                    }
                    break;
                }
                case "depot":
                {
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 0.65f, 0), new Vector3(2.7f, 1.3f, 2.7f));
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 1.32f, 0), new Vector3(2.2f, 0.12f, 2.2f));
                    Part(visual, PrimitiveType.Cube, "metal_light", new Vector3(0, 1.6f, 0), new Vector3(1.2f, 0.5f, 1.2f));
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 2.0f, 0), Vector3.one * 0.4f);
                    break;
                }
                case "barracks":
                {
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 1.0f, 0), new Vector3(4.5f, 2.0f, 4.5f));
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0, 2.3f, -0.8f), new Vector3(4.6f, 0.9f, 2.6f), new Vector3(-12, 0, 0), "Roof");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 0.85f, 2.3f), new Vector3(1.8f, 1.5f, 0.15f), null, "Door");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(-2.28f, 1.6f, 0), new Vector3(0.12f, 0.8f, 3.6f));
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(2.28f, 1.6f, 0), new Vector3(0.12f, 0.8f, 3.6f));
                    break;
                }
                case "factory":
                {
                    Part(visual, PrimitiveType.Cube, body, new Vector3(0, 1.1f, 0), new Vector3(5.4f, 2.2f, 5.4f));
                    Part(visual, PrimitiveType.Cylinder, "metal_dark", new Vector3(-1.6f, 3.0f, -1.6f), new Vector3(0.8f, 1.0f, 0.8f), null, "Stack1");
                    Part(visual, PrimitiveType.Cylinder, "metal_dark", new Vector3(-0.4f, 2.8f, -1.6f), new Vector3(0.6f, 0.8f, 0.6f), null, "Stack2");
                    Part(visual, PrimitiveType.Cube, "metal_light", new Vector3(0.9f, 2.5f, 0.9f), new Vector3(2.4f, 0.6f, 2.4f));
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 1.0f, 2.75f), new Vector3(2.8f, 1.6f, 0.15f), null, "Door");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0.9f, 3.1f, 0.9f), Vector3.one * 0.5f);
                    break;
                }
                case "turret":
                {
                    Part(visual, PrimitiveType.Cylinder, body, new Vector3(0, 0.6f, 0), new Vector3(1.7f, 0.6f, 1.7f));
                    var head = new GameObject("Head").transform;
                    head.SetParent(visual, false);
                    head.localPosition = new Vector3(0, 1.5f, 0);
                    Part(head, PrimitiveType.Cube, "metal_light", Vector3.zero, new Vector3(0.9f, 0.7f, 0.9f));
                    Part(head, PrimitiveType.Cube, "metal_dark", new Vector3(0, 0.05f, 0.7f), new Vector3(0.22f, 0.22f, 1.1f), null, "Barrel");
                    Part(head, PrimitiveType.Sphere, accent, new Vector3(0, 0.45f, 0), Vector3.one * 0.35f);
                    var muzzle = new GameObject("Muzzle").transform;
                    muzzle.SetParent(head, false);
                    muzzle.localPosition = new Vector3(0, 0.05f, 1.3f);
                    break;
                }
            }
            return visual;
        }

        public static GameObject BuildMineralVisual(Vector3 pos, Transform parent)
        {
            var root = new GameObject("MineralNode");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            int shards = Random.Range(4, 7);
            for (int i = 0; i < shards; i++)
            {
                float ang = i * (360f / shards) + Random.Range(-15f, 15f);
                float r = Random.Range(0.15f, 0.6f);
                var p = Quaternion.Euler(0, ang, 0) * new Vector3(0, 0, r);
                float h = Random.Range(0.5f, 1.3f);
                Part(root.transform, PrimitiveType.Cube, "crystal",
                    p + new Vector3(0, h * 0.35f, 0),
                    new Vector3(0.35f, h, 0.35f),
                    new Vector3(Random.Range(-18f, 18f), ang, Random.Range(-18f, 18f)));
            }
            var col = root.AddComponent<SphereCollider>();
            col.radius = 1.1f;
            col.center = new Vector3(0f, 0.5f, 0f);
            root.layer = LayerMask.NameToLayer("Minerals");
            return root;
        }
    }
}
