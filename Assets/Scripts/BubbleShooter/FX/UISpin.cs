using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>Ghoomta hai (sunburst, loading ring).</summary>
    public class UISpin : MonoBehaviour
    {
        public float degreesPerSecond = 20f;

        private void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
