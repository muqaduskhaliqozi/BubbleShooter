using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleShooter.FX
{
    /// <summary>Floating bubble ko tap karne ka handler.</summary>
    public class UIBubbleTapTarget : MonoBehaviour, IPointerDownHandler
    {
        public UIFloatingBubbles owner;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner != null)
                owner.Pop(transform as RectTransform);
        }
    }
}
