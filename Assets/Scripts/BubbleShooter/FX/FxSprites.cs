using UnityEngine;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Code se banne wale (procedural) sprites jo effects aur UI mein use hote hain.
    /// Har sprite sirf ek dafa banta hai aur phir cache se milta hai,
    /// is liye koi image import karne ki zaroorat nahi.
    /// </summary>
    public static class FxSprites
    {
        private static Sprite softDot;
        private static Sprite circle;
        private static Sprite ring;
        private static Sprite sparkle;
        private static Sprite star;
        private static Sprite roundedRect;
        private static Sprite roundedFrame;
        private static Sprite vignette;
        private static Sprite verticalGradient;
        private static Sprite sunburst;
        private static Sprite pixel;

        /// <summary>Narm (soft) gol nuqta: glow, trail, bokeh ke liye.</summary>
        public static Sprite SoftDot
        {
            get
            {
                if (softDot == null)
                {
                    softDot = Build("FX_SoftDot", 64, (nx, ny) =>
                    {
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float a = Mathf.Clamp01(1f - d);
                        return new Color(1f, 1f, 1f, a * a);
                    });
                }
                return softDot;
            }
        }

        /// <summary>Saaf kinare wala bhara hua circle (anti-aliased).</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    const int size = 64;
                    circle = Build("FX_Circle", size, (nx, ny) =>
                    {
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float a = Mathf.Clamp01((1f - d) * size * 0.5f);
                        return new Color(1f, 1f, 1f, a);
                    });
                }
                return circle;
            }
        }

        /// <summary>Patla ring (shockwave / reticle).</summary>
        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                {
                    const int size = 128;
                    ring = Build("FX_Ring", size, (nx, ny) =>
                    {
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        float edge = Mathf.Abs(d - 0.86f);
                        float a = Mathf.Clamp01((0.1f - edge) * size * 0.25f);
                        float glow = Mathf.Clamp01(1f - edge / 0.16f) * 0.35f;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01(a + glow));
                    });
                }
                return ring;
            }
        }

        /// <summary>Chaar nok wala chamakta sitara (sparkle).</summary>
        public static Sprite Sparkle
        {
            get
            {
                if (sparkle == null)
                {
                    sparkle = Build("FX_Sparkle", 64, (nx, ny) =>
                    {
                        float ax = Mathf.Abs(nx);
                        float ay = Mathf.Abs(ny);
                        float arm = Mathf.Max(
                            Mathf.Clamp01(1f - ax / 0.12f) * Mathf.Clamp01(1f - ay),
                            Mathf.Clamp01(1f - ay / 0.12f) * Mathf.Clamp01(1f - ax));
                        float core = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny) / 0.35f);
                        float a = Mathf.Clamp01(arm * arm + core * core);
                        return new Color(1f, 1f, 1f, a);
                    });
                }
                return sparkle;
            }
        }

        /// <summary>Paanch nok wala sunehri sitara (outline aur gradient ke saath).</summary>
        public static Sprite Star
        {
            get
            {
                if (star == null)
                {
                    const int size = 128;
                    star = Build("FX_Star", size, (nx, ny) =>
                    {
                        float d = StarDistance(nx, ny);
                        // d < 0: andar. Outline + gradient
                        float aa = 2.5f / size;
                        float alpha = Mathf.Clamp01((-d) / aa + 0.5f);
                        if (alpha <= 0f) return new Color(0f, 0f, 0f, 0f);

                        Color top = new Color(1f, 0.93f, 0.35f);
                        Color bottom = new Color(1f, 0.62f, 0.05f);
                        Color body = Color.Lerp(bottom, top, (ny + 1f) * 0.5f);

                        // upar wali chamak
                        if (ny > 0.05f && nx < 0.15f && d < -0.09f)
                            body = Color.Lerp(body, Color.white, 0.35f);

                        Color outline = new Color(0.62f, 0.3f, 0.02f);
                        float o = Mathf.Clamp01((d + 0.07f) / aa + 0.5f);
                        Color c = Color.Lerp(body, outline, o);
                        c.a = alpha;
                        return c;
                    });
                }
                return star;
            }
        }

        /// <summary>9-slice gol kinaron wala bhara hua rectangle (panel, chip).</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (roundedRect == null)
                    roundedRect = BuildRounded("FX_RoundedRect", false);
                return roundedRect;
            }
        }

        /// <summary>9-slice gol kinaron wala sirf frame (neon border).</summary>
        public static Sprite RoundedFrame
        {
            get
            {
                if (roundedFrame == null)
                    roundedFrame = BuildRounded("FX_RoundedFrame", true);
                return roundedFrame;
            }
        }

        /// <summary>Kinaron par andhera, beech mein saaf (screen vignette).</summary>
        public static Sprite Vignette
        {
            get
            {
                if (vignette == null)
                {
                    vignette = Build("FX_Vignette", 128, (nx, ny) =>
                    {
                        float d = Mathf.Sqrt(nx * nx * 0.8f + ny * ny * 0.8f);
                        float a = Mathf.Clamp01((d - 0.55f) / 0.6f);
                        return new Color(1f, 1f, 1f, a * a);
                    });
                }
                return vignette;
            }
        }

        /// <summary>Upar se neeche: neeche saaf (alpha 0), upar bhara (alpha 1).</summary>
        public static Sprite VerticalGradient
        {
            get
            {
                if (verticalGradient == null)
                {
                    const int w = 4;
                    const int h = 128;
                    Texture2D tex = NewTexture(w, h, "FX_VerticalGradient");
                    Color[] px = new Color[w * h];
                    for (int y = 0; y < h; y++)
                    {
                        float a = y / (float)(h - 1);
                        for (int x = 0; x < w; x++)
                            px[y * w + x] = new Color(1f, 1f, 1f, a);
                    }
                    tex.SetPixels(px);
                    tex.Apply();
                    verticalGradient = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
                    verticalGradient.name = "FX_VerticalGradient";
                }
                return verticalGradient;
            }
        }

        /// <summary>Roshni ki shuaaein (rays) - win panel ke peeche ghoomti hain.</summary>
        public static Sprite Sunburst
        {
            get
            {
                if (sunburst == null)
                {
                    sunburst = Build("FX_Sunburst", 256, (nx, ny) =>
                    {
                        float d = Mathf.Sqrt(nx * nx + ny * ny);
                        if (d > 1f) return new Color(1f, 1f, 1f, 0f);
                        float ang = Mathf.Atan2(ny, nx);
                        float rays = Mathf.Abs(Mathf.Sin(ang * 8f));
                        float ray = Mathf.Clamp01((rays - 0.55f) / 0.25f);
                        float fade = Mathf.Clamp01(1f - d) * Mathf.Clamp01(d / 0.12f);
                        float center = Mathf.Clamp01(1f - d / 0.45f) * 0.6f;
                        return new Color(1f, 1f, 1f, Mathf.Clamp01(ray * fade * 0.9f + center * center));
                    });
                }
                return sunburst;
            }
        }

        /// <summary>Saada safed square (confetti, line segments).</summary>
        public static Sprite Pixel
        {
            get
            {
                if (pixel == null)
                {
                    Texture2D tex = NewTexture(4, 4, "FX_Pixel");
                    Color[] px = new Color[16];
                    for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                    tex.SetPixels(px);
                    tex.Apply();
                    pixel = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    pixel.name = "FX_Pixel";
                }
                return pixel;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private delegate Color PixelFunc(float nx, float ny);

        private static Texture2D NewTexture(int w, int h, string name)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        /// <summary>Square texture banata hai. nx, ny = -1..1 (beech 0).</summary>
        private static Sprite Build(string name, int size, PixelFunc func)
        {
            Texture2D tex = NewTexture(size, size, name);
            Color[] px = new Color[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f - half) / half;
                    float ny = (y + 0.5f - half) / half;
                    px[y * size + x] = func(nx, ny);
                }
            }

            tex.SetPixels(px);
            tex.Apply();

            Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s.name = name;
            return s;
        }

        private static Sprite BuildRounded(string name, bool frameOnly)
        {
            const int size = 96;
            const float radius = 36f;
            const float stroke = 7f;

            Texture2D tex = NewTexture(size, size, name);
            Color[] px = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = RoundedBoxDistance(x + 0.5f, y + 0.5f, size, size, radius);
                    float fill = Mathf.Clamp01(0.5f - d);
                    float a;

                    if (frameOnly)
                    {
                        float inner = Mathf.Clamp01(0.5f - (d + stroke));
                        float line = Mathf.Clamp01(fill - inner);
                        // narm glow andar ki taraf
                        float glow = Mathf.Clamp01(1f - (-d) / (stroke * 2.6f)) * 0.35f * fill;
                        a = Mathf.Clamp01(line + glow);
                    }
                    else
                    {
                        a = fill;
                    }

                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(px);
            tex.Apply();

            Vector4 border = new Vector4(radius + 4f, radius + 4f, radius + 4f, radius + 4f);
            Sprite s = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
            s.name = name;
            return s;
        }

        /// <summary>Signed distance: rounded rectangle (pixels). Andar manfi.</summary>
        private static float RoundedBoxDistance(float x, float y, float w, float h, float r)
        {
            float px = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
            float py = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
            float ox = Mathf.Max(px, 0f);
            float oy = Mathf.Max(py, 0f);
            float outside = Mathf.Sqrt(ox * ox + oy * oy);
            float inside = Mathf.Min(Mathf.Max(px, py), 0f);
            return outside + inside - r;
        }

        /// <summary>Paanch nok wale sitare ka andaazan distance (-1..1 space).</summary>
        private static float StarDistance(float x, float y)
        {
            // thora neeche khiska hua taake sitara beech mein lage
            y += 0.06f;

            const float outer = 0.95f;
            const float inner = 0.42f;

            float ang = Mathf.Atan2(x, y);              // upar se ghadi ki suyi
            float seg = Mathf.PI * 2f / 5f;
            float a = Mathf.Repeat(ang + seg * 0.5f, seg) - seg * 0.5f;
            float r = Mathf.Sqrt(x * x + y * y);

            // Nok (a=0) se andar wale kone (a=seg/2) tak seedhi line
            Vector2 tip = new Vector2(0f, outer);
            Vector2 valley = new Vector2(Mathf.Sin(seg * 0.5f) * inner, Mathf.Cos(seg * 0.5f) * inner);
            Vector2 p = new Vector2(Mathf.Sin(Mathf.Abs(a)) * r, Mathf.Cos(Mathf.Abs(a)) * r);

            Vector2 e = valley - tip;
            Vector2 n = new Vector2(-e.y, e.x).normalized;   // bahar ki taraf
            return Vector2.Dot(p - tip, n);
        }
    }
}
