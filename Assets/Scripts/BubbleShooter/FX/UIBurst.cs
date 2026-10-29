using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>UI mein chhota sa phatne wala burst (bubble droplets + ring).</summary>
    public class UIBurst : MonoBehaviour
    {
        private class Bit
        {
            public RectTransform rt;
            public Image img;
            public Vector2 vel;
            public float size;
        }

        private readonly List<Bit> bits = new List<Bit>();
        private RectTransform ringRt;
        private Image ringImg;
        private Color color;
        private float baseSize;
        private float t;
        private const float Life = 0.55f;

        public static void Spawn(RectTransform parent, Vector2 anchoredPos, Color color, float size)
        {
            if (parent == null) return;
            GameObject go = new GameObject("UIBurst", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = Vector2.zero;
            UIBurst b = go.AddComponent<UIBurst>();
            b.Init(color, size);
        }

        private void Init(Color c, float size)
        {
            color = c;
            color.a = 1f;
            baseSize = size;

            ringImg = MakeImage("Ring", FxSprites.Ring, out ringRt);

            int n = 8;
            for (int i = 0; i < n; i++)
            {
                RectTransform rt;
                Image img = MakeImage("Drop", FxSprites.Circle, out rt);
                float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float spd = Random.Range(1.6f, 3.2f) * size;
                bits.Add(new Bit
                {
                    rt = rt,
                    img = img,
                    vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd,
                    size = size * Random.Range(0.16f, 0.26f)
                });
            }
            Update();
        }

        private Image MakeImage(string n, Sprite s, out RectTransform rt)
        {
            GameObject go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            Image img = go.AddComponent<Image>();
            img.sprite = s;
            img.raycastTarget = false;
            return img;
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Life);

            float ringSize = baseSize * Mathf.Lerp(0.8f, 2.1f, Ease.OutCubic(k));
            ringRt.sizeDelta = new Vector2(ringSize, ringSize);
            Color rc = Color.Lerp(Color.white, color, 0.4f);
            rc.a = 1f - k;
            ringImg.color = rc;

            float dt = Time.unscaledDeltaTime;
            foreach (Bit b in bits)
            {
                b.vel *= Mathf.Exp(-3.5f * dt);
                b.vel.y -= baseSize * 4f * dt;
                b.rt.anchoredPosition += b.vel * dt;
                float s = b.size * (1f - k);
                b.rt.sizeDelta = new Vector2(s, s);
                Color c = color;
                c.a = 1f - k * k;
                b.img.color = c;
            }

            if (t >= Life)
                Destroy(gameObject);
        }
    }
}
