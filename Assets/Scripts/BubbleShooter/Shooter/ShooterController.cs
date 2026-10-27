
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.Level;

namespace BubbleShooter.Shooter
{
    public class ShooterController : MonoBehaviour
    {
        [Header("Positions")]
        [SerializeField] private Transform currentBubblePosition;
        [SerializeField] private Transform nextBubblePosition;

        [Header("Config")]
        [SerializeField] private float shootingSpeed = 14f;

        [Header("Bubble Size")]
        [Tooltip("1 = grid bubble jitni size, 0.8 = thori chhoti preview.")]
        [SerializeField] private float nextBubbleScale = 1f;

        [Header("Sorting")]
        [Tooltip("Shooter bubbles ka sorting order.")]
        [SerializeField] private int shooterBubbleSortingOrder = 10;

        [Header("Swap (current <-> next)")]
        [Tooltip("Swap animation duration in seconds.")]
        [SerializeField] private float swapDuration = 0.25f;

        [Tooltip("Swap animation arc, bubble diameter ka hissa.")]
        [SerializeField] private float swapArc = 0.35f;

        [Tooltip("0 = bubble diameter ka 1.3 guna.")]
        [SerializeField] private float swapTapRadius = 0f;

        [Tooltip("Khali chhor dein to Main Camera use hota hai.")]
        [SerializeField] private Camera inputCamera;

        [Header("References")]
        [SerializeField] private BubbleGrid bubbleGrid;
        [SerializeField] private GameObject bubblePrefab;
        [SerializeField] private BubbleColorGenerator colorGenerator;

        private BubbleController currentBubble;
        private BubbleController nextBubble;
        private List<BubbleColor> availableColors;
        private bool swapping;

        public BubbleController CurrentBubble => currentBubble;
        public BubbleController NextBubble => nextBubble;
        public bool IsSwapping => swapping;
        public int BubbleSortingOrder => shooterBubbleSortingOrder;

        public event Action<BubbleController, Vector2> OnBubbleShot;

        private void Awake()
        {
            if (bubbleGrid == null)
                bubbleGrid = FindObjectOfType<BubbleGrid>();
        }

        public void Initialize(
            BubbleGrid grid,
            GameObject prefab,
            BubbleColorGenerator generator,
            Transform currentPos,
            Transform nextPos,
            float speed = 14f)
        {
            bubbleGrid = grid;
            bubblePrefab = prefab;
            colorGenerator = generator;
            currentBubblePosition = currentPos;
            nextBubblePosition = nextPos;
            shootingSpeed = speed;
        }

        public void SetupShooter(List<BubbleColor> levelColors)
        {
            availableColors = levelColors;

            StopAllCoroutines();
            swapping = false;

            if (currentBubble != null)
                Destroy(currentBubble.gameObject);

            if (nextBubble != null)
                Destroy(nextBubble.gameObject);

            currentBubble = null;
            nextBubble = null;

            SpawnCurrentBubble();
            SpawnNextBubble();
        }

        // ---------------------------------------------------------------
        // Color / size helpers
        // ---------------------------------------------------------------

        private BubbleColor PickColor(BubbleColor fallback)
        {
            if (bubbleGrid != null)
            {
                List<BubbleColor> gridColors = bubbleGrid.GetColorsInGrid();

                if (gridColors.Count > 0)
                    return gridColors[
                        UnityEngine.Random.Range(0, gridColors.Count)];
            }

            if (colorGenerator != null)
                return colorGenerator.GetRandomColor(availableColors);

            return fallback;
        }

        private void FitToGrid(
            BubbleController bubble,
            float multiplier = 1f)
        {
            if (bubble == null)
                return;

            if (bubbleGrid != null)
                bubbleGrid.FitBubbleSize(bubble);

            if (!Mathf.Approximately(multiplier, 1f))
                bubble.transform.localScale *= multiplier;
        }

        public void EnsureColorsInGrid()
        {
            if (bubbleGrid == null)
                return;

            List<BubbleColor> gridColors = bubbleGrid.GetColorsInGrid();

            if (gridColors.Count == 0)
                return;

            RecolorIfMissing(currentBubble, gridColors);
            RecolorIfMissing(nextBubble, gridColors);
        }

        private void RecolorIfMissing(
            BubbleController bubble,
            List<BubbleColor> gridColors)
        {
            if (bubble == null || gridColors.Contains(bubble.Color))
                return;

            bubble.SetColor(
                gridColors[UnityEngine.Random.Range(0, gridColors.Count)]);
        }

        // ---------------------------------------------------------------
        // Spawning
        // ---------------------------------------------------------------

        private void SpawnCurrentBubble()
        {
            if (bubblePrefab == null || currentBubblePosition == null)
                return;

            BubbleColor color = PickColor(BubbleColor.Red);

            GameObject obj = Instantiate(
                bubblePrefab,
                currentBubblePosition.position,
                Quaternion.identity,
                transform);

            currentBubble = obj.GetComponent<BubbleController>();

            if (currentBubble != null)
            {
                currentBubble.SetColor(color);
                FitToGrid(currentBubble);
                currentBubble.SetSortingOrder(shooterBubbleSortingOrder);
            }
        }

