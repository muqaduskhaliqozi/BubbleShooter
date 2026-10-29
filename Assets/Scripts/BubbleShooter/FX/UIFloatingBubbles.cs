using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Kisi RectTransform ke andar rang-birangi bubbles neeche se upar tairti hain.
    /// popOnTap = true ho to bubble ko chhoone par wo phoot jati hai.
    /// </summary>
    public class UIFloatingBubbles : MonoBehaviour
    {
        public int count = 14;
        public Vector2 sizeRange = new Vector2(0.05f, 0.13f);   // container ki chaurai ka hissa
        public Vector2 speedRange = new Vector2(0.05f, 0.12f);  // container ki unchai / second
        public float alpha = 0.85f;
        public bool popOnTap = true;

        [Tooltip("Khali = normal glossy bubble. Diya ho (jaise blurred bokeh) to wohi sprite, bina gloss ke.")]
        public Sprite bodySprite;
        [Tooltip("0 = bubbles poori chaurai mein; 1 = sirf kinaron ke paas (foreground bokeh ke liye).")]
        [Range(0f, 1f)] public float edgeBias;
        [Tooltip("Bari bubbles ko kitna shaffaf karein (0 = sab barabar).")]
        [Range(0f, 1f)] public float sizeFade = 0.45f;

        public static readonly Color[] Palette =
        {
            new Color(1f, 0.25f, 0.3f),
            new Color(0.25f, 0.55f, 1f),
            new Color(0.2f, 0.9f, 0.35f),
            new Color(1f, 0.85f, 0.15f),
            new Color(0.75f, 0.35f, 1f),
            new Color(0.2f, 0.9f, 0.95f),
            new Color(1f, 0.55f, 0.15f)
        };

        private class Item
        {
            public RectTransform rt;
            public Image body;
            public Image gloss;
            public float size;
            public float speed;
            public float wobble;
            public float phase;
            public float x;
            public bool popping;
        }

        private readonly List<Item> items = new List<Item>();
        private RectTransform area;

        private IEnumerator Start()
        {
            area = transform as RectTransform;

            // Canvas Scaler ko naya size lagane do (2 frame), phir bubbles banao
            yield return null;
            yield return null;

            for (int i = 0; i < count; i++)
                items.Add(CreateItem(true));
        }

        private Item CreateItem(bool randomHeight)
        {
            GameObject go = new GameObject("FloatBubble", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

            Image body = go.AddComponent<Image>();
            body.sprite = bodySprite != null ? bodySprite : Bubble.BubbleSpriteFactory.GetBaseSprite();
            body.raycastTarget = popOnTap;

            GameObject g = new GameObject("Gloss", typeof(RectTransform));
            g.transform.SetParent(go.transform, false);
            RectTransform grt = g.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = grt.offsetMax = Vector2.zero;
            Image gloss = g.AddComponent<Image>();
            gloss.sprite = Bubble.BubbleSpriteFactory.GetGlossSprite();
            gloss.raycastTarget = false;
            gloss.enabled = bodySprite == null;

            Item item = new Item { rt = rt, body = body, gloss = gloss };

            if (popOnTap)
            {
                UIBubbleTapTarget tap = go.AddComponent<UIBubbleTapTarget>();
                tap.owner = this;
            }

            Respawn(item, randomHeight);
            return item;
        }

        private void Respawn(Item item, bool randomHeight)
        {
            Rect r = area != null ? area.rect : new Rect(0, 0, 800, 600);
            float w = Mathf.Max(1f, r.width);
            float h = Mathf.Max(1f, r.height);

            item.size = Random.Range(sizeRange.x, sizeRange.y) * w;
            item.speed = Random.Range(speedRange.x, speedRange.y) * h;
            item.wobble = Random.Range(6f, 22f);
            item.phase = Random.Range(0f, 10f);
            float side = Random.Range(-0.48f, 0.48f);
            if (edgeBias > 0f)
            {
                // kinaron ki taraf dhakelo (aadhi bubble screen se bahar bhi ho sakti hai)
                float edge = (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.42f, 0.56f);
                side = Mathf.Lerp(side, edge, edgeBias);
            }
            item.x = side * w;
            item.popping = false;

            float y = randomHeight
                ? Random.Range(-0.5f, 0.5f) * h
                : -0.5f * h - item.size;

            item.rt.sizeDelta = new Vector2(item.size, item.size);
            item.rt.anchoredPosition = new Vector2(item.x, y);
            item.rt.localScale = Vector3.one;

            Color c = Palette[Random.Range(0, Palette.Length)];
            // bari bubbles thori zyada shafaf (door lagti hain)
            float a = alpha * Mathf.Lerp(1f, 1f - sizeFade, Mathf.InverseLerp(sizeRange.x * w, sizeRange.y * w, item.size));
            c.a = a;
            item.body.color = c;
            item.gloss.color = new Color(1f, 1f, 1f, a);
        }

        private void Update()
        {
            if (area == null) return;
            float h = area.rect.height;
            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < items.Count; i++)
            {
                Item it = items[i];
                if (it.popping) continue;

                Vector2 p = it.rt.anchoredPosition;
                p.y += it.speed * dt;
                p.x = it.x + Mathf.Sin(Time.unscaledTime * 1.3f + it.phase) * it.wobble;
                it.rt.anchoredPosition = p;

                if (p.y - it.size > h * 0.5f)
                    Respawn(it, false);
            }
        }

        public void Pop(RectTransform target)
        {
            foreach (Item it in items)
            {
                if (it.rt == target && !it.popping)
                {
                    StartCoroutine(PopRoutine(it));
                    return;
                }
            }
        }

        private IEnumerator PopRoutine(Item it)
        {
            it.popping = true;
            Color c = it.body.color;
            UIBurst.Spawn(transform as RectTransform, it.rt.anchoredPosition, c, it.size);

            float t = 0f;
            const float dur = 0.14f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                it.rt.localScale = Vector3.one * (1f + k * 0.4f);
                Color bc = c;
                bc.a = c.a * (1f - k);
                it.body.color = bc;
                it.gloss.color = new Color(1f, 1f, 1f, bc.a);
                yield return null;
            }

            Respawn(it, false);
        }
    }
}
