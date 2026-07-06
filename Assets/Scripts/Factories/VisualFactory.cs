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

        // ---- shared limb helpers (give each race a distinct leg silhouette) ----

        /// <summary>Terran armored biped leg: thigh, shin, boot. side = +1 right / -1 left.</summary>
        static void LegTerran(Transform v, float side, string body, float s)
        {
            float x = 0.2f * s * side;
            Part(v, PrimitiveType.Cube, "metal_dark", new Vector3(x, 0.52f * s, 0f),       new Vector3(0.24f * s, 0.46f * s, 0.28f * s), null, "Thigh");
            Part(v, PrimitiveType.Cube, body,         new Vector3(x, 0.20f * s, 0.02f * s), new Vector3(0.22f * s, 0.42f * s, 0.26f * s), null, "Shin");
            Part(v, PrimitiveType.Cube, "metal_dark", new Vector3(x, 0.04f * s, 0.10f * s), new Vector3(0.26f * s, 0.14f * s, 0.42f * s), null, "Boot");
        }

        /// <summary>Protoss digitigrade (reverse-jointed) leg — sleek, glowing knee.</summary>
        static void LegProtoss(Transform v, float side, string body, string accent, float s)
        {
            float x = 0.24f * s * side;
            Part(v, PrimitiveType.Cube,   "metal_light", new Vector3(x, 0.66f * s, 0.06f * s), new Vector3(0.15f * s, 0.5f * s, 0.17f * s), new Vector3(28f, 0, 0),  "Thigh");
            Part(v, PrimitiveType.Cube,   body,          new Vector3(x, 0.30f * s, -0.06f * s), new Vector3(0.12f * s, 0.5f * s, 0.14f * s), new Vector3(-34f, 0, 0), "Shin");
            Part(v, PrimitiveType.Cube,   "metal_light", new Vector3(x, 0.04f * s, 0.13f * s), new Vector3(0.13f * s, 0.1f * s, 0.42f * s), null, "Talon");
            Part(v, PrimitiveType.Sphere, accent,        new Vector3(x, 0.52f * s, 0.18f * s), Vector3.one * 0.09f * s, null, "Knee");
        }

        /// <summary>Zerg splayed beast leg: raised bug-haunch down to a chitin point.
        /// side = +1 right / -1 left, fz = +1 front / -1 rear.</summary>
        static void LegZerg(Transform v, float side, float fz, string body, float s)
        {
            float x = 0.3f * s * side;
            float z = fz * 0.4f * s;
            Part(v, PrimitiveType.Cube, body,         new Vector3(x * 0.7f, 0.55f * s, z * 0.6f), new Vector3(0.11f * s, 0.4f * s, 0.11f * s),  new Vector3(0, 0, side * 52f),          "Haunch");
            Part(v, PrimitiveType.Cube, "metal_dark", new Vector3(x, 0.2f * s, z),                new Vector3(0.08f * s, 0.46f * s, 0.08f * s), new Vector3(fz * -18f, 0, side * -18f), "Claw");
        }

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
                    LegTerran(visual, 1f, body, s); LegTerran(visual, -1f, body, s);
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0, 0.78f * s, 0),          new Vector3(0.5f * s, 0.2f * s, 0.34f * s), null, "Pelvis");
                    Part(visual, PrimitiveType.Cube,   body,          new Vector3(0, 1.06f * s, 0),          new Vector3(0.62f * s, 0.56f * s, 0.5f * s), null, "Torso");
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0, 1.06f * s, -0.32f * s), new Vector3(0.46f * s, 0.5f * s, 0.22f * s), null, "Pack");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.5f * s, 0.02f * s),   Vector3.one * 0.34f * s, null, "Head");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.5f * s, 0.18f * s),   new Vector3(0.26f * s, 0.1f * s, 0.08f * s), null, "Visor");
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0.34f * s, 1.0f * s, 0.28f * s), new Vector3(0.14f * s, 0.14f * s, 0.66f * s), new Vector3(18, 0, 0), "ToolArm");
                    Part(visual, PrimitiveType.Sphere, accent,        new Vector3(0.34f * s, 0.98f * s, 0.62f * s), Vector3.one * 0.13f * s, null, "Welder");
                    var muzzleW = new GameObject("Muzzle").transform;
                    muzzleW.SetParent(visual, false);
                    muzzleW.localPosition = new Vector3(0.34f * s, 0.98f * s, 0.72f * s);
                    break;
                }
                case "soldier":
                {
                    LegTerran(visual, 1f, body, s); LegTerran(visual, -1f, body, s);
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0, 0.8f * s, 0),           new Vector3(0.56f * s, 0.22f * s, 0.36f * s), null, "Pelvis");
                    Part(visual, PrimitiveType.Cube,   body,          new Vector3(0, 1.1f * s, 0),           new Vector3(0.7f * s, 0.6f * s, 0.54f * s), null, "Torso");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.18f * s, 0.28f * s),  new Vector3(0.3f * s, 0.14f * s, 0.06f * s), null, "Chestlight");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0.42f * s, 1.32f * s, 0),  Vector3.one * 0.32f * s, null, "PauldronR");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(-0.42f * s, 1.32f * s, 0), Vector3.one * 0.32f * s, null, "PauldronL");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.56f * s, 0.02f * s),  Vector3.one * 0.34f * s, null, "Helmet");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.56f * s, 0.2f * s),   new Vector3(0.26f * s, 0.09f * s, 0.08f * s), null, "Visor");
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0.34f * s, 1.02f * s, 0.5f * s),  new Vector3(0.16f * s, 0.16f * s, 0.9f * s), null, "Gun");
                    Part(visual, PrimitiveType.Sphere, accent,        new Vector3(0.34f * s, 1.08f * s, 0.42f * s), Vector3.one * 0.1f * s, null, "GunSight");
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0f, 1.82f * s, -0.12f * s), new Vector3(0.08f * s, 0.38f * s, 0.08f * s), new Vector3(12f, 0, 0), "CommsMast");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0f, 2.04f * s, -0.1f * s), Vector3.one * 0.09f * s, null, "CommsLight");
                    var muzzle = new GameObject("Muzzle").transform;
                    muzzle.SetParent(visual, false);
                    muzzle.localPosition = new Vector3(0.34f * s, 1.02f * s, 1.0f * s);
                    break;
                }
                case "ranged":
                {
                    LegTerran(visual, 1f, body, s * 0.9f); LegTerran(visual, -1f, body, s * 0.9f);
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0, 0.76f * s, 0),          new Vector3(0.42f * s, 0.2f * s, 0.3f * s), null, "Pelvis");
                    Part(visual, PrimitiveType.Cube,   body,          new Vector3(0, 1.04f * s, 0),          new Vector3(0.5f * s, 0.56f * s, 0.42f * s), null, "Torso");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.46f * s, 0.02f * s),  Vector3.one * 0.3f * s, null, "Head");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.46f * s, 0.16f * s),  new Vector3(0.22f * s, 0.08f * s, 0.08f * s), null, "Visor");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(-0.12f * s, 1.72f * s, -0.05f * s), new Vector3(0.04f * s, 0.3f * s, 0.04f * s), new Vector3(0, 0, 10f), "Antenna");
                    Part(visual, PrimitiveType.Cube,   "metal_dark",  new Vector3(0.22f * s, 1.02f * s, 0.4f * s), new Vector3(0.1f * s, 0.1f * s, 1.3f * s), null, "Rifle");
                    Part(visual, PrimitiveType.Sphere, accent,        new Vector3(0.22f * s, 1.1f * s, 0.3f * s), Vector3.one * 0.1f * s, null, "Scope");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(-0.28f * s, 1.02f * s, 0.22f * s), new Vector3(0.08f * s, 0.08f * s, 0.9f * s), new Vector3(0, -10f, 0), "Stabilizer");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(-0.42f * s, 1.28f * s, -0.12f * s), new Vector3(0.04f * s, 0.46f * s, 0.04f * s), new Vector3(0, 0, -12f), "ScannerFin");
                    var muzzle2 = new GameObject("Muzzle").transform;
                    muzzle2.SetParent(visual, false);
                    muzzle2.localPosition = new Vector3(0.22f * s, 1.02f * s, 1.05f * s);
                    break;
                }
                case "zergling":
                {
                    Part(visual, PrimitiveType.Sphere, body,   new Vector3(0, 0.5f * s, 0),          new Vector3(0.8f * s, 0.5f * s, 1.05f * s), null, "Carapace");
                    Part(visual, PrimitiveType.Sphere, body,   new Vector3(0, 0.55f * s, 0.55f * s), new Vector3(0.5f * s, 0.45f * s, 0.55f * s), null, "Head");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0.13f * s, 0.62f * s, 0.75f * s),  Vector3.one * 0.1f * s, null, "EyeR");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(-0.13f * s, 0.62f * s, 0.75f * s), Vector3.one * 0.1f * s, null, "EyeL");
                    LegZerg(visual, 1f, 1f, body, s);  LegZerg(visual, -1f, 1f, body, s);
                    LegZerg(visual, 1f, -1f, body, s); LegZerg(visual, -1f, -1f, body, s);
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0.28f * s, 0.95f * s, 0.05f * s),  new Vector3(0.08f * s, 0.7f * s, 0.12f * s), new Vector3(30f, 0, -28f), "ScytheR");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(-0.28f * s, 0.95f * s, 0.05f * s), new Vector3(0.08f * s, 0.7f * s, 0.12f * s), new Vector3(30f, 0, 28f), "ScytheL");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0, 0.42f * s, -0.78f * s), new Vector3(0.12f * s, 0.12f * s, 0.75f * s), new Vector3(-22f, 0, 0), "TailBlade");
                    var muzzleZ = new GameObject("Muzzle").transform;
                    muzzleZ.SetParent(visual, false);
                    muzzleZ.localPosition = new Vector3(0, 0.6f * s, 0.8f * s);
                    break;
                }
                case "hydralisk":
                {
                    Part(visual, PrimitiveType.Sphere, body, new Vector3(0, 0.5f * s, -0.1f * s), new Vector3(0.75f * s, 0.6f * s, 0.9f * s), null, "Base");
                    LegZerg(visual, 1f, 1f, body, s); LegZerg(visual, -1f, 1f, body, s);
                    Part(visual, PrimitiveType.Capsule, body,   new Vector3(0, 1.15f * s, 0.15f * s), new Vector3(0.5f * s, 0.6f * s, 0.5f * s), new Vector3(18f, 0, 0), "Torso");
                    Part(visual, PrimitiveType.Sphere,  body,   new Vector3(0, 1.7f * s, 0.32f * s),  new Vector3(0.55f * s, 0.52f * s, 0.55f * s), null, "Hood");
                    Part(visual, PrimitiveType.Sphere,  accent, new Vector3(0, 1.72f * s, 0.55f * s), Vector3.one * 0.16f * s, null, "Face");
                    for (int i = 0; i < 3; i++)
                        Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3((i - 1) * 0.2f * s, 1.55f * s, -0.15f * s),
                            new Vector3(0.06f * s, 0.55f * s, 0.06f * s), new Vector3(-28f + i * 6f, 0, (i - 1) * 16f), "Spine");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 1.96f * s, 0.08f * s), new Vector3(0.22f * s, 0.09f * s, 0.5f * s), new Vector3(-14f, 0, 0), "CrestGlow");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(0.3f * s, 1.2f * s, 0.3f * s),  new Vector3(0.09f * s, 0.09f * s, 0.5f * s), new Vector3(20f, 0, 0), "ArmR");
                    Part(visual, PrimitiveType.Cube, "metal_dark", new Vector3(-0.3f * s, 1.2f * s, 0.3f * s), new Vector3(0.09f * s, 0.09f * s, 0.5f * s), new Vector3(20f, 0, 0), "ArmL");
                    var muzzleH = new GameObject("Muzzle").transform;
                    muzzleH.SetParent(visual, false);
                    muzzleH.localPosition = new Vector3(0, 1.6f * s, 0.6f * s);
                    break;
                }
                case "zealot":
                {
                    LegProtoss(visual, 1f, body, accent, s); LegProtoss(visual, -1f, body, accent, s);
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(0, 0.84f * s, 0),          new Vector3(0.4f * s, 0.22f * s, 0.32f * s), null, "Pelvis");
                    Part(visual, PrimitiveType.Cube,   body,          new Vector3(0, 1.16f * s, 0),          new Vector3(0.56f * s, 0.62f * s, 0.44f * s), null, "Torso");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.22f * s, 0.23f * s),  new Vector3(0.22f * s, 0.3f * s, 0.05f * s), null, "Chestplate");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(0.44f * s, 1.42f * s, 0),  new Vector3(0.22f * s, 0.2f * s, 0.34f * s), new Vector3(0, 0, -18f), "GuardR");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(-0.44f * s, 1.42f * s, 0), new Vector3(0.22f * s, 0.2f * s, 0.34f * s), new Vector3(0, 0, 18f),  "GuardL");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.68f * s, 0.02f * s),  Vector3.one * 0.32f * s, null, "Head");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.68f * s, 0.18f * s),  new Vector3(0.24f * s, 0.08f * s, 0.08f * s), null, "Visor");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(0.12f * s, 1.9f * s, -0.08f * s),  new Vector3(0.05f * s, 0.32f * s, 0.12f * s), new Vector3(-18f, 0, -12f), "CrestR");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(-0.12f * s, 1.9f * s, -0.08f * s), new Vector3(0.05f * s, 0.32f * s, 0.12f * s), new Vector3(-18f, 0, 12f),  "CrestL");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(0.42f * s, 1.1f * s, 0.12f * s),  new Vector3(0.12f * s, 0.12f * s, 0.4f * s), null, "ForearmR");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0.42f * s, 1.05f * s, 0.55f * s), new Vector3(0.07f * s, 0.16f * s, 0.7f * s), null, "BladeR");
                    Part(visual, PrimitiveType.Cube,   "metal_light", new Vector3(-0.42f * s, 1.1f * s, 0.12f * s), new Vector3(0.12f * s, 0.12f * s, 0.4f * s), null, "ForearmL");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(-0.42f * s, 1.05f * s, 0.55f * s), new Vector3(0.07f * s, 0.16f * s, 0.7f * s), null, "BladeL");
                    var muzzleZe = new GameObject("Muzzle").transform;
                    muzzleZe.SetParent(visual, false);
                    muzzleZe.localPosition = new Vector3(0.42f * s, 1.05f * s, 0.9f * s);
                    break;
                }
                case "stalker":
                {
                    Part(visual, PrimitiveType.Sphere, body,          new Vector3(0, 1.2f * s, 0),          new Vector3(0.75f * s, 0.5f * s, 0.95f * s), null, "Hull");
                    Part(visual, PrimitiveType.Sphere, "metal_light", new Vector3(0, 1.3f * s, -0.15f * s), new Vector3(0.5f * s, 0.4f * s, 0.5f * s), null, "Carapace");
                    Part(visual, PrimitiveType.Sphere, accent,        new Vector3(0, 1.22f * s, 0.42f * s), Vector3.one * 0.22f * s, null, "Eye");
                    Part(visual, PrimitiveType.Cube,   accent,        new Vector3(0, 1.1f * s, 0.6f * s),   new Vector3(0.09f * s, 0.09f * s, 0.5f * s), null, "Cannon");
                    for (int i = 0; i < 4; i++)
                    {
                        float sx = (i % 2 == 0) ? 1f : -1f;
                        float sz = (i < 2) ? 1f : -1f;
                        Part(visual, PrimitiveType.Cube, "metal_light", new Vector3(sx * 0.4f * s, 1.15f * s, sz * 0.35f * s),  new Vector3(0.09f * s, 0.1f * s, 0.55f * s), new Vector3(sz * 55f, 0, sx * -30f), "Thigh");
                        Part(visual, PrimitiveType.Cube, "metal_dark",  new Vector3(sx * 0.66f * s, 0.55f * s, sz * 0.6f * s), new Vector3(0.07f * s, 0.85f * s, 0.07f * s), new Vector3(sz * -18f, 0, sx * -14f), "Shin");
                    }
                    var muzzleSt = new GameObject("Muzzle").transform;
                    muzzleSt.SetParent(visual, false);
                    muzzleSt.localPosition = new Vector3(0, 1.1f * s, 0.9f * s);
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
                case "bubble":
                {
                    Part(visual, PrimitiveType.Sphere, "crystal", new Vector3(0, 0.75f * s, 0), Vector3.one * 0.72f * s, null, "SoapBody");
                    Part(visual, PrimitiveType.Sphere, "rally", new Vector3(0.18f * s, 0.95f * s, 0.22f * s), Vector3.one * 0.18f * s, null, "Shine");
                    var muzzleBubble = new GameObject("Muzzle").transform;
                    muzzleBubble.SetParent(visual, false);
                    muzzleBubble.localPosition = new Vector3(0, 0.75f * s, 0.6f * s);
                    break;
                }
                case "poison_bubble":
                {
                    Part(visual, PrimitiveType.Sphere, "zerg_accent", new Vector3(0, 0.75f * s, 0), Vector3.one * 0.78f * s, null, "PoisonBody");
                    Part(visual, PrimitiveType.Sphere, "crystal", new Vector3(-0.18f * s, 0.95f * s, 0.18f * s), Vector3.one * 0.16f * s, null, "GasPocket");
                    var muzzlePoison = new GameObject("Muzzle").transform;
                    muzzlePoison.SetParent(visual, false);
                    muzzlePoison.localPosition = new Vector3(0, 0.75f * s, 0.6f * s);
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
                    for (int i = 0; i < 3; i++)
                    {
                        float z = (-0.55f + i * 0.55f) * s;
                        Part(visual, PrimitiveType.Cylinder, "metal_light", new Vector3(-0.82f * s, 0.35f * s, z), new Vector3(0.18f * s, 0.08f * s, 0.18f * s), new Vector3(90f, 0, 0), "WheelL");
                        Part(visual, PrimitiveType.Cylinder, "metal_light", new Vector3(0.82f * s, 0.35f * s, z), new Vector3(0.18f * s, 0.08f * s, 0.18f * s), new Vector3(90f, 0, 0), "WheelR");
                    }
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
                case "sensor":
                {
                    Part(visual, PrimitiveType.Cylinder, body, new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.45f, 1.4f), null, "Base");
                    Part(visual, PrimitiveType.Cylinder, "metal_dark", new Vector3(0, 1.55f, 0), new Vector3(0.35f, 1.7f, 0.35f), null, "Mast");
                    Part(visual, PrimitiveType.Sphere, accent, new Vector3(0, 2.85f, 0), Vector3.one * 0.55f, null, "Beacon");
                    Part(visual, PrimitiveType.Cylinder, "metal_light", new Vector3(0, 2.2f, 0), new Vector3(1.25f, 0.07f, 1.25f), null, "DishRing");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0, 2.2f, 0.75f), new Vector3(0.12f, 0.12f, 1.2f), new Vector3(0, 0, 6f), "ScannerBoom");
                    Part(visual, PrimitiveType.Cube, accent, new Vector3(0.75f, 2.2f, 0), new Vector3(1.2f, 0.12f, 0.12f), new Vector3(0, 0, -6f), "ScannerBoom");
                    break;
                }
                case "bubble_spring":
                {
                    Part(visual, PrimitiveType.Cylinder, "crystal", new Vector3(0, 0.35f, 0), new Vector3(1.25f, 0.35f, 1.25f), null, "FoamBasin");
                    Part(visual, PrimitiveType.Sphere, "rally", new Vector3(0, 0.95f, 0), Vector3.one * 0.7f, null, "FoamDome");
                    Part(visual, PrimitiveType.Sphere, "crystal", new Vector3(0.55f, 1.35f, 0.15f), Vector3.one * 0.32f, null, "BubbleBud");
                    Part(visual, PrimitiveType.Sphere, "crystal", new Vector3(-0.35f, 1.25f, -0.25f), Vector3.one * 0.24f, null, "BubbleBud");
                    break;
                }
                case "poison_pool":
                {
                    Part(visual, PrimitiveType.Cylinder, "zerg_body", new Vector3(0, 0.22f, 0), new Vector3(1.35f, 0.22f, 1.35f), null, "PoolRim");
                    Part(visual, PrimitiveType.Cylinder, "zerg_accent", new Vector3(0, 0.5f, 0), new Vector3(1.05f, 0.12f, 1.05f), null, "PoisonSurface");
                    Part(visual, PrimitiveType.Sphere, "zerg_accent", new Vector3(0.35f, 1.0f, 0.2f), Vector3.one * 0.24f, null, "ToxicBubble");
                    Part(visual, PrimitiveType.Sphere, "zerg_accent", new Vector3(-0.4f, 0.9f, -0.15f), Vector3.one * 0.18f, null, "ToxicBubble");
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
