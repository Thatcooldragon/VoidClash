using System.Collections.Generic;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Creates/caches URP materials by name. Uses baked .mat assets from the
    /// GameDatabase when available, otherwise builds them in memory (same definitions).</summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static void Clear() => Cache.Clear();

        public static Material Get(string name)
        {
            if (Cache.TryGetValue(name, out var m) && m != null) return m;
            if (name == "ground")
            {
                m = Build(name);
                Cache[name] = m;
                return m;
            }
            Material baked = G.DB != null ? G.DB.FindMaterial(name) : null;
            m = baked != null ? baked : Build(name);
            Cache[name] = m;
            return m;
        }

        /// <summary>Every named material definition. Editor setup enumerates these to bake assets.</summary>
        public static readonly string[] AllNames =
        {
            "ground", "cliff", "rock", "metal_dark", "metal_light",
            "player_body", "enemy_body", "player_accent", "enemy_accent",
            "zerg_body", "zerg_accent", "protoss_body", "protoss_accent",
            "crystal", "crystal_dim", "particle_add", "ghost_valid", "ghost_invalid",
            "hp_back", "hp_green", "hp_yellow", "hp_red", "hp_build",
            "ring_player", "ring_enemy", "ring_hover", "fog", "marker_move", "marker_attack",
            "projectile_player", "projectile_enemy", "rally", "frost", "overdrive"
        };

        public static Material Build(string name)
        {
            switch (name)
            {
                case "ground":
                {
                    var m = Lit("mat_ground", new Color(0.7f, 0.78f, 0.86f));
                    m.mainTexture = TextureFactory.MakeGround();
                    m.mainTextureScale = new Vector2(1f, 1f);
                    m.SetFloat("_Smoothness", 0.16f);
                    return m;
                }
                case "cliff":
                {
                    var m = Lit("mat_cliff", new Color(0.78f, 0.76f, 0.85f), 0.08f);
                    m.mainTexture = TextureFactory.MakeRock(512, 0.95f, 0.95f, 1.1f);
                    m.mainTextureScale = new Vector2(0.9f, 0.9f);
                    SetNormal(m, TextureFactory.MakeNormalMap(512, 14f, 3.2f, 4, 8.7f));
                    return m;
                }
                case "rock":
                {
                    var m = Lit("mat_rock", new Color(0.95f, 0.9f, 1f), 0.12f);
                    m.mainTexture = TextureFactory.MakeRock(512, 1.05f, 1f, 1.05f);
                    m.mainTextureScale = new Vector2(1.6f, 1.6f);
                    SetNormal(m, TextureFactory.MakeNormalMap(512, 18f, 3.0f, 4, 5.3f));
                    return m;
                }
                case "metal_dark": return Lit("mat_metal_dark", new Color(0.22f, 0.24f, 0.28f), 0.55f, 0.6f);
                case "metal_light": return Lit("mat_metal_light", new Color(0.55f, 0.58f, 0.64f), 0.6f, 0.7f);
                case "player_body": return Lit("mat_player_body", new Color(0.23f, 0.35f, 0.55f), 0.45f, 0.5f);
                case "enemy_body": return Lit("mat_enemy_body", new Color(0.55f, 0.22f, 0.2f), 0.45f, 0.5f);
                case "player_accent": return Emissive("mat_player_accent", new Color(0.25f, 0.62f, 1f), 2.2f);
                case "enemy_accent": return Emissive("mat_enemy_accent", new Color(1f, 0.3f, 0.25f), 2.2f);
                case "zerg_body": return Lit("mat_zerg_body", new Color(0.42f, 0.18f, 0.28f), 0.55f, 0.05f);
                case "zerg_accent": return Emissive("mat_zerg_accent", new Color(1f, 0.25f, 0.55f), 2.4f);
                case "protoss_body": return Lit("mat_protoss_body", new Color(0.55f, 0.45f, 0.2f), 0.7f, 0.85f);
                case "protoss_accent": return Emissive("mat_protoss_accent", new Color(0.35f, 0.95f, 1f), 2.6f);
                case "crystal": return Emissive("mat_crystal", new Color(0.3f, 0.9f, 1f), 1.6f, new Color(0.2f, 0.55f, 0.75f));
                case "crystal_dim": return Lit("mat_crystal_dim", new Color(0.25f, 0.32f, 0.38f), 0.4f);
                case "particle_add":
                {
                    var m = new Material(FindShader("Universal Render Pipeline/Particles/Unlit")) { name = "mat_particle_add" };
                    m.mainTexture = TextureFactory.MakeSoftCircle();
                    SetTransparent(m, true);
                    return m;
                }
                case "ghost_valid": return UnlitTransparent("mat_ghost_valid", new Color(0.2f, 1f, 0.4f, 0.45f));
                case "ghost_invalid": return UnlitTransparent("mat_ghost_invalid", new Color(1f, 0.2f, 0.2f, 0.45f));
                case "hp_back": return UnlitColor("mat_hp_back", new Color(0.06f, 0.06f, 0.08f, 1f));
                case "hp_green": return UnlitColor("mat_hp_green", new Color(0.25f, 0.95f, 0.3f, 1f));
                case "hp_yellow": return UnlitColor("mat_hp_yellow", new Color(0.95f, 0.85f, 0.2f, 1f));
                case "hp_red": return UnlitColor("mat_hp_red", new Color(0.95f, 0.25f, 0.2f, 1f));
                case "hp_build": return UnlitColor("mat_hp_build", new Color(0.3f, 0.75f, 1f, 1f));
                case "ring_player": return RingMat("mat_ring_player", new Color(0.3f, 1f, 0.5f, 0.9f));
                case "ring_enemy": return RingMat("mat_ring_enemy", new Color(1f, 0.3f, 0.25f, 0.9f));
                case "ring_hover": return RingMat("mat_ring_hover", new Color(0.9f, 0.95f, 1f, 0.35f));
                case "fog":
                {
                    var m = UnlitTransparent("mat_fog", new Color(0f, 0f, 0f, 1f));
                    m.renderQueue = 3500;
                    return m;
                }
                case "marker_move": return RingMat("mat_marker_move", new Color(0.3f, 1f, 0.5f, 1f));
                case "marker_attack": return RingMat("mat_marker_attack", new Color(1f, 0.25f, 0.2f, 1f));
                case "projectile_player": return Emissive("mat_proj_player", new Color(0.4f, 0.8f, 1f), 3.5f);
                case "projectile_enemy": return Emissive("mat_proj_enemy", new Color(1f, 0.45f, 0.3f), 3.5f);
                case "rally": return Emissive("mat_rally", new Color(0.4f, 1f, 0.6f), 2f);
                case "frost": return UnlitTransparent("mat_frost", new Color(0.55f, 0.85f, 1f, 0.45f));
                case "overdrive":
                {
                    var m = UnlitTransparent("mat_overdrive", new Color(0.6f, 0.4f, 0.12f, 1f));
                    SetTransparent(m, true); // additive warm glow aura
                    return m;
                }
                default:
                    Debug.LogError($"VoidClash: unknown material '{name}'");
                    return Lit("mat_missing", Color.magenta);
            }
        }

        static Shader FindShader(string name)
        {
            var s = Shader.Find(name);
            if (s == null) Debug.LogError($"VoidClash: shader not found '{name}'");
            return s;
        }

        static void SetNormal(Material m, Texture2D normal)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1f);
        }

        static Material Lit(string name, Color c, float smooth = 0.3f, float metallic = 0f)
        {
            var m = new Material(FindShader("Universal Render Pipeline/Lit")) { name = name, color = c };
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metallic);
            return m;
        }

        static Material Emissive(string name, Color emission, float intensity, Color? baseColor = null)
        {
            var m = Lit(name, baseColor ?? (emission * 0.35f), 0.6f, 0.2f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission * intensity);
            return m;
        }

        static Material UnlitColor(string name, Color c)
        {
            var m = new Material(FindShader("Universal Render Pipeline/Unlit")) { name = name, color = c };
            m.SetColor("_BaseColor", c);
            return m;
        }

        static Material UnlitTransparent(string name, Color c)
        {
            var m = new Material(FindShader("Universal Render Pipeline/Unlit")) { name = name };
            SetTransparent(m, false);
            m.color = c;
            m.SetColor("_BaseColor", c);
            return m;
        }

        static Material RingMat(string name, Color c)
        {
            var m = UnlitTransparent(name, c);
            m.mainTexture = TextureFactory.MakeRing();
            m.SetTexture("_BaseMap", m.mainTexture);
            return m;
        }

        /// <summary>Standard URP transparent surface setup (works for Unlit and Particles/Unlit).</summary>
        public static void SetTransparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1f); // Transparent
            m.SetFloat("_Blend", additive ? 2f : 0f); // Additive : Alpha
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive
                ? (float)UnityEngine.Rendering.BlendMode.One
                : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_ALPHAMODULATE_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
