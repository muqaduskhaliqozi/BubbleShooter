using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BubbleShooter.Grid;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Gameplay ka poora pas-manzar (background) code se banata hai:
    ///  - asli bg.png world mein, poori screen par (pehle UI mein 11% par dhundla tha)
    ///  - har level ka apna rang / theme
    ///  - tairti hui roshni ki bubbles (bokeh) aur timtimate sitare
    ///  - bubbles ke peeche shaffaf "glass" panel aur upar neon chhat
    ///  - neeche khatre ki lakeer (danger line) jo bubbles qareeb aane par laal dhadakti hai
    /// Scene mein kuch lagane ki zaroorat nahi; GameManager Apply() call karta hai.
    /// </summary>
    public class GameplayBackdrop : MonoBehaviour
    {
        [System.Serializable]
        public struct Theme
        {
            public string name;
            public Color bgTint;        // bg.png par multiply
            public Color overlayTop;    // upar se aane wali rangeen roshni
            public Color panel;         // bubbles ke peeche glass panel
            public Color accent;        // neon chhat / bokeh
            public Color cameraClear;
        }

        public static readonly Theme[] Themes =
        {
            new Theme { name = "Crystal Lagoon",
                bgTint = new Color(1f, 1f, 1f), overlayTop = new Color(0.2f, 0.75f, 1f, 0.28f),
                panel = new Color(0.02f, 0.06f, 0.22f, 0.42f), accent = new Color(0.35f, 0.9f, 1f),
                cameraClear = new Color(0.03f, 0.08f, 0.3f) },
            new Theme { name = "Twilight Grove",
                bgTint = new Color(0.85f, 0.7f, 1f), overlayTop = new Color(0.75f, 0.35f, 1f, 0.34f),
                panel = new Color(0.1f, 0.03f, 0.22f, 0.45f), accent = new Color(0.9f, 0.55f, 1f),
                cameraClear = new Color(0.12f, 0.04f, 0.25f) },
            new Theme { name = "Emerald Jungle",
                bgTint = new Color(0.62f, 1f, 0.85f), overlayTop = new Color(0.2f, 1f, 0.6f, 0.26f),
                panel = new Color(0.01f, 0.13f, 0.12f, 0.45f), accent = new Color(0.45f, 1f, 0.65f),
                cameraClear = new Color(0.02f, 0.15f, 0.14f) },
            new Theme { name = "Sunset Cliffs",
                bgTint = new Color(1f, 0.72f, 0.85f), overlayTop = new Color(1f, 0.45f, 0.35f, 0.33f),
                panel = new Color(0.2f, 0.04f, 0.13f, 0.42f), accent = new Color(1f, 0.65f, 0.4f),
                cameraClear = new Color(0.25f, 0.05f, 0.15f) },
            new Theme { name = "Midnight Caves",
                bgTint = new Color(0.5f, 0.55f, 0.85f), overlayTop = new Color(0.35f, 0.4f, 1f, 0.3f),
                panel = new Color(0.01f, 0.02f, 0.09f, 0.52f), accent = new Color(0.55f, 0.7f, 1f),
                cameraClear = new Color(0.02f, 0.03f, 0.1f) },
            new Theme { name = "Aurora Peaks",
                bgTint = new Color(0.7f, 1f, 1f), overlayTop = new Color(0.25f, 1f, 0.85f, 0.3f),
                panel = new Color(0.0f, 0.1f, 0.18f, 0.45f), accent = new Color(0.4f, 1f, 0.9f),
                cameraClear = new Color(0.01f, 0.12f, 0.2f) }
        };

        private const int OrderBg = -100;
        private const int OrderOverlay = -99;
        private const int OrderVignette = -98;
        private const int OrderBokeh = -95;
        private const int OrderPanel = -90;
        private const int OrderFrame = -89;
        private const int OrderDanger = -5;

        private static GameplayBackdrop instance;
        public static GameplayBackdrop Instance => instance;

        private Camera cam;
        private SpriteRenderer bg;
        private SpriteRenderer overlay;
        private SpriteRenderer vignette;
        private SpriteRenderer panel;
        private SpriteRenderer ceiling;
        private SpriteRenderer ceilingGlow;
        private SpriteRenderer dangerGlow;
        private readonly List<SpriteRenderer> dangerDashes = new List<SpriteRenderer>();

        private class Bokeh
        {
            public SpriteRenderer sr;
            public float speed;
            public float phase;
            public float baseAlpha;
            public float wobble;
            public float x;
            public bool sparkle;
        }

        private readonly List<Bokeh> bokeh = new List<Bokeh>();

        private Theme theme = Themes[0];
        private float dangerY = -3f;
        private float danger;          // 0..1 (bubbles kitni qareeb hain)
        private float dangerShown;
        private float halfW;
        private float halfH;
        private bool built;

        public Theme CurrentTheme => theme;
        public float DangerLineY => dangerY;

        /// <summary>Scene mein backdrop dhoondo ya naya banao.</summary>
        public static GameplayBackdrop Ensure()
        {
            if (instance != null) return instance;
            GameObject go = new GameObject("GameplayBackdrop");
            instance = go.AddComponent<GameplayBackdrop>();
            return instance;
        }

        public static Theme ThemeForLevel(int levelNumber)
        {
            int i = Mathf.Max(0, levelNumber - 1) % Themes.Length;
            return Themes[i];
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ==============================================================
        // Build
        // ==============================================================

        private void Build()
        {
            if (built) return;
            built = true;

            cam = Camera.main;
            float size = cam != null && cam.orthographic ? cam.orthographicSize : 5f;
            float aspect = cam != null ? cam.aspect : 0.5625f;
            halfH = size;
            halfW = size * aspect;

            // ---- Background image: UI wali "bg" se sprite uthao ----
            Sprite bgSprite = StealUiBackgroundSprite();

            bg = MakeRenderer("BG", bgSprite != null ? bgSprite : FxSprites.Pixel, OrderBg);
            CoverScreen(bg);

            overlay = MakeRenderer("ThemeLight", FxSprites.VerticalGradient, OrderOverlay);
            StretchTo(overlay, halfW * 2.2f, halfH * 2.2f, 0f);

            vignette = MakeRenderer("Vignette", FxSprites.Vignette, OrderVignette);
            StretchTo(vignette, halfW * 2.4f, halfH * 2.4f, 0f);
            vignette.color = new Color(0f, 0f, 0.05f, 0.75f);

            // ---- Bokeh / sitare ----
            for (int i = 0; i < 18; i++)
            {
                bool sp = i >= 13;
                SpriteRenderer sr = MakeRenderer(sp ? "Twinkle" : "Bokeh",
                    sp ? FxSprites.Sparkle : FxSprites.SoftDot, OrderBokeh);
                Bokeh b = new Bokeh
                {
                    sr = sr,
                    sparkle = sp,
                    speed = sp ? 0f : Random.Range(0.08f, 0.28f),
                    phase = Random.Range(0f, 10f),
                    baseAlpha = sp ? Random.Range(0.35f, 0.7f) : Random.Range(0.06f, 0.2f),
                    wobble = Random.Range(0.05f, 0.25f),
                    x = Random.Range(-halfW, halfW)
                };
                float s = sp ? Random.Range(0.12f, 0.24f) : Random.Range(0.35f, 1.3f);
                sr.transform.localScale = new Vector3(s, s, 1f);
                sr.transform.position = new Vector3(b.x, Random.Range(-halfH, halfH), 4f);
                bokeh.Add(b);
            }

            // ---- Glass panel + neon chhat ----
            panel = MakeRenderer("PlayfieldGlass", FxSprites.RoundedRect, OrderPanel);
            panel.drawMode = SpriteDrawMode.Sliced;

            // Gradient upar roshan, neeche shaffaf: chhat ke neeche halki roshni
            ceilingGlow = MakeRenderer("CeilingGlow", FxSprites.VerticalGradient, OrderFrame);
            ceiling = MakeRenderer("CeilingLine", FxSprites.Pixel, OrderFrame + 1);

            // ---- Danger line ----
            dangerGlow = MakeRenderer("DangerGlow", FxSprites.VerticalGradient, OrderDanger - 1);
            dangerGlow.flipY = true;
            int dashes = 26;
            for (int i = 0; i < dashes; i++)
            {
                SpriteRenderer d = MakeRenderer("DangerDash", FxSprites.Circle, OrderDanger);
                dangerDashes.Add(d);
            }

            HideOldBoundsVisuals();
        }

        private SpriteRenderer MakeRenderer(string n, Sprite s, int order)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        private void CoverScreen(SpriteRenderer sr)
        {
            Vector2 spriteSize = sr.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;
            float scale = Mathf.Max(halfW * 2f / spriteSize.x, halfH * 2f / spriteSize.y) * 1.04f;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.transform.position = new Vector3(0f, 0f, 5f);
        }

        private static void StretchTo(SpriteRenderer sr, float w, float h, float y)
        {
            Vector2 s = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / s.x, h / s.y, 1f);
            sr.transform.position = new Vector3(0f, y, 4.5f);
        }

        /// <summary>
        /// GameplayCanvas ki 'bg' Image (bg.png, 11% alpha) bubbles ke upar dhund daal rahi thi.
        /// Uska sprite world background ke liye le kar UI wali image band kar dete hain.
        /// </summary>
        private static Sprite StealUiBackgroundSprite()
        {
            Sprite found = null;
            Image[] images = FindObjectsOfType<Image>(true);
            foreach (Image img in images)
            {
                if (img == null || img.gameObject.name != "bg") continue;
                if (img.sprite == null) continue;

                // GameplayCanvas ki stretched bg (bg.png). UI kit wali chhoti bg ko chhoro.
                RectTransform rt = img.rectTransform;
                bool stretched = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
                if (!stretched) continue;

                if (found == null || img.sprite.rect.height > found.rect.height)
                    found = img.sprite;

                img.enabled = false;
                img.raycastTarget = false;
            }
            return found;
        }

        /// <summary>Purani saada walls / lose line (grey squares) chhupao, ab neon frame hai.</summary>
        private static void HideOldBoundsVisuals()
        {
            GameObject bounds = GameObject.Find("GameplayBounds");
            if (bounds == null) return;
            foreach (SpriteRenderer sr in bounds.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;
        }

        // ==============================================================
        // Apply per level
        // ==============================================================

        /// <summary>Level shuru par call karo: theme lagao aur panel/lines ko grid ke mutabiq rakho.</summary>
        public void Apply(int levelNumber, BubbleGrid grid, float dangerLineY)
        {
            Build();

            theme = ThemeForLevel(levelNumber);
            dangerY = dangerLineY;
            danger = 0f;
            dangerShown = 0f;

            if (cam != null)
                cam.backgroundColor = theme.cameraClear;

            bg.color = theme.bgTint;
            overlay.color = theme.overlayTop;

            float d = grid != null ? grid.BubbleDiameter : 0.4f;
            float top = (grid != null ? grid.CeilingY : 3.6f) + d * 0.5f + 0.06f;
            float bottom = dangerY;
            float width = halfW * 2f - 0.08f;

            // Glass panel
            Color pc = theme.panel;
            panel.color = pc;
            // Sliced sprite ka size world units mein hota hai
            panel.size = new Vector2(width, Mathf.Max(1f, top - bottom + 0.4f));
            panel.transform.position = new Vector3(0f, (top + bottom) * 0.5f + 0.1f, 3f);
            panel.transform.localScale = Vector3.one;

            // Neon chhat
            float lineH = Mathf.Max(0.05f, d * 0.16f);
            SetWorldSize(ceiling, width, lineH);
            ceiling.transform.position = new Vector3(0f, top, 2f);
            Color ac = theme.accent;
            ceiling.color = new Color(ac.r, ac.g, ac.b, 0.95f);

            StretchGlow(ceilingGlow, width, d * 2.2f, top - d * 1.1f, new Color(ac.r, ac.g, ac.b, 0.28f));

            // Danger line dashes
            float dashW = width / dangerDashes.Count;
            for (int i = 0; i < dangerDashes.Count; i++)
            {
                SpriteRenderer s = dangerDashes[i];
                SetWorldSize(s, dashW * 0.55f, Mathf.Max(0.04f, d * 0.13f));
                s.transform.position = new Vector3(-width * 0.5f + dashW * (i + 0.5f), dangerY, 2f);
            }
            StretchGlow(dangerGlow, width, d * 3f, dangerY + d * 1.5f, new Color(1f, 0.2f, 0.25f, 0f));

            // Bokeh rangon ko theme ke mutabiq
            for (int i = 0; i < bokeh.Count; i++)
            {
                Color c = i % 3 == 0 ? Color.white : theme.accent;
                c.a = 0f;
                bokeh[i].sr.color = c;
            }
        }

        private static void SetWorldSize(SpriteRenderer sr, float w, float h)
        {
            Vector2 s = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / s.x, h / s.y, 1f);
        }

        private static void StretchGlow(SpriteRenderer sr, float w, float h, float centerY, Color c)
        {
            Vector2 s = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(w / s.x, h / s.y, 1f);
            sr.transform.position = new Vector3(0f, centerY, 2.5f);
            sr.color = c;
        }

        /// <summary>
        /// 0 = bubbles door, 1 = bilkul lakeer par. GameManager har shot ke baad batata hai.
        /// </summary>
        public void SetDanger(float value)
        {
            danger = Mathf.Clamp01(value);
        }

        // ==============================================================
        // Animate
        // ==============================================================

        private void Update()
        {
            if (!built) return;

            float t = Time.time;
            float dt = Time.deltaTime;

            // Bokeh: dheere upar tairti hain
            foreach (Bokeh b in bokeh)
            {
                Vector3 p = b.sr.transform.position;
                Color c = b.sr.color;

                if (b.sparkle)
                {
                    float tw = Mathf.Max(0f, Mathf.Sin(t * 1.7f + b.phase * 3f));
                    c.a = b.baseAlpha * tw * tw;
                    b.sr.transform.Rotate(0f, 0f, 40f * dt);
                }
                else
                {
                    p.y += b.speed * dt;
                    p.x = b.x + Mathf.Sin(t * 0.5f + b.phase) * b.wobble;
                    if (p.y > halfH + 1f)
                    {
                        p.y = -halfH - 1f;
                        b.x = Random.Range(-halfW, halfW);
                    }
                    c.a = b.baseAlpha * (0.75f + 0.25f * Mathf.Sin(t * 0.9f + b.phase));
                }

                b.sr.color = c;
                b.sr.transform.position = p;
            }

            // Theme ki roshni halki si saans leti hai
            Color oc = theme.overlayTop;
            oc.a *= 0.85f + 0.15f * Mathf.Sin(t * 0.6f);
            overlay.color = oc;

            // Chhat ki chamak
            Color cc = ceiling.color;
            cc.a = 0.75f + 0.25f * Mathf.Sin(t * 2.2f);
            ceiling.color = cc;

            // Danger line
            dangerShown = Mathf.MoveTowards(dangerShown, danger, dt * 1.5f);
            float speed = Mathf.Lerp(2f, 9f, dangerShown);
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * speed);
            Color baseLine = Color.Lerp(new Color(1f, 1f, 1f), new Color(1f, 0.2f, 0.25f), Mathf.Clamp01(dangerShown * 2f));
            float lineAlpha = Mathf.Lerp(0.22f, 0.95f, dangerShown) * Mathf.Lerp(1f, 0.55f + 0.45f * pulse, dangerShown);

            for (int i = 0; i < dangerDashes.Count; i++)
            {
                // "chalti" hui lakeer
                float wave = 0.75f + 0.25f * Mathf.Sin(t * 3f - i * 0.6f);
                Color dc = baseLine;
                dc.a = lineAlpha * wave;
                dangerDashes[i].color = dc;
            }

            Color gc = new Color(1f, 0.15f, 0.2f, Mathf.Clamp01((dangerShown - 0.4f) / 0.6f) * 0.45f * (0.6f + 0.4f * pulse));
            dangerGlow.color = gc;

            if (vignette != null)
            {
                Color vc = Color.Lerp(new Color(0f, 0f, 0.05f, 0.75f), new Color(0.35f, 0f, 0.02f, 0.85f),
                    Mathf.Clamp01((dangerShown - 0.6f) / 0.4f) * (0.6f + 0.4f * pulse));
                vignette.color = vc;
            }
        }
    }
}
