
using System;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;
using BubbleShooter.FX;

namespace BubbleShooter.Shooter
{
    public class BubbleProjectile : MonoBehaviour
    {
        [Header("Projectile Config")]
        [SerializeField] private float speed = 14f;
        [SerializeField] private float leftWallX = -3.2f;
        [SerializeField] private float rightWallX = 3.2f;
        [SerializeField] private float ceilingY = 3.8f;

        [Header("Collision")]
        [Tooltip("Hit distance = bubble diameter x yeh value. 0.92 aam tor par sahi rehta hai.")]
        [SerializeField] private float hitDistanceFactor = 0.92f;

        [Tooltip("Ek frame mein zyada se zyada kitna chale (diameter ka hissa). " +
                 "Kam FPS par bubble doosri bubbles ke beech se nikal na jaye.")]
        [SerializeField] private float maxStepFactor = 0.25f;

        private Vector2 direction;
        private bool isMoving;
        private BubbleController bubbleController;
        private BubbleGrid bubbleGrid;
        private Color trailColor = Color.white;
        private float trailTimer;

        private Action<BubbleController, GridCoordinate, bool>
            onAttachedCallback;

        public bool IsMoving => isMoving;

        public void Initialize(
            BubbleController controller,
            BubbleGrid grid,
            float leftX,
            float rightX,
            float topY,
            Action<BubbleController, GridCoordinate, bool> onAttached)
        {
            bubbleController = controller;
            bubbleGrid = grid;
            leftWallX = leftX;
            rightWallX = rightX;
            ceilingY = topY;
            onAttachedCallback = onAttached;

            if (controller != null)
                trailColor = BubbleColorUtility.GetColorValue(controller.Color);
        }

        public void Launch(Vector2 dir, float moveSpeed)
        {
            direction = dir.normalized;
            speed = moveSpeed;
            isMoving = true;

            if (bubbleController != null)
                bubbleController.SetMoving(true);
        }

        private void Update()
        {
            if (!isMoving || bubbleController == null || bubbleGrid == null)
                return;

            float diameter = bubbleGrid.BubbleDiameter;
            float total = speed * Time.deltaTime;
            float maxStep = Mathf.Max(0.02f, diameter * maxStepFactor);
            int steps = Mathf.Clamp(Mathf.CeilToInt(total / maxStep), 1, 64);
            float stepLen = total / steps;

            for (int i = 0; i < steps && isMoving; i++)
                Step(stepLen);

            // Trail (har ~0.016s par ek nuqta, FPS se azad)
            if (isMoving && GameFX.Instance != null)
            {
                trailTimer -= Time.deltaTime;
                if (trailTimer <= 0f)
                {
                    trailTimer = 0.016f;
                    GameFX.Instance.Trail(transform.position, trailColor, diameter);
                }
            }
        }

        private void Step(float distance)
        {
            Vector3 nextPos =
                transform.position +
                (Vector3)(direction * distance);

            // 1. Wall bouncing
            if (nextPos.x <= leftWallX)
            {
                nextPos.x = leftWallX + (leftWallX - nextPos.x);
                direction.x = Mathf.Abs(direction.x);
            }
            else if (nextPos.x >= rightWallX)
            {
                nextPos.x = rightWallX - (nextPos.x - rightWallX);
                direction.x = -Mathf.Abs(direction.x);
            }

            transform.position = nextPos;

            // 2. Ceiling detection
            if (nextPos.y >= ceilingY)
            {
                SnapToGrid(nextPos, true);
                return;
            }

            // 3. Find the nearest bubble
            float hitDistance =
                bubbleGrid.BubbleDiameter * hitDistanceFactor;

            BubbleController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (BubbleController b in bubbleGrid.GetAllBubbles())
            {
                if (b == null || !b.IsAttached || b == bubbleController)
                    continue;

                float dist = Vector2.Distance(nextPos, b.transform.position);

                if (dist <= hitDistance && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = b;
                }
            }

            if (nearest == null)
                return;

            // First direct hit: unlock the rope only.
            if (nearest.IsLocked)
            {
                nearest.ApplyHit();

                // Attach the shot bubble, but skip match/floating processing.
                SnapToGrid(nextPos, false, nearest, true);
                return;
            }

            // Second direct hit: destroy the previously locked bubble.
            if (nearest.CanBeDestroyedByDirectHit)
            {
                GridCoordinate target = nearest.GridPosition;

                BubbleController removed =
                    bubbleGrid.RemoveBubble(target.Row, target.Column);

                if (removed != null)
                {
                    removed.Pop();

                    // Put the shot bubble into the cell that was freed.
                    SnapToExactCell(target);
                    return;
                }
            }

            // Ordinary bubble collision.
            SnapToGrid(nextPos, false, nearest);
        }

