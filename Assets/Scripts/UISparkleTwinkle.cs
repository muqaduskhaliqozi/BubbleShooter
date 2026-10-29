using UnityEngine;
using UnityEngine.UI;

/// <summary>Chhota sitara jo timtimata (twinkle) aur ghoomta hai.</summary>
public class UISparkleTwinkle : MonoBehaviour
{
    public float phase;
    public float speed = 2.2f;

    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    private void Update()
    {
        float s = Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * speed + phase));
        transform.localScale = Vector3.one * (s * s);
        transform.Rotate(0f, 0f, 90f * Time.unscaledDeltaTime);
        if (img != null)
        {
            Color c = img.color;
            c.a = s;
            img.color = c;
        }
    }
}