        private void SpawnNextBubble()
        {
            if (bubblePrefab == null || nextBubblePosition == null)
                return;

            BubbleColor color = PickColor(BubbleColor.Blue);

            GameObject obj = Instantiate(
                bubblePrefab,
                nextBubblePosition.position,
                Quaternion.identity,
                transform);

            nextBubble = obj.GetComponent<BubbleController>();

            if (nextBubble != null)
            {
                nextBubble.SetColor(color);
                FitToGrid(nextBubble, nextBubbleScale);
                nextBubble.SetSortingOrder(shooterBubbleSortingOrder);
            }
        }

        // ---------------------------------------------------------------
        // Swap
        // ---------------------------------------------------------------

        private float GetSwapRadius()
        {
            if (swapTapRadius > 0f)
                return swapTapRadius;

            float diameter =
                bubbleGrid != null ? bubbleGrid.BubbleDiameter : 0.8f;

            return diameter * 1.3f;
        }

        public bool IsOnSwapZone(Vector2 screenPos)
        {
            if (nextBubble == null || nextBubblePosition == null)
                return false;

            Camera cam = inputCamera != null
                ? inputCamera
                : Camera.main;

            if (cam == null)
                return false;

            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(
                    screenPos.x,
                    screenPos.y,
                    Mathf.Abs(cam.transform.position.z)));

            Vector2 center = nextBubblePosition.position;

            return Vector2.Distance(world, center) <= GetSwapRadius();
        }

        public bool SwapBubbles()
        {
            if (swapping || currentBubble == null || nextBubble == null)
                return false;

            StartCoroutine(SwapRoutine());
            return true;
        }

        private IEnumerator SwapRoutine()
        {
            swapping = true;

            BubbleController a = currentBubble;
            BubbleController b = nextBubble;

            Vector3 aFrom = a.transform.position;
            Vector3 bFrom = b.transform.position;

            Vector3 aScale = a.transform.localScale;
            Vector3 bScale = b.transform.localScale;

            currentBubble = b;
            nextBubble = a;

            float diameter =
                bubbleGrid != null ? bubbleGrid.BubbleDiameter : 0.8f;

            Vector3 dir = bFrom - aFrom;

            Vector3 perp =
                new Vector3(-dir.y, dir.x, 0f).normalized
                * (swapArc * diameter);

            float t = 0f;
            float duration = Mathf.Max(0.01f, swapDuration);

            while (t < duration)
            {
                t += Time.deltaTime;

                float k = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(t / duration));

                float arc = Mathf.Sin(k * Mathf.PI);

                if (a != null)
                {
                    a.transform.position =
                        Vector3.Lerp(aFrom, bFrom, k) + perp * arc;

                    a.transform.localScale =
                        Vector3.Lerp(aScale, bScale, k);
                }

                if (b != null)
                {
                    b.transform.position =
                        Vector3.Lerp(bFrom, aFrom, k) - perp * arc;

                    b.transform.localScale =
                        Vector3.Lerp(bScale, aScale, k);
                }

                yield return null;
            }

            if (a != null)
            {
                a.transform.position = bFrom;
                a.transform.localScale = bScale;
            }

            if (b != null)
            {
                b.transform.position = aFrom;
                b.transform.localScale = aScale;
            }

            swapping = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (nextBubblePosition == null)
                return;

            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);

            Gizmos.DrawWireSphere(
                nextBubblePosition.position,
                GetSwapRadius());
        }

        // ---------------------------------------------------------------
        // Shooting
        // ---------------------------------------------------------------

        public bool Shoot(
            Vector2 direction,
            float leftWallX,
            float rightWallX,
            float ceilingY,
            Action<BubbleController, GridCoordinate, bool> onAttached)
        {
            if (currentBubble == null || swapping)
                return false;

            BubbleController shotBubble = currentBubble;
            currentBubble = null;

            shotBubble.transform.parent = null;

            BubbleProjectile projectile =
                shotBubble.gameObject.AddComponent<BubbleProjectile>();

            projectile.Initialize(
                shotBubble,
                bubbleGrid,
                leftWallX,
                rightWallX,
                ceilingY,
                onAttached);

            projectile.Launch(direction, shootingSpeed);

            OnBubbleShot?.Invoke(shotBubble, direction);

            AdvanceNextBubble();

            return true;
        }

        private void AdvanceNextBubble()
        {
            if (nextBubble != null)
            {
                currentBubble = nextBubble;

                currentBubble.transform.SetParent(transform);
                currentBubble.transform.position =
                    currentBubblePosition.position;

                FitToGrid(currentBubble);
                currentBubble.SetSortingOrder(shooterBubbleSortingOrder);

                nextBubble = null;
            }
            else
            {
                SpawnCurrentBubble();
            }

            SpawnNextBubble();
        }
    }
}
