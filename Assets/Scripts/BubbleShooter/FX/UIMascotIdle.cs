using UnityEngine;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Menu ka bubble mascot: halki saans (squash / stretch), kabhi kabhi chhoti si uchhal,
    /// aur tap karne par khushi se uchhalta hai.
    /// </summary>
    public class UIMascotIdle : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler
    {
        public float breathAmount = 0.035f;
        public float breathSpeed = 2.2f;

        private RectTransform rt;
        private Vector2 basePos;
        private bool hasBase;
        private float hopTimer;
        private float hopTime = -1f;
        private const float HopDuration = 0.45f;

        private void OnEnable()
        {
            rt = transform as RectTransform;
            if (rt != null)
            {
                basePos = rt.anchoredPosition;
                hasBase = true;
            }
            hopTimer = Random.Range(2.5f, 4.5f);
            UnityEngine.UI.Image img = GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.raycastTarget = true;
        }

        private void OnDisable()
        {
            if (rt != null && hasBase)
            {
                rt.anchoredPosition = basePos;
                rt.localScale = Vector3.one;
            }
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            Hop();
        }

        public void Hop()
        {
            hopTime = 0f;
        }

        private void Update()
        {
            if (rt == null) return;

            float t = Time.unscaledTime * breathSpeed;
            float b = Mathf.Sin(t);
            float sx = 1f - b * breathAmount * 0.6f;
            float sy = 1f + b * breathAmount;
            float y = 0f;

            hopTimer -= Time.unscaledDeltaTime;
            if (hopTimer <= 0f && hopTime < 0f)
            {
                Hop();
                hopTimer = Random.Range(3f, 5.5f);
            }

            if (hopTime >= 0f)
            {
                hopTime += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(hopTime / HopDuration);
                y = Mathf.Sin(k * Mathf.PI) * 45f;
                float squash = Mathf.Sin(k * Mathf.PI * 2f) * 0.06f;
                sx *= 1f + squash;
                sy *= 1f - squash;
                if (k >= 1f) hopTime = -1f;
            }

            rt.localScale = new Vector3(sx, sy, 1f);
            rt.anchoredPosition = basePos + new Vector2(0f, y);
        }
    }
}
