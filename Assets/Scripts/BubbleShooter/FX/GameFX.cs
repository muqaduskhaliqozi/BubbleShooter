using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Gameplay ke world-space effects: bubble pop burst, projectile trail, score popups,
    /// camera shake. Khud ban jata hai (GameFX.Instance), scene mein lagane ki zaroorat nahi.
    /// Sab particles pool se aate hain, is liye garbage nahi banta.
    /// </summary>
    public class GameFX : MonoBehaviour
    {
        public const int SortingBase = 60;

        private static GameFX instance;

        public static GameFX Instance
        {
            get
            {
                if (instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("GameFX");
                    instance = go.AddComponent<GameFX>();
                }
                return instance;
            }
        }

        public static bool Exists => instance != null;

        // --------------------------------------------------------------
        // Particle pool
        // --------------------------------------------------------------

        private class Particle
        {
            public Transform tr;
            public SpriteRenderer sr;
            public bool alive;
            public Vector2 vel;
            public float gravity;
            public float drag;
            public float life;
            public float maxLife;
            public float startSize;
            public float endSize;
            public float spin;
            public Color color;
            public float startAlpha;
            public bool fadeIn;
        }

        private readonly List<Particle> particles = new List<Particle>(256);
        private int searchIndex;
        private const int MaxParticles = 500;

        // --------------------------------------------------------------
        // Floating texts
        // --------------------------------------------------------------

        private class FloatText
        {
            public TextMeshPro tmp;
            public bool alive;
            public float life;
            public float maxLife;
            public Vector3 start;
            public float rise;
            public float scale;
            public Color color;
        }

        private readonly List<FloatText> texts = new List<FloatText>();

        // --------------------------------------------------------------
        // Camera shake
        // --------------------------------------------------------------

        private Camera cam;
        private Vector3 camBasePos;
        private bool camBaseSet;
        private float shakeTime;
        private float shakeDuration;
        private float shakeAmplitude;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            CaptureCamera();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                RestoreCamera();
                instance = null;
            }
        }

        private void CaptureCamera()
        {
            cam = Camera.main;
            if (cam != null)
            {
                camBasePos = cam.transform.position;
                camBaseSet = true;
            }
        }

        private void RestoreCamera()
        {
            if (cam != null && camBaseSet)
                cam.transform.position = camBasePos;
        }

        // ==============================================================
        // Public API
        // ==============================================================

        /// <summary>Bubble phatne ka poora effect: chamak, ring, boondein, sitare.</summary>
        public void PopBurst(Vector3 pos, Color color, float diameter)
        {
            float d = Mathf.Max(0.15f, diameter);
            Color light = Color.Lerp(color, Color.white, 0.45f);

            // 1) safed chamak
            Spawn(FxSprites.SoftDot, pos, Vector2.zero, 0f, 0f, 0.18f,
                d * 1.9f, d * 0.6f, 0f, new Color(1f, 1f, 1f, 0.9f), 2);

            // 2) phailta hua ring
            Spawn(FxSprites.Ring, pos, Vector2.zero, 0f, 0f, 0.35f,
                d * 0.9f, d * 2.3f, 0f, light, 1);

            // 3) rangeen boondein
            int drops = 7;
            for (int i = 0; i < drops; i++)
            {
                float ang = (i / (float)drops) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
                float spd = Random.Range(2.2f, 4.8f) * d;
                Vector2 v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
                float size = d * Random.Range(0.22f, 0.36f);
                Spawn(FxSprites.Circle, pos, v, 9f * d, 2.2f, Random.Range(0.4f, 0.65f),
                    size, size * 0.15f, 0f, i % 3 == 0 ? light : color, 3);
            }

            // 4) do chhote sitare
            for (int i = 0; i < 2; i++)
            {
                Vector2 v = Random.insideUnitCircle * d * 2f + Vector2.up * d;
                Spawn(FxSprites.Sparkle, pos + (Vector3)(Random.insideUnitCircle * d * 0.3f), v, 0f, 2f,
                    Random.Range(0.35f, 0.5f), d * 0.7f, 0f, Random.Range(-260f, 260f),
                    new Color(1f, 1f, 0.92f, 1f), 4);
            }
        }

        /// <summary>Halka sa "phus" - ghalat shot wali bubble ke liye.</summary>
        public void Fizzle(Vector3 pos, Color color, float diameter)
        {
            float d = Mathf.Max(0.15f, diameter);
            for (int i = 0; i < 5; i++)
            {
                Vector2 v = Random.insideUnitCircle * d * 1.5f;
                Spawn(FxSprites.SoftDot, pos, v, 0f, 2f, 0.4f, d * 0.6f, 0f, 0f,
                    new Color(color.r, color.g, color.b, 0.6f), 1);
            }
        }

        /// <summary>Udti hui projectile ke peeche chamakti lakeer.</summary>
        public void Trail(Vector3 pos, Color color, float diameter)
        {
            float d = Mathf.Max(0.15f, diameter);
            Color c = Color.Lerp(color, Color.white, 0.3f);
            c.a = 0.55f;
            Spawn(FxSprites.SoftDot, pos + (Vector3)(Random.insideUnitCircle * d * 0.08f), Vector2.zero,
                0f, 0f, 0.28f, d * 1.05f, d * 0.1f, 0f, c, -1);

            if (Random.value < 0.25f)
            {
                Spawn(FxSprites.Sparkle, pos + (Vector3)(Random.insideUnitCircle * d * 0.35f),
                    Random.insideUnitCircle * d * 0.6f, 0f, 1f, 0.3f, d * 0.45f, 0f,
                    Random.Range(-200f, 200f), new Color(1f, 1f, 1f, 0.9f), 0);
            }
        }

        /// <summary>Bubble lagne par chhoti si dhool / chamak.</summary>
        public void ImpactPuff(Vector3 pos, Color color, float diameter)
        {
            float d = Mathf.Max(0.15f, diameter);
            Color light = Color.Lerp(color, Color.white, 0.6f);
            light.a = 0.7f;
            Spawn(FxSprites.Ring, pos, Vector2.zero, 0f, 0f, 0.22f, d * 0.8f, d * 1.5f, 0f, light, 1);
        }

        /// <summary>Upar uthta hua score / text.</summary>
        public void FloatingText(Vector3 pos, string text, Color color, float size = 1f, float duration = 0.9f)
        {
            FloatText ft = GetText();
            if (ft == null) return;

            ft.alive = true;
            ft.life = 0f;
            ft.maxLife = duration;
            ft.start = new Vector3(pos.x, pos.y, -1f);
            ft.rise = 0.9f;
            ft.scale = size;
            ft.color = color;

            ft.tmp.text = text;
            ft.tmp.color = color;
            ft.tmp.transform.position = ft.start;
            ft.tmp.transform.localScale = Vector3.zero;
            ft.tmp.gameObject.SetActive(true);
        }

        /// <summary>Camera ko jhatka. amplitude world units mein.</summary>
        public void Shake(float amplitude, float duration)
        {
            if (cam == null) CaptureCamera();
            if (amplitude < shakeAmplitude * (1f - shakeTime / Mathf.Max(0.0001f, shakeDuration)))
                return;   // pehle wala bara shake abhi chal raha hai

            shakeAmplitude = amplitude;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeTime = 0f;
        }

        /// <summary>Saare effects foran hata do (level restart par).</summary>
        public void ClearAll()
        {
            foreach (Particle p in particles)
            {
                p.alive = false;
                if (p.tr != null) p.tr.gameObject.SetActive(false);
            }
            foreach (FloatText t in texts)
            {
                t.alive = false;
                if (t.tmp != null) t.tmp.gameObject.SetActive(false);
            }
            shakeTime = shakeDuration;
            RestoreCamera();
        }

        // ==============================================================
        // Internals
        // ==============================================================

        private void Spawn(
            Sprite sprite, Vector3 pos, Vector2 vel, float gravity, float drag, float life,
            float startSize, float endSize, float spin, Color color, int sortOffset)
        {
            Particle p = GetParticle();
            if (p == null) return;

            p.alive = true;
            p.vel = vel;
            p.gravity = gravity;
            p.drag = drag;
            p.life = 0f;
            p.maxLife = Mathf.Max(0.01f, life);
            p.startSize = startSize;
            p.endSize = endSize;
            p.spin = spin;
            p.color = color;
            p.startAlpha = color.a;

            p.sr.sprite = sprite;
            p.sr.color = color;
            p.sr.sortingOrder = SortingBase + sortOffset;
            p.tr.position = new Vector3(pos.x, pos.y, -0.5f);
            p.tr.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            p.tr.localScale = Vector3.one * startSize;
            p.tr.gameObject.SetActive(true);
        }

        private Particle GetParticle()
        {
            int n = particles.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = (searchIndex + i) % n;
                if (!particles[idx].alive)
                {
                    searchIndex = (idx + 1) % n;
                    return particles[idx];
                }
            }

            if (n >= MaxParticles) return null;

            GameObject go = new GameObject("FxParticle");
            go.transform.SetParent(transform, false);
            Particle p = new Particle
            {
                tr = go.transform,
                sr = go.AddComponent<SpriteRenderer>()
            };
            particles.Add(p);
            return p;
        }

        private FloatText GetText()
        {
            foreach (FloatText t in texts)
                if (!t.alive) return t;

            if (texts.Count >= 24) return null;

            GameObject go = new GameObject("FxText");
            go.transform.SetParent(transform, false);
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4.2f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 1.5f);
            tmp.sortingOrder = SortingBase + 10;
            tmp.outlineWidth = 0.22f;
            tmp.outlineColor = new Color32(20, 20, 60, 255);

            FloatText ft = new FloatText { tmp = tmp };
            texts.Add(ft);
            go.SetActive(false);
            return ft;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;   // pause

            for (int i = 0; i < particles.Count; i++)
            {
                Particle p = particles[i];
                if (!p.alive) continue;

                p.life += dt;
                float k = p.life / p.maxLife;

                if (k >= 1f)
                {
                    p.alive = false;
                    p.tr.gameObject.SetActive(false);
                    continue;
                }

                p.vel.y -= p.gravity * dt;
                if (p.drag > 0f) p.vel *= Mathf.Exp(-p.drag * dt);

                Vector3 pos = p.tr.position;
                pos.x += p.vel.x * dt;
                pos.y += p.vel.y * dt;
                p.tr.position = pos;

                if (p.spin != 0f) p.tr.Rotate(0f, 0f, p.spin * dt);

                float size = Mathf.Lerp(p.startSize, p.endSize, Ease.OutCubic(k));
                p.tr.localScale = new Vector3(size, size, 1f);

                Color c = p.color;
                c.a = p.startAlpha * (1f - k * k);
                p.sr.color = c;
            }

            for (int i = 0; i < texts.Count; i++)
            {
                FloatText t = texts[i];
                if (!t.alive) continue;

                t.life += dt;
                float k = t.life / t.maxLife;
                if (k >= 1f)
                {
                    t.alive = false;
                    t.tmp.gameObject.SetActive(false);
                    continue;
                }

                float pop = k < 0.2f ? Ease.OutBack(k / 0.2f, 3f) : 1f;
                t.tmp.transform.localScale = Vector3.one * (t.scale * pop);
                t.tmp.transform.position = t.start + Vector3.up * (t.rise * Ease.OutCubic(k));

                Color c = t.color;
                c.a = k > 0.65f ? 1f - (k - 0.65f) / 0.35f : 1f;
                t.tmp.color = c;
            }
        }

        private void LateUpdate()
        {
            if (cam == null || !camBaseSet) return;

            if (shakeTime < shakeDuration)
            {
                shakeTime += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(shakeTime / shakeDuration);
                float a = shakeAmplitude * k * k;
                Vector2 off = new Vector2(
                    (Mathf.PerlinNoise(Time.unscaledTime * 35f, 0.3f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0.7f, Time.unscaledTime * 35f) - 0.5f) * 2f) * a;
                cam.transform.position = camBasePos + new Vector3(off.x, off.y, 0f);
            }
            else
            {
                cam.transform.position = camBasePos;
            }
        }
    }
}
