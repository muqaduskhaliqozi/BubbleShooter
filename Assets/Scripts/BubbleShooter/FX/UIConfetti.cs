using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>Rangeen kaghaz (confetti) jo upar se girta hai. UI ke upar chalta hai.</summary>
    public class UIConfetti : MonoBehaviour
    {
        private class Piece
        {
            public RectTransform rt;
            public Image img;
            public Vector2 vel;
            public float spin;
            public float flip;
            public float phase;
        }

        private readonly List<Piece> pieces = new List<Piece>();
        private float t;
        private float life = 3.2f;
        private RectTransform area;

        public static UIConfetti Burst(RectTransform parent, int amount = 90)
        {
            if (parent == null) return null;
            GameObject go = new GameObject("Confetti", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            UIConfetti c = go.AddComponent<UIConfetti>();
            c.area = rt;
            c.Spawn(amount);
            return c;
        }

        private void Spawn(int amount)
        {
            Rect r = area.rect;
            float w = Mathf.Max(100f, r.width);
            float h = Mathf.Max(100f, r.height);

            for (int i = 0; i < amount; i++)
            {
                GameObject go = new GameObject("Bit", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float s = Random.Range(0.012f, 0.022f) * w;
                rt.sizeDelta = new Vector2(s, s * Random.Range(0.45f, 0.8f));

                // do taraf se "cannon" jaisa nikalta hai
                bool left = (i % 2) == 0;
                rt.anchoredPosition = new Vector2((left ? -0.5f : 0.5f) * w, -0.1f * h);
                float ang = left ? Random.Range(50f, 80f) : Random.Range(100f, 130f);
                float spd = Random.Range(0.7f, 1.25f) * h;

                Image img = go.AddComponent<Image>();
                img.sprite = FxSprites.Pixel;
                img.raycastTarget = false;
                img.color = UIFloatingBubbles.Palette[Random.Range(0, UIFloatingBubbles.Palette.Length)];

                pieces.Add(new Piece
                {
                    rt = rt,
                    img = img,
                    vel = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * spd,
                    spin = Random.Range(-540f, 540f),
                    flip = Random.Range(4f, 10f),
                    phase = Random.Range(0f, 6f)
                });
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float h = Mathf.Max(100f, area.rect.height);

            foreach (Piece p in pieces)
            {
                p.vel.y -= h * 0.9f * dt;
                p.vel *= Mathf.Exp(-1.6f * dt);
                if (p.vel.y < -h * 0.25f) p.vel.y = -h * 0.25f;   // hawa ki rukawat
                p.rt.anchoredPosition += p.vel * dt;
                p.rt.Rotate(0f, 0f, p.spin * dt);
                float sx = Mathf.Cos(Time.unscaledTime * p.flip + p.phase);
                p.rt.localScale = new Vector3(sx, 1f, 1f);

                if (t > life - 0.6f)
                {
                    Color c = p.img.color;
                    c.a = Mathf.Clamp01((life - t) / 0.6f);
                    p.img.color = c;
                }
            }

            if (t >= life)
                Destroy(gameObject);
        }
    }
}
