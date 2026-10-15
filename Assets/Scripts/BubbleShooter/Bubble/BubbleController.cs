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
        }

        public void SetColor(BubbleColor newColor, Sprite customSprite = null)
        {
            color = newColor;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null) return;

            if (customSprite != null)
                spriteRenderer.sprite = customSprite;
            else if (useGlossyLook)
                spriteRenderer.sprite = BubbleSpriteFactory.GetBaseSprite();

            spriteRenderer.color = BubbleColorUtility.GetColorValue(newColor);

            if (useGlossyLook)
                EnsureGloss();
        }
        public void SetSortingOrder(int order)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = order;

            // gloss hamesha bubble se ek upar rahe
            if (glossRenderer != null)
                glossRenderer.sortingOrder = order + 1;
        }

        private void EnsureGloss()
        {
            if (glossRenderer == null)
            {
                GameObject go = new GameObject("Gloss");
                go.transform.SetParent(transform, false);
                glossRenderer = go.AddComponent<SpriteRenderer>();
                glossRenderer.sprite = BubbleSpriteFactory.GetGlossSprite();
            }

            glossRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            glossRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

            // gloss ka size base sprite ke barabar rakhein
            Vector3 baseSize = spriteRenderer.sprite.bounds.size;
            Vector3 glossSize = glossRenderer.sprite.bounds.size;
            glossRenderer.transform.localScale =
                new Vector3(baseSize.x / glossSize.x, baseSize.y / glossSize.y, 1f);
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
            {
                circleCollider.enabled = true;
            }
        }

        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        public void Pop(Action onComplete = null)
        {
            isAttached = false;

            if (circleCollider != null)
            {
                circleCollider.enabled = false;
            }

            StartCoroutine(PopRoutine(onComplete));
        }

        private IEnumerator PopRoutine(Action onComplete)
        {
            float duration = 0.15f;
            float elapsed = 0f;

            Vector3 startScale = transform.localScale;
            Vector3 targetScale = startScale * 1.3f;

            UnityEngine.Color startColor = spriteRenderer != null
                ? spriteRenderer.color
                : UnityEngine.Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float t = elapsed / duration;

                transform.localScale = Vector3.Lerp(
                    startScale,
                    targetScale,
                    t
                );

                if (spriteRenderer != null)
                {
                    UnityEngine.Color c = startColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    spriteRenderer.color = c;
                }

                if (glossRenderer != null)
                {
                    glossRenderer.color = new UnityEngine.Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t));
                }

                yield return null;
            }

            onComplete?.Invoke();

            Destroy(gameObject);
        }

        public void Fall(Vector2 initialVelocity, Action onComplete = null)
        {
            isAttached = false;
            isFalling = true;

            if (circleCollider != null)
            {
                circleCollider.enabled = false;
            }

            StartCoroutine(FallRoutine(initialVelocity, onComplete));
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
                -180f,
                180f
            );

            while (
                elapsed < lifetime &&
                transform.position.y > -15f)
            {
                elapsed += Time.deltaTime;

                velocity.y -= gravity * Time.deltaTime;

                transform.position +=
                    (Vector3)velocity * Time.deltaTime;

                transform.Rotate(
                    0f,
                    0f,
                    rotSpeed * Time.deltaTime
                );

                yield return null;
            }

            onComplete?.Invoke();

            Destroy(gameObject);
        }
    }
}