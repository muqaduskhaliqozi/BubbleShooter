using UnityEngine;
using BubbleShooter.Grid;

namespace BubbleShooter.Shooter
{
    /// <summary>
    /// Next bubble ke gird ghoomta hua "swap" ka circle (do ghoomte teer).
    /// Sprite code se khud banta hai, koi image import nahi karni.
    /// Is script ko ek khali GameObject par lagayen aur Anchor mein "Next Bubble Position" wala Transform dein.
    /// </summary>
    public class SwapIndicator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Wahi Transform jo ShooterController ke 'Next Bubble Position' mein laga hai.")]
        [SerializeField] private Transform anchor;
        [SerializeField] private BubbleGrid bubbleGrid;
        [Tooltip("Optional. Swap ke dauran circle tez ghoomta hai.")]
        [SerializeField] private ShooterController shooter;

        [Header("Look")]
        [Tooltip("Circle ka size = bubble diameter x yeh value.")]
        [SerializeField] private float sizeMultiplier = 2.2f;
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.55f);
        [Tooltip("ON: circle ka order shooter bubbles se ek kam rehta hai (bubbles ke peeche). OFF: neeche wala Sorting Order.")]
        [SerializeField] private bool autoSortingOrder = true;
        [SerializeField] private int sortingOrder = 9;

        [Header("Spin (degrees per second)")]
        [SerializeField] private float idleSpeed = 45f;
        [SerializeField] private float swapSpeed = 540f;

        private Transform iconTransform;
        private SpriteRenderer iconRenderer;

        private static Sprite cachedSprite;

        private void Awake()
        {
            if (bubbleGrid == null) bubbleGrid = FindObjectOfType<BubbleGrid>();
            if (shooter == null) shooter = FindObjectOfType<ShooterController>();

            GameObject go = new GameObject("SwapIcon");
            go.transform.SetParent(transform, false);

            iconTransform = go.transform;
            iconRenderer = go.AddComponent<SpriteRenderer>();
            iconRenderer.sprite = GetSprite();
            iconRenderer.color = color;
            iconRenderer.sortingOrder = GetOrder();
        }

        private int GetOrder()
        {
            if (autoSortingOrder && shooter != null)
                return shooter.BubbleSortingOrder - 1;

            return sortingOrder;
        }

        private void LateUpdate()
        {
            if (anchor == null || iconRenderer == null) return;

            bool show = shooter == null || shooter.NextBubble != null;
            iconRenderer.enabled = show;
            if (!show) return;

            iconTransform.position = anchor.position;

            // Bubbles ki sorting layer / order ke saath mel rakho
            iconRenderer.sortingOrder = GetOrder();

            if (shooter != null && shooter.NextBubble != null)
            {
                SpriteRenderer bubbleRenderer = shooter.NextBubble.GetComponent<SpriteRenderer>();
                if (bubbleRenderer != null)
                    iconRenderer.sortingLayerID = bubbleRenderer.sortingLayerID;
            }

            float diameter = bubbleGrid != null ? bubbleGrid.BubbleDiameter : 0.8f;
            float spriteSize = iconRenderer.sprite.bounds.size.x;
            float scale = diameter * sizeMultiplier / spriteSize;
            iconTransform.localScale = new Vector3(scale, scale, 1f);

            float speed = (shooter != null && shooter.IsSwapping) ? swapSpeed : idleSpeed;
            iconTransform.Rotate(0f, 0f, speed * Time.deltaTime);
        }

        // ---------------------------------------------------------------
        // Sprite: do arc + teer ke sirey (ek baar banta hai, phir cache)
        // ---------------------------------------------------------------

        private const int Size = 256;
        private const int Samples = 2;                 // anti-aliasing
        private const float RingRadius = 0.74f;
        private const float RingHalfWidth = 0.055f;
        private const float HeadLength = 0.22f;
        private const float HeadHalfWidth = 0.17f;

        private static readonly float[] ArcStart = { 15f, 195f };
        private static readonly float[] ArcEnd = { 150f, 330f };

        private static Sprite GetSprite()
        {
            if (cachedSprite != null) return cachedSprite;

            // Teer ke sirey (triangle) pehle se nikal lo
            Vector2[] tips = new Vector2[2];
            Vector2[] base1 = new Vector2[2];
            Vector2[] base2 = new Vector2[2];

            for (int i = 0; i < 2; i++)
            {
                float e = ArcEnd[i] * Mathf.Deg2Rad;
                Vector2 u = new Vector2(Mathf.Cos(e), Mathf.Sin(e));      // radial
                Vector2 t = new Vector2(-Mathf.Sin(e), Mathf.Cos(e));     // tangent (counter-clockwise)
                Vector2 p = u * RingRadius;

                tips[i] = p + t * HeadLength;
                base1[i] = p + u * HeadHalfWidth;
                base2[i] = p - u * HeadHalfWidth;
            }

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hit = 0;

                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            float nx = ((x + (sx + 0.5f) / Samples) / Size) * 2f - 1f;
                            float ny = ((y + (sy + 0.5f) / Samples) / Size) * 2f - 1f;

                            if (Covered(nx, ny, tips, base1, base2))
                                hit++;
                        }
                    }

                    byte a = (byte)Mathf.RoundToInt(255f * hit / (Samples * Samples));
                    pixels[y * Size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            cachedSprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            return cachedSprite;
        }

        private static bool Covered(float nx, float ny, Vector2[] tips, Vector2[] base1, Vector2[] base2)
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float angle = Mathf.Atan2(ny, nx) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            for (int i = 0; i < 2; i++)
            {
                if (angle >= ArcStart[i] && angle <= ArcEnd[i] &&
                    Mathf.Abs(r - RingRadius) <= RingHalfWidth)
                    return true;

                if (InTriangle(nx, ny, tips[i], base1[i], base2[i]))
                    return true;
            }

            return false;
        }

        private static float Sign(float px, float py, Vector2 a, Vector2 b)
        {
            return (px - b.x) * (a.y - b.y) - (a.x - b.x) * (py - b.y);
        }

        private static bool InTriangle(float px, float py, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(px, py, a, b);
            float d2 = Sign(px, py, b, c);
            float d3 = Sign(px, py, c, a);

            bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool pos = d1 > 0f || d2 > 0f || d3 > 0f;

            return !(neg && pos);
        }
    }
}