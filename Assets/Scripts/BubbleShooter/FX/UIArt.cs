using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Menu / Loading ki art (Assets/Resources/UI) aur "Lilita One" font (Assets/Resources/Fonts)
    /// load karta hai, aur Canvas ko phone ke liye set karta hai.
    /// </summary>
    public static class UIArt
    {
        /// <summary>Mockup ka reference size (portrait phone).</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        private static TMP_FontAsset displayFont;
        private static bool fontTried;

        /// <summary>Resources/UI/name.png se sprite. Na mile to null (warning ke saath).</summary>
        public static Sprite Get(string name)
        {
            Sprite s;
            if (cache.TryGetValue(name, out s) && s != null)
                return s;

            s = Resources.Load<Sprite>("UI/" + name);

            if (s == null)
            {
                // Agar import "Sprite" ke bajaye "Default" texture ho gaya to bhi chale
                Texture2D tex = Resources.Load<Texture2D>("UI/" + name);
                if (tex != null)
                {
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    s.name = name;
                }
            }

            if (s == null)
                Debug.LogWarning("[UIArt] Sprite nahi mila: Resources/UI/" + name);

            cache[name] = s;
            return s;
        }

        /// <summary>Gol, mota "Lilita One" font (mockup jaisa). Na mile to TMP ka default.</summary>
        public static TMP_FontAsset DisplayFont
        {
            get
            {
                if (displayFont == null && !fontTried)
                {
                    fontTried = true;
                    Font f = Resources.Load<Font>("Fonts/LilitaOne-Regular");
                    if (f != null)
                    {
                        displayFont = TMP_FontAsset.CreateFontAsset(f);
                        if (displayFont != null) displayFont.name = "LilitaOne (runtime)";
                    }
                    else
                    {
                        Debug.LogWarning("[UIArt] Font nahi mila: Resources/Fonts/LilitaOne-Regular");
                    }
                }
                return displayFont;
            }
        }

        public static void ApplyFont(TMP_Text t)
        {
            if (t == null) return;
            TMP_FontAsset f = DisplayFont;
            if (f != null) t.font = f;
        }

        /// <summary>Text banao (display font, outline ke saath).</summary>
        public static TextMeshProUGUI Text(string name, Transform parent, float size, Color color,
            Color32 outline, float outlineWidth = 0.22f)
        {
            RectTransform rt = UIBuild.Rect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            ApplyFont(t);
            t.alignment = TextAlignmentOptions.Center;
            t.fontSize = size;
            t.color = color;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            t.outlineWidth = outlineWidth;
            t.outlineColor = outline;
            return t;
        }

        /// <summary>Image banao (Resources/UI sprite ke saath).</summary>
        public static Image Image(string name, Transform parent, string spriteName, Vector2 size)
        {
            Image img = UIBuild.Image(name, parent, Get(spriteName), Color.white);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = size;
            Sprite s = img.sprite;
            if (s != null && (s.border.x > 0f || s.border.z > 0f))
                img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>
        /// Canvas ko "Scale With Screen Size" (1080x1920) par set karo, taake har phone par
        /// layout mockup jaisa rahe (pehle "Constant Pixel Size" tha).
        /// </summary>
        public static void ConfigureScaler(Canvas canvas)
        {
            if (canvas == null) return;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
        }

        /// <summary>
        /// Element ko screen ke hisse (anchor) par rakho. anchor = (0..1, 0..1) screen par jagah,
        /// offset = reference pixels mein.
        /// </summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        /// <summary>Background ko poori screen par bina khinchaav (aspect bachaa kar) phailao.</summary>
        public static void CoverScreen(Image bg)
        {
            if (bg == null) return;
            RectTransform rt = bg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            AspectRatioFitter fit = bg.GetComponent<AspectRatioFitter>();
            if (fit == null) fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            float aspect = 9f / 16f;
            if (bg.sprite != null && bg.sprite.rect.height > 0f)
                aspect = bg.sprite.rect.width / bg.sprite.rect.height;
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = aspect;
        }

        /// <summary>Notch / status bar se bachne ke liye safe-area container.</summary>
        public static RectTransform SafeArea(Transform canvasRoot)
        {
            RectTransform rt = UIBuild.Stretch("SafeArea", canvasRoot);
            rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }
    }
}
