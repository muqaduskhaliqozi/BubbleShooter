using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>Dil ki dharkan jaisi scale pulse (Play button waghera).</summary>
    public class UIPulse : MonoBehaviour
    {
        public float amount = 0.06f;
        public float speed = 2.4f;

        private Vector3 baseScale = Vector3.one;
        private bool hasBase;

        private void OnEnable()
        {
            if (!hasBase)
            {
                baseScale = transform.localScale;
                hasBase = true;
            }
        }

        private void OnDisable()
        {
            if (hasBase) transform.localScale = baseScale;
        }

        private void Update()
        {
            // do jhatkon wali dharkan (lub-dub)
            float t = Mathf.Repeat(Time.unscaledTime * speed, Mathf.PI * 2f);
            float beat = Mathf.Max(0f, Mathf.Sin(t)) * 0.7f + Mathf.Max(0f, Mathf.Sin(t * 2f + 0.6f)) * 0.3f;
            UIButtonJuice juice = GetComponent<UIButtonJuice>();
            float press = juice != null ? juice.Factor : 1f;
            transform.localScale = baseScale * (1f + beat * amount) * press;
        }
    }
}
