using UnityEngine;

namespace BubbleShooter.FX
{
    /// <summary>
    /// RectTransform ko phone ke "safe area" (notch / status bar se bahar) mein rakhta hai.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafe;
        private Vector2Int lastScreen;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
                Apply();
        }

        private void Apply()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null || Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            lastSafe = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
