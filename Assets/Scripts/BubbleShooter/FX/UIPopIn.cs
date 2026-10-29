using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>
    /// Enable hone par "pop" ke saath bara hota hai (OutBack). Time.timeScale = 0 mein bhi chalta hai.
    /// </summary>
    public class UIPopIn : MonoBehaviour
    {
        public float delay;
        public float duration = 0.45f;
        public float fromScale = 0.3f;

        private Vector3 baseScale = Vector3.one;
        private bool hasBase;
        private Coroutine routine;

        private void OnEnable()
        {
            if (!hasBase)
            {
                baseScale = transform.localScale;
                hasBase = true;
            }
            Play();
        }

        private void OnDisable()
        {
            if (hasBase) transform.localScale = baseScale;
        }

        public void Play()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            transform.localScale = baseScale * fromScale;
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            float w = 0f;
            while (w < delay)
            {
                w += Time.unscaledDeltaTime;
                yield return null;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                transform.localScale = baseScale * Mathf.LerpUnclamped(fromScale, 1f, Ease.OutBack(k, 2.2f));
                if (cg != null) cg.alpha = Mathf.Clamp01(k * 3f);
                yield return null;
            }

            transform.localScale = baseScale;
            if (cg != null) cg.alpha = 1f;
            routine = null;
        }
    }
}
