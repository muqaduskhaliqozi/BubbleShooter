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
            Texture2D tex = NewTexture();
            float c = (Size - 1) / 2f;
            float r = Size / 2f - 2f;

            Vector3 light = new Vector3(-0.4f, 0.5f, 0.75f).normalized;
            Vector3 bounce = new Vector3(0.3f, -0.7f, 0.5f).normalized;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x - c) / r;
                    float ny = (y - c) / r;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);

                    if (d > 1.02f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d * d));
                    Vector3 n = new Vector3(nx, ny, nz);

                    float diffuse = Mathf.Clamp01(Vector3.Dot(n, light));
                    float shade = 0.45f + diffuse * 0.55f;

                    float b = Mathf.Pow(Mathf.Clamp01(Vector3.Dot(n, bounce)), 3f) * 0.2f;
                    shade += b;

                    // kinare gehre
                    shade *= Mathf.Lerp(1f, 0.6f, Mathf.Pow(d, 3f));
                    shade = Mathf.Clamp01(shade);

                    float alpha = Mathf.Clamp01((1f - d) * r / 1.5f);
                    tex.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

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

                        // 3) neeche-right ki rim reflection
                        if (nx > -0.1f && ny < 0.1f && d > 0.72f)
                        {
                            float rim = Mathf.Clamp01((d - 0.72f) / 0.2f) *
                                        Mathf.Clamp01((0.98f - d) / 0.06f);
                            a += rim * 0.28f;
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