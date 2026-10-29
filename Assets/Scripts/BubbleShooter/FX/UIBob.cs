using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>UI element ko upar-neeche halka sa jhulata hai.</summary>
    public class UIBob : MonoBehaviour
    {
        public float amplitude = 12f;
        public float speed = 1.6f;
        public float rotationAmplitude = 0f;
        public float phase;

        private RectTransform rt;
        private Vector2 basePos;
        private bool hasBase;

        private void OnEnable()
        {
            rt = transform as RectTransform;
            if (rt != null && !hasBase)
            {
                basePos = rt.anchoredPosition;
                hasBase = true;
            }
        }

        private void Update()
        {
            if (rt == null) return;
            float t = Time.unscaledTime * speed + phase;
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(t) * amplitude);
            if (rotationAmplitude > 0f)
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.7f) * rotationAmplitude);
        }
    }
}
