using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BubbleShooter.Debugging
{
    /// <summary>
    /// Temporary diagnostic. Kisi bhi GameObject par lagao, Play karo aur kisi button par click karo.
    /// Console batayega ke click par sabse upar kaunsa UI object hai (aur kaunsa Input Module chal raha hai).
    /// Masla milne ke baad ye script hata do.
    /// </summary>
    public class UIClickDebugger : MonoBehaviour
    {
        private readonly List<RaycastResult> results = new List<RaycastResult>();

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            if (EventSystem.current == null)
            {
                Debug.LogError("[UIClickDebugger] Scene mein EventSystem NAHI hai.");
                return;
            }

            PointerEventData data = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            results.Clear();
            EventSystem.current.RaycastAll(data, results);

            string module = EventSystem.current.currentInputModule != null
                ? EventSystem.current.currentInputModule.GetType().Name
                : "none";

            if (results.Count == 0)
            {
                Debug.Log("[UIClickDebugger] Click par koi UI object nahi mila. " +
                          "Canvas par GraphicRaycaster hai? Button ki Image par Raycast Target ON hai? " +
                          $"(Input Module: {module})");
                return;
            }

            string all = "";
            for (int i = 0; i < results.Count && i < 5; i++)
                all += (i == 0 ? "" : " > ") + results[i].gameObject.name;

            Debug.Log($"[UIClickDebugger] Sabse upar: '{results[0].gameObject.name}'. " +
                      $"Neeche ki layers: {all}. (Input Module: {module}, touchSupported: {Input.touchSupported})");
        }
    }
}