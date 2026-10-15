using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BubbleShooter.Bubble;

namespace BubbleShooter.UI
{
    public class MissIndicatorUI : MonoBehaviour
    {
        [Header("References (Build Preview inhe khud bana deta hai)")]
        [SerializeField] private RectTransform bubble;   // gaarne wali bubble
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image glossImage;
        [SerializeField] private TMP_Text label;

        [Header("Sprites (exported PNGs)")]
        [SerializeField] private Sprite baseSprite;
        [SerializeField] private Sprite glossSprite;

        [Header("Look")]
        [SerializeField] private float bubbleSize = 48f;
        [SerializeField] private Color normalColor = new Color(1f, 0.9f, 0.05f, 1f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private Color textColor = Color.white;

        [Header("Animation")]
        [SerializeField] private float fallDistance = 90f;
        [SerializeField] private float fallTime = 0.45f;
        [SerializeField] private float returnTime = 0.35f;

        private CanvasGroup group;
        private Vector2 homePos;
        private int lastUsed = -1;
        private int lastMax = -1;
        private Coroutine anim;

        // ------------------------------------------------------------
        // Hierarchy mein preview banana
        // ------------------------------------------------------------
        [ContextMenu("Build Preview In Hierarchy")]
        public void BuildPreview()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }

            // Purana layout group animation se takrata hai, hata do
            HorizontalLayoutGroup old = GetComponent<HorizontalLayoutGroup>();
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }

            // Bubble (left side)
            GameObject b = new GameObject("MissBubble", typeof(RectTransform), typeof(CanvasGroup));
            b.transform.SetParent(transform, false);
            bubble = b.GetComponent<RectTransform>();
            bubble.anchorMin = bubble.anchorMax = new Vector2(0.5f, 0.5f);
            bubble.sizeDelta = new Vector2(bubbleSize, bubbleSize);
            bubble.anchoredPosition = new Vector2(-45f, 0f);

            Sprite bs = baseSprite != null ? baseSprite : BubbleSpriteFactory.GetBaseSprite();
            Sprite gs = glossSprite != null ? glossSprite : BubbleSpriteFactory.GetGlossSprite();
            bodyImage = MakeImage("Body", bubble, bs);
            glossImage = MakeImage("Gloss", bubble, gs);
            bodyImage.color = normalColor;

            // Text (right side)
            GameObject t = new GameObject("MissText", typeof(RectTransform), typeof(TextMeshProUGUI));
            t.transform.SetParent(transform, false);
            RectTransform tr = t.GetComponent<RectTransform>();
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(100f, 50f);
            tr.anchoredPosition = new Vector2(35f, 0f);

            label = t.GetComponent<TextMeshProUGUI>();
            label.text = "0/5";
            label.fontSize = 34;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = textColor;
            label.raycastTarget = false;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
        }

        private Image MakeImage(string n, Transform parent, Sprite s)
        {
            GameObject go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();
            img.sprite = s;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------
        // Runtime
        // ------------------------------------------------------------
        private void Awake()
        {
            // Agar Build Preview nahi chala to khud bana lo
            if (bubble == null || label == null || bodyImage == null || glossImage == null)
                BuildPreview();

            group = bubble.GetComponent<CanvasGroup>();
            if (group == null) group = bubble.gameObject.AddComponent<CanvasGroup>();
            homePos = bubble.anchoredPosition;
        }

        public void SetMisses(int used, int max)
        {
            if (bubble == null || label == null) return;

            if (group == null)
            {
                group = bubble.GetComponent<CanvasGroup>();
                if (group == null) group = bubble.gameObject.AddComponent<CanvasGroup>();
                homePos = bubble.anchoredPosition;
            }

            // Purani animations rok do, taake level badalne par kuch atka na rahe
            StopAllCoroutines();
            anim = null;
            label.rectTransform.localScale = Vector3.one;

            // max 0 = unlimited: sirf andar ki cheezein chhupao, MissBar khud active rahe
            bool show = max > 0;
            bubble.gameObject.SetActive(show);
            label.gameObject.SetActive(show);

            if (!show)
            {
                lastUsed = -1;
                lastMax = max;
                return;
            }

            // Naya level, retry, ya chhupne ke baad dobara aana: seedha set karo
            bool snap = lastMax != max || used <= lastUsed || lastUsed < 0
                        || !gameObject.activeInHierarchy;

            lastMax = max;
            lastUsed = used;

            if (snap)
            {
                ApplyInstant(used, max);
                return;
            }

            anim = StartCoroutine(MissRoutine(used, max));
        }
        // baaqi code wahi rahe

        private void ApplyInstant(int used, int max)
        {
            bool allUsed = used >= max;

            label.text = used + "/" + max;
            label.color = allUsed ? dangerColor : textColor;
            bodyImage.color = (max - used) <= 1 ? dangerColor : normalColor;

            bubble.anchoredPosition = homePos;
            bubble.localRotation = Quaternion.identity;
            bubble.localScale = Vector3.one;
            group.alpha = allUsed ? 0f : 1f;
        }

        private IEnumerator MissRoutine(int used, int max)
        {
            // 1) Bubble gir kar ghaib
            float t = 0f;
            float spin = Random.Range(-140f, 140f);

            while (t < fallTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fallTime);
                float e = k * k;   // gravity jaisi tezi

                bubble.anchoredPosition = homePos + Vector2.down * (fallDistance * e);
                bubble.localRotation = Quaternion.Euler(0f, 0f, spin * k);
                group.alpha = 1f - k;
                yield return null;
            }

            // 2) Number badhao
            bool allUsed = used >= max;
            label.text = used + "/" + max;
            label.color = allUsed ? dangerColor : textColor;

            // Number ka chhota sa pop
            StartCoroutine(TextPop());

            if (allUsed)
            {
                // Sab misses khatam: bubble wapis nahi aayegi
                group.alpha = 0f;
                yield break;
            }

            // 3) Bubble peeche se wapas aaye (chhoti se bari, overshoot ke saath)
            bodyImage.color = (max - used) <= 1 ? dangerColor : normalColor;
            bubble.anchoredPosition = homePos;
            bubble.localRotation = Quaternion.identity;

            t = 0f;
            while (t < returnTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / returnTime);

                // 0 -> 1.2 -> 1.0
                float s = k < 0.7f
                    ? Mathf.Lerp(0f, 1.2f, k / 0.7f)
                    : Mathf.Lerp(1.2f, 1f, (k - 0.7f) / 0.3f);

                bubble.localScale = new Vector3(s, s, 1f);
                group.alpha = Mathf.Clamp01(k * 2f);
                yield return null;
            }

            bubble.localScale = Vector3.one;
            group.alpha = 1f;
        }

        private IEnumerator TextPop()
        {
            float t = 0f;
            const float dur = 0.25f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI) * 0.35f;
                label.rectTransform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            label.rectTransform.localScale = Vector3.one;
        }
    }
}