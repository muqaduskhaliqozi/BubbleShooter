using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>Animation ke liye easing functions (t = 0..1).</summary>
    public static class Ease
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * t + 1f;
        }

        public static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float InOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * Mathf.Clamp01(t)) - 1f) * 0.5f;
        }

        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return t * t * ((overshoot + 1f) * t + overshoot) + 1f;
        }

        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f || t >= 1f) return t;
            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
    }

    /// <summary>Choti madadgar functions UI banane ke liye.</summary>
    public static class UIBuild
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rt = Rect(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            RectTransform rt = Rect(name, parent);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Kisi transform ke upar wala Canvas ka root RectTransform.</summary>
        public static RectTransform RootCanvasRect(Transform t)
        {
            if (t == null) return null;
            Canvas c = t.GetComponentInParent<Canvas>();
            if (c == null) return null;
            c = c.rootCanvas;
            return c.transform as RectTransform;
        }

        /// <summary>Kya screen ka yeh point kisi click hone wale UI (Button, Toggle...) ke upar hai?</summary>
        public static bool IsOverInteractableUI(Vector2 screenPos)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;

            PointerEventData data = new PointerEventData(es) { position = screenPos };
            List<RaycastResult> results = new List<RaycastResult>();
            es.RaycastAll(data, results);

            foreach (RaycastResult r in results)
            {
                if (r.gameObject == null) continue;
                Selectable s = r.gameObject.GetComponentInParent<Selectable>();
                if (s != null && s.IsInteractable() && s.isActiveAndEnabled)
                    return true;
            }
            return false;
        }
    }
}
