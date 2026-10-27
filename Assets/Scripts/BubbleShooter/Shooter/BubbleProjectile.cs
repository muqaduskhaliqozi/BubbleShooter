
using System;
using UnityEngine;
using BubbleShooter.Bubble;
using BubbleShooter.Grid;

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

        private Vector2 direction;
        private bool isMoving;
        private BubbleController bubbleController;
        private BubbleGrid bubbleGrid;

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

            Vector3 nextPos =
                transform.position +
                (Vector3)(direction * speed * Time.deltaTime);

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
                Debug.LogWarning(
                    $"[BubbleProjectile] Could not attach at {targetCoord}.");

                // Avoid leaving the game stuck in Shooting state.
                Destroy(gameObject);
                return;
            }

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

            bool added = bubbleGrid.AddBubble(
                targetCoord.Row,
                targetCoord.Column,
                bubbleController);

            if (!added)
            {
                Debug.LogWarning(
                    $"[BubbleProjectile] Failed to attach bubble at {targetCoord}.");

                // Avoid leaving the game stuck in Shooting state.
                Destroy(gameObject);
                return;
            }

            Destroy(this);

            // Pass the flag to the GameManager.
            onAttachedCallback?.Invoke(
                bubbleController, targetCoord, skipMatchProcessing);
        }
    }
}
