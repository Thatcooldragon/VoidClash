using UnityEngine;

namespace VoidClash
{
    /// <summary>Generates every texture the game uses. Pure CPU, deterministic.</summary>
    public static class TextureFactory
    {
        /// <summary>Multi-octave Perlin (fractional Brownian motion).</summary>
        static float Fbm(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 0.5f, freq = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * freq + i * 17.13f, y * freq + i * 31.7f) * amp;
                amp *= gain;
                freq *= lacunarity;
            }
            return sum;
        }

        /// <summary>Ridged noise in [0,1]: 1 at "crack" lines.</summary>
        static float Ridge(float x, float y)
            => 1f - Mathf.Abs(Mathf.PerlinNoise(x, y) * 2f - 1f);

        public static Texture2D MakeGround(int size = 1024)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "tex_ground";
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[size * size];
            var deck = new Color(0.075f, 0.09f, 0.11f);
            var panel = new Color(0.105f, 0.125f, 0.15f);
            var seam = new Color(0.015f, 0.025f, 0.035f);
            var glow = new Color(0.14f, 0.34f, 0.43f);
            var hazard = new Color(0.55f, 0.38f, 0.11f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int panelX = x / 128;
                    int panelY = y / 128;
                    bool checker = (panelX + panelY) % 2 == 0;
                    float fx = x / (float)size;
                    float fy = y / (float)size;
                    float n = Fbm(fx * 18f + 2.3f, fy * 18f + 5.7f, 4) - 0.45f;
                    Color c = Color.Lerp(deck, panel, checker ? 0.62f : 0.34f);
                    c += new Color(n, n, n) * 0.055f;

                    int lx = x & 127;
                    int ly = y & 127;
                    int edge = Mathf.Min(Mathf.Min(lx, 127 - lx), Mathf.Min(ly, 127 - ly));
                    if (edge < 3) c = Color.Lerp(c, seam, 0.82f);
                    else if (edge < 6) c = Color.Lerp(c, glow, 0.18f);

                    bool bolt = (lx == 18 || lx == 110) && (ly == 18 || ly == 110);
                    if (bolt || (Mathf.Abs(lx - ly) < 2 && lx > 78 && ly < 122 && ((panelX + panelY) % 5 == 0)))
                        c = Color.Lerp(c, hazard, bolt ? 0.65f : 0.35f);

                    bool dataLine = (lx == 63 || ly == 63) && ((panelX * 17 + panelY * 23) % 4 == 0);
                    if (dataLine) c = Color.Lerp(c, glow, 0.42f);

                    px[y * size + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            }
            tex.SetPixels32(px); tex.Apply(true);
            return tex;
        }

        /// <summary>Tangent-space normal map derived from an fBm heightfield.
        /// Plain RGB encoding with A=255 (URP's UnpackNormalmapRGorAG handles it).</summary>
        public static Texture2D MakeNormalMap(int size, float noiseScale, float strength, int octaves, float seed)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "tex_normal";
            tex.wrapMode = TextureWrapMode.Mirror;
            var px = new Color32[size * size];
            float H(int x, int y) => Fbm(x / (float)size * noiseScale + seed, y / (float)size * noiseScale + seed, octaves)
                                   + Ridge(x / (float)size * noiseScale * 1.6f + seed * 2f, y / (float)size * noiseScale * 1.6f + seed * 2f) * 0.35f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (H(x + 1, y) - H(x - 1, y)) * strength;
                    float dy = (H(x, y + 1) - H(x, y - 1)) * strength;
                    var n = new Vector3(-dx, -dy, 1f).normalized;
                    px[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            tex.SetPixels32(px); tex.Apply(true);
            return tex;
        }

        public static Texture2D MakeRock(int size = 512, float tintR = 1f, float tintG = 1f, float tintB = 1.08f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.name = "tex_rock";
            tex.wrapMode = TextureWrapMode.Mirror;
            var px = new Color32[size * size];
            var dark = new Color(0.16f, 0.15f, 0.185f);
            var mid = new Color(0.32f, 0.30f, 0.36f);
            var light = new Color(0.47f, 0.45f, 0.52f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x / (float)size, fy = y / (float)size;
                    // horizontal strata bands distorted by noise
                    float warp = Fbm(fx * 5f, fy * 5f, 3) * 0.5f;
                    float band = Mathf.PerlinNoise(3.7f, (fy + warp) * 9f);
                    float grain = Fbm(fx * 30f + 5f, fy * 30f + 9f, 3);
                    Color c = Color.Lerp(dark, mid, band);
                    c = Color.Lerp(c, light, grain * grain * 0.8f);
                    float ridge = Ridge(fx * 7f + 31f, fy * 7f + 47f);
                    if (ridge > 0.9f) c *= 0.55f; // deep fissures
                    px[y * size + x] = new Color(Mathf.Clamp01(c.r * tintR), Mathf.Clamp01(c.g * tintG), Mathf.Clamp01(c.b * tintB), 1f);
                }
            }
            tex.SetPixels32(px); tex.Apply(true);
            return tex;
        }

        public static Texture2D MakeSoftCircle(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "tex_softcircle";
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        public static Texture2D MakeRing(int size = 128, float inner = 0.72f, float outer = 0.95f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "tex_ring";
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                    float a = 0f;
                    if (d > inner && d < outer)
                    {
                        float t = Mathf.InverseLerp(inner, (inner + outer) * 0.5f, d) *
                                  (1f - Mathf.InverseLerp((inner + outer) * 0.5f, outer, d));
                        a = Mathf.Clamp01(t * 4f);
                    }
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        /// <summary>Filled rounded square used for UI buttons/panels (9-sliceable).</summary>
        public static Texture2D MakeRoundedRect(int size = 32, int radius = 6)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "tex_rounded";
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f;
                    int cx = Mathf.Min(x, size - 1 - x);
                    int cy = Mathf.Min(y, size - 1 - y);
                    if (cx < radius && cy < radius)
                    {
                        float d = Vector2.Distance(new Vector2(cx, cy), new Vector2(radius, radius));
                        a = Mathf.Clamp01(radius - d + 0.5f);
                    }
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        public static Texture2D MakeWhite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.name = "tex_white";
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Sprite _uiSprite, _circleSprite;
        public static Sprite UISprite
        {
            get
            {
                if (_uiSprite == null)
                {
                    var t = MakeRoundedRect();
                    _uiSprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                        new Vector4(8, 8, 8, 8));
                }
                return _uiSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null)
                {
                    var t = MakeSoftCircle(64);
                    _circleSprite = Sprite.Create(t, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
                }
                return _circleSprite;
            }
        }
    }
}
