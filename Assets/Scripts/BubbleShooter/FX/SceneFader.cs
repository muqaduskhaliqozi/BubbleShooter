using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Scene badalte waqt screen ko dheere se andhera / roshan karta hai.
    /// SceneFader.LoadScene("MainMenu") istemal karein.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        private static SceneFader instance;
        private Image overlay;
        private bool busy;

        private static readonly Color FadeColor = new Color(0.02f, 0.05f, 0.16f, 1f);

        private static SceneFader Get()
        {
            if (instance != null) return instance;

            GameObject go = new GameObject("SceneFader");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFader>();

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            go.AddComponent<CanvasScaler>();

            GameObject img = new GameObject("Overlay", typeof(RectTransform));
            img.transform.SetParent(go.transform, false);
            RectTransform rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            instance.overlay = img.AddComponent<Image>();
            instance.overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, 0f);
            instance.overlay.raycastTarget = false;
            return instance;
        }

        /// <summary>Andhera karo, scene load karo, phir roshan karo.</summary>
        public static void LoadScene(string sceneName, float duration = 0.35f)
        {
            SceneFader f = Get();
            if (f.busy) return;
            f.StartCoroutine(f.FadeAndLoad(sceneName, duration));
        }

        /// <summary>Sirf naye scene par roshni wapas laane ke liye (andhere se shuru).</summary>
        public static void FadeInFromBlack(float duration = 0.45f)
        {
            SceneFader f = Get();
            if (f.busy) return;
            f.StartCoroutine(f.Fade(1f, 0f, duration));
        }

        private IEnumerator FadeAndLoad(string sceneName, float duration)
        {
            busy = true;
            overlay.raycastTarget = true;   // fade ke dauran double tap na ho
            yield return Fade(overlay.color.a, 1f, duration);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError("[SceneFader] Scene load nahi hua: " + sceneName);
            }
            else
            {
                while (!op.isDone) yield return null;
            }

            yield return null;
            yield return Fade(1f, 0f, duration * 1.2f);
            overlay.raycastTarget = false;
            busy = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(from, to, Ease.InOutSine(t / duration));
                overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, a);
                yield return null;
            }
            overlay.color = new Color(FadeColor.r, FadeColor.g, FadeColor.b, to);
        }
    }
}
