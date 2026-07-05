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
            var baseCol = new Color32(24, 29, 35, 255);
            var subtle = new Color32(28, 34, 41, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool checker = ((x / 64) + (y / 64)) % 2 == 0;
                    px[y * size + x] = checker ? baseCol : subtle;
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
