using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alibi.Unity
{
    public static class Theme
    {
        public static readonly Color Night = Hex("#0D1015");
        public static readonly Color Bar = Hex("#10131A");
        public static readonly Color Panel = Hex("#141820");
        public static readonly Color Panel2 = Hex("#181C25");
        public static readonly Color Panel3 = Hex("#1F2530");
        public static readonly Color Line = Hex("#2A313D");
        public static readonly Color Line2 = Hex("#3A4352");
        public static readonly Color Text = Hex("#ECE6D8");
        public static readonly Color Text2 = Hex("#C9C2B2");
        public static readonly Color Muted = Hex("#A39E92");
        public static readonly Color Dim = Hex("#6E747D");
        public static readonly Color Gold = Hex("#D2AD62");
        public static readonly Color GoldText = Hex("#F4DFB0");
        public static readonly Color Ink = Hex("#17130B");
        public static readonly Color Red = Hex("#C4483D");
        public static readonly Color RedSoft = Hex("#3A1D1D");
        public static readonly Color RedText = Hex("#F09A8F");
        public static readonly Color Blue = Hex("#A9C3E6");
        public static readonly Color BlueLine = Hex("#3A5576");
        public static readonly Color Parchment = Hex("#EFE4C9");
        public static readonly Color ParchInk = Hex("#2A2218");
        public static readonly Color ParchMuted = Hex("#6B5A40");
        public static readonly Color Shade = new Color(0.02f, 0.03f, 0.04f, 0.78f);

        static Font serif, serifItalic, sans, sansSemi, sansBold, mono;
        static readonly Dictionary<int, Sprite> rounded = new Dictionary<int, Sprite>();
        static readonly Dictionary<int, Sprite> outlines = new Dictionary<int, Sprite>();
        static Sprite star, ring;
        static Material litBase;
        static Shader ghostShader;
        static readonly Dictionary<string, Texture2D> floors = new Dictionary<string, Texture2D>();

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }

        public static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        public static Color Shift(Color c, float f)
        {
            if (f < 0) return new Color(c.r * (1 + f), c.g * (1 + f), c.b * (1 + f), c.a);
            return new Color(c.r + (1 - c.r) * f, c.g + (1 - c.g) * f, c.b + (1 - c.b) * f, c.a);
        }

        static Font Load(string name)
        {
            var f = Resources.Load<Font>("AlibiFonts/" + name);
            if (f != null) return f;
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        public static Font Serif { get { return serif != null ? serif : (serif = Load("CormorantGaramond-Bold")); } }
        public static Font SerifItalic { get { return serifItalic != null ? serifItalic : (serifItalic = Load("CormorantGaramond-SemiBoldItalic")); } }
        public static Font Sans { get { return sans != null ? sans : (sans = Load("Inter-Regular")); } }
        public static Font SansSemi { get { return sansSemi != null ? sansSemi : (sansSemi = Load("Inter-SemiBold")); } }
        public static Font SansBold { get { return sansBold != null ? sansBold : (sansBold = Load("Inter-Bold")); } }
        public static Font Mono { get { return mono != null ? mono : (mono = Load("JetBrainsMono-Medium")); } }

        static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        public static Sprite Rounded(int r)
        {
            Sprite s;
            if (rounded.TryGetValue(r, out s) && s != null) return s;
            s = BuildRounded(r, -1f);
            rounded[r] = s;
            return s;
        }

        public static Sprite OutlineSprite(int r)
        {
            Sprite s;
            if (outlines.TryGetValue(r, out s) && s != null) return s;
            s = BuildRounded(r, 1.6f);
            outlines[r] = s;
            return s;
        }

        static Sprite BuildRounded(int r, float width)
        {
            int size = r * 2 + 5;
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float half = size / 2f;
            float core = half - r - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - core, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - core, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - d - 1.5f + 0.5f);
                    if (width > 0) a *= Mathf.Clamp01(d - (r - 1.5f - width) + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            float b = r + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        public static Sprite Star
        {
            get
            {
                if (star != null) return star;
                const int size = 64;
                var tex = NewTexture(size, size);
                var pts = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                    float rad = i % 2 == 0 ? 30f : 12.5f;
                    pts[i] = new Vector2(32f + Mathf.Cos(ang) * rad, 33f + Mathf.Sin(ang) * rad);
                }
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int hits = 0;
                        for (int sy = 0; sy < 4; sy++)
                            for (int sx = 0; sx < 4; sx++)
                                if (Inside(pts, new Vector2(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f))) hits++;
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 16));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                star = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return star;
            }
        }

        static bool Inside(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) && (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)) inside = !inside;
            }
            return inside;
        }

        public static Sprite Ring
        {
            get
            {
                if (ring != null) return ring;
                const int size = 128;
                var tex = NewTexture(size, size);
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - 64f, dy = y + 0.5f - 64f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.Abs(d - 56f) / 5f);
                        float ang = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f;
                        float seg = ang * 14f - Mathf.Floor(ang * 14f);
                        if (seg > 0.62f) a = 0;
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return ring;
            }
        }

        public static bool UsesSrp
        {
            get { return GraphicsSettings.currentRenderPipeline != null; }
        }

        public static Material Lit(Color c, float smoothness = 0.15f)
        {
            if (litBase == null)
            {
                litBase = Resources.Load<Material>("AlibiLit");
                if (litBase == null)
                {
                    Shader sh = null;
                    if (UsesSrp) sh = Shader.Find("Universal Render Pipeline/Lit");
                    if (sh == null) sh = Shader.Find("Standard");
                    if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
                    litBase = new Material(sh);
                }
            }
            var m = new Material(litBase);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        public static Material Ghost(Color c)
        {
            if (ghostShader == null) ghostShader = Shader.Find("Sprites/Default");
            var m = new Material(ghostShader);
            m.color = c;
            m.renderQueue = 3000;
            return m;
        }

        public static Material Floor(string kind, float tilesX, float tilesY)
        {
            var m = Lit(Color.white, kind == "marble" ? 0.45f : 0.12f);
            m.mainTexture = FloorTexture(kind);
            m.mainTextureScale = new Vector2(tilesX, tilesY);
            return m;
        }

        public static Texture2D FloorTexture(string kind)
        {
            Texture2D tex;
            if (floors.TryGetValue(kind, out tex) && tex != null) return tex;
            const int size = 128;
            tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    px[y * size + x] = FloorPixel(kind, x, y, size);
                }
            }
            tex.SetPixels(px);
            tex.Apply(true);
            floors[kind] = tex;
            return tex;
        }

        static Color FloorPixel(string kind, int x, int y, int size)
        {
            float u = x / (float)size, v = y / (float)size;
            switch (kind)
            {
                case "oak":
                case "walnut":
                case "stairs":
                {
                    Color baseC = kind == "oak" ? Hex("#7A5A3C") : kind == "walnut" ? Hex("#5A4030") : Hex("#6A5442");
                    int plank = y / 16;
                    float tone = (Mathf.PerlinNoise(plank * 3.1f, 0.5f) - 0.5f) * 0.25f;
                    float grain = (Mathf.PerlinNoise(u * 3f + plank * 7f, v * 40f) - 0.5f) * 0.18f;
                    float seam = (y % 16 == 0) ? -0.35f : 0f;
                    float endSeam = ((x + plank * 37) % 64 == 0) ? -0.3f : 0f;
                    return Shift(baseC, tone + grain + seam + endSeam);
                }
                case "marble":
                {
                    float n = Mathf.PerlinNoise(u * 4f, v * 4f) + 0.5f * Mathf.PerlinNoise(u * 9f, v * 9f);
                    float vein = Mathf.Abs(Mathf.Sin((u + v + n * 0.6f) * 9f));
                    float k = Mathf.Lerp(0.08f, -0.22f, Mathf.Pow(1f - vein, 8f));
                    bool grout = x % 64 == 0 || y % 64 == 0;
                    return Shift(Hex("#8C867D"), grout ? -0.3f : k);
                }
                case "tile":
                {
                    bool dark = ((x / 32) + (y / 32)) % 2 == 0;
                    bool grout = x % 32 == 0 || y % 32 == 0;
                    Color c = dark ? Hex("#4E5658") : Hex("#6C7476");
                    float n = (Mathf.PerlinNoise(u * 12f, v * 12f) - 0.5f) * 0.08f;
                    return grout ? Hex("#2E3436") : Shift(c, n);
                }
                case "carpet":
                {
                    float d = Mathf.Abs(((x + y) % 32) - 16) + Mathf.Abs(((x - y + 256) % 32) - 16);
                    bool line = d > 14.5f && d < 16.5f;
                    float n = (Mathf.PerlinNoise(u * 30f, v * 30f) - 0.5f) * 0.1f;
                    return Shift(line ? Hex("#5C6B3E") : Hex("#2F4A38"), n);
                }
                case "runner":
                {
                    bool border = (x > 18 && x < 23) || (x > 105 && x < 110);
                    bool edge = x < 14 || x > 114;
                    float n = (Mathf.PerlinNoise(u * 25f, v * 25f) - 0.5f) * 0.1f;
                    if (edge) return Shift(Hex("#5A4030"), n);
                    return Shift(border ? Hex("#B08A45") : Hex("#6E2A2A"), n);
                }
                default:
                    return Hex("#555555");
            }
        }
    }
}
