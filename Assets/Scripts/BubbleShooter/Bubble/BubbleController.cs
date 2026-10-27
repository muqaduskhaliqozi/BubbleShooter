
using System;
using System.Collections;
using UnityEngine;
using BubbleShooter.Grid;

namespace BubbleShooter.Bubble
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BubbleController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CircleCollider2D circleCollider;

        [Header("Visuals")]
        [SerializeField] private bool useGlossyLook = true;
        private SpriteRenderer glossRenderer;

        [Header("Lock System")]
        [SerializeField] private SpriteRenderer lockRenderer;

        private int lockHitsRemaining = 0;
        private bool wasLocked = false;

        public bool IsLocked => lockHitsRemaining > 0;
        public int LockHitsRemaining => lockHitsRemaining;

        // Lock khulne ke baad bubble direct hit se destroy ho sakti hai.
        public bool CanBeDestroyedByDirectHit =>
            wasLocked && lockHitsRemaining == 0;

        [Header("Bubble State")]
        private BubbleColor color;
        private GridCoordinate gridPosition;
        private bool isAttached;
        private bool isFalling;
        private bool isMoving;

        public BubbleColor Color => color;
        public GridCoordinate GridPosition => gridPosition;
        public bool IsAttached => isAttached;
        public bool IsFalling => isFalling;
        public bool IsMoving => isMoving;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (circleCollider == null)
                circleCollider = GetComponent<CircleCollider2D>();

            if (lockRenderer == null)
            {
                Transform lockTransform = transform.Find("LockIcon");

                if (lockTransform != null)
                    lockRenderer =
                        lockTransform.GetComponent<SpriteRenderer>();
            }

            if (lockRenderer != null)
                lockRenderer.gameObject.SetActive(false);
        }

        public void SetColor(
            BubbleColor newColor,
            Sprite customSprite = null)
        {
            color = newColor;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
                return;

            if (customSprite != null)
                spriteRenderer.sprite = customSprite;
            else if (useGlossyLook)
                spriteRenderer.sprite = BubbleSpriteFactory.GetBaseSprite();

            spriteRenderer.color =
                BubbleColorUtility.GetColorValue(newColor);

            if (useGlossyLook)
                EnsureGloss();
        }


        public void SetLocked(int hitsRequired = 1)
        {
            // One hit removes the rope.
            // The bubble itself remains in the grid.
            lockHitsRemaining = Mathf.Max(0, hitsRequired);
            wasLocked = lockHitsRemaining > 0;

            if (IsLocked)
                EnsureLockVisual();

            UpdateLockVisual();
        }

        public bool ApplyHit()
        {
            if (!IsLocked)
                return false;

            lockHitsRemaining = Mathf.Max(0, lockHitsRemaining - 1);

            if (lockHitsRemaining == 0)
            {
                UpdateLockVisual();
                Debug.Log(
                    "[BubbleController] Rope removed. Bubble remains alive.");
            }

            // True means this hit was used to unlock the bubble.
            // It does NOT mean the bubble should be destroyed.
            return true;
        }



        private static Sprite generatedRopeSprite;

        private void EnsureLockVisual()
        {
            if (lockRenderer == null)
            {
                Transform existing = transform.Find("LockIcon");

                if (existing != null)
                    lockRenderer = existing.GetComponent<SpriteRenderer>();

                if (lockRenderer == null)
                {
                    GameObject icon = new GameObject("RopeIcon");
                    icon.transform.SetParent(transform, false);
                    icon.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                    icon.transform.localScale = Vector3.one * 0.85f;

                    lockRenderer = icon.AddComponent<SpriteRenderer>();
                }
            }

            // Existing icon ko bhi rope sprite se replace karein.
            lockRenderer.sprite = GetRopeSprite();
            lockRenderer.color = UnityEngine.Color.white;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                lockRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                lockRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
            }
        }

        private static Sprite GetRopeSprite()
        {
            if (generatedRopeSprite != null)
                return generatedRopeSprite;

            const int size = 96;
            Texture2D texture = new Texture2D(
                size, size, TextureFormat.RGBA32, false);

            texture.name = "Runtime_CrossPatternRope";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color shadow = new Color(0.24f, 0.10f, 0.035f, 1f);
            Color rope = new Color(0.62f, 0.34f, 0.13f, 1f);
            Color light = new Color(0.88f, 0.61f, 0.32f, 1f);

            Color[] pixels = new Color[size * size];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            // Two thick diagonal ropes making an X.
            DrawRopeLine(pixels, size, 15, 15, 81, 81, shadow, 10f);
            DrawRopeLine(pixels, size, 15, 81, 81, 15, shadow, 10f);

            DrawRopeLine(pixels, size, 15, 15, 81, 81, rope, 7f);
            DrawRopeLine(pixels, size, 15, 81, 81, 15, rope, 7f);

            // Rope highlights for a braided appearance.
            DrawRopeLine(pixels, size, 17, 16, 80, 79, light, 2f);
            DrawRopeLine(pixels, size, 16, 80, 79, 17, light, 2f);

            // Small central knot.
            for (int y = 39; y <= 57; y++)
            {
                for (int x = 39; x <= 57; x++)
                {
                    float dx = x - 48f;
                    float dy = y - 48f;

                    if (dx * dx + dy * dy <= 64f)
                        pixels[y * size + x] = rope;

                    if (dx * dx + dy * dy <= 25f)
                        pixels[y * size + x] = light;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            generatedRopeSprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size);

            generatedRopeSprite.name = "CrossPatternRope";
            return generatedRopeSprite;
        }

        private static void DrawRopeLine(
            Color[] pixels,
            int size,
            float x1,
            float y1,
            float x2,
            float y2,
            Color color,
            float thickness)
        {
            float radius = thickness * 0.5f;
            float radiusSquared = radius * radius;

            int minX = Mathf.Max(0, Mathf.FloorToInt(
                Mathf.Min(x1, x2) - radius));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(
                Mathf.Max(x1, x2) + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(
                Mathf.Min(y1, y2) - radius));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(
                Mathf.Max(y1, y2) + radius));

            float dx = x2 - x1;
            float dy = y2 - y1;
            float lengthSquared = dx * dx + dy * dy;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float t = ((x - x1) * dx + (y - y1) * dy)
                              / lengthSquared;

                    t = Mathf.Clamp01(t);

                    float px = x1 + t * dx;
                    float py = y1 + t * dy;

                    float ox = x - px;
                    float oy = y - py;

                    if (ox * ox + oy * oy <= radiusSquared)
                        pixels[y * size + x] = color;
                }
            }
        }


        private void UpdateLockVisual()
        {
            if (IsLocked)
                EnsureLockVisual();

            if (lockRenderer == null)
                return;

            lockRenderer.gameObject.SetActive(IsLocked);

            if (spriteRenderer != null)
            {
                lockRenderer.sortingLayerID =
                    spriteRenderer.sortingLayerID;

                lockRenderer.sortingOrder =
                    spriteRenderer.sortingOrder + 2;
            }
        }

        public void SetSortingOrder(int order)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = order;

            if (glossRenderer != null)
                glossRenderer.sortingOrder = order + 1;

            if (lockRenderer != null)
                lockRenderer.sortingOrder = order + 2;
        }

        private void EnsureGloss()
        {
            if (glossRenderer == null)
            {
                GameObject go = new GameObject("Gloss");
                go.transform.SetParent(transform, false);

                glossRenderer = go.AddComponent<SpriteRenderer>();
                glossRenderer.sprite =
                    BubbleSpriteFactory.GetGlossSprite();
            }

            glossRenderer.sortingLayerID =
                spriteRenderer.sortingLayerID;

            glossRenderer.sortingOrder =
                spriteRenderer.sortingOrder + 1;

            Vector3 baseSize = spriteRenderer.sprite.bounds.size;
            Vector3 glossSize = glossRenderer.sprite.bounds.size;

            glossRenderer.transform.localScale = new Vector3(
                baseSize.x / glossSize.x,
                baseSize.y / glossSize.y,
                1f);
        }

        public void SetGridPosition(int row, int col)
        {
            gridPosition = new GridCoordinate(row, col);
        }

        public BubbleColor GetColor()
        {
            return color;
        }

        public GridCoordinate GetGridPosition()
        {
            return gridPosition;
        }

        public void Attach()
        {
            isAttached = true;
            isMoving = false;
            isFalling = false;

            if (circleCollider != null)
                circleCollider.enabled = true;
        }

        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        public void Pop(Action onComplete = null)
        {
            isAttached = false;

            if (circleCollider != null)
                circleCollider.enabled = false;

            StartCoroutine(PopRoutine(onComplete));
        }

        private IEnumerator PopRoutine(Action onComplete)
        {
            float duration = 0.15f;
            float elapsed = 0f;

            Vector3 startScale = transform.localScale;
            Vector3 targetScale = startScale * 1.3f;

            UnityEngine.Color startColor =
                spriteRenderer != null
                    ? spriteRenderer.color
                    : UnityEngine.Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.localScale =
                    Vector3.Lerp(startScale, targetScale, t);

                if (spriteRenderer != null)
                {
                    UnityEngine.Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderer.color = c;
                }

                if (glossRenderer != null)
                {
                    glossRenderer.color = new UnityEngine.Color(
                        1f, 1f, 1f, Mathf.Lerp(1f, 0f, t));
                }

                if (lockRenderer != null)
                {
                    UnityEngine.Color lockColor = lockRenderer.color;
                    lockColor.a = Mathf.Lerp(1f, 0f, t);
                    lockRenderer.color = lockColor;
                }

                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }

        public void Fall(
            Vector2 initialVelocity,
            Action onComplete = null)
        {
            isAttached = false;
            isFalling = true;

            if (circleCollider != null)
                circleCollider.enabled = false;

            StartCoroutine(
                FallRoutine(initialVelocity, onComplete));
        }

        private IEnumerator FallRoutine(
            Vector2 initialVelocity,
            Action onComplete)
        {
            float gravity = 25f;
            Vector2 velocity = initialVelocity;

            float elapsed = 0f;
            float lifetime = 1.5f;

            float rotSpeed = UnityEngine.Random.Range(
                -180f, 180f);

            while (elapsed < lifetime &&
                   transform.position.y > -15f)
            {
                elapsed += Time.deltaTime;
                velocity.y -= gravity * Time.deltaTime;

                transform.position +=
                    (Vector3)velocity * Time.deltaTime;

                transform.Rotate(
                    0f, 0f, rotSpeed * Time.deltaTime);

                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
