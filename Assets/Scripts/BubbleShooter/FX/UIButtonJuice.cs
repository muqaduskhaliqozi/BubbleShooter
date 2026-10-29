using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>Button dabane par squish, chhorne par bounce.</summary>
    public class UIButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.9f;

        /// <summary>Abhi ka dabaav (1 = normal). UIPulse isay apne scale mein mila leta hai.</summary>
        public float Factor { get; private set; } = 1f;

        private Vector3 baseScale = Vector3.one;
        private bool hasBase;
        private bool pressed;
        private Coroutine routine;

        private void Awake()
        {
            baseScale = transform.localScale;
            hasBase = true;
        }

        private bool HasActivePulse()
        {
            UIPulse p = GetComponent<UIPulse>();
            return p != null && p.enabled;
        }

        private void OnDisable()
        {
            Factor = 1f;
            pressed = false;
            if (hasBase) transform.localScale = baseScale;
            routine = null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            AnimateTo(pressedScale, 0.08f, false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;
            AnimateTo(1f, 0.35f, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;
            AnimateTo(1f, 0.2f, false);
        }

        private void AnimateTo(float target, float duration, bool elastic)
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(target, duration, elastic));
        }

        private IEnumerator Run(float target, float duration, bool elastic)
        {
            float start = Factor;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float e = elastic ? Ease.OutElastic(k) : Ease.OutCubic(k);
                Factor = Mathf.LerpUnclamped(start, target, e);
                if (!HasActivePulse()) transform.localScale = baseScale * Factor;
                yield return null;
            }
            Factor = target;
            if (!HasActivePulse()) transform.localScale = baseScale * target;
            routine = null;
        }
    }
}
