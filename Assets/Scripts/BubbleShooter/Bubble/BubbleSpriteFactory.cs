using UnityEngine;

namespace BubbleShooter.Bubble
{
    public static class BubbleSpriteFactory
    {
        private const int Size = 256;
        private static Sprite baseSprite;
        private static Sprite glossSprite;

        public static Sprite GetBaseSprite()
        {
            if (baseSprite == null) baseSprite = BuildBase();
            return baseSprite;
        }

        public static Sprite GetGlossSprite()
        {
            if (glossSprite == null) glossSprite = BuildGloss();
            return glossSprite;
        }

        private static Sprite BuildBase()
        {
            // Grey-scale shading: color SpriteRenderer.color se multiply hota hai.
            // Upgrade: gehri 3D shading, andar se roshni (subsurface), saaf gehra kinara.
            Texture2D tex = NewTexture();
            float c = (Size - 1) / 2f;
            float r = Size / 2f - 2f;

            Vector3 light = new Vector3(-0.45f, 0.55f, 0.7f).normalized;
            Vector3 bounce = new Vector3(0.35f, -0.75f, 0.45f).normalized;

            Color[] px = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x - c) / r;
                    float ny = (y - c) / r;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);

                    if (d > 1.02f)
                    {
                        px[y * Size + x] = Color.clear;
                        continue;
                    }

                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d * d));
                    Vector3 n = new Vector3(nx, ny, nz);

                    float diffuse = Mathf.Clamp01(Vector3.Dot(n, light));
                    float shade = 0.5f + diffuse * 0.5f;

                    // neeche se aane wali halki roshni (bounce)
                    shade += Mathf.Pow(Mathf.Clamp01(Vector3.Dot(n, bounce)), 3f) * 0.22f;

                    // andar ki chamak (bubble ke beech thori roshni, glassy)
                    float inner = Mathf.Clamp01(1f - Mathf.Sqrt((nx + 0.1f) * (nx + 0.1f) + (ny + 0.15f) * (ny + 0.15f)) / 0.75f);
                    shade += inner * inner * 0.12f;

                    // kinare gehre
                    shade *= Mathf.Lerp(1f, 0.55f, Mathf.Pow(d, 3.2f));

                    // saaf patli outline (rang ka gehra roop)
                    float rim = Mathf.Clamp01((d - 0.9f) / 0.08f);
                    shade = Mathf.Lerp(shade, shade * 0.62f, rim);

                    shade = Mathf.Clamp01(shade);

                    float alpha = Mathf.Clamp01((1f - d) * r / 1.5f);
                    px[y * Size + x] = new Color(shade, shade, shade, alpha);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 256f);
        }

        private static Sprite BuildGloss()
        {
            Texture2D tex = NewTexture();
            float c = (Size - 1) / 2f;
            float r = Size / 2f - 2f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x - c) / r;
                    float ny = (y - c) / r;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);

                    float a = 0f;

                    if (d < 0.98f)
                    {
                        // 1) bada soft highlight (upar-left)
                        float dx = (nx + 0.35f) / 0.38f;
                        float dy = (ny - 0.42f) / 0.26f;
                        float e = Mathf.Sqrt(dx * dx + dy * dy);
                        if (e < 1f) a += Mathf.Pow(1f - e, 1.4f) * 0.85f;

                        // 2) chhota tez chamakta point
                        float hx = nx + 0.28f;
                        float hy = ny - 0.5f;
                        float h = Mathf.Sqrt(hx * hx + hy * hy) / 0.09f;
                        if (h < 1f) a += (1f - h) * 0.9f;

                        // 2b) neeche wala patla chaand (crescent) reflection
                        float cx = nx - 0.05f;
                        float cy = ny + 0.62f;
                        float cres = Mathf.Sqrt(cx * cx / 0.42f + cy * cy / 0.06f);
                        if (cres < 1f && ny < -0.35f) a += (1f - cres) * 0.35f;

                        // 3) neeche-right ki rim reflection
                        // (naram kinaron ke saath, pehle yahan saaf chaukor kona dikhta tha)
                        if (d > 0.72f)
                        {
                            float side = Mathf.Clamp01((nx + 0.25f) / 0.45f) *
                                         Mathf.Clamp01((0.25f - ny) / 0.45f);
                            float rim = Mathf.Clamp01((d - 0.72f) / 0.2f) *
                                        Mathf.Clamp01((0.98f - d) / 0.06f);
                            a += rim * side * 0.3f;
                        }
                    }

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 256f);
        }

        private static Texture2D NewTexture()
        {
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }
}