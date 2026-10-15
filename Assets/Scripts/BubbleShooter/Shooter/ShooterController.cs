using System;
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
        [Tooltip("Next bubble ka size, grid ki bubble ke muqable mein. " +
                 "1 = bilkul grid jitni. 0.8 = thodi chhoti preview.")]
        [SerializeField] private float nextBubbleScale = 1f;
        [Header("Sorting")]
        [Tooltip("Shooter bubbles ka order. Shooter base ke order se zyada rakhein.")]
        [SerializeField] private int shooterBubbleSortingOrder = 10;


        [Header("References")]
        [SerializeField] private BubbleGrid bubbleGrid;
        [SerializeField] private GameObject bubblePrefab;
        [SerializeField] private BubbleColorGenerator colorGenerator;

        private BubbleController currentBubble;
        private BubbleController nextBubble;
        private List<BubbleColor> availableColors;

        public BubbleController CurrentBubble => currentBubble;
        public BubbleController NextBubble => nextBubble;

        public event Action<BubbleController, Vector2> OnBubbleShot;

        private void Awake()
        {
            if (bubbleGrid == null)
                bubbleGrid = FindObjectOfType<BubbleGrid>();
        }

        public void Initialize(BubbleGrid grid, GameObject prefab, BubbleColorGenerator generator, Transform currentPos, Transform nextPos, float speed = 14f)
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

            // Clear old shooter bubbles if any
            if (currentBubble != null) Destroy(currentBubble.gameObject);
            if (nextBubble != null) Destroy(nextBubble.gameObject);

            SpawnCurrentBubble();
            SpawnNextBubble();

        }

        // ---------------------------------------------------------------
        // Color / size helpers
        // ---------------------------------------------------------------

        /// <summary>
        /// Naye bubble ka color: pehle grid mein maujooda colors mein se,
        /// warna level ke colors (colorGenerator), warna fallback.
        /// </summary>
        private BubbleColor PickColor(BubbleColor fallback)
        {
            if (bubbleGrid != null)
            {
                List<BubbleColor> gridColors = bubbleGrid.GetColorsInGrid();
                if (gridColors.Count > 0)
                    return gridColors[UnityEngine.Random.Range(0, gridColors.Count)];
            }

            if (colorGenerator != null)
                return colorGenerator.GetRandomColor(availableColors);

            return fallback;
        }

        /// <summary>Bubble ka size grid ke Bubble Diameter ke barabar (multiplier se thora kam/zyada).</summary>
        private void FitToGrid(BubbleController bubble, float multiplier = 1f)
        {
            if (bubble == null) return;

            if (bubbleGrid != null)
                bubbleGrid.FitBubbleSize(bubble);

            if (!Mathf.Approximately(multiplier, 1f))
                bubble.transform.localScale *= multiplier;
        }

        /// <summary>
        /// Pops ke baad kuch colors grid se khatam ho sakte hain.
        /// Current aur next bubble ka color agar grid mein nahi rahe to grid ke maujooda colors mein se badal do.
        /// </summary>
        public void EnsureColorsInGrid()
        {
            if (bubbleGrid == null) return;

            List<BubbleColor> gridColors = bubbleGrid.GetColorsInGrid();
            if (gridColors.Count == 0) return;

            RecolorIfMissing(currentBubble, gridColors);
            RecolorIfMissing(nextBubble, gridColors);
        }

        private void RecolorIfMissing(BubbleController bubble, List<BubbleColor> gridColors)
        {
            if (bubble == null || gridColors.Contains(bubble.Color)) return;

            bubble.SetColor(gridColors[UnityEngine.Random.Range(0, gridColors.Count)]);
        }

        // ---------------------------------------------------------------
        // Spawning
        // ---------------------------------------------------------------

        private void SpawnCurrentBubble()
        {
            if (bubblePrefab == null || currentBubblePosition == null) return;

            BubbleColor color = PickColor(BubbleColor.Red);
            GameObject obj = Instantiate(bubblePrefab, currentBubblePosition.position, Quaternion.identity, transform);
            currentBubble = obj.GetComponent<BubbleController>();
            if (currentBubble != null)
            {
                currentBubble.SetColor(color);
                FitToGrid(currentBubble);
                currentBubble.SetColor(color);
                FitToGrid(currentBubble);
                currentBubble.SetColor(color);
                FitToGrid(currentBubble);
                currentBubble.SetSortingOrder(shooterBubbleSortingOrder);   // <-- nayi line
                currentBubble.SetSortingOrder(shooterBubbleSortingOrder);   // <-- nayi line
            }
        }

        private void SpawnNextBubble()
        {
            if (bubblePrefab == null || nextBubblePosition == null) return;

            BubbleColor color = PickColor(BubbleColor.Blue);
            GameObject obj = Instantiate(bubblePrefab, nextBubblePosition.position, Quaternion.identity, transform);
            nextBubble = obj.GetComponent<BubbleController>();
            if (nextBubble != null)
            {
                nextBubble.SetColor(color);
                FitToGrid(nextBubble, nextBubbleScale);
                nextBubble.SetColor(color);
                FitToGrid(nextBubble, nextBubbleScale);
                nextBubble.SetSortingOrder(shooterBubbleSortingOrder);      // <-- nayi line
            }
        }

        // ---------------------------------------------------------------
        // Shooting
        // ---------------------------------------------------------------

        public bool Shoot(Vector2 direction, float leftWallX, float rightWallX, float ceilingY, Action<BubbleController, GridCoordinate> onAttached)
        {
            if (currentBubble == null) return false;

            BubbleController shotBubble = currentBubble;
            currentBubble = null;

            shotBubble.transform.parent = null;

            // Attach Projectile Component
            BubbleProjectile projectile = shotBubble.gameObject.AddComponent<BubbleProjectile>();
            projectile.Initialize(shotBubble, bubbleGrid, leftWallX, rightWallX, ceilingY, onAttached);
            projectile.Launch(direction, shootingSpeed);

            OnBubbleShot?.Invoke(shotBubble, direction);

            // Move Next -> Current, and generate new Next
            AdvanceNextBubble();

            return true;
        }

        private void AdvanceNextBubble()
        {
            if (nextBubble != null)
            {
                currentBubble = nextBubble;
                currentBubble.transform.SetParent(transform);
                currentBubble.transform.position = currentBubblePosition.position;

                // Preview size se hata kar poora grid jitna size do
                FitToGrid(currentBubble);


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