        private void SnapToExactCell(GridCoordinate targetCoord)
        {
            isMoving = false;

            if (bubbleController != null)
                bubbleController.SetMoving(false);

            bool added = bubbleGrid.AddBubble(
                targetCoord.Row,
                targetCoord.Column,
                bubbleController);

            if (!added)
            {
                FailAttach(targetCoord);
                return;
            }

            PlayLandingFeedback(targetCoord);

            Destroy(this);

            // This is a normal shot resolution.
            onAttachedCallback?.Invoke(
                bubbleController, targetCoord, false);
        }

        private void SnapToGrid(
            Vector3 hitPosition,
            bool isCeilingHit,
            BubbleController hitBubble = null,
            bool skipMatchProcessing = false)
        {
            isMoving = false;

            if (bubbleController != null)
                bubbleController.SetMoving(false);

            GridCoordinate targetCoord;

            if (isCeilingHit)
            {
                GridCoordinate rawCoord =
                    bubbleGrid.WorldToGridPosition(hitPosition);

                targetCoord = new GridCoordinate(0, rawCoord.Column);

                if (!bubbleGrid.IsEmpty(
                    targetCoord.Row, targetCoord.Column))
                {
                    targetCoord = bubbleGrid.FindNearestEmptyNeighbor(
                        targetCoord, hitPosition);
                }
            }
            else if (hitBubble != null)
            {
                targetCoord = bubbleGrid.FindAttachCellForHit(
                    hitBubble, hitPosition);
            }
            else
            {
                GridCoordinate rawCoord =
                    bubbleGrid.WorldToGridPosition(hitPosition);

                targetCoord = bubbleGrid.FindNearestEmptyNeighbor(
                    rawCoord, hitPosition);
            }

            // Fallback if the target cell is invalid or occupied.
            if (!bubbleGrid.IsValidCoordinate(targetCoord) ||
                !bubbleGrid.IsEmpty(
                    targetCoord.Row, targetCoord.Column))
            {
                targetCoord =
                    bubbleGrid.WorldToGridPosition(hitPosition);
            }

            // Aakhri koshish: qareeb ka koi bhi khali cell
            if (!bubbleGrid.IsEmpty(targetCoord.Row, targetCoord.Column))
            {
                GridCoordinate any;
                if (FindClosestEmptyCell(hitPosition, out any))
                    targetCoord = any;
            }

            bool added = bubbleGrid.AddBubble(
                targetCoord.Row,
                targetCoord.Column,
                bubbleController);

            if (!added)
            {
                FailAttach(targetCoord);
                return;
            }

            PlayLandingFeedback(targetCoord);

            Destroy(this);

            // Pass the flag to the GameManager.
            onAttachedCallback?.Invoke(
                bubbleController, targetCoord, skipMatchProcessing);
        }

        /// <summary>Lagne par: squash, padosiyon mein lehar, chhoti si chamak.</summary>
        private void PlayLandingFeedback(GridCoordinate coord)
        {
            if (bubbleController != null)
            {
                bubbleController.PlayLandSquash();

                if (GameFX.Instance != null)
                    GameFX.Instance.ImpactPuff(bubbleController.transform.position, trailColor, bubbleGrid.BubbleDiameter);
            }

            bubbleGrid.RippleFrom(coord, 0.12f, 2);
        }

        /// <summary>Hit position ke sab se qareeb khali cell (2 rows ke daire mein).</summary>
        private bool FindClosestEmptyCell(Vector3 pos, out GridCoordinate best)
        {
            best = new GridCoordinate(0, 0);
            GridCoordinate guess = bubbleGrid.WorldToGridPosition(pos);
            float bestDist = float.MaxValue;
            bool found = false;

            for (int r = guess.Row - 2; r <= guess.Row + 2; r++)
            {
                for (int c = guess.Column - 3; c <= guess.Column + 3; c++)
                {
                    if (!bubbleGrid.IsEmpty(r, c)) continue;
                    float d = Vector2.Distance(pos, bubbleGrid.GridToWorldPosition(r, c));
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = new GridCoordinate(r, c);
                        found = true;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Bubble grid mein lag hi na saki (grid bhar gaya). Pehle yahan game hamesha ke liye
        /// "Shooting" state mein atak jata tha. Ab bubble phoot jati hai aur game aage chalta hai.
        /// </summary>
        private void FailAttach(GridCoordinate targetCoord)
        {
            Debug.LogWarning(
                $"[BubbleProjectile] Could not attach at {targetCoord}. Bubble removed.");

            Action<BubbleController, GridCoordinate, bool> cb = onAttachedCallback;
            BubbleController b = bubbleController;
            Destroy(this);

            if (b != null)
                b.Pop(false);

            // skipMatchProcessing = true: GameManager seedha Ready state par wapas aata hai.
            cb?.Invoke(b, targetCoord, true);
        }
    }
}
